using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Application.Interfaces;
using PDVStore.Application.Mappings;
using PDVStore.DTOs;

namespace PDVStore.API.Controllers;

/// <summary>
/// Clientes e fiado. A leitura é liberada a qualquer autenticado porque o PDV
/// precisa listar clientes para venda fiada; o cadastro/baixa de fiado é
/// exclusivo do Administrador (menu original).
/// </summary>
[ApiController]
[Route("api/clientes")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clientes;

    public ClientesController(IClienteService clientes) => _clientes = clientes;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClienteDto>>> Listar(
        [FromQuery] bool apenasAtivos = true,
        [FromQuery] string? filtro = null,
        CancellationToken ct = default)
    {
        var clientes = await _clientes.ListarAsync(apenasAtivos, filtro, ct);
        return Ok(clientes.Select(c => c.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDto>> Obter(int id, CancellationToken ct)
    {
        var cliente = await _clientes.ObterPorIdAsync(id, ct);
        if (cliente == null) return NotFound();

        return Ok(cliente.ToDto());
    }

    [HttpPost]
    [Authorize(Policy = "Administrador")]
    public async Task<ActionResult<ClienteDto>> Criar([FromBody] ClienteRequest request, CancellationToken ct)
    {
        var cliente = await _clientes.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = cliente.Id }, cliente.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Administrador")]
    public async Task<ActionResult<ClienteDto>> Atualizar(int id, [FromBody] ClienteRequest request, CancellationToken ct)
    {
        var cliente = await _clientes.AtualizarAsync(id, request, ct);
        return Ok(cliente.ToDto());
    }

    /// <summary>Desativa o cliente (soft delete). Cliente inativo não compra fiado.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Administrador")]
    public async Task<IActionResult> Desativar(int id, CancellationToken ct)
    {
        var ok = await _clientes.AtualizarStatusAsync(id, false, ct);
        if (!ok) return NotFound();

        return NoContent();
    }

    /// <summary>Reativa um cliente desativado (recurso do serviço original).</summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Policy = "Administrador")]
    public async Task<IActionResult> AlterarStatus(int id, [FromBody] StatusRequest request, CancellationToken ct)
    {
        var ok = await _clientes.AtualizarStatusAsync(id, request.Ativo, ct);
        if (!ok) return NotFound();

        return NoContent();
    }

    /// <summary>Recebe (baixa) parte do saldo devedor do cliente.</summary>
    [HttpPost("{id:int}/receber-fiado")]
    [Authorize(Policy = "Administrador")]
    public async Task<ActionResult<ClienteDto>> ReceberFiado(int id, [FromBody] ReceberFiadoRequest request, CancellationToken ct)
    {
        var ok = await _clientes.ReceberFiadoAsync(id, request.Valor, ct);
        if (!ok) return NotFound();

        var cliente = await _clientes.ObterPorIdAsync(id, ct);
        return Ok(cliente!.ToDto());
    }
}

/// <summary>
/// Fornecedores. Acesso exclusivo do Administrador, pois alimentam as compras.
/// </summary>
[ApiController]
[Route("api/fornecedores")]
[Authorize(Policy = "Administrador")]
public class FornecedoresController : ControllerBase
{
    private readonly IFornecedorService _fornecedores;

    public FornecedoresController(IFornecedorService fornecedores) => _fornecedores = fornecedores;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FornecedorDto>>> Listar(
        [FromQuery] bool apenasAtivos = true,
        [FromQuery] string? filtro = null,
        CancellationToken ct = default)
    {
        var fornecedores = await _fornecedores.ListarAsync(apenasAtivos, filtro, ct);
        return Ok(fornecedores.Select(f => f.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FornecedorDto>> Obter(int id, CancellationToken ct)
    {
        var fornecedor = await _fornecedores.ObterPorIdAsync(id, ct);
        if (fornecedor == null) return NotFound();

        return Ok(fornecedor.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<FornecedorDto>> Criar([FromBody] FornecedorRequest request, CancellationToken ct)
    {
        var fornecedor = await _fornecedores.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = fornecedor.Id }, fornecedor.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<FornecedorDto>> Atualizar(int id, [FromBody] FornecedorRequest request, CancellationToken ct)
    {
        var fornecedor = await _fornecedores.AtualizarAsync(id, request, ct);
        return Ok(fornecedor.ToDto());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desativar(int id, CancellationToken ct)
    {
        var ok = await _fornecedores.AtualizarStatusAsync(id, false, ct);
        if (!ok) return NotFound();

        return NoContent();
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> AlterarStatus(int id, [FromBody] StatusRequest request, CancellationToken ct)
    {
        var ok = await _fornecedores.AtualizarStatusAsync(id, request.Ativo, ct);
        if (!ok) return NotFound();

        return NoContent();
    }
}
