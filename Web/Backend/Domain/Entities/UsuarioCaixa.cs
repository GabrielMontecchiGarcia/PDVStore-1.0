namespace PDVStore.Domain.Entities;

/// <summary>
/// Usuário do caixa / funcionário do sistema.
/// Replica exatamente a entidade do projeto Windows, incluindo os métodos de
/// compatibilidade (Get/Set), a autenticação BCrypt e as regras de permissão.
///
/// Regras de negócio preservadas:
/// - Nome é o login e deve ser único (validado em tela/serviço);
/// - A senha NUNCA é armazenada em texto puro (SenhaHash gerado por BCrypt);
/// - Usuário inativo não acessa o sistema (bloqueio no login);
/// - Perfis: Operador (1) somente PDV, Administrador (2) acesso total,
///   Estoquista (3) somente cadastro de produtos.
/// </summary>
public class UsuarioCaixa : IHasId
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? FotoPath { get; set; }
    public bool Ativo { get; set; } = true;
    public TipoPermissao Permissao { get; set; } = TipoPermissao.Operador;

    // Getter de compatibilidade mantido do projeto original (código legado usava GetFotoPath).
    public string? GetFotoPath() => FotoPath;

    // Setter de compatibilidade mantido para não alterar a nomenclatura original.
    public void SetFotoPath(string? value) => FotoPath = value;

    // Espelha o campo Ativo para o código legado.
    public bool GetAtivo() => Ativo;

    // Altera o estado ativo/inativo (usado no soft delete de usuários).
    public void SetAtivo(bool value) => Ativo = value;

    // Autentica comparando a senha informada com o hash BCrypt armazenado.
    // Retorna true apenas quando o hash confere; nunca compara texto puro.
    public bool Autenticar(string senha)
    {
        if (string.IsNullOrEmpty(SenhaHash)) return false;
        return BCrypt.Net.BCrypt.Verify(senha, SenhaHash);
    }

    // Gera e grava o hash BCrypt da senha. Nunca guarda a senha original.
    public void SetSenha(string senha) => SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha);

    // Regra de negócio: somente Administrador possui acesso total.
    public bool EhAdmin() => Permissao == TipoPermissao.Administrador;

    // Regra de negócio: somente Administrador gerencia usuários.
    public bool PodeGerenciarUsuarios() => Permissao == TipoPermissao.Administrador;

    // Texto amigável em PT-BR usado em telas/combos e nos DTOs da API.
    public static string DescreverPermissao(TipoPermissao permissao) => permissao switch
    {
        TipoPermissao.Operador => "Operador (Caixa)",
        TipoPermissao.Administrador => "Administrador",
        TipoPermissao.Estoquista => "Estoquista",
        _ => permissao.ToString()
    };
}

/// <summary>
/// Perfis de acesso. Os valores numéricos são os mesmos do banco original
/// (1 = Operador, 2 = Administrador, 3 = Estoquista).
/// </summary>
public enum TipoPermissao
{
    Operador = 1,
    Administrador = 2,
    Estoquista = 3
}
