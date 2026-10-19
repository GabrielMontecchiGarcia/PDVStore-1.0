using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PDVStore.Domain.Entities;

namespace PDVStore.API.Security;

/// <summary>
/// Gera o token JWT após a autenticação. As claims carregam o Id, o nome e o
/// perfil (role) do usuário; o perfil é o mesmo texto do enum TipoPermissao
/// ("Administrador", "Operador", "Estoquista") e é usado nas policies.
/// </summary>
public class JwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public (string Token, DateTime ExpiraEm) GerarToken(UsuarioCaixa usuario)
    {
        var expiraEm = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Role, usuario.Permissao.ToString()),
            // Claim auxiliar com o código numérico do perfil (1/2/3).
            new("permissao", ((int)usuario.Permissao).ToString())
        };

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiraEm,
            signingCredentials: credenciais);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}
