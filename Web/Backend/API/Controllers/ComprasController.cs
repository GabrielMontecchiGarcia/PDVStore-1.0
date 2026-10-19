using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.API.Common;
using PDVStore.Application.Interfaces;
using PDVStore.Application.Mappings;
using PDVStore.DTOs;

namespace PDVStore.API.Controllers;

/// <summary>
/// Compras de mercadoria. Acesso exclusivo do Administrador, pois a operação
/// altera estoque e custo dos produtos.
/// </summary>
[ApiController]
[Route("api/compras")]
[Authorize(Policy = "Administrador")]
public class ComprasController : ControllerBase
{
    private readonly ICompraService _compras;
    private readonly PeriodoService _periodo;

    public ComprasController(ICompraService compras, PeriodoService periodo)
    {
        _compras = compras;
        _periodo = periodo;
    }

    /// <summary>Registra a compra e dá entrada automática no estoque.</summary>
    [HttpPost]
    public async Task<ActionResult<CompraDto>> Registrar([FromBody] CompraRequest request, CancellationToken ct)
    {
        var compra = await _compras.RegistrarCompraAsync(request, User.ObterUsuarioId(), ct);
        return CreatedAtAction(nameof(Obter), new { id = compra.Id }, compra.ToDto());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompraDto>> Obter(int id, CancellationToken ct)
    {
        var compra = await _compras.ObterPorIdAsync(id, ct);
        if (compra == null) return NotFound();

        return Ok(compra.ToDto());
    }

    /// <summary>
    /// Lista compras por período. Sem filtros, retorna todas (comportamento do
    /// serviço original); o frontend envia o período desejado.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompraDto>>> Listar(
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        CancellationToken ct)
    {
        DateTime? inicioUtc = null;
        DateTime? fimUtc = null;

        if (inicio.HasValue || fim.HasValue)
        {
            var periodo = _periodo.Converter(inicio, fim);
            inicioUtc = periodo.InicioUtc;
            fimUtc = periodo.FimUtc;
        }

        var compras = await _compras.ListarAsync(inicioUtc, fimUtc, ct);
        return Ok(compras.Select(c => c.ToDto()));
    }

    /// <summary>Cancela a compra reduzindo o estoque dos itens.</summary>
    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id, CancellationToken ct)
    {
        var ok = await _compras.CancelarCompraAsync(id, ct);
        if (!ok) return BadRequest(new ErroResposta
        {
            Status = StatusCodes.Status400BadRequest,
            Mensagem = "Não foi possível cancelar a compra. Verifique se ela existe e não está cancelada.",
            TraceId = HttpContext.TraceIdentifier
        });

        return NoContent();
    }
}
