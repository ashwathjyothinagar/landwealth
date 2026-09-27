using LandWealth.Application.Common.Exceptions;
using LandWealth.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LandWealth.Api.Middleware;

public sealed class ProblemDetailsExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            RequestValidationException validation => (StatusCodes.Status400BadRequest, "Validation failed", Format(validation)),
            DomainException domain => (StatusCodes.Status400BadRequest, "Domain rule violated", domain.Message),
            NotFoundException notFound => (StatusCodes.Status404NotFound, "Not found", notFound.Message),
            UnauthorizedAccessException unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized", unauthorized.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "An unexpected error occurred.")
        };

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail
            }
        });
    }

    private static string Format(RequestValidationException exception)
        => string.Join(" ", exception.Errors.SelectMany(error => error.Value));
}
