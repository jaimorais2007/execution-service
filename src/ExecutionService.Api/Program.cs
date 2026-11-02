using ExecutionService.Api.Filters;
using ExecutionService.Application;
using ExecutionService.Infrastructure;
using ExecutionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Presentation services
builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// OpenTelemetry configuration
var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "ExecutionService";
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(serviceName)
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/health");
                opts.RecordException = true;
            });

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            tracing.AddOtlpExporter();
        }
    });

var app = builder.Build();

// Ensure database schema is created on startup if using EF
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ExecutionDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Could not automatically initialize database on startup");
    }
}

// Swagger enabled in all environments for API demonstration
app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;
