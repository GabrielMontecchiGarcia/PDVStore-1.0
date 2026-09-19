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
        // Getter de compatibilidade: devolve o caminho da foto do usuário gravado em FotoPath.
        // Foi mantido porque o código antigo usava GetFotoPath()/SetFotoPath; o EF Core
        // mapeia somente a propriedade pública FotoPath.
        public string? GetFotoPath()
        {
            return FotoPath;
        }

        // Setter de compatibilidade: atualiza o caminho da foto do usuário (FotoPath).
        // Mantido para não quebrar as chamadas existentes nos formulários de usuário.
        public void SetFotoPath(string? value)
        {
            FotoPath = value;
        }

        // Getter de compatibilidade: devolve se o usuário está ativo (true/false), usado
        // para decidir se ele pode acessar o sistema. Espelha o campo Ativo mapeado
        // no banco de dados.
        public bool GetAtivo()
        {
            return Ativo;
        }

        // Setter de compatibilidade: altera o estado ativo/inativo do usuário (Ativo).
        // É usado pelo módulo de gestão de usuários para liberar ou bloquear o acesso
        // de um funcionário ao caixa.
        public void SetAtivo(bool value)
        {
            Ativo = value;
        }

        // === NOVO: Sistema de Permissões ===
        public TipoPermissao Permissao { get; set; } = TipoPermissao.Operador;

        // Autentica o usuário comparando a senha informada com o hash BCrypt armazenado em
        // SenhaHash. Retorna true apenas se o hash conferir; a senha nunca é comparada
        // em texto puro. É chamada pelo frmLogin/UsuarioService no momento do login.
        public bool Autenticar(string senha)
        {
            return BCrypt.Net.BCrypt.Verify(senha, SenhaHash);
        }

        // Gera e grava o hash BCrypt da nova senha do usuário. Nunca armazena a senha
        // original em texto puro; é esse hash que Autenticar() validará depois.
        // Usado no cadastro/edição de usuários (frmGerenciarUsuarios).
        public void SetSenha(string senha)
        {
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha);
        }
        // Métodos de verificação de permissão
        // Verifica se o usuário logado possui o perfil Administrador. Regra de negócio usada
        // pela interface e pelos serviços para liberar telas/ações restritas, como
        // relatórios e a gestão de usuários.
        public bool EhAdmin() => Permissao == TipoPermissao.Administrador;
        // Regra de negócio de permissão: somente Administrador pode gerenciar usuários.
        // A tela frmGerenciarUsuarios consulta este método antes de listar/editar
        // contas de funcionários do caixa.
        public bool PodeGerenciarUsuarios() => Permissao == TipoPermissao.Administrador;

        // Converte um perfil de permissão no texto amigável em PT-BR para exibição em telas,
        // combos e listagens de usuários. É estática porque não depende de um usuário
        // específico e pode ser usada em qualquer contexto da aplicação.
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

