using System.Net;
using System.Text.Json;
using ScrumBoard.Application.Exceptions;
using ScrumBoard.Domain.Exceptions;

namespace ScrumBoard.Api.Middleware;

/// <summary>
/// Traduce las excepciones de Application/Domain a códigos HTTP en un solo lugar,
/// para que los controllers no repitan try/catch en cada acción. No expone stack
/// traces ni detalles internos: solo el mensaje de negocio y el status code.
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
            var (status, mensaje) = MapearExcepcion(ex);

            if (status == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Error no controlado procesando {Path}", context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;

            await context.Response.WriteAsync(JsonSerializer.Serialize(new { mensaje }));
        }
    }

    private static (HttpStatusCode Status, string Mensaje) MapearExcepcion(Exception ex) => ex switch
    {
        RecursoNoEncontradoException => (HttpStatusCode.NotFound, ex.Message),
        CredencialesInvalidasException => (HttpStatusCode.Unauthorized, ex.Message),
        DomainException => (HttpStatusCode.Conflict, ex.Message),
        ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
        _ => (HttpStatusCode.InternalServerError, "Ocurrió un error inesperado. Intentá de nuevo más tarde.")
    };
}
