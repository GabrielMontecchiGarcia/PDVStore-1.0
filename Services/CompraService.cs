using Microsoft.EntityFrameworkCore;
using PDVStore.Data;
using PDVStore.Models;

namespace PDVStore.Services
{
    public class CompraService
    {
        private readonly PDVContext _context;

        public CompraService(PDVContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Registra a compra, dá entrada no estoque dos itens e registra as
        /// movimentações de entrada correspondentes em uma única transação.
        /// </summary>
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
                compra.DataCompra = DateTime.UtcNow;
                compra.ValorTotal = compra.Itens.Sum(i => i.Subtotal);

                _context.Compras.Add(compra);
                await _context.SaveChangesAsync();

                foreach (var item in compra.Itens)
                {
                    var produto = await _context.Produtos.FindAsync(item.ProdutoId);
                    if (produto == null)
                        throw new InvalidOperationException($"Produto ID {item.ProdutoId} não encontrado.");

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

        public async Task<Compra?> ObterPorIdAsync(int id)
        {
            return await _context.Compras
                .Include(c => c.Fornecedor)
                .Include(c => c.UsuarioCaixa)
                .Include(c => c.Itens)
                .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Compra>> ListarAsync(DateTime? inicio = null, DateTime? fim = null)
        {
            var query = _context.Compras
                .Include(c => c.Fornecedor)
                .AsNoTracking();

            if (inicio.HasValue) query = query.Where(c => c.DataCompra >= inicio);
            if (fim.HasValue) query = query.Where(c => c.DataCompra <= fim);

            return await query.OrderByDescending(c => c.DataCompra).ToListAsync();
        }

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