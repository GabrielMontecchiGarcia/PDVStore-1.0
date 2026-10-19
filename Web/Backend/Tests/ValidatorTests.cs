using PDVStore.Application.Validators;
using PDVStore.Domain.Entities;
using PDVStore.DTOs;

namespace PDVStore.Tests;

/// <summary>
/// Testes das validações server-side (FluentValidation). Garantem que as mesmas
/// mensagens/regras das telas WinForms também sejam aplicadas via API.
/// </summary>
public class ValidatorTests
{
    [Fact]
    public void Cliente_ComCpfInvalido_DeveFalharComMensagemDaTela()
    {
        var resultado = new ClienteRequestValidator().Validate(new ClienteRequest
        {
            Nome = "Cliente",
            CpfCnpj = "111.111.111-11",
            Telefone = "(69) 99999-9999",
            Email = "cliente@teste.com",
            Endereco = "Rua 1",
            LimiteCredito = 100m
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "Informe um CPF ou CNPJ válido.");
    }

    [Fact]
    public void Cliente_ComLimiteMenorQueDez_DeveFalhar()
    {
        var resultado = new ClienteRequestValidator().Validate(new ClienteRequest
        {
            Nome = "Cliente",
            CpfCnpj = "529.982.247-25",
            Telefone = "(69) 99999-9999",
            Email = "cliente@teste.com",
            Endereco = "Rua 1",
            LimiteCredito = 5m
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage.Contains("limite de crédito válido"));
    }

    [Fact]
    public void Cliente_Valido_DevePassar()
    {
        var resultado = new ClienteRequestValidator().Validate(new ClienteRequest
        {
            Nome = "Cliente",
            CpfCnpj = "529.982.247-25",
            Telefone = "(69) 99999-9999",
            Email = "cliente@teste.com",
            Endereco = "Rua 1",
            LimiteCredito = 100m
        });

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Produto_SemCamposObrigatorios_DeveFalhar()
    {
        var resultado = new ProdutoRequestValidator().Validate(new ProdutoRequest
        {
            CodigoBarras = "",
            Nome = "",
            Categoria = "",
            Descricao = ""
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "Código de barras é obrigatório.");
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "Nome do produto é obrigatório.");
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "Categoria é obrigatória.");
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "Descrição é obrigatória.");
    }

    [Fact]
    public void Fornecedor_ComCnpjInvalido_DeveFalhar()
    {
        var resultado = new FornecedorRequestValidator().Validate(new FornecedorRequest
        {
            Nome = "Fornecedor",
            Cnpj = "11.111.111/1111-11",
            Telefone = "(69) 3333-3333",
            Email = "fornecedor@teste.com",
            InscricaoEstadual = "11004249011401"
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "Informe um CNPJ válido.");
    }

    [Fact]
    public void Venda_SemItens_DeveFalhar()
    {
        var resultado = new VendaRequestValidator().Validate(new VendaRequest
        {
            FormaPagamento = FormasPagamento.Pix,
            Itens = new List<ItemVendaRequest>()
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "A venda deve conter pelo menos um item.");
    }

    [Fact]
    public void Venda_ComQuantidadeZero_DeveFalhar()
    {
        var resultado = new VendaRequestValidator().Validate(new VendaRequest
        {
            FormaPagamento = FormasPagamento.Pix,
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = 1, Quantidade = 0, PrecoUnitario = 10m }
            }
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "Quantidade deve ser maior que zero.");
    }

    [Fact]
    public void Compra_ComCustoNegativo_DeveFalhar()
    {
        var resultado = new CompraRequestValidator().Validate(new CompraRequest
        {
            FornecedorId = 1,
            Itens = new List<ItemCompraRequest>
            {
                new() { ProdutoId = 1, Quantidade = 1, PrecoCusto = -1m }
            }
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "Preço de custo não pode ser negativo.");
    }

    [Fact]
    public void Login_SemUsuarioESenha_DeveFalhar()
    {
        var resultado = new LoginValidator().Validate(new LoginRequest());

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "Informe usuário e senha.");
    }

    [Fact]
    public void Usuario_ComSenhasDiferentes_DeveFalhar()
    {
        var resultado = new CriarUsuarioValidator().Validate(new CriarUsuarioRequest
        {
            Nome = "novo",
            Senha = "123456",
            ConfirmacaoSenha = "654321",
            Permissao = (int)TipoPermissao.Operador
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "As senhas não coincidem!");
    }
}
