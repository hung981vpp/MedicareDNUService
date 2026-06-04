using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "FrontendCorsPolicy";

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForPharmacyBillingServiceThatIsAtLeast32BytesLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PharmacyBillingService";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PharmacyBillingService";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
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

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient("swagger-proxy")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false
    });
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

var medicalBaseUrl = GetRequiredUri("MEDICAL_API_URL", "http://medical-api:8080");
var pharmacyBaseUrl = GetRequiredUri("PHARMACY_API_URL", "http://pharmacy-billing-service:8080");
var appointmentBaseUrl = GetRequiredUri("APPOINTMENT_API_URL", "http://appointment-service:8080");

app.UseCors(frontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;

    if (path.Equals("/", StringComparison.OrdinalIgnoreCase))
    {
        await context.Response.WriteAsJsonAsync(new
        {
            service = "Clinic API Gateway",
            gateway = "Ocelot",
            routes = new
            {
                appointment = "/appointment",
                medical = "/medical",
                pharmacy = "/pharmacy",
                health = "/health"
            }
        });
        return;
    }

    if (path.Equals("/health", StringComparison.OrdinalIgnoreCase))
    {
        await context.Response.WriteAsJsonAsync(new
        {
            status = "Healthy",
            service = "Clinic API Gateway",
            gateway = "Ocelot",
            timestamp = DateTime.UtcNow
        });
        return;
    }

    if (path.Equals("/swagger/v1/swagger.json", StringComparison.OrdinalIgnoreCase))
    {
        var routePrefix = GetRoutePrefixFromReferer(context.Request.Headers.Referer.ToString());
        var upstream = GetUpstream(routePrefix, appointmentBaseUrl, medicalBaseUrl, pharmacyBaseUrl);

        await ProxySwaggerJsonAsync(context, context.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("swagger-proxy"), upstream, routePrefix);
        return;
    }

    var swaggerRoute = TryParseGatewaySwaggerRoute(path);
    if (swaggerRoute is not null)
    {
        await ProxySwaggerAsync(
            swaggerRoute.Value.Service,
            swaggerRoute.Value.Path,
            context,
            context.RequestServices.GetRequiredService<IHttpClientFactory>(),
            appointmentBaseUrl,
            medicalBaseUrl,
            pharmacyBaseUrl);
        return;
    }

    await next();
});

