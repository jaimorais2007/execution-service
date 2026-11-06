using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RegistryService.Api.Filters;
using RegistryService.Application;
using RegistryService.Infrastructure;
using RegistryService.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts => opts.EnableAnnotations());
builder.Services.AddHealthChecks();

var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "RegistryService";
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RegistryDbContext>();
    try
    {
        await db.Database.EnsureCreatedAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Could not automatically initialize database on startup");
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health");
app.MapControllers();

await app.RunAsync();

public partial class Program;
