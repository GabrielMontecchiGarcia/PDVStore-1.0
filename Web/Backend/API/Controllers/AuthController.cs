using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PDVStore.API.Common;
using PDVStore.API.Security;
using PDVStore.Application.Interfaces;
using PDVStore.Application.Mappings;
using PDVStore.DTOs;

namespace PDVStore.API.Controllers;

/// <summary>
/// Endpoints de autenticação. Replica o fluxo do frmLogin:
/// - credenciais inválidas → "Credenciais inválidas!";
/// - usuário inativo → "Usuário inativo. Contate o administrador.";
/// - sucesso → token JWT com Id, nome e perfil.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioService _usuarios;
    private readonly JwtTokenService _tokens;

    public AuthController(IUsuarioService usuarios, JwtTokenService tokens)
    {
        _usuarios = usuarios;
        _tokens = tokens;
    }

    /// <summary>Autentica o usuário e devolve o token JWT + dados do perfil.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var usuario = await _usuarios.AutenticarAsync(request.Nome, request.Senha, ct);

        if (usuario == null)
        {
            return Unauthorized(new ErroResposta
            {
                Status = StatusCodes.Status401Unauthorized,
                Mensagem = "Credenciais inválidas!",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // A senha confere, mas o usuário está desativado (regra do frmLogin).
        if (!usuario.Ativo)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErroResposta
            {
                Status = StatusCodes.Status403Forbidden,
                Mensagem = "Usuário inativo. Contate o administrador.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var (token, expiraEm) = _tokens.GerarToken(usuario);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiraEm = expiraEm,
            Usuario = usuario.ToDto()
        });
    }

    /// <summary>Retorna o usuário autenticado (usado para restaurar a sessão no frontend).</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UsuarioDto>> Me(CancellationToken ct)
    {
        var usuario = await _usuarios.ObterPorIdAsync(User.ObterUsuarioId(), ct);
        if (usuario == null) return NotFound();

        return Ok(usuario.ToDto());
    }

    /// <summary>Troca a senha do próprio usuário, exigindo a senha atual correta.</summary>
    [HttpPost("alterar-senha")]
    [Authorize]
    public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaRequest request, CancellationToken ct)
    {
        var ok = await _usuarios.AlterarSenhaAsync(User.ObterUsuarioId(), request.SenhaAtual, request.NovaSenha, ct);

        if (!ok)
        {
            return BadRequest(new ErroResposta
            {
                Status = StatusCodes.Status400BadRequest,
                Mensagem = "Senha atual incorreta.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return NoContent();
    }
}
