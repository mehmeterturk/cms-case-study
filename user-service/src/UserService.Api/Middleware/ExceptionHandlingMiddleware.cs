using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Common.Exceptions;

namespace UserService.Api.Middleware;

/// <summary>
/// Tüm istisnaları yakalar ve tutarlı bir ProblemDetails JSON gövdesine çevirir.
/// Böylece controller'lar try/catch ile dağılmaz.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Doğrulama hatası",
                validation.Errors.Any()
                    ? string.Join(" ", validation.Errors.Select(e => e.ErrorMessage))
                    : validation.Message),
            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Kayıt bulunamadı",
                notFound.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Sunucu hatası",
                "Beklenmeyen bir hata oluştu.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "İşlenmemiş istisna");
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
