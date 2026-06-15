using System.Text.Json;
using ContentService.Application.Common.Exceptions;
using ContentService.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.Api.Middleware;

/// <summary>
/// Tüm istisnaları yakalayıp tutarlı bir ProblemDetails JSON gövdesine çevirir.
/// Domain/uygulama istisnalarını uygun HTTP durum koduna eşler.
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
            DomainException domain => (
                StatusCodes.Status409Conflict,
                "İşlem çakışması",
                domain.Message),
            UpstreamServiceException upstream => (
                StatusCodes.Status502BadGateway,
                "Bağımlı servis hatası",
                upstream.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Sunucu hatası",
                "Beklenmeyen bir hata oluştu.")
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "İstek işlenirken hata oluştu ({Status})", status);
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
