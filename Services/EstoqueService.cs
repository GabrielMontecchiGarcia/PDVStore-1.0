using Microsoft.EntityFrameworkCore;
using PDVStore.Data;
using PDVStore.Models;

namespace PDVStore.Services
{
    public class EstoqueService
    {
        private readonly PDVContext _context;

        // CONSTRUTOR: injeta o PVDContext no campo _context. É o serviço central de
        // estoque e movimentações, usado por outras camadas (PDV, relatórios,
        // ajustes manuais). Quem o chama: o container de DI da aplicação. Não
        // lança exceções; apenas guarda a dependência para os demais métodos.
        public EstoqueService(PDVContext context)
        {
            _context = context;
        }

        // Lista todos os produtos, normalmente somente os ativos (soft delete), em
        // ordem alfabética. Usa AsNoTracking por ser leitura. É a fonte padrão
        // para preencher combos/grids de produtos. Quem chama: telas de cadastro,
        // consulta de produtos e o PDV. Retorna lista vazia se não houver
        // produtos correspondentes.
        public async Task<List<Produto>> GetAllAsync(bool apenasAtivos = true)
        {
            var query = _context.Produtos.AsNoTracking();
            if (apenasAtivos)
                query = query.Where(p => p.Ativo);
            return await query.OrderBy(p => p.Nome).ToListAsync();
        }

        // Busca um produto pela chave primária (Id) com rastreamento do EF (sem
        // AsNoTracking), permitindo alterá-lo depois sem nova consulta. Retorna
        // null se não existir. É DEPENDÊNCIA de VendaService e CompraService para
        // validar/baixar/adicionar estoque nos itens. Quem chama: várias telas.
        public async Task<Produto?> GetByIdAsync(int id)
        {
            return await _context.Produtos.FindAsync(id);
        }

        // Pesquisa produtos ativos por nome ou código de barras (Contains) — usado
        // principalmente para agilizar a busca no PDV e em telas de estoque.
        // Regra: se o termo estiver vazio, delega para GetAllAsync (retorna a
        // lista completa). Ordena por nome. Usa AsNoTracking por ser leitura.
        // Quem chama: a tela de PDV ao digitar parcialmente o código do produto.
        public async Task<List<Produto>> BuscarAsync(string filtro)
        {
            var termo = filtro.Trim();
            if (string.IsNullOrEmpty(termo))
                return await GetAllAsync();

            return await _context.Produtos
                .AsNoTracking()
                .Where(p => p.Ativo &&
                            (p.Nome.Contains(termo) ||
                             (p.CodigoBarras != null && p.CodigoBarras.Contains(termo))))
                .OrderBy(p => p.Nome)
                .ToListAsync();
        }

        // Adiciona um NOVO produto ao banco (cadastro). Depende apenas do PVDContext;
        // não valida estoque/duplicidade (a validação fica na tela). Persiste com
        // SaveChangesAsync após AddAsync. Quem chama: a tela de cadastro de
        // produtos. Não lança exceções próprias — erros de banco (ex.: campo
        // obrigatório) sobem como exceção do EF para a camada de interface.
        public async Task AddAsync(Produto produto)
        {
            await _context.Produtos.AddAsync(produto);
            await _context.SaveChangesAsync();
        }

        // Atualiza um produto EXISTENTE (dados de cadastro). O EF usa Update, marcando
        // a entidade como modificada; o Id já preenchido determina o registro a
        // alterar. Persiste com SaveChangesAsync. Quem chama: a tela de edição de
        // produtos. Não mexe em estoque/movimentações — isso é feito por
        // BaixarEstoqueAsync/AdicionarEstoqueAsync ou pelas vendas/compras.
        public async Task UpdateAsync(Produto produto)
        {
            _context.Produtos.Update(produto);
            await _context.SaveChangesAsync();
        }

        // "Deleta" um produto na forma de SOFT DELETE: em vez de remover a linha do
        // banco (que quebraria vendas/compras históricas), apenas marca Ativo =
        // false. Retorna sem fazer nada se o produto não existir. Quem chama: a
        // tela de listagem de produtos (botão excluir). Produtos inativos somem
        // das listas/pesquisas, pois as consultas filtram p.Ativo por padrão.
        public async Task DeleteAsync(int id)
        {
            var p = await _context.Produtos.FindAsync(id);
            if (p != null)
            {
                p.Ativo = false; // soft delete
                await _context.SaveChangesAsync();
            }
        }

