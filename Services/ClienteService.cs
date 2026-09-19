using Microsoft.EntityFrameworkCore;
using PDVStore.Data;
using PDVStore.Models;

namespace PDVStore.Services
{
    public class ClienteService
    {
        private readonly PDVContext _context;

        // CONSTRUTOR: recebe o PDVContext via injeção de dependência e o guarda em
        // _context. É a porta de entrada para todas as operações com clientes.
        // Quem o chama: o container de DI da aplicação. Não lança exceções;
        // apenas armazena a dependência recebida para uso nos demais métodos.
        public ClienteService(PDVContext context)
        {
            _context = context;
        }

        // Lista clientes para exibição em telas de seleção/consulta. Regras:
        // por padrão retorna somente clientes ATIVOS (apenasAtivos = true, soft
        // delete do domínio) e aplica um filtro opcional por nome ou CPF/CNPJ
        // (Contains). Ordena por nome para facilitar a busca. Usa AsNoTracking
        // por ser leitura. Quem chama: formulários de cliente, venda e cadastro.
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

        // Busca um cliente pela chave primária (Id), rastreando o objeto para poder
        // alterá-lo depois (sem AsNoTracking). Retorna null se o Id não existir.
        // Depende do PVDContext e é usado por VendaService para validar o cliente
        // em vendas fiadas e consultar o saldo devedor/limite de crédito.
        public async Task<Cliente?> ObterPorIdAsync(int id)
        {
            return await _context.Clientes.FindAsync(id);
        }

        // Insere ou atualiza um cliente (padrão "upsert"). Regras: o Nome é
        // obrigatório — se vazio, lança ArgumentException. Quando Id == 0 trata-se
        // de cadastro novo: grava CadastradoEm (UTC) e adiciona a entidade; caso
        // contrário, atualiza o registro existente via Update. Persiste com
        // SaveChangesAsync e retorna o cliente. Quem chama: a tela de cadastro de
        // clientes. Note que a exclusão é tratada separadamente (AtualizarStatus).
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

        // Altera o status Ativo do cliente (soft delete / reativação): em vez de
        // apagar o registro, o cliente é "desativado" para não aparecer em
        // listas/vendas. Retorna false se o Id não existir; caso contrário
        // atualiza o campo Ativo e persiste. Quem chama: a tela de listagem de
        // clientes (botão ativar/desativar). Não interfere no saldo devedor.
        public async Task<bool> AtualizarStatusAsync(int id, bool ativo)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return false;
            cliente.Ativo = ativo;
            await _context.SaveChangesAsync();
            return true;
        }

        // Registra o pagamento (baixa) de parte do saldo devedor de um cliente que
        // comprou fiado. Regras: o valor precisa ser maior que zero (senão
        // ArgumentException) e o cliente deve existir (senão retorna false). O
        // saldo é reduzido com proteção Math.Max(0, ...) para nunca ficar
        // negativo. Complementa a lógica de venda fiada de VendaService, que
        // AUMENTA o SaldoDevedor no momento da venda. Quem chama: tela de
        // recebimento de fiados.
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