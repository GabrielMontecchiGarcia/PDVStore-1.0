using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PDVStore.Data;
using PDVStore.Models;

namespace PDVStore.Services
{
    public class UsuarioService
    {
        private readonly PDVContext _context;

        // CONSTRUTOR: injeta o PVDContext no campo _context. Este serviço cuida de
        // cadastro, autenticação, permissões e gerenciamento dos usuários do
        // caixa. Quem o chama: o container de DI e os formulários de
        // login/usuários. Não lança exceções; apenas guarda a dependência.
        public UsuarioService(PDVContext context)
        {
            _context = context;
        }

        // ====================== CRUD ======================

        // Cria um novo usuário. Regras: o Nome é obrigatório (senão
        // ArgumentException); a senha informada é convertida em hash seguro
        // (SetSenha — o hash nunca é armazenado em texto puro); define CreatedAt
        // (UTC) e ativa o usuário (SetAtivo(true)). Insere no banco e persiste.
        // Quem chama: a tela de cadastro de usuários. Complemento: a autenticação
        // (AutenticarAsync) compara a senha digitada com esse hash.
        public async Task<UsuarioCaixa> CriarAsync(UsuarioCaixa usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario.Nome))
                throw new ArgumentException("Nome é obrigatório.");

            usuario.SetSenha(usuario.SenhaHash); // Garante hash (ajuste se receber senha limpa)
            usuario.CreatedAt = DateTime.UtcNow;
            usuario.SetAtivo(true);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        // Busca um usuário pelo Id. Usa AsNoTracking (leitura) e retorna null se não
        // encontrar. É DEPENDÊNCIA de EhAdministradorAsync, que consulta a
        // permissão do usuário, e de telas que exibem dados do usuário logado.
        public async Task<UsuarioCaixa?> ObterPorIdAsync(int id)
        {
            return await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        // Busca um usuário pelo nome de login (único por convenção). É a base da
        // autenticação: AutenticarAsync usa este método para localizar o usuário
        // antes de verificar a senha. Retorna null se o nome não existir. Usa
        // AsNoTracking por ser leitura.
        public async Task<UsuarioCaixa?> ObterPorNomeAsync(string nome)
        {
            return await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Nome == nome);
        }

        // Lista todos os usuários, por padrão somente os ativos (soft delete sem
        // excluir histórico). Usa AsNoTracking (leitura). Quem chama: a tela de
        // gerenciamento de usuários para exibição em grid/seleção.
        public async Task<IEnumerable<UsuarioCaixa>> ListarTodosAsync(bool apenasAtivos = true)
        {
            var query = _context.Usuarios.AsNoTracking();

            if (apenasAtivos)
                query = query.Where(u => u.Ativo);

            return await query.ToListAsync();
        }

        // Atualiza os campos PERMITIDOS de um usuário existente. Regra: o usuário
        // precisa existir no banco, senão lança KeyNotFoundException. Para
        // segurança, NÃO altera a senha (isso é feito separadamente em
        // AlterarSenhaAsync) nem o CreatedAt; apenas Nome, Permissao, Ativo e
        // FotoPath. Persiste com SaveChangesAsync. Quem chama: a tela de edição.
        public async Task AtualizarAsync(UsuarioCaixa usuario)
        {
            var existing = await _context.Usuarios.FindAsync(usuario.Id);
            if (existing == null)
                throw new KeyNotFoundException("Usuário não encontrado.");

            // Atualiza campos permitidos
            existing.Nome = usuario.Nome;
            existing.Permissao = usuario.Permissao;
            existing.SetAtivo(usuario.GetAtivo());
            existing.SetFotoPath(usuario.GetFotoPath());

            await _context.SaveChangesAsync();
        }

        // "Exclui" um usuário com SOFT DELETE: apenas desativa (Ativo = false), sem
        // apagar o registro — preservando vendas/movimentações históricas que o
        // referenciam. Retorna false se o usuário não existir; senão persiste.
        // Quem chama: a tela de gerenciamento de usuários (botão excluir).
        public async Task<bool> DeletarAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return false;

            usuario.SetAtivo(false); // Soft delete
            await _context.SaveChangesAsync();
            return true;
        }

        // ====================== Autenticação ======================

        // Autentica um usuário pelo nome + senha. Fluxo: localiza via ObterPorNomeAsync
        // e valida a senha contra o hash armazenado com o método Autenticar do
        // modelo (BCrypt). Retorna null quando o usuário ou a senha não
        // conferem — NUNCA informa qual dos dois falhou (boa prática de
        // segurança). Quem chama: a tela de login do sistema.
        public async Task<UsuarioCaixa?> AutenticarAsync(string nome, string senha)
        {
            var usuario = await ObterPorNomeAsync(nome);
            if (usuario == null || !usuario.Autenticar(senha))
                return null;

            return usuario;
        }

        // Altera a senha de um usuário. Regras de segurança: exige que o usuário
        // exista e que a senhaAtual esteja correta (confirmada via Autenticar); em
        // qualquer falha retorna false sem alterar nada. Só então aplica nova
        // senha (SetSenha, que gera o hash), persiste e retorna true. Quem chama:
        // a tela de troca de senha (perfil do usuário).
        public async Task<bool> AlterarSenhaAsync(int id, string senhaAtual, string novaSenha)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null || !usuario.Autenticar(senhaAtual))
                return false;

            usuario.SetSenha(novaSenha);
            await _context.SaveChangesAsync();
            return true;
        }

        // ====================== Permissões ======================

        // Verifica se um usuário é Administrador. Depende de ObterPorIdAsync e do
        // método EhAdmin() do modelo, que compara a Permissao com
        // TipoPermissao.Administrador. Retorna false para usuários inexistentes
        // ou sem permissão de admin. Quem chama: telas/estimativas que precisam
        // liberar ou bloquear ações exclusivas de administrador.
        public async Task<bool> EhAdministradorAsync(int id)
        {
            var usuario = await ObterPorIdAsync(id);
            return usuario?.EhAdmin() == true;
        }

        // Lista apenas usuários com a permissão de Administrador. Depende do enum
        // TipoPermissao.Administrador e usa AsNoTracking (leitura). Quem chama:
        // telas de gerenciamento que precisam exibir/filtrar os administradores,
        // ex.: para grandes relatórios ou repasse de responsabilidades.
        public async Task<IEnumerable<UsuarioCaixa>> ListarAdministradoresAsync()
        {
            return await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Permissao == TipoPermissao.Administrador)
                .ToListAsync();
        }

        // Consulta genérica que busca qualquer entidade pela chave primária usando o
        // DbSet correto pelo tipo T (restrito a classes que implementam IHasId).
        // Retorna null se não encontrar. Quem chama: trechos que precisam de uma
        // busca por Id sem criar um método específico (padrão genérico de
        // utilidade). Não valida regras de negócio além da própria existência.
        public T? GetById<T>(int id) where T : class, IHasId
        {
            return _context.Set<T>().Find(id);
        }
    }
}