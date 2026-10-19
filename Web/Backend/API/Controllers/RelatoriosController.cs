using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.API.Common;
using PDVStore.Application.Interfaces;
using PDVStore.Application.Mappings;
using PDVStore.DTOs;

namespace PDVStore.API.Controllers;

/// <summary>
/// Relatórios e exportações. Acesso exclusivo do Administrador.
/// As exportações equivalem aos botões "Exportar PDF/Excel" das telas WinForms.
/// Classe parcial: a exportação fica em arquivo separado.
/// </summary>
[ApiController]
[Route("api/relatorios")]
[Authorize]
public partial class RelatoriosController : ControllerBase
{
    private readonly IRelatorioService _relatorios;
    private readonly IProdutoService _produtos;
    private readonly IClienteService _clientes;
    private readonly IFornecedorService _fornecedores;
    private readonly IUsuarioService _usuarios;
    private readonly IVendaService _vendas;
    private readonly ICompraService _compras;
    private readonly IExportacaoService _exportacao;
    private readonly PeriodoService _periodo;

    public RelatoriosController(
        IRelatorioService relatorios,
        IProdutoService produtos,
        IClienteService clientes,
        IFornecedorService fornecedores,
        IUsuarioService usuarios,
        IVendaService vendas,
        ICompraService compras,
        IExportacaoService exportacao,
        PeriodoService periodo)
    {
        _relatorios = relatorios;
        _produtos = produtos;
        _clientes = clientes;
        _fornecedores = fornecedores;
        _usuarios = usuarios;
        _vendas = vendas;
        _compras = compras;
        _exportacao = exportacao;
        _periodo = periodo;
    }

    /// <summary>Ranking dos produtos mais vendidos no período.</summary>
    [HttpGet("itens-mais-vendidos")]
    [Authorize(Policy = "Administrador")]
    public async Task<ActionResult<IEnumerable<ItemRelatorioDto>>> ItensMaisVendidos(
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        [FromQuery] int topN = 10,
        CancellationToken ct = default)
    {
        var (inicioUtc, fimUtc) = _periodo.Converter(inicio, fim);
        var itens = await _relatorios.ItensMaisVendidosAsync(inicioUtc, fimUtc, Math.Clamp(topN, 1, 100), ct);
        return Ok(itens);
    }

    /// <summary>Produtos ativos com estoque igual ou abaixo do mínimo.</summary>
    [HttpGet("estoque-minimo")]
    [Authorize(Policy = "Administrador")]
    public async Task<ActionResult<IEnumerable<ProdutoDto>>> EstoqueMinimo(CancellationToken ct)
    {
        var produtos = await _relatorios.EstoqueMinimoAsync(ct);
        return Ok(produtos.Select(p => p.ToDto()));
    }

    /// <summary>Total geral de vendas registradas (indicador do sistema).</summary>
    [HttpGet("total-vendas")]
    [Authorize(Policy = "Administrador")]
    public async Task<ActionResult<object>> TotalVendas(CancellationToken ct) =>
        Ok(new { total = await _relatorios.TotalVendasAsync(ct) });
}