app.MapGet("/", () => Results.Ok(new
{
    service = "Clinic API Gateway",
    gateway = "Ocelot",
    routes = new
    {
        appointment = "/appointment",
        medical = "/medical",
        pharmacy = "/pharmacy",
        health = "/health"
    }
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "Clinic API Gateway",
    gateway = "Ocelot",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/swagger/v1/swagger.json", async (
    HttpContext context,
    IHttpClientFactory httpClientFactory) =>
{
    var routePrefix = GetRoutePrefixFromReferer(context.Request.Headers.Referer.ToString());
    var upstream = GetUpstream(routePrefix, appointmentBaseUrl, medicalBaseUrl, pharmacyBaseUrl);

    await ProxySwaggerJsonAsync(context, httpClientFactory.CreateClient("swagger-proxy"), upstream, routePrefix);
}).AllowAnonymous();

app.Map("/{service}/swagger", async (
    string service,
    HttpContext context,
    IHttpClientFactory httpClientFactory) =>
{
    await ProxySwaggerAsync(service, string.Empty, context, httpClientFactory, appointmentBaseUrl, medicalBaseUrl, pharmacyBaseUrl);
}).AllowAnonymous();

app.Map("/{service}/swagger/{**path}", async (
    string service,
    string? path,
    HttpContext context,
    IHttpClientFactory httpClientFactory) =>
{
    await ProxySwaggerAsync(service, path ?? string.Empty, context, httpClientFactory, appointmentBaseUrl, medicalBaseUrl, pharmacyBaseUrl);
}).AllowAnonymous();

await app.UseOcelot();

app.Run();

static Uri GetRequiredUri(string key, string fallback)
{
    var value = Environment.GetEnvironmentVariable(key);
    return Uri.TryCreate(string.IsNullOrWhiteSpace(value) ? fallback : value, UriKind.Absolute, out var uri)
        ? uri
        : throw new InvalidOperationException($"{key} must be an absolute URL.");
}

static string GetRoutePrefixFromReferer(string referer)
{
    if (referer.Contains("/pharmacy/", StringComparison.OrdinalIgnoreCase)) return "pharmacy";
    if (referer.Contains("/appointment/", StringComparison.OrdinalIgnoreCase)) return "appointment";
    return "medical";
}

static Uri GetUpstream(string routePrefix, Uri appointmentBaseUrl, Uri medicalBaseUrl, Uri pharmacyBaseUrl)
    => routePrefix.ToLowerInvariant() switch
    {
        "appointment" => appointmentBaseUrl,
        "medical" => medicalBaseUrl,
        "pharmacy" => pharmacyBaseUrl,
        _ => throw new InvalidOperationException("Unsupported swagger route.")
    };

static (string Service, string Path)? TryParseGatewaySwaggerRoute(string requestPath)
{
    var parts = requestPath.Trim('/').Split('/', 3, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length < 2 || !parts[1].Equals("swagger", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    var service = parts[0].ToLowerInvariant();
    if (service is not ("appointment" or "medical" or "pharmacy"))
    {
        return null;
    }

    return (service, parts.Length == 2 ? string.Empty : parts[2]);
}

static async Task ProxySwaggerAsync(
    string service,
    string path,
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    Uri appointmentBaseUrl,
    Uri medicalBaseUrl,
    Uri pharmacyBaseUrl)
{
    var routePrefix = service.ToLowerInvariant();
    if (routePrefix is not ("appointment" or "medical" or "pharmacy"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(new { message = "Unknown gateway route" });
        return;
    }

    var upstream = GetUpstream(routePrefix, appointmentBaseUrl, medicalBaseUrl, pharmacyBaseUrl);
    var normalizedPath = path.Trim('/');

    if (string.IsNullOrEmpty(normalizedPath) || normalizedPath.Equals("index.html", StringComparison.OrdinalIgnoreCase))
    {
        await WriteSwaggerUiShellAsync(context, routePrefix, GetSwaggerTitle(routePrefix));
        return;
    }

    using var responseMessage = await httpClientFactory
        .CreateClient("swagger-proxy")
        .GetAsync(new Uri(upstream, $"/swagger/{normalizedPath}"), HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

    if (normalizedPath.Equals("v1/swagger.json", StringComparison.OrdinalIgnoreCase))
    {
        await WriteRewrittenSwaggerJsonAsync(context, responseMessage, routePrefix);
        return;
    }

    context.Response.StatusCode = (int)responseMessage.StatusCode;
    CopyResponseHeaders(context.Response, responseMessage);
    await responseMessage.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
}

static async Task WriteSwaggerUiShellAsync(HttpContext context, string routePrefix, string title)
{
    var html = $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>{{title}}</title>
    <link rel="stylesheet" type="text/css" href="/{{routePrefix}}/swagger/swagger-ui.css">
    <link rel="icon" type="image/png" href="/{{routePrefix}}/swagger/favicon-32x32.png" sizes="32x32">
    <link rel="icon" type="image/png" href="/{{routePrefix}}/swagger/favicon-16x16.png" sizes="16x16">
    <style>
        html { box-sizing: border-box; overflow-y: scroll; }
        *, *:before, *:after { box-sizing: inherit; }
        body { margin: 0; background: #fafafa; }
    </style>
</head>
<body>
    <div id="swagger-ui"></div>
    <script src="/{{routePrefix}}/swagger/swagger-ui-bundle.js"></script>
    <script src="/{{routePrefix}}/swagger/swagger-ui-standalone-preset.js"></script>
    <script>
        window.onload = function () {
            window.ui = SwaggerUIBundle({
                urls: [{ url: "/{{routePrefix}}/swagger/v1/swagger.json", name: "{{title}} v1" }],
                dom_id: "#swagger-ui",
                deepLinking: false,
                presets: [SwaggerUIBundle.presets.apis, SwaggerUIStandalonePreset],
                layout: "StandaloneLayout",
                validatorUrl: null
            });
        };
    </script>
</body>
</html>
""";

    context.Response.StatusCode = StatusCodes.Status200OK;
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(html, context.RequestAborted);
}

static string GetSwaggerTitle(string routePrefix)
{
    return routePrefix.Equals("medical", StringComparison.OrdinalIgnoreCase)
        ? "N2 Medical Record Service API"
        : routePrefix.Equals("appointment", StringComparison.OrdinalIgnoreCase)
            ? "Appointment Service API"
        : "Pharmacy & Billing Service API";
}

static async Task ProxySwaggerJsonAsync(
    HttpContext context,
    HttpClient httpClient,
    Uri upstreamBaseUrl,
    string routePrefix)
{
    using var responseMessage = await httpClient.GetAsync(
        new Uri(upstreamBaseUrl, "/swagger/v1/swagger.json"),
        HttpCompletionOption.ResponseHeadersRead,
        context.RequestAborted);

    await WriteRewrittenSwaggerJsonAsync(context, responseMessage, routePrefix);
}

static async Task WriteRewrittenSwaggerJsonAsync(
    HttpContext context,
    HttpResponseMessage responseMessage,
    string routePrefix)
{
    if (!responseMessage.IsSuccessStatusCode)
    {
        context.Response.StatusCode = (int)responseMessage.StatusCode;
        await responseMessage.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        return;
    }

    var json = await responseMessage.Content.ReadAsStringAsync(context.RequestAborted);
    var node = JsonNode.Parse(json) as JsonObject;

    if (node is not null)
    {
        node["servers"] = new JsonArray(new JsonObject
        {
            ["url"] = $"/{routePrefix}"
        });

        json = node.ToJsonString();
    }

    context.Response.StatusCode = (int)responseMessage.StatusCode;
    context.Response.ContentType = "application/json; charset=utf-8";
    await context.Response.WriteAsync(json, context.RequestAborted);
}

static void CopyResponseHeaders(HttpResponse response, HttpResponseMessage responseMessage)
{
    foreach (var header in responseMessage.Headers)
    {
        response.Headers[header.Key] = header.Value.ToArray();
    }

    foreach (var header in responseMessage.Content.Headers)
    {
        response.Headers[header.Key] = header.Value.ToArray();
    }

    response.Headers.Remove("transfer-encoding");
}
