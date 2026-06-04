using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AppointmentService.Data;
using AppointmentService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AppointmentService.Services;

public sealed class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisherWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public OutboxPublisherWorker(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisherWorker> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisherWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events in background worker.");
            }

            await Task.Delay(3000, stoppingToken);
        }

        _logger.LogInformation("OutboxPublisherWorker stopped.");
    }

    private async Task ProcessOutboxEventsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();

        var pendingEvents = await dbContext.OutboxEvents
            .Where(e => e.Status == "Pending" && e.RetryCount < 5)
            .OrderBy(e => e.OccurredAt)
            .Take(10)
            .ToListAsync(stoppingToken);

        if (!pendingEvents.Any())
        {
            return;
        }

        _logger.LogInformation("Found {Count} pending outbox events to publish.", pendingEvents.Count);

        var medicalBaseUrl = _configuration["ServiceUrls:MedicalRecordService"] ?? "http://medical-api:8080";
        var systemToken = GenerateSystemToken();

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", systemToken);

        foreach (var ev in pendingEvents)
        {
            var targetEndpoint = ev.EventType switch
            {
                "appointment.confirmed" => $"{medicalBaseUrl.TrimEnd('/')}/api/v1/medical/events/appointment-confirmed",
                "patient.checked_in" => $"{medicalBaseUrl.TrimEnd('/')}/api/v1/medical/events/patient-checked-in",
                _ => null
            };

            if (targetEndpoint is null)
            {
                _logger.LogWarning("Unsupported event type {EventType} for Outbox Event {Id}", ev.EventType, ev.Id);
                ev.Status = "Failed";
                ev.ErrorMessage = $"Unsupported event type: {ev.EventType}";
                await dbContext.SaveChangesAsync(stoppingToken);
                continue;
            }

            try
            {
                var content = new StringContent(ev.Payload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(targetEndpoint, content, stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    ev.Status = "Processed";
                    ev.ProcessedAt = DateTime.UtcNow;
                    _logger.LogInformation("Successfully published outbox event {EventCode} ({EventType}) to {Endpoint}", ev.EventCode, ev.EventType, targetEndpoint);
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync(stoppingToken);
                    ev.RetryCount++;
                    ev.ErrorMessage = $"HTTP {response.StatusCode}: {errorResponse}";
                    _logger.LogWarning("Failed to publish event {EventCode}. Status: {Status}. Response: {Response}. Retry count: {Retry}", ev.EventCode, response.StatusCode, errorResponse, ev.RetryCount);
                }
            }
            catch (Exception ex)
            {
                ev.RetryCount++;
                ev.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Exception while publishing outbox event {EventCode}", ev.EventCode);
            }

            if (ev.RetryCount >= 5 && ev.Status != "Processed")
            {
                ev.Status = "Failed";
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }

    private string GenerateSystemToken()
    {
        var jwtKey = _configuration["Jwt:SharedSecret"]
            ?? _configuration["Jwt:Key"]
            ?? "SuperSecretKeyForPharmacyBillingServiceThatIsAtLeast32BytesLong!";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "PharmacyBillingService";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "PharmacyBillingService";

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "SystemWorker"),
                new Claim(ClaimTypes.Role, "Admin")
            }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
