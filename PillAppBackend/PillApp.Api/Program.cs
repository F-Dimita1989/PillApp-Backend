using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using PillApp.Api.Data;
using PillApp.Api.Infrastructure;
using PillApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SupabaseDb");
var keepaliveSecret = builder.Configuration["Security:KeepaliveSecret"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing database configuration. Set ConnectionStrings__SupabaseDb.");
}

if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(keepaliveSecret))
{
    throw new InvalidOperationException(
        "Missing keepalive configuration. Set Security__KeepaliveSecret in non-development environments.");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Il catalogo AIFA cambia una volta al mese: la cache evita di interrogare Supabase
// a ogni carattere digitato dall'utente. SizeLimit impedisce che una raffica di
// termini di ricerca diversi faccia crescere la memoria senza controllo.
builder.Services.AddMemoryCache(options => options.SizeLimit = 512);
builder.Services.AddScoped<FarmaciReadService>();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
            return;
        }

        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .WithMethods("GET");
        }
    });
});

var permitPerMinute = builder.Configuration.GetValue<int?>("RateLimiting:PermitPerMinute") ?? 300;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter =
            context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? ((int)retryAfter.TotalSeconds).ToString()
                : "60";
        await Task.CompletedTask;
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // L'IP del proxy di Render non è noto in anticipo. ForwardLimit = 1 fa leggere solo
    // l'ultimo valore di X-Forwarded-For, quello scritto dal proxy stesso, così un client
    // non può falsificare il proprio IP per aggirare il rate limiting.
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

app.UseExceptionHandler();
app.UseForwardedHeaders();
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

    await next();
});

app.UseRateLimiter();
app.UseCors("ConfiguredOrigins");

app.MapGet("/", () => Results.Ok(new
{
    message = "PillApp API online",
    service = "PillApp.Api",
    endpoints = new
    {
        health = "/health",
        search = "/api/farmaci/search?q=",
        lookup = "/api/farmaci/{aic}"
    }
}));

// Health check leggero per Render: non tocca il database, così un problema di rete
// verso Supabase non provoca il riavvio a ciclo continuo del servizio.
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "PillApp.Api",
    uptime = "alive"
}));

// Interrogato dal workflow GitHub Actions per impedire a Supabase di mettere in pausa
// il progetto dopo 7 giorni di inattività.
app.MapGet("/keepalive-db", async (HttpRequest request, AppDbContext db) =>
{
    if (!string.IsNullOrWhiteSpace(keepaliveSecret))
    {
        if (!request.Headers.TryGetValue("X-KEEPALIVE", out var incomingSecret) ||
            !FixedTimeEquals(keepaliveSecret, incomingSecret.ToString()))
        {
            return Results.Unauthorized();
        }
    }

    var canConnect = await db.Database.CanConnectAsync();

    if (!canConnect)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new
    {
        status = "ok",
        database = "reachable"
    });
});

app.MapControllers();

app.Run();

static bool FixedTimeEquals(string expected, string provided)
{
    var expectedBytes = Encoding.UTF8.GetBytes(expected);
    var providedBytes = Encoding.UTF8.GetBytes(provided);

    return expectedBytes.Length == providedBytes.Length &&
           CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
}

public partial class Program;
