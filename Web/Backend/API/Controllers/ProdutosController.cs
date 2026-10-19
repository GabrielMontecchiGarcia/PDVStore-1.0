using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.API.Common;
using PDVStore.Application.Interfaces;
using PDVStore.Application.Mappings;
using PDVStore.DTOs;

namespace PDVStore.API.Controllers;

/// <summary>
/// Produtos e estoque.
/// Leitura liberada para qualquer usuário autenticado (o Operador precisa
/// consultar produtos no PDV). Cadastro: Administrador/Estoquista.
/// Ajustes de estoque e histórico: somente Administrador (menu original).
/// </summary>
[ApiController]
[Route("api/produtos")]
[Authorize]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _produtos;

    public ProdutosController(IProdutoService produtos) => _produtos = produtos;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProdutoDto>>> Listar(
        [FromQuery] bool apenasAtivos = true,
        [FromQuery] string? filtro = null,
        CancellationToken ct = default)
    {
        var produtos = await _produtos.ListarAsync(apenasAtivos, filtro, ct);
        return Ok(produtos.Select(p => p.ToDto()));
    }

    /// <summary>Busca por código de barras, usada pelo leitor do PDV.</summary>
    [HttpGet("codigo-barras/{codigo}")]
    public async Task<ActionResult<ProdutoDto>> ObterPorCodigo(string codigo, CancellationToken ct)
    {
        var produto = await _produtos.ObterPorCodigoBarrasAsync(codigo, ct);
        if (produto == null) return NotFound();

        return Ok(produto.ToDto());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProdutoDto>> Obter(int id, CancellationToken ct)
    {
        var produto = await _produtos.ObterPorIdAsync(id, ct);
        if (produto == null) return NotFound();

        return Ok(produto.ToDto());
    }

    /// <summary>Cadastra produto (todas as validações da tela são aplicadas).</summary>
    [HttpPost]
    [Authorize(Policy = "Produtos")]
    public async Task<ActionResult<ProdutoDto>> Criar([FromBody] ProdutoRequest request, CancellationToken ct)
    {
        var produto = await _produtos.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = produto.Id }, produto.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Produtos")]
    public async Task<ActionResult<ProdutoDto>> Atualizar(int id, [FromBody] ProdutoRequest request, CancellationToken ct)
    {
        var produto = await _produtos.AtualizarAsync(id, request, ct);
        return Ok(produto.ToDto());
    }

    /// <summary>Desativa o produto (soft delete), preservando o histórico.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Produtos")]
    public async Task<IActionResult> Desativar(int id, CancellationToken ct)
    {
        await _produtos.DesativarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Histórico de movimentações (todas ou por produto/período).</summary>
    [HttpGet("movimentacoes")]
    [Authorize(Policy = "Administrador")]
    public async Task<ActionResult<IEnumerable<MovimentacaoEstoqueDto>>> Historico(
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        CancellationToken ct)
    {
        var movimentacoes = await _produtos.ObterHistoricoAsync(inicio, fim, ct);
        return Ok(movimentacoes.Select(m => m.ToDto()));
    }

    /// <summary>Entrada manual de estoque com registro de movimentação.</summary>
    [HttpPost("{id:int}/estoque/entrada")]
    [Authorize(Policy = "Administrador")]
    public async Task<IActionResult> AdicionarEstoque(int id, [FromBody] AjusteEstoqueRequest request, CancellationToken ct)
    {
        var ok = await _produtos.AdicionarEstoqueAsync(id, request.Quantidade, request.Motivo, User.ObterUsuarioId(), ct);
        if (!ok) return NotFound();

        return NoContent();
    }

    /// <summary>Saída manual de estoque; bloqueada quando o saldo é insuficiente.</summary>
    [HttpPost("{id:int}/estoque/saida")]
    [Authorize(Policy = "Administrador")]
    public async Task<IActionResult> BaixarEstoque(int id, [FromBody] AjusteEstoqueRequest request, CancellationToken ct)
    {
        var ok = await _produtos.BaixarEstoqueAsync(id, request.Quantidade, request.Motivo, User.ObterUsuarioId(), ct);
        if (!ok) return BadRequest(new ErroResposta
        {
            Status = StatusCodes.Status400BadRequest,
            Mensagem = "Não foi possível baixar o estoque. Verifique o produto e o saldo disponível.",
            TraceId = HttpContext.TraceIdentifier
        });

        return NoContent();
    }
}
