using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PDVStore.Domain.Exceptions;

namespace PDVStore.API.Common;

/// <summary>
/// Extensões para obter dados do usuário autenticado a partir das claims do JWT.
/// Nenhum endpoint aceita o Id do operador pelo corpo: ele sempre vem do token,
/// evitando personificação (mesmo papel do Session.CurrentUser no WinForms).
/// </summary>
public static class ClaimsExtensions
{
    public static int ObterUsuarioId(this ClaimsPrincipal user)
    {
        var valor = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(valor, out var id))
            throw new AcessoNegadoException("Usuário não autenticado.");

        return id;
    }

    public static string ObterNome(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public static bool EhAdministrador(this ClaimsPrincipal user) =>
        user.IsInRole("Administrador");

    public static bool EhOperador(this ClaimsPrincipal user) =>
        user.IsInRole("Operador");
}
