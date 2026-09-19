using Microsoft.EntityFrameworkCore;
using PDVStore.Data;
using PDVStore.Models;

namespace PDVStore.Services
{
    public class EstoqueService
    {
        private readonly PDVContext _context;

        public EstoqueService(PDVContext context)
        {
            _context = context;
        }

        public async Task<List<Produto>> GetAllAsync(bool apenasAtivos = true)
        {
            var query = _context.Produtos.AsNoTracking();
            if (apenasAtivos)
                query = query.Where(p => p.Ativo);
            return await query.OrderBy(p => p.Nome).ToListAsync();
        }

        public async Task<Produto?> GetByIdAsync(int id)
        {
            return await _context.Produtos.FindAsync(id);
        }

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

        public async Task AddAsync(Produto produto)
        {
            await _context.Produtos.AddAsync(produto);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Produto produto)
        {
            _context.Produtos.Update(produto);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var p = await _context.Produtos.FindAsync(id);
            if (p != null)
            {
                p.Ativo = false; // soft delete
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>Baixa estoque e registra a movimentação de saída (uso em ajuste manual).</summary>
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

        /// <summary>Adiciona estoque e registra a movimentação de entrada (uso em ajuste manual).</summary>
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

        public async Task<Produto?> GetByCodigoBarrasAsync(string codigoBarras)
        {
            return await _context.Produtos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CodigoBarras == codigoBarras && p.Ativo);
        }

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