using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Application.Interfaces;
using PDVStore.Application.Mappings;
using PDVStore.Domain.Entities;
using PDVStore.DTOs;

namespace PDVStore.API.Controllers;

/// <summary>
/// CRUD de usuários. Acesso exclusivo do Administrador, como no menu original
/// (a tela de usuários só aparece para esse perfil).
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Policy = "Administrador")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarios;

    public UsuariosController(IUsuarioService usuarios) => _usuarios = usuarios;

    /// <summary>Lista usuários, por padrão apenas ativos, com filtro por nome.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioDto>>> Listar(
        [FromQuery] bool apenasAtivos = true,
        [FromQuery] string? filtro = null,
        CancellationToken ct = default)
    {
        var usuarios = await _usuarios.ListarAsync(apenasAtivos, filtro, ct);
        return Ok(usuarios.Select(u => u.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> Obter(int id, CancellationToken ct)
    {
        var usuario = await _usuarios.ObterPorIdAsync(id, ct);
        if (usuario == null) return NotFound();

        return Ok(usuario.ToDto());
    }

    /// <summary>Cadastra usuário com nome único, senha mínima de 6 e perfil válido.</summary>
    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Criar([FromBody] CriarUsuarioRequest request, CancellationToken ct)
    {
        var usuario = await _usuarios.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = usuario.Id }, usuario.ToDto());
    }

    /// <summary>Edita perfil/senha/foto. O nome permanece imutável, como na tela.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> Atualizar(int id, [FromBody] AtualizarUsuarioRequest request, CancellationToken ct)
    {
        var usuario = await _usuarios.AtualizarAsync(id, request, ct);
        return Ok(usuario.ToDto());
    }

    /// <summary>Desativa o usuário (soft delete), preservando o histórico.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desativar(int id, CancellationToken ct)
    {
        await _usuarios.DesativarAsync(id, ct);
        return NoContent();
    }
}
