using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "FrontendCorsPolicy";

var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForPharmacyBillingServiceThatIsAtLeast32BytesLong!";
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

builder.Services.AddHttpClient("proxy")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false
    });

var app = builder.Build();

var medicalBaseUrl = GetRequiredUri("MEDICAL_API_URL", "http://medical-api:8080");
var pharmacyBaseUrl = GetRequiredUri("PHARMACY_API_URL", "http://pharmacy-billing-service:8080");
var appointmentBaseUrl = GetRequiredUri("APPOINTMENT_API_URL", "http://appointment-service:8080");

app.UseCors(frontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (IsPublicRequest(context.Request)
        || context.User.Identity?.IsAuthenticated == true)
    {
        await next();
        return;
    }

    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    await context.Response.WriteAsJsonAsync(new { message = "Missing or invalid JWT token." });
});

app.MapGet("/", () => Results.Ok(new
{
    service = "Clinic API Gateway",
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
    timestamp = DateTime.UtcNow
}));

app.MapGet("/swagger/v1/swagger.json", async (
    HttpContext context,
    IHttpClientFactory httpClientFactory) =>
{
    var referer = context.Request.Headers.Referer.ToString();
    var routePrefix = referer.Contains("/pharmacy/", StringComparison.OrdinalIgnoreCase)
        ? "pharmacy"
        : referer.Contains("/appointment/", StringComparison.OrdinalIgnoreCase)
            ? "appointment"
        : "medical";
    var upstream = routePrefix switch
    {
        "pharmacy" => pharmacyBaseUrl,
        "appointment" => appointmentBaseUrl,
        _ => medicalBaseUrl
    };

    await ProxySwaggerJsonAsync(context, httpClientFactory.CreateClient("proxy"), upstream, routePrefix);
});

app.Map("/{service}/{**path}", async (
    string service,
    string? path,
    HttpContext context,
    IHttpClientFactory httpClientFactory) =>
{
    var upstream = service.ToLowerInvariant() switch
    {
        "medical" => medicalBaseUrl,
        "pharmacy" => pharmacyBaseUrl,
        "appointment" => appointmentBaseUrl,
        _ => null
    };

    if (upstream is null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(new
        {
            message = "Unknown gateway route",
            supportedRoutes = new[] { "/appointment", "/medical", "/pharmacy" }
        });
        return;
    }

    await ProxyAsync(context, httpClientFactory.CreateClient("proxy"), upstream, service.ToLowerInvariant(), path ?? string.Empty);
});

app.Run();

static Uri GetRequiredUri(string key, string fallback)
{
    var value = Environment.GetEnvironmentVariable(key);
    return Uri.TryCreate(string.IsNullOrWhiteSpace(value) ? fallback : value, UriKind.Absolute, out var uri)
        ? uri
        : throw new InvalidOperationException($"{key} must be an absolute URL.");
}

