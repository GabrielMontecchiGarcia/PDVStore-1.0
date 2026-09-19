using Microsoft.EntityFrameworkCore;
using PDVStore.Data;
using PDVStore.Models;

namespace PDVStore.Services
{
    public class FornecedorService
    {
        private readonly PDVContext _context;

        public FornecedorService(PDVContext context)
        {
            _context = context;
        }

        public async Task<List<Fornecedor>> ListarAsync(bool apenasAtivos = true, string filtro = "")
        {
            var query = _context.Fornecedores.AsNoTracking();
            if (apenasAtivos)
                query = query.Where(f => f.Ativo);

            if (!string.IsNullOrWhiteSpace(filtro))
                query = query.Where(f => f.Nome.Contains(filtro) ||
                                         (f.Cnpj != null && f.Cnpj.Contains(filtro)));

            return await query.OrderBy(f => f.Nome).ToListAsync();
        }

        public async Task<Fornecedor?> ObterPorIdAsync(int id)
        {
            return await _context.Fornecedores.FindAsync(id);
        }

        public async Task<Fornecedor> SalvarAsync(Fornecedor fornecedor)
        {
            if (string.IsNullOrWhiteSpace(fornecedor.Nome))
                throw new ArgumentException("Nome do fornecedor é obrigatório.");

            if (fornecedor.Id == 0)
                _context.Fornecedores.Add(fornecedor);
            else
                _context.Fornecedores.Update(fornecedor);

            await _context.SaveChangesAsync();
            return fornecedor;
        }

        public async Task<bool> AtualizarStatusAsync(int id, bool ativo)
        {
            var fornecedor = await _context.Fornecedores.FindAsync(id);
            if (fornecedor == null) return false;
            fornecedor.Ativo = ativo;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}