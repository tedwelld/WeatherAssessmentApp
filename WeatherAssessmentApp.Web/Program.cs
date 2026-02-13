using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WeatherAssessmentApp.Application;
using WeatherAssessmentApp.Infrastructure;
using WeatherAssessmentApp.Infrastructure.Persistence;
using WeatherAssessmentApp.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Web API + enum serialization as strings for cleaner client contracts.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register application and infrastructure layers.
builder.Services.AddApplication(); // Includes sync history services.
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Global API throttling to protect backend resources and third-party quota.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "global";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

var app = builder.Build();

// Swagger stays enabled to support endpoint testing from browser UI.
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseRateLimiter();
app.UseCors("frontend");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    // Ensure schema is up-to-date and demo records exist on every startup.
    await dbContext.Database.MigrateAsync();
    await dbContext.SeedDemoLocationsAsync();
}

app.Run();
