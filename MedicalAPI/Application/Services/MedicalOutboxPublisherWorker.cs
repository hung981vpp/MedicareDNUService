using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MedicalAPI.Domain.Constants;
using MedicalAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MedicalAPI.Application.Services;

public sealed class MedicalOutboxPublisherWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MedicalOutboxPublisherWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public MedicalOutboxPublisherWorker(
        IServiceProvider serviceProvider,
        ILogger<MedicalOutboxPublisherWorker> logger,
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
        _logger.LogInformation("MedicalOutboxPublisherWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing N2 outbox events.");
            }

            await Task.Delay(3000, stoppingToken);
        }

        _logger.LogInformation("MedicalOutboxPublisherWorker stopped.");
    }

    private async Task ProcessOutboxEventsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MedicalDbContext>();

        var pendingEvents = await db.OutboxEvents
            .Where(e => e.Status == MedicalStatuses.PendingPublish && e.RetryCount < 5)
            .OrderBy(e => e.OccurredAt)
            .Take(10)
            .ToListAsync(stoppingToken);

        if (!pendingEvents.Any())
        {
            return;
        }

        _logger.LogInformation("N2 Outbox found {Count} pending events to publish to N3.", pendingEvents.Count);

        var pharmacyBaseUrl = _configuration["ServiceUrls:PharmacyBillingService"] ?? "http://pharmacy-billing-service:8080";
        var systemToken = GenerateSystemToken();

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", systemToken);

        foreach (var ev in pendingEvents)
        {
            var targetEndpoint = ev.EventType switch
            {
                "prescription.created" => $"{pharmacyBaseUrl.TrimEnd('/')}/api/events/prescription-created",
                _ => null
            };

            if (targetEndpoint is null)
            {
                _logger.LogWarning("Unsupported event type {EventType} for Outbox Event {Id}", ev.EventType, ev.Id);
                ev.Status = "Thất bại"; // Mark as Failed in Vietnamese
                ev.ErrorMessage = $"Unsupported event type: {ev.EventType}";
                await db.SaveChangesAsync(stoppingToken);
                continue;
            }

            try
            {
                var content = new StringContent(ev.Payload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(targetEndpoint, content, stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    ev.Status = MedicalStatuses.Published;
                    ev.PublishedAt = DateTime.UtcNow;
                    _logger.LogInformation("Successfully published N2 event {EventCode} ({EventType}) to {Endpoint}", ev.EventCode, ev.EventType, targetEndpoint);
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync(stoppingToken);
                    ev.RetryCount++;
                    ev.ErrorMessage = $"HTTP {response.StatusCode}: {errorResponse}";
                    _logger.LogWarning("Failed to publish event {EventCode} to N3. Status: {Status}. Retry: {Retry}", ev.EventCode, response.StatusCode, ev.RetryCount);
                }
            }
            catch (Exception ex)
            {
                ev.RetryCount++;
                ev.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Exception while publishing N2 event {EventCode}", ev.EventCode);
            }

            if (ev.RetryCount >= 5 && ev.Status != MedicalStatuses.Published)
            {
                ev.Status = "Thất bại";
            }

            await db.SaveChangesAsync(stoppingToken);
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
