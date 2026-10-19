using Microsoft.Extensions.Logging;
using PDVStore.Domain.Entities;
using PDVStore.DTOs;

namespace PDVStore.Services;

/// <summary>
/// Fluxo transacional de registro da venda.
/// </summary>
public partial class VendaService
{
    /// <summary>
    /// Registra a venda aplicando todas as validações de domínio. O operador
    /// (UsuarioCaixaId) vem SEMPRE do token JWT, nunca do corpo da requisição.
    /// </summary>
    public async Task<Venda> RegistrarVendaAsync(VendaRequest request, int usuarioId, CancellationToken ct = default)
    {
        await using var transacao = await _unitOfWork.IniciarTransacaoAsync(ct);

        try
        {
            if (request.Itens == null || request.Itens.Count == 0)
                throw new Domain.Exceptions.RegraDeNegocioException("A venda deve conter pelo menos um item.");

            if (string.IsNullOrWhiteSpace(request.FormaPagamento))
                throw new Domain.Exceptions.RegraDeNegocioException("Selecione uma forma de pagamento.");

            // 0. Exige caixa aberto para vender (regra de domínio do PDV).
            var caixa = await _caixaService.ObterCaixaAbertoAsync(ct);
            if (caixa == null)
                throw new Domain.Exceptions.RegraDeNegocioException(
                    "Nenhum caixa aberto. Abra o caixa antes de registrar vendas.");

            var itens = request.Itens.Select(i => new ItemVenda
            {
                ProdutoId = i.ProdutoId,
                Quantidade = i.Quantidade,
                PrecoUnitario = i.PrecoUnitario
            }).ToList();

            decimal total = itens.Sum(i => i.Subtotal) - request.Desconto;
            bool fiado = FormasPagamento.Eh(request.FormaPagamento, FormasPagamento.Fiado);

            // 0.1 Fiado: cliente ativo e limite de crédito.
            var clienteFiado = fiado ? await ValidarFiadoAsync(request.ClienteId, total, ct) : null;

            ValidarTotalEValorRecebido(request, total);

            // 1. Validar estoque antes de qualquer gravação.
            var produtos = await ValidarEstoqueAsync(itens, ct);

            // 2. Gravar venda (cabeçalho + itens).
            var venda = new Venda
            {
                UsuarioCaixaId = usuarioId,
                CaixaId = caixa.Id,
                ClienteId = request.ClienteId,
                DataVenda = DateTime.UtcNow,
                Desconto = request.Desconto,
                FormaPagamento = request.FormaPagamento,
                ValorTotal = total,
                Status = StatusVenda.Concluida,
                Itens = itens
            };

            await _vendas.AdicionarAsync(venda, ct);
            await _unitOfWork.SalvarAsync(ct);

            // 3. Baixa estoque + movimentações vinculadas à venda.
            await BaixarEstoqueERegistrarMovimentacoesAsync(venda, produtos, usuarioId, ct);

            // 4. Fiado aumenta o saldo devedor do cliente.
            if (fiado && clienteFiado != null)
                clienteFiado.SaldoDevedor += venda.ValorTotal;

            // 5. Pagamento (mock) — se falhar, a transação é desfeita.
            bool pagamentoOk = await _pagamento.ProcessarPagamentoAsync(venda, ct);
            if (!pagamentoOk)
                throw new Domain.Exceptions.RegraDeNegocioException("Falha ao processar pagamento.");

            await _unitOfWork.SalvarAsync(ct);
            await transacao.CommitAsync(ct);

            _logger.LogInformation(
                "Venda {VendaId} registrada por usuário {UsuarioId}. Forma: {Forma}. Total: {Total}",
                venda.Id, usuarioId, venda.FormaPagamento, venda.ValorTotal);

            return venda;
        }
        catch
        {
            await transacao.RollbackAsync(ct);
            throw;
        }
    }
}
