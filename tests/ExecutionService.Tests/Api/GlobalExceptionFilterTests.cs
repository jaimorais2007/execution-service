using ExecutionService.Api.Filters;
using ExecutionService.Application.Common;
using ExecutionService.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExecutionService.Tests.Api;

public class GlobalExceptionFilterTests
{
    [Fact]
    public void OnException_DomainException_DeveRetornarBadRequest()
    {
        var filter = new GlobalExceptionFilter(NullLogger<GlobalExceptionFilter>.Instance);
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = new DomainException("Regra violada")
        };

        filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        var badRequest = Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public void OnException_NotFoundException_DeveRetornarNotFound()
    {
        var filter = new GlobalExceptionFilter(NullLogger<GlobalExceptionFilter>.Instance);
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = new NotFoundException("Recurso não encontrado")
        };

        filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        var notFound = Assert.IsType<NotFoundObjectResult>(context.Result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public void OnException_UnhandledException_DeveRetornarInternalServerError()
    {
        var filter = new GlobalExceptionFilter(NullLogger<GlobalExceptionFilter>.Instance);
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = new InvalidOperationException("Erro inesperado")
        };

        filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        var serverError = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(500, serverError.StatusCode);
    }
}