static bool IsPublicRequest(HttpRequest request)
{
    var path = request.Path.Value ?? string.Empty;
    var isGet = HttpMethods.IsGet(request.Method);
    var isPost = HttpMethods.IsPost(request.Method);
    return path == "/"
        || HttpMethods.IsOptions(request.Method)
        || path.Equals("/health", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/swagger/", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/swagger/", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/pharmacy/api/auth/login", StringComparison.OrdinalIgnoreCase)
        || (isPost && path.Equals("/pharmacy/api/auth/register", StringComparison.OrdinalIgnoreCase))
        || path.Equals("/pharmacy/api/events/simulate-prescription-created", StringComparison.OrdinalIgnoreCase)
        || (isGet && path.StartsWith("/appointment/api/specialties", StringComparison.OrdinalIgnoreCase))
        || (isGet && path.StartsWith("/appointment/api/doctors", StringComparison.OrdinalIgnoreCase))
        || (isGet && path.StartsWith("/appointment/api/doctor-schedules", StringComparison.OrdinalIgnoreCase))
        || (isPost && path.Equals("/appointment/api/appointments", StringComparison.OrdinalIgnoreCase))
        || path.EndsWith("/health", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("/api/health", StringComparison.OrdinalIgnoreCase);
}

static async Task ProxyAsync(HttpContext context, HttpClient httpClient, Uri upstreamBaseUrl, string routePrefix, string path)
{
    var targetUri = BuildTargetUri(context.Request, upstreamBaseUrl, path);

    using var requestMessage = new HttpRequestMessage
    {
        Method = new HttpMethod(context.Request.Method),
        RequestUri = targetUri
    };

    CopyRequestHeaders(context.Request, requestMessage);

    if (HttpMethods.IsPost(context.Request.Method)
        || HttpMethods.IsPut(context.Request.Method)
        || HttpMethods.IsPatch(context.Request.Method)
        || HttpMethods.IsDelete(context.Request.Method))
    {
        requestMessage.Content = new StreamContent(context.Request.Body);
        foreach (var header in context.Request.Headers)
        {
            requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    using var responseMessage = await httpClient.SendAsync(
        requestMessage,
        HttpCompletionOption.ResponseHeadersRead,
        context.RequestAborted);

    if (ShouldServeSwaggerUiShell(path, responseMessage))
    {
        await WriteSwaggerUiShellAsync(context, routePrefix, GetSwaggerTitle(routePrefix));
        return;
    }

    if (ShouldRewriteSwaggerAsset(path, responseMessage))
    {
        await WriteRewrittenSwaggerAssetAsync(context, responseMessage, routePrefix);
        return;
    }

    if (ShouldRewriteSwaggerJson(path, responseMessage))
    {
        await WriteRewrittenSwaggerJsonAsync(context, responseMessage, routePrefix);
        return;
    }

    context.Response.StatusCode = (int)responseMessage.StatusCode;
    CopyResponseHeaders(context.Response, responseMessage);
    await responseMessage.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
}

static Uri BuildTargetUri(HttpRequest request, Uri upstreamBaseUrl, string path)
{
    var builder = new UriBuilder(upstreamBaseUrl);
    var basePath = builder.Path.TrimEnd('/');
    var requestedPath = path.TrimStart('/');
    builder.Path = string.IsNullOrEmpty(requestedPath) ? basePath : $"{basePath}/{requestedPath}";
    builder.Query = request.QueryString.HasValue ? request.QueryString.Value![1..] : string.Empty;
    return builder.Uri;
}

static void CopyRequestHeaders(HttpRequest request, HttpRequestMessage requestMessage)
{
    foreach (var header in request.Headers)
    {
        if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
    }

    requestMessage.Headers.TryAddWithoutValidation("X-Forwarded-Host", request.Host.Value);
    requestMessage.Headers.TryAddWithoutValidation("X-Forwarded-Proto", request.Scheme);
    requestMessage.Headers.AcceptEncoding.Clear();
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

static bool ShouldServeSwaggerUiShell(string path, HttpResponseMessage responseMessage)
{
    if (!responseMessage.IsSuccessStatusCode)
    {
        return false;
    }

    var normalizedPath = path.Trim('/').ToLowerInvariant();
    var mediaType = responseMessage.Content.Headers.ContentType?.MediaType;
    return normalizedPath is "swagger" or "swagger/index.html"
        && (mediaType?.Contains("html", StringComparison.OrdinalIgnoreCase) == true
            || mediaType?.Contains("javascript", StringComparison.OrdinalIgnoreCase) == true);
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

static bool ShouldRewriteSwaggerAsset(string path, HttpResponseMessage responseMessage)
{
    if (!responseMessage.IsSuccessStatusCode)
    {
        return false;
    }

    var normalizedPath = path.Trim('/').ToLowerInvariant();
    var mediaType = responseMessage.Content.Headers.ContentType?.MediaType;
    return normalizedPath is "swagger/index.js"
        && mediaType?.Contains("javascript", StringComparison.OrdinalIgnoreCase) == true;
}

static async Task WriteRewrittenSwaggerAssetAsync(
    HttpContext context,
    HttpResponseMessage responseMessage,
    string routePrefix)
{
    var content = await responseMessage.Content.ReadAsStringAsync(context.RequestAborted);
    content = content
        .Replace("/swagger/v1/swagger.json", $"/{routePrefix}/swagger/v1/swagger.json", StringComparison.OrdinalIgnoreCase)
        .Replace("\"swagger/v1/swagger.json\"", $"\"/{routePrefix}/swagger/v1/swagger.json\"", StringComparison.OrdinalIgnoreCase);

    context.Response.StatusCode = (int)responseMessage.StatusCode;
    context.Response.ContentType = responseMessage.Content.Headers.ContentType?.ToString()
        ?? "text/plain; charset=utf-8";
    await context.Response.WriteAsync(content, context.RequestAborted);
}

static string GetSwaggerTitle(string routePrefix)
{
    return routePrefix.Equals("medical", StringComparison.OrdinalIgnoreCase)
        ? "N2 Medical Record Service API"
        : routePrefix.Equals("appointment", StringComparison.OrdinalIgnoreCase)
            ? "Appointment Service API"
        : "Pharmacy & Billing Service API";
}

static bool ShouldRewriteSwaggerJson(string path, HttpResponseMessage responseMessage)
{
    if (!responseMessage.IsSuccessStatusCode)
    {
        return false;
    }

    return path.Trim('/').Equals("swagger/v1/swagger.json", StringComparison.OrdinalIgnoreCase);
}

static async Task WriteRewrittenSwaggerJsonAsync(
    HttpContext context,
    HttpResponseMessage responseMessage,
    string routePrefix)
{
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

    if (!responseMessage.IsSuccessStatusCode)
    {
        context.Response.StatusCode = (int)responseMessage.StatusCode;
        await responseMessage.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        return;
    }

    await WriteRewrittenSwaggerJsonAsync(context, responseMessage, routePrefix);
}
