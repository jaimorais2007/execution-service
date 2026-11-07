using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RegistryService.Application.DTOs;
using RegistryService.Domain.Enums;
using Xunit;

namespace RegistryService.Tests.Api;

public class ControllersIntegrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HealthCheck_DeveRetornarOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Customer_E_Vehicle_FluxoCompletoDeEndpoints_DeveResponderComSucesso()
    {
        // 1. Criar Cliente (POST /api/Customer)
        var createCustomer = new CreateCustomerDto
        {
            Name = "Fernanda Lima",
            PersonType = PersonType.Individual,
            Document = "52998224725",
            DateOfBirth = new DateTime(1992, 8, 10, 0, 0, 0, DateTimeKind.Utc),
            Email = $"fernanda.{Guid.NewGuid():N}@email.com"
        };
        var customerResp = await _client.PostAsJsonAsync("/api/Customer", createCustomer);
        Assert.Equal(HttpStatusCode.Created, customerResp.StatusCode);

        var customer = await customerResp.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.NotNull(customer);

        // 2. Buscar Cliente por ID (GET /api/Customer/{id} e GET /api/Customers/{id})
        var getCustomer = await _client.GetAsync($"/api/Customer/{customer.Id}");
        Assert.Equal(HttpStatusCode.OK, getCustomer.StatusCode);

        var getCustomerPlural = await _client.GetAsync($"/api/Customers/{customer.Id}");
        Assert.Equal(HttpStatusCode.OK, getCustomerPlural.StatusCode);

        // 3. Listar Clientes (GET /api/Customer)
        var listCustomers = await _client.GetAsync("/api/Customer");
        Assert.Equal(HttpStatusCode.OK, listCustomers.StatusCode);

        // 4. Editar Cliente (PUT /api/Customer/{id})
        var updateCustomer = new UpdateCustomerDto
        {
            Name = "Fernanda Lima Atualizada",
            PersonType = PersonType.Individual,
            Document = "52998224725",
            DateOfBirth = new DateTime(1992, 8, 10, 0, 0, 0, DateTimeKind.Utc),
            Email = "fernanda.atualizada@email.com"
        };
        var updateCustomerResp = await _client.PutAsJsonAsync($"/api/Customer/{customer.Id}", updateCustomer);
        Assert.Equal(HttpStatusCode.OK, updateCustomerResp.StatusCode);

        // 5. Criar Veículo (POST /api/Vehicle)
        var randomLetters = $"{(char)('A' + Random.Shared.Next(26))}{(char)('A' + Random.Shared.Next(26))}{(char)('A' + Random.Shared.Next(26))}";
        var randomDigits = Random.Shared.Next(1000, 9999);
        var uniquePlate = $"{randomLetters}{randomDigits}";

        var createVehicle = new CreateVehicleDto
        {
            CustomerId = customer.Id,
            Plate = uniquePlate,
            Brand = "Chevrolet",
            Model = "Onix",
            Year = 2022
        };
        var vehicleResp = await _client.PostAsJsonAsync("/api/Vehicle", createVehicle);
        Assert.Equal(HttpStatusCode.Created, vehicleResp.StatusCode);

        var vehicle = await vehicleResp.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(vehicle);
        Assert.Equal(customer.Id, vehicle.CustomerId);

        // 6. Buscar Veículo por ID (GET /api/Vehicle/{id} e GET /api/Vehicles/{id})
        var getVehicle = await _client.GetAsync($"/api/Vehicle/{vehicle.Id}");
        Assert.Equal(HttpStatusCode.OK, getVehicle.StatusCode);

        var getVehiclePlural = await _client.GetAsync($"/api/Vehicles/{vehicle.Id}");
        Assert.Equal(HttpStatusCode.OK, getVehiclePlural.StatusCode);

        // 7. Buscar Veículo por Cliente (GET /api/Vehicle/customer/{customerId} e GET /api/Vehicle?customerId=)
        var byCustomerRoute = await _client.GetAsync($"/api/Vehicle/customer/{customer.Id}");
        Assert.Equal(HttpStatusCode.OK, byCustomerRoute.StatusCode);
        var vehiclesOfCustomer = await byCustomerRoute.Content.ReadFromJsonAsync<List<VehicleDto>>();
        Assert.NotNull(vehiclesOfCustomer);
        Assert.Single(vehiclesOfCustomer);

        var byCustomerQuery = await _client.GetAsync($"/api/Vehicle?customerId={customer.Id}");
        Assert.Equal(HttpStatusCode.OK, byCustomerQuery.StatusCode);

        // 8. Editar Veículo (PUT /api/Vehicle/{id})
        var updateVehicle = new UpdateVehicleDto
        {
            Plate = uniquePlate,
            Brand = "Chevrolet",
            Model = "Onix Plus",
            Year = 2023
        };
        var updateVehicleResp = await _client.PutAsJsonAsync($"/api/Vehicle/{vehicle.Id}", updateVehicle);
        Assert.Equal(HttpStatusCode.OK, updateVehicleResp.StatusCode);

        // 9. LogicalDeletion e Delete de Veículo
        var logicalVehicleResp = await _client.PutAsync($"/api/Vehicle/{vehicle.Id}/LogicalDeletion", null);
        Assert.Equal(HttpStatusCode.OK, logicalVehicleResp.StatusCode);

        var deleteVehicleResp = await _client.DeleteAsync($"/api/Vehicle/{vehicle.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteVehicleResp.StatusCode);

        // 10. LogicalDeletion e Delete de Cliente
        var logicalCustomerResp = await _client.PutAsync($"/api/Customer/{customer.Id}/LogicalDeletion", null);
        Assert.Equal(HttpStatusCode.OK, logicalCustomerResp.StatusCode);

        var deleteCustomerResp = await _client.DeleteAsync($"/api/Customer/{customer.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCustomerResp.StatusCode);
    }
}
