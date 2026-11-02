using ExecutionService.Domain.Entities;

namespace ExecutionService.Application.Parts;

public sealed record PartDto(
    Guid Id,
    string Name,
    string Code,
    decimal Price,
    int QuantityInStock,
    int QuantityReserved,
    int AvailableQuantity,
    bool Inactive,
    DateTime CreatedAt)
{
    public static PartDto From(Part part) => new(
        part.Id,
        part.Name,
        part.Code,
        part.Price,
        part.QuantityInStock,
        part.QuantityReserved,
        part.AvailableQuantity,
        part.Inactive,
        part.CreatedAt);
}

public sealed record CreatePartRequest(
    string Name,
    string Code,
    int InitialQuantity,
    decimal Price);

public sealed record UpdatePartRequest(
    string Name,
    string Code,
    decimal Price);

public sealed record StockQuantityRequest(int Quantity);
