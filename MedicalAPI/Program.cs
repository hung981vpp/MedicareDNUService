using MedicalAPI.Application.Common;
using MedicalAPI.Application.Services;
using MedicalAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error =>
                new ApiError(
                    entry.Key,
                    "VALIDATION_ERROR",
                    string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Dữ liệu không hợp lệ" : error.ErrorMessage)))
            .ToArray();

        var traceId = context.HttpContext.Request.Headers.TryGetValue("X-Request-Id", out var requestId)
            && !string.IsNullOrWhiteSpace(requestId)
                ? requestId.ToString()
                : context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", traceId, errors));
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "N2 Medical Record Service API",
        Version = "v1",
        Description = "ASP.NET Core Web API cho de tai 05 - Medical Record Service"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Dán JWT token, không cần nhập chữ Bearer.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            Array.Empty<string>()
        }
    });
    options.OrderActionsBy(apiDescription =>
    {
        var path = apiDescription.RelativePath ?? string.Empty;
        return path.Equals("health", StringComparison.OrdinalIgnoreCase)
            ? "000_health"
            : $"100_{path}";
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddDbContext<MedicalDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MedicalDb")));
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

var jwtKey = builder.Configuration["Jwt:SharedSecret"]
    ?? builder.Configuration["Jwt:Key"]
    ?? "SuperSecretKeyForPharmacyBillingServiceThatIsAtLeast32BytesLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PharmacyBillingService";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PharmacyBillingService";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<MedicalOutboxPublisherWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MedicalDbContext>();
    MedicalDbSeeder.Seed(db);
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "N2 Medical Record Service API v1");
    options.DocumentTitle = "N2 Medical Record Service API";
});

app.UseCors("DefaultCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "N2 Medical Record Service",
    timestamp = DateTime.UtcNow
}))
.WithTags("Health")
.WithName("HealthCheck")
.WithSummary("Kiểm tra trạng thái hoạt động của service")
.AllowAnonymous();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();
app.MapGet("/api/v1/medical", () => Results.Ok(new
{
    service = "N2 Medical Record Service",
    topic = "De tai 05 - He thong dat lich va quan ly phong kham",
    status = "Dang hoat dong",
    swagger = "/swagger",
    endpoints = new[]
    {
        "/health",
        "/api/v1/medical/patients",
        "/api/v1/medical/visits/today",
        "/api/v1/medical/records",
        "/api/v1/medical/prescriptions",
        "/api/v1/medical/clinical-orders",
        "/api/v1/medical/events/outbox"
    }
}));

app.Run();