        // Baixa (reduz) o estoque de um produto e registra uma movimentação de
        // "Saída" — usado em AJUSTE MANUAL quando há divergência de estoque.
        // Regras: quantidade > 0 (senão ArgumentException); produto deve existir
        // (senão false); estoque suficiente para a baixa (senão false, sem
        // lançar erro). Usa Session.CurrentUser?.Id para identificar quem fez o
        // ajuste. Persiste produto + movimentação juntos. Quem chama: ajuste
        // manual de estoque (não é usado pelas vendas, que têm fluxo próprio).
        public async Task<bool> BaixarEstoqueAsync(int produtoId, int quantidade, string? motivo = null)
        {
            if (quantidade <= 0)
                throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));

            var produto = await _context.Produtos.FindAsync(produtoId);
            if (produto == null)
                return false;

            if (produto.Estoque < quantidade)
                return false; // insufficient stock

            produto.Estoque -= quantidade;
            _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                ProdutoId = produtoId,
                Tipo = "Saída",
                Quantidade = quantidade,
                DataMovimentacao = DateTime.UtcNow,
                UsuarioId = Session.CurrentUser?.Id,
                Motivo = motivo
            });
            await _context.SaveChangesAsync();
            return true;
        }

        // Adiciona (aumenta) o estoque de um produto e registra a movimentação de
        // "Entrada" — usado em AJUSTE MANUAL (ex.: correção de saldo ou devolução
        // contada). Regras: quantidade > 0 (senão ArgumentException) e o produto
        // deve existir (senão false). Grava o usuário logado via
        // Session.CurrentUser?.Id e o motivo do ajuste. Persiste produto +
        // movimentação. É o oposto de BaixarEstoqueAsync; vendas e compras têm
        // seus próprios fluxos de movimentação.
        public async Task<bool> AdicionarEstoqueAsync(int produtoId, int quantidade, string? motivo = null)
        {
            if (quantidade <= 0)
                throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));

            var produto = await _context.Produtos.FindAsync(produtoId);
            if (produto == null)
                return false;

            produto.Estoque += quantidade;
            _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                ProdutoId = produtoId,
                Tipo = "Entrada",
                Quantidade = quantidade,
                DataMovimentacao = DateTime.UtcNow,
                UsuarioId = Session.CurrentUser?.Id,
                Motivo = motivo
            });
            await _context.SaveChangesAsync();
            return true;
        }

        // Busca um produto pelo código de barras, considerando somente produtos
        // ATIVOS — exatamente o cenário do PDV ao passar o leitor de código ou
        // digitar o código manualmente. Retorna null se o código não existir ou
        // o produto estiver inativo. Usa AsNoTracking (leitura) e FirstOrDefault.
        public async Task<Produto?> GetByCodigoBarrasAsync(string codigoBarras)
        {
            return await _context.Produtos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CodigoBarras == codigoBarras && p.Ativo);
        }

        // Retorna o histórico de movimentações (Entrada/Saída) de um período, com o
        // produto e o usuário responsáveis e a mais recente primeiro. Usa
        // AsNoTracking por ser relatório. Filtros de data opcionais: se null,
        // retorna tudo.
        // Quem chama: a tela de histórico e o DashboardViewModel (que agrupa por
        // dia — entradas x saídas — para o gráfico de movimentações do estoque).
        public async Task<List<MovimentacaoEstoque>> ObterHistoricoMovimentacoesAsync(DateTime? inicio = null, DateTime? fim = null)
        {
            var query = _context.MovimentacoesEstoque
                .Include(m => m.Produto)
                .Include(m => m.Usuario)
                .AsNoTracking()
                .AsQueryable();

            if (inicio.HasValue) query = query.Where(m => m.DataMovimentacao >= inicio);
            if (fim.HasValue) query = query.Where(m => m.DataMovimentacao <= fim);

            return await query.OrderByDescending(m => m.DataMovimentacao).ToListAsync();
        }
    }
}