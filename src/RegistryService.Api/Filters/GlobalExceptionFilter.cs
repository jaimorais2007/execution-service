using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RegistryService.Domain.Exceptions;

namespace RegistryService.Api.Filters;

public class GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var activity = Activity.Current;

        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.AddException(exception);
        }

        switch (exception)
        {
            case DomainException domainEx:
                logger.LogWarning("DomainException handled: {Message}", domainEx.Message);
                context.Result = new BadRequestObjectResult(new { Message = domainEx.Message });
                context.ExceptionHandled = true;
                break;

            case KeyNotFoundException notFoundEx:
                logger.LogInformation("KeyNotFoundException handled: {Message}", notFoundEx.Message);
                context.Result = new NotFoundObjectResult(new { Message = notFoundEx.Message });
                context.ExceptionHandled = true;
                break;

            default:
                logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
                context.Result = new ObjectResult(new { Message = "Ocorreu um erro interno inesperado." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
                context.ExceptionHandled = true;
                break;
        }
    }
}

