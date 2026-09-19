using Microsoft.EntityFrameworkCore;
using PDVStore.Data;
using PDVStore.Models;

namespace PDVStore.Services
{
    public class ClienteService
    {
        private readonly PDVContext _context;

        public ClienteService(PDVContext context)
        {
            _context = context;
        }

        public async Task<List<Cliente>> ListarAsync(bool apenasAtivos = true, string filtro = "")
        {
            var query = _context.Clientes.AsNoTracking();
            if (apenasAtivos)
                query = query.Where(c => c.Ativo);

            if (!string.IsNullOrWhiteSpace(filtro))
                query = query.Where(c => c.Nome.Contains(filtro) ||
                                         (c.CpfCnpj != null && c.CpfCnpj.Contains(filtro)));

            return await query.OrderBy(c => c.Nome).ToListAsync();
        }

        public async Task<Cliente?> ObterPorIdAsync(int id)
        {
            return await _context.Clientes.FindAsync(id);
        }

        public async Task<Cliente> SalvarAsync(Cliente cliente)
        {
            if (string.IsNullOrWhiteSpace(cliente.Nome))
                throw new ArgumentException("Nome do cliente é obrigatório.");

            if (cliente.Id == 0)
            {
                cliente.CadastradoEm = DateTime.UtcNow;
                _context.Clientes.Add(cliente);
            }
            else
            {
                _context.Clientes.Update(cliente);
            }

            await _context.SaveChangesAsync();
            return cliente;
        }

        public async Task<bool> AtualizarStatusAsync(int id, bool ativo)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return false;
            cliente.Ativo = ativo;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>Registra o pagamento (baixa) de uma parte do saldo devedor do cliente.</summary>
        public async Task<bool> ReceberFiadoAsync(int clienteId, decimal valor)
        {
            if (valor <= 0)
                throw new ArgumentException("Valor deve ser maior que zero.", nameof(valor));

            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null) return false;

            cliente.SaldoDevedor = Math.Max(0, cliente.SaldoDevedor - valor);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}