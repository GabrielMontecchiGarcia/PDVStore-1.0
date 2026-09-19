using Microsoft.EntityFrameworkCore;
using PDVStore.Data;
using PDVStore.Models;

namespace PDVStore.Services
{
    public class CompraService
    {
        private readonly PDVContext _context;

        // CONSTRUTOR: injeta o PVDContext no campo _context. É o serviço responsável
        // por registrar compras de mercadorias dos fornecedores. Quem o chama: o
        // container de DI ao instanciar telas/reportes. Não lança exceções;
        // apenas armazena a dependência para uso nos demais métodos.
        public CompraService(PDVContext context)
        {
            _context = context;
        }

        // Registra uma compra de fornecedor e dá entrada no estoque, tudo dentro de
        // UMA transação com rollback automático em caso de erro (CommitAsync /
        // RollbackAsync). Regras de negócio: precisa ter pelo menos um item
        // (InvalidOperationException); quantidade > 0 e preço de custo >= 0 em
        // cada item; todos os Produtos informados devem existir. Opera na
        // seguinte ordem: valida itens -> calcula ValorTotal -> grava a compra ->
        // AUMENTA o estoque de cada produto (e atualiza o PrecoCusto se positivo)
        // -> registra MovimentacaoEstoque tipo "Entrada" -> commita. Quem chama:
        // a tela de compras. Qualquer falha desfaz tudo (dados voltam ao estado
        // anterior), deixando o estoque consistente.
        public async Task<Compra> RegistrarCompraAsync(Compra compra)
        {
            if (compra.Itens == null || !compra.Itens.Any())
                throw new InvalidOperationException("A compra deve conter pelo menos um item.");

            foreach (var item in compra.Itens)
            {
                if (item.Quantidade <= 0)
                    throw new InvalidOperationException("Quantidade dos itens deve ser maior que zero.");
                if (item.PrecoCusto < 0)
                    throw new InvalidOperationException("Preço de custo não pode ser negativo.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in compra.Itens)
                {
                    var produto = await _context.Produtos.FindAsync(item.ProdutoId);
                    if (produto == null)
                        throw new InvalidOperationException($"Produto ID {item.ProdutoId} não encontrado.");
                }

                compra.DataCompra = DateTime.UtcNow;
                compra.ValorTotal = compra.Itens.Sum(i => i.Subtotal);

                _context.Compras.Add(compra);
                await _context.SaveChangesAsync();

                foreach (var item in compra.Itens)
                {
                    var produto = await _context.Produtos.FindAsync(item.ProdutoId);

                    produto.Estoque += item.Quantidade;
                    if (item.PrecoCusto > 0)
                        produto.PrecoCusto = item.PrecoCusto;

                    _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                    {
                        ProdutoId = produto.Id,
                        Tipo = "Entrada",
                        Quantidade = item.Quantidade,
                        PrecoUnitario = item.PrecoCusto,
                        DataMovimentacao = DateTime.UtcNow,
                        UsuarioId = compra.UsuarioCaixaId,
                        Motivo = $"Compra #{compra.Id} - {compra.NumeroNota ?? "sem nota"}"
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return compra;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Carrega uma compra com todos os dados de exibição: fornecedor, usuário do
        // caixa, e os itens com seus respectivos produtos (Includes). Usado para
        // detalhar/confirmar uma compra na tela após o registro. Retorna null se
        // o Id não existir. Depende do PVDContext para montar a consulta.
        public async Task<Compra?> ObterPorIdAsync(int id)
        {
            return await _context.Compras
                .Include(c => c.Fornecedor)
                .Include(c => c.UsuarioCaixa)
                .Include(c => c.Itens)
                .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // Lista compras em um período (datas opcionais), incluindo o fornecedor para
        // exibição e ordenando da mais recente para a mais antiga. Usa
        // AsNoTracking por ser consulta de leitura para relatórios/consulta.
        // Se inicio/fim forem null, o filtro de data é ignorado (retorna tudo).
        // Quem chama: a tela de histórico de compras e o relatório de compras.
        public async Task<List<Compra>> ListarAsync(DateTime? inicio = null, DateTime? fim = null)
        {
            var query = _context.Compras
                .Include(c => c.Fornecedor)
                .AsNoTracking();

            if (inicio.HasValue) query = query.Where(c => c.DataCompra >= inicio);
            if (fim.HasValue) query = query.Where(c => c.DataCompra <= fim);

            return await query.OrderByDescending(c => c.DataCompra).ToListAsync();
        }

        // Cancela uma compra, desfazendo o efeito dela no estoque dentro de uma
        // transação com rollback. Regras: retorna false se a compra não existir
        // ou já estiver cancelada (não lança exceção). Para cada item o estoque
        // do produto é reduzido (com Math.Max(0,...) para nunca ficar negativo) e
        // o status da compra vira "Cancelada". Em qualquer falha faz Rollback e
        // retorna false. Quem chama: a tela de histórico de compras (botão
        // cancelar). Complemento da regra de negócio da compra (estoque inverte).
        public async Task<bool> CancelarCompraAsync(int id)
        {
            var compra = await _context.Compras
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (compra == null || compra.Status == "Cancelada")
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in compra.Itens)
                {
                    var produto = await _context.Produtos.FindAsync(item.ProdutoId);
                    if (produto != null)
                        produto.Estoque = Math.Max(0, produto.Estoque - item.Quantidade);
                }

                compra.Status = "Cancelada";
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
    }
}