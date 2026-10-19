using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDVStore.Domain.Exceptions;
using PDVStore.DTOs;

namespace PDVStore.Middleware;

/// <summary>
/// Tratamento global de exceções. Converte erros de domínio/validação em
/// respostas JSON padronizadas (ErroResposta), registra logs estruturados e
/// evita expor detalhes internos (stack trace) para o cliente.
///
/// Mapeamento:
/// - NaoEncontradoException → 404
/// - AcessoNegadoException / UnauthorizedAccessException → 403
/// - RegraDeNegocioException / DomainException → 400
/// - ValidationException (FluentValidation) → 400 com lista de detalhes
/// - ArgumentException / InvalidOperationException → 400
/// - DbUpdateException → 409
/// - demais → 500 (mensagem genérica)
/// </summary>
public class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Requisição cancelada pelo cliente: não há resposta a enviar.
            _logger.LogInformation("Requisição {Metodo} {Caminho} cancelada pelo cliente.",
                context.Request.Method, context.Request.Path);
        }
        catch (Exception ex)
        {
            await TratarAsync(context, ex);
        }
    }

    private async Task TratarAsync(HttpContext context, Exception ex)
    {
        var (status, mensagem, detalhes) = Mapear(ex);

        if (status >= 500)
            _logger.LogError(ex, "Erro não tratado em {Metodo} {Caminho}. TraceId={TraceId}",
                context.Request.Method, context.Request.Path, context.TraceIdentifier);
        else
            _logger.LogWarning("Erro de negócio/validação em {Metodo} {Caminho}: {Mensagem}. TraceId={TraceId}",
                context.Request.Method, context.Request.Path, mensagem, context.TraceIdentifier);

        if (context.Response.HasStarted)
        {
            _logger.LogError("Resposta já iniciada; não é possível enviar o erro padronizado.");
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";

        var resposta = new ErroResposta
        {
            Status = status,
            Mensagem = mensagem,
            Detalhes = detalhes,
            TraceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(resposta, JsonOptions));
    }

    /// <summary>Define status HTTP, mensagem amigável e detalhes por tipo de exceção.</summary>
    private static (int Status, string Mensagem, List<string>? Detalhes) Mapear(Exception ex) => ex switch
    {
        NaoEncontradoException => (StatusCodes.Status404NotFound, ex.Message, null),
        AcessoNegadoException => (StatusCodes.Status403Forbidden, ex.Message, null),
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Acesso negado.", null),
        ValidationException validation => (
            StatusCodes.Status400BadRequest,
            "Um ou mais campos estão inválidos.",
            validation.Errors.Select(e => e.ErrorMessage).Distinct().ToList()),
        RegraDeNegocioException => (StatusCodes.Status400BadRequest, ex.Message, null),
        DomainException => (StatusCodes.Status400BadRequest, ex.Message, null),
        KeyNotFoundException => (StatusCodes.Status404NotFound, ex.Message, null),
        ArgumentException => (StatusCodes.Status400BadRequest, ex.Message, null),
        InvalidOperationException => (StatusCodes.Status400BadRequest, ex.Message, null),
        DbUpdateException => (
            StatusCodes.Status409Conflict,
            "Não foi possível salvar os dados. Verifique se o registro já existe ou está vinculado a outro cadastro.",
            null),
        _ => (StatusCodes.Status500InternalServerError,
            "Ocorreu um erro inesperado. Consulte os logs do sistema.", null)
    };
}
