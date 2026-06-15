using System.Reflection;
using AppointmentService.Common;
using AppointmentService.Data;
using AppointmentService.Serialization;
using AppointmentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "FrontendCorsPolicy";

builder.Services.AddDbContext<AppointmentDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("AppointmentDatabase");
    
    if (!string.IsNullOrEmpty(connectionString) && (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://")))
    {
        var databaseUri = new Uri(connectionString);
        var userInfo = databaseUri.UserInfo.Split(':');
        var user = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        var host = databaseUri.Host;
        var port = databaseUri.Port == -1 ? 5432 : databaseUri.Port;
        var database = databaseUri.LocalPath.TrimStart('/');

        connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
    }

    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IAppointmentService, DbAppointmentService>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<OutboxPublisherWorker>();

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

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? "Invalid request payload"
                    : error.ErrorMessage))
                .ToArray();

            return new BadRequestObjectResult(ApiResponse<object>.Fail("Invalid request payload", errors));
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "https://frontend.clinic.example.com")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

/*
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var sharedSecret = builder.Configuration["Jwt:SharedSecret"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sharedSecret!)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });
*/

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Appointment Service API",
        Version = "v1",
        Description = "N1 Appointment Service backend API. Public API routes use /api/doctors, /api/specialties and /api/doctor-schedules. Appointment booking and operation routes require JWT. Service-to-service contracts for N2/N3 use /api/integration/appointments."
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

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();
    dbContext.Database.Migrate();
    EnsureWaitingQueueIndexes(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(frontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void EnsureWaitingQueueIndexes(AppointmentDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw("""
        DROP INDEX IF EXISTS "IX_WaitingQueues_QueueDate_QueueNumber";
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_WaitingQueues_DoctorId_QueueDate_QueueNumber"
            ON "WaitingQueues" ("DoctorId", "QueueDate", "QueueNumber");
        """);
}
