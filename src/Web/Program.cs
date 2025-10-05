
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

// CORS Configuration
//if (builder.Environment.IsDevelopment())
//{
//    builder.Services.AddCors(options =>
//    {
//        options.AddPolicy("DevelopmentCORS", policy =>
//        {
//            policy.WithOrigins("https://localhost:44447") // Angular dev server
//                  .AllowAnyMethod()
//                  .AllowAnyHeader()
//                  .AllowCredentials();
//        });
//    });
//}
//else
//{
//    builder.Services.AddCors(options =>
//    {
//        options.AddPolicy("ProductionCORS", policy =>
//        {
//            policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
//                  .AllowAnyMethod()
//                  .AllowAnyHeader()
//                  .AllowCredentials();
//        });
//    });
//}

// Rate Limiting (Production)
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

// ============================================
// MIDDLEWARE PIPELINE
// ============================================

// 1. Global Exception Handler (MUST BE FIRST)
app.UseExceptionHandler(options => { });

// 2. Development-specific middleware
if (app.Environment.IsDevelopment())
{
    // Initialize and seed database
    await app.InitialiseDatabaseAsync();

    // CORS for Angular dev server
    app.UseCors("DevelopmentCORS");

    // Request logging
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("→ {Method} {Path}", context.Request.Method, context.Request.Path);
        await next();
        logger.LogInformation("← {StatusCode}", context.Response.StatusCode);
    });
}
else
{
    // Production security
    app.UseHsts();

    // Production CORS
    app.UseCors("ProductionCORS");

    // Rate limiting
    app.UseRateLimiter();
}

// 3. HTTPS Redirection
app.UseHttpsRedirection();

// 4. Response Compression
app.UseResponseCompression();

// 5. Static Files (Angular build output)
app.UseStaticFiles();

// 6. Authentication (CRITICAL - before Authorization)
app.UseAuthentication();

// 7. Authorization (CRITICAL - after Authentication)
app.UseAuthorization();

// 8. Swagger/OpenAPI Documentation
app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

// 9. Endpoint Mapping
app.MapDefaultEndpoints();           // Aspire health checks (/health, /alive)
app.MapEndpoints();                  // API endpoints (/api/TodoLists, etc.)
app.MapRazorPages();                 // Identity UI pages (/Identity/Account/...)
app.MapFallbackToFile("index.html"); // SPA fallback (MUST BE LAST)

// ============================================
// RUN APPLICATION
// ============================================
app.Run();

// Partial class for integration testing
public partial class Program { }
