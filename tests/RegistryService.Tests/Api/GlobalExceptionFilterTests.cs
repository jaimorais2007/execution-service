using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using RegistryService.Api.Filters;
using RegistryService.Domain.Exceptions;
using Xunit;

namespace RegistryService.Tests.Api;

public class GlobalExceptionFilterTests
{
    private static ExceptionContext CreateContext(Exception exception)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ExceptionContext(actionContext, [])
        {
            Exception = exception
        };
    }

    [Fact]
    public void OnException_DomainException_DeveRetornarBadRequest()
    {
        using var activity = new Activity("test").Start();
        var filter = new GlobalExceptionFilter(NullLogger<GlobalExceptionFilter>.Instance);
        var context = CreateContext(new DomainException("Erro de domínio"));

        filter.OnException(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.True(context.ExceptionHandled);
    }

    [Fact]
    public void OnException_KeyNotFoundException_DeveRetornarNotFound()
    {
        var filter = new GlobalExceptionFilter(NullLogger<GlobalExceptionFilter>.Instance);
        var context = CreateContext(new KeyNotFoundException("Registro não encontrado"));

        filter.OnException(context);

        var result = Assert.IsType<NotFoundObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.True(context.ExceptionHandled);
    }

    [Fact]
    public void OnException_UnhandledException_DeveRetornarInternalServerError()
    {
        var filter = new GlobalExceptionFilter(NullLogger<GlobalExceptionFilter>.Instance);
        var context = CreateContext(new InvalidOperationException("Falha inesperada"));

        filter.OnException(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.True(context.ExceptionHandled);
    }
}

