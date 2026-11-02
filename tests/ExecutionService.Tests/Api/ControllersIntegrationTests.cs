using System.Net;
using System.Net.Http.Json;
using ExecutionService.Application.Catalog;
using ExecutionService.Application.Executions;
using ExecutionService.Application.Parts;
using ExecutionService.Domain.Entities;
using ExecutionService.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExecutionService.Tests.Api;

public class ControllersIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ControllersIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PartsController_CrudEndpoints_DevemResponderComSucesso()
    {
        // 1. Create
        var createRequest = new CreatePartRequest("Vela de Ignição NGK", $"NGK-{Guid.NewGuid():N}", 8, 32.50m);
        var createResponse = await _client.PostAsJsonAsync("/api/Parts", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<PartDto>();
        Assert.NotNull(created);
        var partId = created.Id;

        // 2. GetById
        var getResponse = await _client.GetAsync($"/api/Parts/{partId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // 3. AddStock
        var addStockResponse = await _client.PostAsJsonAsync($"/api/Parts/{partId}/add-stock", new StockQuantityRequest(4));
        Assert.Equal(HttpStatusCode.OK, addStockResponse.StatusCode);

        // 4. RemoveStock
        var removeStockResponse = await _client.PostAsJsonAsync($"/api/Parts/{partId}/remove-stock", new StockQuantityRequest(2));
        Assert.Equal(HttpStatusCode.OK, removeStockResponse.StatusCode);

        // 5. LogicalDeletion
        var deletionResponse = await _client.PutAsync($"/api/Parts/{partId}/LogicalDeletion", null);
        Assert.Equal(HttpStatusCode.OK, deletionResponse.StatusCode);

        // 6. Delete
        var deleteResponse = await _client.DeleteAsync($"/api/Parts/{partId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ServiceController_CrudEndpoints_DevemResponderComSucesso()
    {
        // 1. Create
        var createRequest = new SaveServiceRequest("Revisão Preventiva", "Checagem de 30 itens", 250m);
        var createResponse = await _client.PostAsJsonAsync("/api/Service", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ServiceDto>();
        Assert.NotNull(created);
        var serviceId = created.Id;

        // 2. GetAll
        var getAllResponse = await _client.GetAsync("/api/Service");
        Assert.Equal(HttpStatusCode.OK, getAllResponse.StatusCode);

        // 3. Update
        var updateResponse = await _client.PutAsJsonAsync($"/api/Service/{serviceId}", new SaveServiceRequest("Revisão Completa", "40 itens", 300m));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // 4. LogicalDeletion
        var deletionResponse = await _client.PutAsync($"/api/Service/{serviceId}/LogicalDeletion", null);
        Assert.Equal(HttpStatusCode.OK, deletionResponse.StatusCode);

        // 5. Delete
        var deleteResponse = await _client.DeleteAsync($"/api/Service/{serviceId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ExecutionsController_EndpointsAvancoDeStatus_DevemFuncionar()
    {
        // Seed order
        Guid orderId;
        Guid serviceOrderId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ExecutionDbContext>();
            var part = new Part("Fluido Freio", $"FLD-{Guid.NewGuid():N}", 5, 40);
            var order = ExecutionOrder.Enqueue(serviceOrderId, [(part, 1)], []);
            db.Parts.Add(part);
            db.ExecutionOrders.Add(order);
            await db.SaveChangesAsync();
            orderId = order.Id;
        }

        // Get by ServiceOrderId
        var getByOs = await _client.GetAsync($"/api/Executions/by-order/{serviceOrderId}");
        Assert.Equal(HttpStatusCode.OK, getByOs.StatusCode);

        // Start
        var startResponse = await _client.PostAsync($"/api/Executions/{orderId}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        // Finish diagnosis
        var finishDiagnosisResponse = await _client.PostAsJsonAsync(
            $"/api/Executions/{orderId}/finish-diagnosis",
            new ExecutionNotesRequest("Diagnóstico aprovado"));
        Assert.Equal(HttpStatusCode.OK, finishDiagnosisResponse.StatusCode);

        // Complete
        var completeResponse = await _client.PostAsJsonAsync(
            $"/api/Executions/{orderId}/complete",
            new ExecutionNotesRequest("Reparo finalizado"));
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
    }

    [Fact]
    public async Task ExecutionsController_Fail_DeveRetornarOk()
    {
        Guid orderId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ExecutionDbContext>();
            var order = ExecutionOrder.Enqueue(Guid.NewGuid(), [], []);
            db.ExecutionOrders.Add(order);
            await db.SaveChangesAsync();
            orderId = order.Id;
        }

        var failResponse = await _client.PostAsJsonAsync(
            $"/api/Executions/{orderId}/fail",
            new FailExecutionRequest("Falha de teste"));
        Assert.Equal(HttpStatusCode.OK, failResponse.StatusCode);
    }
}
