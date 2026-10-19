namespace PDVStore.DTOs;

/// <summary>
/// Payload de login. O login é feito pelo Nome (mesma regra do sistema Windows),
/// e a senha é conferida contra o hash BCrypt armazenado.
/// </summary>
public class LoginRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

/// <summary>Resposta do login: token JWT, expiração e dados do usuário.</summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public UsuarioDto Usuario { get; set; } = new();
}

/// <summary>
/// Dados públicos do usuário. Nunca inclui SenhaHash.
/// PermissaoCodigo preserva o valor numérico do enum (1/2/3) e Permissao é o
/// nome usado nas roles do JWT.
/// </summary>
public class UsuarioDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Permissao { get; set; } = string.Empty;
    public int PermissaoCodigo { get; set; }
    public string PermissaoDescricao { get; set; } = string.Empty;
    public string? FotoPath { get; set; }
    public bool Ativo { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Cadastro de usuário. A senha deve ter ao menos 6 caracteres (tela original).</summary>
public class CriarUsuarioRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string ConfirmacaoSenha { get; set; } = string.Empty;
    public int Permissao { get; set; }
    public string? FotoPath { get; set; }
}

/// <summary>
/// Edição de usuário. O nome não é alterável (campo desabilitado no sistema
/// original), por isso não faz parte do payload. A senha é sempre redefinida,
/// replicando o comportamento da tela frmCadastroUsuario.
/// </summary>
public class AtualizarUsuarioRequest
{
    public string Senha { get; set; } = string.Empty;
    public string ConfirmacaoSenha { get; set; } = string.Empty;
    public int Permissao { get; set; }
    public string? FotoPath { get; set; }
}

/// <summary>Troca da própria senha (exige a senha atual correta).</summary>
public class AlterarSenhaRequest
{
    public string SenhaAtual { get; set; } = string.Empty;
    public string NovaSenha { get; set; } = string.Empty;
}

/// <summary>Ativação/desativação lógica de um cadastro.</summary>
public class StatusRequest
{
    public bool Ativo { get; set; }
}
