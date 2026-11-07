using RegistryService.Api.Configuration;
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
builder.Services.AddSwaggerGen(opts =>
{
    opts.EnableAnnotations();
    opts.AddJwtSecurity();
});

// Exige o JWT da Lambda de autenticação em todas as rotas (exceto /health)
builder.Services.AddJwtAuthentication();
builder.Services.AddHealthChecks();

builder.Services.AddOTelConfiguration(builder.Configuration);
builder.Logging.AddOTelLogging();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

await app.RunAsync();

public partial class Program;
