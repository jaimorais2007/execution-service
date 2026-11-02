var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Swagger fica habilitado em todos os ambientes: é a forma de demonstração combinada pelo grupo.
app.UseSwagger();
app.UseSwaggerUI();

// Rota pública usada pelo API Gateway (GET /execution/health) e pelas probes do Kubernetes.
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

// Expõe a classe Program para os testes com WebApplicationFactory.
public partial class Program;
