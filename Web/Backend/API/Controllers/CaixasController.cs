using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.API.Common;
using PDVStore.Application.Interfaces;
using PDVStore.Application.Mappings;
using PDVStore.DTOs;

namespace PDVStore.API.Controllers;

/// <summary>
/// Caixa. A consulta do caixa aberto é liberada ao Operador (o PDV precisa saber
/// se pode vender); abertura, sangria, fechamento e histórico são exclusivos do
/// Administrador, como no menu original.
/// </summary>
[ApiController]
[Route("api/caixas")]
[Authorize]
public class CaixasController : ControllerBase
{
    private readonly ICaixaService _caixas;

    public CaixasController(ICaixaService caixas) => _caixas = caixas;

    /// <summary>Retorna o caixa aberto ou 204 quando não há nenhum.</summary>
    [HttpGet("aberto")]
    public async Task<ActionResult<CaixaDto>> ObterAberto(CancellationToken ct)
    {
        var caixa = await _caixas.ObterCaixaAbertoAsync(ct);
        if (caixa == null) return NoContent();

        return Ok(caixa.ToDto());
    }

    [HttpGet]
    [Authorize(Policy = "Administrador")]
    public async Task<ActionResult<IEnumerable<CaixaDto>>> Listar(CancellationToken ct)
    {
        var caixas = await _caixas.ListarAsync(ct);
        return Ok(caixas.Select(c => c.ToDto()));
    }

    /// <summary>Abre o caixa com o fundo de troco inicial.</summary>
    [HttpPost("abrir")]
    [Authorize(Policy = "Administrador")]
    public async Task<ActionResult<CaixaDto>> Abrir([FromBody] AbrirCaixaRequest request, CancellationToken ct)
    {
        var caixa = await _caixas.AbrirCaixaAsync(request.ValorInicial, User.ObterUsuarioId(), ct);
        return Ok(caixa.ToDto());
    }

    /// <summary>Registra sangria no caixa aberto.</summary>
    [HttpPost("{id:int}/sangria")]
    [Authorize(Policy = "Administrador")]
    public async Task<IActionResult> Sangria(int id, [FromBody] SangriaRequest request, CancellationToken ct)
    {
        var ok = await _caixas.RegistrarSangriaAsync(id, request.Valor, ct);
        if (!ok) return BadRequest(new ErroResposta
        {
            Status = StatusCodes.Status400BadRequest,
            Mensagem = "Não foi possível registrar a sangria. Verifique se o caixa está aberto.",
            TraceId = HttpContext.TraceIdentifier
        });

        return NoContent();
    }

    /// <summary>Fecha o caixa calculando o valor final.</summary>
    [HttpPost("{id:int}/fechar")]
    [Authorize(Policy = "Administrador")]
    public async Task<IActionResult> Fechar(int id, CancellationToken ct)
    {
        var ok = await _caixas.FecharCaixaAsync(id, ct);
        if (!ok) return BadRequest(new ErroResposta
        {
            Status = StatusCodes.Status400BadRequest,
            Mensagem = "Não foi possível fechar o caixa. Verifique se ele está aberto.",
            TraceId = HttpContext.TraceIdentifier
        });

        return NoContent();
    }
}
