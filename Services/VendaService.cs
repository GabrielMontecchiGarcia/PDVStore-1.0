using Microsoft.EntityFrameworkCore;
using PDVStore.Data;
using PDVStore.Integrations;
using PDVStore.Models;
using PDVStore.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PDVLoja.Services
{
    public class VendaService
    {
        private readonly PDVContext _context;
        private readonly PagamentoIntegrator _pagamentoIntegrator;
        private readonly CaixaService _caixaService;

        // CONSTRUTOR: recebe três dependências injetadas — PDVContext (acesso ao
        // banco), PagamentoIntegrator (processa o pagamento da venda, mock) e
        // CaixaService (exigir caixa aberto). É o serviço mais crítico do PDV e
        // concentra todas as regras de venda. Não lança exceções; guarda as
        // dependências nos campos privados para os métodos abaixo.
        public VendaService(PDVContext context, PagamentoIntegrator pagamentoIntegrator, CaixaService caixaService)
        {
            _context = context;
            _pagamentoIntegrator = pagamentoIntegrator;
            _caixaService = caixaService;
        }

        // Registra uma venda completa dentro de UMA transação com rollback (Commit/
        // Rollback em qualquer falha). ORDEM e REGRAS: (0) exige CAIXA ABERTO
        // (dependência de CaixaService.ObterCaixaAbertoAsync, senão lança
        // InvalidOperationException); (0.1) venda FIADA exige cliente ativo e
        // respeita o limite de crédito (SaldoDevedor + total <= LimiteCredito);
        // calcula ValorTotal (itens - desconto, nunca negativo); (1) valida
        // estoque de cada produto antes de gravar; (2) grava cabeçalho+itens;
        // (3) BAIXA o estoque e registra MovimentacaoEstoque tipo "Saída";
        // (4) venda fiada AUMENTA o SaldoDevedor do cliente; (5) processa o
        // pagamento via PagamentoIntegrator (gera PixTxId no PIX) e só então
        // commita. Quem chama: a tela do PDV ao finalizar o cupom.
        public async Task<Venda> RegistrarVendaAsync(Venda venda)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (venda.Itens == null || !venda.Itens.Any())
                    throw new InvalidOperationException("A venda deve conter pelo menos um item.");

                if (string.IsNullOrWhiteSpace(venda.FormaPagamento))
                    throw new InvalidOperationException("Selecione uma forma de pagamento.");

                // 0. Exige caixa aberto para a venda (regra de domínio)
                var caixa = await _caixaService.ObterCaixaAbertoAsync();
                if (caixa == null)
                    throw new InvalidOperationException("Nenhum caixa aberto. Abra o caixa antes de registrar vendas.");
                venda.CaixaId = caixa.Id;

                // 0.1 Venda fiada exige cliente e respeita limite de crédito
                bool fiado = venda.FormaPagamento.Equals("Fiado", StringComparison.OrdinalIgnoreCase);
                if (fiado)
                {
                    if (!venda.ClienteId.HasValue)
                        throw new InvalidOperationException("Venda fiada exige um cliente selecionado.");

                    var cliente = await _context.Clientes.FindAsync(venda.ClienteId.Value);
                    if (cliente == null || !cliente.Ativo)
                        throw new InvalidOperationException("Cliente informado não está ativo.");

                    decimal total = venda.Itens.Sum(i => i.Subtotal) - venda.Desconto;
                    if (cliente.LimiteCredito > 0 &&
                        cliente.SaldoDevedor + total > cliente.LimiteCredito)
                        throw new InvalidOperationException($"Limite de crédito do cliente excedido. Saldo atual: {cliente.SaldoDevedor:C2}.");
                }

                venda.DataVenda = DateTime.UtcNow;
                venda.ValorTotal = venda.Itens.Sum(i => i.Subtotal) - venda.Desconto;
                if (venda.ValorTotal < 0)
                    throw new InvalidOperationException("O valor total da venda não pode ser negativo.");

                // 1. Validar estoque antes de qualquer gravação
                foreach (var item in venda.Itens)
                {
                    var produto = await _context.Produtos.FindAsync(item.ProdutoId);
                    if (produto == null)
                        throw new InvalidOperationException($"Produto ID {item.ProdutoId} não encontrado.");
                    if (produto.Estoque < item.Quantidade)
                        throw new InvalidOperationException($"Estoque insuficiente para o produto '{produto.Nome}'.");
                }

                // 2. Salvar venda (cabeçalho + itens)
                _context.Vendas.Add(venda);
                await _context.SaveChangesAsync();

                // 3. Baixar estoque e registrar movimentação (referenciando a venda)
                foreach (var item in venda.Itens)
                {
                    var produto = await _context.Produtos.FindAsync(item.ProdutoId);
                    produto.Estoque -= item.Quantidade;

                    _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                    {
                        ProdutoId = item.ProdutoId,
                        Tipo = "Saída",
                        Quantidade = item.Quantidade,
                        PrecoUnitario = produto.PrecoCusto,
                        DataMovimentacao = DateTime.UtcNow,
                        UsuarioId = venda.UsuarioCaixaId,
                        Motivo = $"Venda #{venda.Id}",
                        ReferenciaVendaId = venda.Id
                    });
                }

                // 4. Venda fiada atualiza saldo devedor do cliente
                if (fiado && venda.ClienteId.HasValue)
                {
                    var cliente = await _context.Clientes.FindAsync(venda.ClienteId.Value);
                    cliente.SaldoDevedor += venda.ValorTotal;
                }

                // 5. Processar pagamento (mock PIX/Cartão/Dinheiro)
                bool pagamentoOk = await _pagamentoIntegrator.ProcessarPagamentoAsync(venda);
                if (!pagamentoOk)
                    throw new InvalidOperationException("Falha ao processar pagamento.");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return venda;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Carrega uma venda completa para detalhamento: itens (com produtos),
        // usuário do caixa e cliente, usando Includes encadeados. Usa AsNoTracking
        // por ser consulta de leitura; retorna null se o Id não existir.
        // Quem chama: a tela de detalhes/consulta de venda ou o cupom impresso.
        public async Task<Venda?> ObterPorIdAsync(int id)
        {
            return await _context.Vendas
                .Include(v => v.Itens)
                .ThenInclude(i => i.Produto)
                .Include(v => v.UsuarioCaixa)
                .Include(v => v.Cliente)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        // Lista as vendas CONCLUÍDAS de um período, incluindo itens, usuário e
        // cliente para exibição. Aceita filtro opcional de CaixaId (ex.: vendas
        // do caixa atual/daquele turno). Ordena da mais recente para a mais
        // antiga. Usa AsNoTracking (leitura). Quem chama: relatórios e o
        // DashboardViewModel (via RelatorioService) para análise por período.
        public async Task<IEnumerable<Venda>> ListarPorPeriodoAsync(DateTime inicio, DateTime fim, int? caixaId = null)
        {
            var query = _context.Vendas
                .Include(v => v.Itens)
                .Include(v => v.UsuarioCaixa)
                .Include(v => v.Cliente)
                .AsNoTracking()
                .Where(v => v.DataVenda >= inicio && v.DataVenda <= fim && v.Status == "Concluida");

            if (caixaId.HasValue)
                query = query.Where(v => v.CaixaId == caixaId.Value);

            return await query.OrderByDescending(v => v.DataVenda).ToListAsync();
        }

        // Soma o ValorTotal das vendas CONCLUÍDAS em um período (com filtro opcional
        // de caixa). Usa SumAsync no banco (agregação eficiente, sem carregar
        // todos os registros). É o indicador "faturamento do período" usado pelo
        // DashboardViewModel (TotalPeriodo) e relatórios.
        public async Task<decimal> CalcularTotalVendasAsync(DateTime inicio, DateTime fim, int? caixaId = null)
        {
            var query = _context.Vendas
                .Where(v => v.DataVenda >= inicio && v.DataVenda <= fim && v.Status == "Concluida");

            if (caixaId.HasValue)
                query = query.Where(v => v.CaixaId == caixaId.Value);

            return await query.SumAsync(v => v.ValorTotal);
        }

        // Conta quantas vendas foram CONCLUÍDAS em um período. Usa CountAsync (conta
        // no banco, sem carregar dados). É o indicador "quantidade de vendas do
        // período" usado pelo DashboardViewModel (QuantidadeVendas).
        public async Task<int> ContarVendasAsync(DateTime inicio, DateTime fim)
        {
            return await _context.Vendas
                .CountAsync(v => v.DataVenda >= inicio && v.DataVenda <= fim && v.Status == "Concluida");
        }

        // Cancela uma venda, desfazendo todos os efeitos dela dentro de uma transação
        // com rollback. Regras: retorna false se a venda não existir ou já estiver
        // cancelada. O que fazem: DEVOLVE cada item ao estoque, registra
        // MovimentacaoEstoque tipo "Entrada" com motivo "Estorno - Venda #id" e
        // ReferenciaVendaId; se a venda era FIADA, desconta o valor do
        // SaldoDevedor do cliente (Math.Max(0,...)); e marca a venda como
        // "Cancelada". Qualquer falha executa Rollback e retorna false. Quem
        // chama: a tela de consulta de vendas (botão cancelar).
        public async Task<bool> CancelarVendaAsync(int vendaId, string motivo)
        {
            var venda = await _context.Vendas
                .Include(v => v.Itens)
                .FirstOrDefaultAsync(v => v.Id == vendaId);

            if (venda == null || venda.Status == "Cancelada")
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in venda.Itens)
                {
                    var produto = await _context.Produtos.FindAsync(item.ProdutoId);
                    if (produto != null)
                        produto.Estoque += item.Quantidade;

                    _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                    {
                        ProdutoId = item.ProdutoId,
                        Tipo = "Entrada",
                        Quantidade = item.Quantidade,
                        DataMovimentacao = DateTime.UtcNow,
                        UsuarioId = Session.CurrentUser?.Id,
                        Motivo = $"Estorno - Venda #{venda.Id}",
                        ReferenciaVendaId = venda.Id
                    });
                }

                // Devolve saldo devedor se a venda era fiada
                if (venda.ClienteId.HasValue &&
                    venda.FormaPagamento.Equals("Fiado", StringComparison.OrdinalIgnoreCase))
                {
                    var cliente = await _context.Clientes.FindAsync(venda.ClienteId.Value);
                    if (cliente != null)
                        cliente.SaldoDevedor = Math.Max(0, cliente.SaldoDevedor - venda.ValorTotal);
                }

                venda.Status = "Cancelada";
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

        // Relatórios rápidos
        // Relatório rápido que retorna TODAS as vendas concluídas de um período (sem
        // incluir itens, apenas o cabeçalho) para análise por forma de pagamento.
        // É a base do dashboard de "pagamentos por forma": o chamador agrupa por
        // FormaPagamento para montar a distribuição (PIX, Cartão, Dinheiro,
        // Fiado). Usa AsNoTracking (leitura). Quem chama: DashboardViewModel.
        public async Task<IEnumerable<Venda>> RelatorioVendasPorFormaPagamentoAsync(DateTime inicio, DateTime fim)
        {
            return await _context.Vendas
                .AsNoTracking()
                .Where(v => v.DataVenda >= inicio && v.DataVenda <= fim && v.Status == "Concluida")
                .ToListAsync();
        }
    }
}