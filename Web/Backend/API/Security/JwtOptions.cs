namespace PDVStore.API.Security;

/// <summary>
/// Configurações do JWT lidas da seção "Jwt" (appsettings/variáveis de ambiente).
/// A chave deve ter no mínimo 32 caracteres e nunca ser versionada em produção:
/// use Jwt__Key como variável de ambiente/secret.
/// </summary>
public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PDVStore.WebAPI";
    public string Audience { get; set; } = "PDVStore.Web";
    public int ExpirationMinutes { get; set; } = 480;
}
