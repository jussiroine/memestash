using System.Threading.RateLimiting;
using MemeStash.Endpoints;
using MemeStash.Models;
using MemeStash.Services;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<AzureStorageOptions>(
    builder.Configuration.GetSection(AzureStorageOptions.SectionName));

// Services
builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddSingleton<IMemeStorageService>(sp => sp.GetRequiredService<BlobStorageService>());
builder.Services.AddRazorPages();

// Request size limit (5 MB)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 6 * 1024 * 1024; // slightly above 5 MB for form overhead
});

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("upload", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Initialize blob container
var storageService = app.Services.GetRequiredService<BlobStorageService>();
await storageService.InitializeAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapMemeEndpoints();

app.Run();
