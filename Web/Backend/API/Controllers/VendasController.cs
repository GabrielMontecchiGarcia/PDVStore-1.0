using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.API.Common;
using PDVStore.Application.Interfaces;
using PDVStore.Application.Mappings;
using PDVStore.DTOs;

namespace PDVStore.API.Controllers;

/// <summary>
/// Vendas. O registro é permitido a Administrador e Operador (PDV).
/// A listagem e o cancelamento são exclusivos do Administrador.
/// </summary>
[ApiController]
[Route("api/vendas")]
[Authorize]
public class VendasController : ControllerBase
{
    private readonly IVendaService _vendas;
    private readonly PeriodoService _periodo;

    public VendasController(IVendaService vendas, PeriodoService periodo)
    {
        _vendas = vendas;
        _periodo = periodo;
    }

    /// <summary>
    /// Registra uma venda. O operador vem do token; as regras de caixa aberto,
    /// estoque, fiado, troco e pagamento são aplicadas pelo serviço.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Pdv")]
    public async Task<ActionResult<VendaDto>> Registrar([FromBody] VendaRequest request, CancellationToken ct)
    {
        var venda = await _vendas.RegistrarVendaAsync(request, User.ObterUsuarioId(), ct);
        return CreatedAtAction(nameof(Obter), new { id = venda.Id }, venda.ToDto());
    }

    /// <summary>Detalha a venda; Operador só pode consultar as próprias vendas.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<VendaDto>> Obter(int id, CancellationToken ct)
    {
        var venda = await _vendas.ObterPorIdAsync(id, ct);
        if (venda == null) return NotFound();

        if (!User.EhAdministrador() && venda.UsuarioCaixaId != User.ObterUsuarioId())
            return Forbid();

        return Ok(venda.ToDto());
    }

    /// <summary>Lista vendas concluídas do período (padrão: mês atual).</summary>
    [HttpGet]
    [Authorize(Policy = "Administrador")]
    public async Task<ActionResult<IEnumerable<VendaDto>>> Listar(
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        [FromQuery] int? caixaId,
        CancellationToken ct)
    {
        var (inicioUtc, fimUtc) = _periodo.Converter(inicio, fim);
        var vendas = await _vendas.ListarPorPeriodoAsync(inicioUtc, fimUtc, caixaId, ct);

        return Ok(vendas.Select(v => v.ToDto()));
    }

    /// <summary>
    /// Cancela a venda devolvendo estoque e saldo devedor (se fiado).
    /// O motivo é apenas registrado em log, pois não existe coluna no banco.
    /// </summary>
    [HttpPost("{id:int}/cancelar")]
    [Authorize(Policy = "Administrador")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarVendaRequest request, CancellationToken ct)
    {
        var ok = await _vendas.CancelarVendaAsync(id, request.Motivo, User.ObterUsuarioId(), ct);
        if (!ok) return BadRequest(new ErroResposta
        {
            Status = StatusCodes.Status400BadRequest,
            Mensagem = "Não foi possível cancelar a venda. Verifique se ela existe e não está cancelada.",
            TraceId = HttpContext.TraceIdentifier
        });

        return NoContent();
    }
}
