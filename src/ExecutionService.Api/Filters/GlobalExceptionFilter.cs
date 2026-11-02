using System.Diagnostics;
using ExecutionService.Application.Common;
using ExecutionService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenTelemetry.Trace;

namespace ExecutionService.Api.Filters;

public class GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var activity = Activity.Current;

        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.RecordException(exception);
        }

        switch (exception)
        {
            case DomainException domainEx:
                logger.LogWarning("DomainException handled: {Message}", domainEx.Message);
                context.Result = new BadRequestObjectResult(new { error = domainEx.Message });
                context.ExceptionHandled = true;
                break;

            case NotFoundException notFoundEx:
                logger.LogInformation("NotFoundException handled: {Message}", notFoundEx.Message);
                context.Result = new NotFoundObjectResult(new { error = notFoundEx.Message });
                context.ExceptionHandled = true;
                break;

            default:
                logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
                context.Result = new ObjectResult(new { error = "Ocorreu um erro interno inesperado." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
                context.ExceptionHandled = true;
                break;
        }
    }
}
