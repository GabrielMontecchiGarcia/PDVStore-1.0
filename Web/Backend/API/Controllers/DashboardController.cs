using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.API.Common;
using PDVStore.Application.Interfaces;
using PDVStore.DTOs;

namespace PDVStore.API.Controllers;

/// <summary>
/// Dashboard gerencial. Acesso exclusivo do Administrador, como no menu original.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "Administrador")]
public class DashboardController : ControllerBase
{
    private readonly IRelatorioService _relatorios;
    private readonly PeriodoService _periodo;

    public DashboardController(IRelatorioService relatorios, PeriodoService periodo)
    {
        _relatorios = relatorios;
        _periodo = periodo;
    }

    /// <summary>
    /// Retorna todos os indicadores do dashboard. Sem datas, usa o mês atual
    /// (1º dia até hoje), igual ao padrão do frmDashboard.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Obter(
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        CancellationToken ct)
    {
        var (inicioUtc, fimUtc) = _periodo.Converter(inicio, fim);
        var dashboard = await _relatorios.ObterDashboardAsync(inicioUtc, fimUtc, ct);
        return Ok(dashboard);
    }
}
