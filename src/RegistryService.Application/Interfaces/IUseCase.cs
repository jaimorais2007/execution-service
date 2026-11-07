using RegistryService.Application.DTOs;

namespace RegistryService.Application.Interfaces;

public interface IUseCase<in TInput, TOutput>
{
    Task<UseCaseResponse<TOutput>> ExecuteAsync(TInput input);
}

