using Microsoft.Extensions.Logging;
using PDVStore.Application.Interfaces;
using PDVStore.Domain.Entities;
using PDVStore.DTOs;
using PDVStore.Repository.Interfaces;

namespace PDVStore.Services;

/// <summary>
/// Serviço de vendas — núcleo do PDV. Replica a ordem e as regras do
/// VendaService original, tudo em UMA transação com rollback:
/// 1. venda com itens e forma de pagamento;
/// 2. caixa aberto obrigatório;
/// 3. fiado exige cliente ativo e respeita limite (limite 0 = sem bloqueio);
/// 4. total = itens - desconto, nunca negativo;
/// 5. valida estoque de todos os produtos;
/// 6. grava venda/itens, baixa estoque e registra movimentações "Saída";
/// 7. fiado aumenta o saldo devedor;
/// 8. processa o pagamento (mock PIX/cartão) e só então commita.
///
/// A classe é parcial para separar registro, validações auxiliares e consultas.
/// </summary>
public partial class VendaService : IVendaService
{
    private readonly IVendaRepository _vendas;
    private readonly IProdutoRepository _produtos;
    private readonly IClienteRepository _clientes;
    private readonly ICaixaService _caixaService;
    private readonly IPagamentoIntegrator _pagamento;
    private readonly IMovimentacaoEstoqueRepository _movimentacoes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VendaService> _logger;

    public VendaService(
        IVendaRepository vendas,
        IProdutoRepository produtos,
        IClienteRepository clientes,
        ICaixaService caixaService,
        IPagamentoIntegrator pagamento,
        IMovimentacaoEstoqueRepository movimentacoes,
        IUnitOfWork unitOfWork,
        ILogger<VendaService> logger)
    {
        _vendas = vendas;
        _produtos = produtos;
        _clientes = clientes;
        _caixaService = caixaService;
        _pagamento = pagamento;
        _movimentacoes = movimentacoes;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
}
