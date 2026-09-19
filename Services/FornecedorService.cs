using Microsoft.EntityFrameworkCore;
using PDVStore.Data;
using PDVStore.Models;

namespace PDVStore.Services
{
    public class FornecedorService
    {
        private readonly PDVContext _context;

        // CONSTRUTOR: injeta o PVDContext no campo _context. É o serviço de cadastro
        // e consulta de fornecedores. Quem o chama: o container de DI ao
        // instanciar telas de fornecedor/compra. Não lança exceções; apenas
        // armazena a dependência recebida para uso nos demais métodos.
        public FornecedorService(PDVContext context)
        {
            _context = context;
        }

        // Lista fornecedores, por padrão somente os ativos (soft delete), com filtro
        // opcional por nome ou CNPJ (Contains). Ordena por nome para facilitar a
        // busca. Usa AsNoTracking por ser leitura. Quem chama: telas de
        // fornecedor e a tela de compras (para selecionar o fornecedor).
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

        // Busca um fornecedor pela chave primária, com rastreamento para permitir
        // alteração posterior. Retorna null se o Id não existir. Depende do
        // PVDContext e é usado também por CompraService (informação do
        // fornecedor nos registros de compra). Quem chama: telas do formulário.
        public async Task<Fornecedor?> ObterPorIdAsync(int id)
        {
            return await _context.Fornecedores.FindAsync(id);
        }

        // Insere ou atualiza um fornecedor (padrão "upsert"). Regra: o Nome é
        // obrigatório — se vazio, lança ArgumentException. Se Id == 0 é cadastro
        // novo (Add); caso contrário, atualiza o existente (Update). Persiste com
        // SaveChangesAsync e retorna o fornecedor. Quem chama: a tela de cadastro
        // de fornecedores.
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

        // Altera o status Ativo do fornecedor (soft delete / reativação): desativa o
        // registro em vez de apagá-lo, evitando inconsistência em compras
        // históricas. Retorna false se o Id não existir; senão atualiza o campo
        // e persiste. Quem chama: a tela de listagem de fornecedores.
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