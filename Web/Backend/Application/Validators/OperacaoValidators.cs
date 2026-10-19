using FluentValidation;
using PDVStore.Domain.Entities;
using PDVStore.DTOs;

namespace PDVStore.Application.Validators;

/// <summary>
/// Validação da venda: itens, forma de pagamento, quantidades, preços e desconto.
/// As regras de caixa aberto, estoque, fiado e troco ficam no VendaService,
/// exatamente como no projeto original.
/// </summary>
public class VendaRequestValidator : AbstractValidator<VendaRequest>
{
    public VendaRequestValidator()
    {
        RuleFor(x => x.Itens)
            .NotEmpty().WithMessage("A venda deve conter pelo menos um item.");

        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.ProdutoId)
                .GreaterThan(0).WithMessage("Produto inválido.");

            item.RuleFor(i => i.Quantidade)
                .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero.");

            item.RuleFor(i => i.PrecoUnitario)
                .GreaterThanOrEqualTo(0).WithMessage("Preço unitário não pode ser negativo.");
        });

        RuleFor(x => x.FormaPagamento)
            .Must(f => FormasPagamento.Todas.Contains(f, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Selecione uma forma de pagamento.");

        RuleFor(x => x.Desconto)
            .GreaterThanOrEqualTo(0).WithMessage("Desconto não pode ser negativo.");

        RuleFor(x => x.ValorRecebido)
            .GreaterThanOrEqualTo(0).When(x => x.ValorRecebido.HasValue)
            .WithMessage("Valor recebido não pode ser negativo.");
    }
}

/// <summary>
/// Validação da compra: fornecedor obrigatório, ao menos um item, quantidade
/// positiva e custo não negativo (mensagens do CompraService original).
/// </summary>
public class CompraRequestValidator : AbstractValidator<CompraRequest>
{
    public CompraRequestValidator()
    {
        RuleFor(x => x.FornecedorId)
            .GreaterThan(0).WithMessage("Selecione um fornecedor.");

        RuleFor(x => x.Itens)
            .NotEmpty().WithMessage("A compra deve conter pelo menos um item.");

        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.ProdutoId)
                .GreaterThan(0).WithMessage("Produto inválido.");

            item.RuleFor(i => i.Quantidade)
                .GreaterThan(0).WithMessage("Quantidade dos itens deve ser maior que zero.");

            item.RuleFor(i => i.PrecoCusto)
                .GreaterThanOrEqualTo(0).WithMessage("Preço de custo não pode ser negativo.");
        });
    }
}

/// <summary>Abertura de caixa: valor inicial não negativo (fundo de troco).</summary>
public class AbrirCaixaValidator : AbstractValidator<AbrirCaixaRequest>
{
    public AbrirCaixaValidator()
    {
        RuleFor(x => x.ValorInicial)
            .GreaterThanOrEqualTo(0).WithMessage("Valor inicial não pode ser negativo.");
    }
}

/// <summary>Sangria: valor estritamente maior que zero (regra do CaixaService).</summary>
public class SangriaValidator : AbstractValidator<SangriaRequest>
{
    public SangriaValidator()
    {
        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("Valor da sangria deve ser maior que zero.");
    }
}

/// <summary>Ajuste manual de estoque: quantidade maior que zero.</summary>
public class AjusteEstoqueValidator : AbstractValidator<AjusteEstoqueRequest>
{
    public AjusteEstoqueValidator()
    {
        RuleFor(x => x.Quantidade)
            .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero.");
    }
}

/// <summary>Recebimento de fiado: valor maior que zero.</summary>
public class ReceberFiadoValidator : AbstractValidator<ReceberFiadoRequest>
{
    public ReceberFiadoValidator()
    {
        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("Valor deve ser maior que zero.");
    }
}
