
using MyProject.Infrastructure.Data;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// SERVICE REGISTRATION
// ============================================

// Aspire Service Defaults (Telemetry, Service Discovery)
builder.AddServiceDefaults();

// Azure Key Vault Configuration
builder.AddKeyVaultIfConfigured();

// Application Layer (CQRS, MediatR, Validation)
builder.AddApplicationServices();

// Infrastructure Layer (Database, Identity)
builder.AddInfrastructureServices();

// Web Layer (API, Swagger, Exception Handling)
builder.AddWebServices();

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1)
                }));

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests. Please try again later."
            }, cancellationToken: token);
        };
    });
}

var app = builder.Build();

// Exception handler FIRST
app.UseExceptionHandler(options => { });

// Development
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    app.UseHsts();
}

// Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();    // ← CRITICAL: Add this
app.UseAuthorization();     // ← CRITICAL: Add this

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

// Endpoints
app.MapDefaultEndpoints();
app.MapEndpoints();
app.MapRazorPages();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
