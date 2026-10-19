using FluentValidation;
using PDVStore.Domain.Common;
using PDVStore.DTOs;

namespace PDVStore.Application.Validators;

/// <summary>
/// Validação do produto com exatamente as mensagens da tela frmGerenciarProdutos.
/// </summary>
public class ProdutoRequestValidator : AbstractValidator<ProdutoRequest>
{
    public ProdutoRequestValidator()
    {
        RuleFor(x => x.CodigoBarras)
            .NotEmpty().WithMessage("Código de barras é obrigatório.");

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome do produto é obrigatório.");

        RuleFor(x => x.Preco)
            .GreaterThanOrEqualTo(0).WithMessage("Informe um preço de venda válido.");

        RuleFor(x => x.PrecoCusto)
            .GreaterThanOrEqualTo(0).WithMessage("Informe um preço de custo válido.");

        RuleFor(x => x.Estoque)
            .GreaterThanOrEqualTo(0).WithMessage("Informe um estoque válido.");

        RuleFor(x => x.EstoqueMinimo)
            .GreaterThanOrEqualTo(0).WithMessage("Informe um estoque mínimo válido.");

        RuleFor(x => x.Categoria)
            .NotEmpty().WithMessage("Categoria é obrigatória.");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição é obrigatória.");
    }
}

/// <summary>
/// Validação do cliente. A tela original é mais restritiva que o serviço
/// (CPF/CNPJ, telefone, e-mail, endereço e limite mínimo); essas regras são
/// aplicadas no servidor para impedir burla via API.
/// </summary>
public class ClienteRequestValidator : AbstractValidator<ClienteRequest>
{
    public ClienteRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome do cliente é obrigatório.");

        RuleFor(x => x.CpfCnpj)
            .Must(doc =>
            {
                var digitos = Mascaras.SomenteDigitos(doc);
                return digitos.Length switch
                {
                    11 => Mascaras.ValidarCpf(digitos),
                    14 => Mascaras.ValidarCnpj(digitos),
                    _ => false
                };
            })
            .WithMessage("Informe um CPF ou CNPJ válido.");

        RuleFor(x => x.Telefone)
            .Must(tel => Mascaras.SomenteDigitos(tel).Length >= 10)
            .WithMessage("Informe um telefone válido com DDD (mínimo 10 dígitos).");

        RuleFor(x => x.Email)
            .Must(Mascaras.ValidarEmail)
            .WithMessage("Informe um e-mail válido.");

        RuleFor(x => x.Endereco)
            .NotEmpty().WithMessage("Informe o endereço do cliente.");

        RuleFor(x => x.LimiteCredito)
            .GreaterThanOrEqualTo(10)
            .WithMessage("Informe um limite de crédito válido (número positivo e maior ou igual a 10).");
    }
}

/// <summary>
/// Validação do fornecedor: nome, CNPJ válido, telefone, e-mail e inscrição
/// estadual (14 dígitos) — mesmas regras da tela frmFornecedores.
/// </summary>
public class FornecedorRequestValidator : AbstractValidator<FornecedorRequest>
{
    public FornecedorRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome do fornecedor é obrigatório.");

        RuleFor(x => x.Cnpj)
            .Must(cnpj => Mascaras.SomenteDigitos(cnpj).Length == 14 && Mascaras.ValidarCnpj(cnpj))
            .WithMessage("Informe um CNPJ válido.");

        RuleFor(x => x.Telefone)
            .Must(tel => Mascaras.SomenteDigitos(tel).Length >= 10)
            .WithMessage("Informe um telefone válido com DDD (mínimo 10 dígitos).");

        RuleFor(x => x.Email)
            .Must(Mascaras.ValidarEmail)
            .WithMessage("Informe um e-mail válido.");

        RuleFor(x => x.InscricaoEstadual)
            .Must(ie => Mascaras.SomenteDigitos(ie).Length == 14 && Mascaras.ValidarInscEstadual(ie))
            .WithMessage("Informe uma inscrição estadual válida.");
    }
}
