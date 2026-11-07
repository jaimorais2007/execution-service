namespace RegistryService.Application.DTOs;

public class UseCaseResponse<T>
{
    public bool IsSuccess { get; set; }
    public string[] Messages { get; set; } = [];
    public T Response { get; set; } = default!;

    public UseCaseResponse() { }

    public UseCaseResponse(bool isSuccess, string[] messages, T response)
    {
        IsSuccess = isSuccess;
        Messages = messages;
        Response = response;
    }

    public static UseCaseResponse<T> Success(T response) =>
        new(true, [], response);

    public static UseCaseResponse<T> Failure(params string[] messages) =>
        new(false, messages, default!);
}

