namespace PDVStore.Models
{
    public class UsuarioCaixa : IHasId
    {
        public int Id { get; set; }
        public string Nome { get; set; }
       // public string Email { get; set; }
        public string SenhaHash { get; set; }
        public DateTime CreatedAt { get; set; }
        // Mapped properties - expose as public properties so EF Core can translate queries
        public string? FotoPath { get; set; }
        public bool Ativo { get; set; } = true;
        // Backwards-compatible accessors for existing code that used Get/Set methods
        public string? GetFotoPath()
        {
            return FotoPath;
        }

        public void SetFotoPath(string? value)
        {
            FotoPath = value;
        }

        public bool GetAtivo()
        {
            return Ativo;
        }

        public void SetAtivo(bool value)
        {
            Ativo = value;
        }

        // === NOVO: Sistema de Permissões ===
        public TipoPermissao Permissao { get; set; } = TipoPermissao.Operador;

        public bool Autenticar(string senha)
        {
            return BCrypt.Net.BCrypt.Verify(senha, SenhaHash);
        }

        public void SetSenha(string senha)
        {
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha);
        }
        // Métodos de verificação de permissão
        public bool EhAdmin() => Permissao == TipoPermissao.Administrador;
        public bool PodeGerenciarUsuarios() => Permissao == TipoPermissao.Administrador;

        public static string DescreverPermissao(TipoPermissao permissao) => permissao switch
        {
            TipoPermissao.Operador => "Operador (Caixa)",
            TipoPermissao.Administrador => "Administrador",
            TipoPermissao.Estoquista => "Estoquista",
            _ => permissao.ToString()
        };
    }

    public enum TipoPermissao
    {
        Operador = 1,      // Acesso somente ao PDV (vendas)
        Administrador = 2  // Acesso total (cadastros, relatórios, usuários, etc.)
        ,
        Estoquista = 3     // Acesso somente ao cadastro de produtos
    }


}

