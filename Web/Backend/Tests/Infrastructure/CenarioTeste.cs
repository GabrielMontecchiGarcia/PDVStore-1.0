using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using PDVStore.Domain.Entities;
using PDVStore.DTOs;
using PDVStore.Repository;
using PDVStore.Repository.Context;
using PDVStore.Repository.Interfaces;
using PDVStore.Repository.Repositories;
using PDVStore.Services;
using PDVStore.Infrastructure.Integrations;

namespace PDVStore.Tests.Infrastructure;

/// <summary>
/// Cenário de teste em memória com todas as dependências reais (repositórios,
/// Unit of Work e serviços). Cada cenário usa um banco InMemory isolado, permitindo
/// validar as mesmas regras de negócio da aplicação sem SQL Server.
/// </summary>
public sealed class CenarioTeste : IAsyncDisposable
{
    public PDVStoreDbContext Context { get; }
    public IUnitOfWork UnitOfWork { get; }
    public UsuarioService Usuarios { get; }
    public ProdutoService Produtos { get; }
    public ClienteService Clientes { get; }
    public FornecedorService Fornecedores { get; }
    public CaixaService Caixas { get; }
    public VendaService Vendas { get; }
    public CompraService Compras { get; }
    public RelatorioService Relatorios { get; }

    private CenarioTeste(PDVStoreDbContext context)
    {
        Context = context;
        UnitOfWork = new UnitOfWork(context);

        var usuarios = new UsuarioRepository(context);
        var produtos = new ProdutoRepository(context);
        var clientes = new ClienteRepository(context);
        var fornecedores = new FornecedorRepository(context);
        var caixas = new CaixaRepository(context);
        var vendas = new VendaRepository(context);
        var compras = new CompraRepository(context);
        var movimentacoes = new MovimentacaoEstoqueRepository(context);
        var relatorios = new RelatorioRepository(context);

        Usuarios = new UsuarioService(usuarios, UnitOfWork);
        Produtos = new ProdutoService(produtos, movimentacoes, UnitOfWork);
        Clientes = new ClienteService(clientes, UnitOfWork);
        Fornecedores = new FornecedorService(fornecedores, UnitOfWork);
        Caixas = new CaixaService(caixas, vendas, UnitOfWork);
        Vendas = new VendaService(
            vendas, produtos, clientes, Caixas,
            new PagamentoIntegrator(NullLogger<PagamentoIntegrator>.Instance),
            movimentacoes, UnitOfWork,
            NullLogger<VendaService>.Instance);
        Compras = new CompraService(compras, produtos, fornecedores, movimentacoes, UnitOfWork,
            NullLogger<CompraService>.Instance);
        Relatorios = new RelatorioService(vendas, movimentacoes, relatorios, TimeZoneInfo.Utc);
    }

    public static CenarioTeste Criar()
    {
        var options = new DbContextOptionsBuilder<PDVStoreDbContext>()
            .UseInMemoryDatabase($"pdvstore-tests-{Guid.NewGuid():N}")
            // O provider InMemory não implementa transações reais; as regras de
            // rollback são validadas pela ordem das operações e no SQL Server.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new CenarioTeste(new PDVStoreDbContext(options));
    }

    /// <summary>Cria um operador ativo e retorna o Id (login: operador / 123456).</summary>
    public async Task<int> CriarOperadorAsync()
    {
        var usuario = await Usuarios.CriarAsync(new CriarUsuarioRequest
        {
            Nome = "operador",
            Senha = "123456",
            ConfirmacaoSenha = "123456",
            Permissao = (int)TipoPermissao.Operador
        });

        return usuario.Id;
    }

    /// <summary>Cria um produto válido para os testes de venda/estoque.</summary>
    public Task<Produto> CriarProdutoAsync(
        string nome = "Produto Teste",
        decimal preco = 10m,
        decimal custo = 5m,
        int estoque = 10,
        int minimo = 2) =>
        Produtos.CriarAsync(new ProdutoRequest
        {
            CodigoBarras = $"789{Guid.NewGuid():N}"[..12],
            Nome = nome,
            Preco = preco,
            PrecoCusto = custo,
            Estoque = estoque,
            EstoqueMinimo = minimo,
            Categoria = "Geral",
            Descricao = "Produto criado nos testes"
        });

    /// <summary>Cria um cliente válido (CPF válido) com o limite informado.</summary>
    public Task<Cliente> CriarClienteAsync(decimal limite = 100m, decimal saldo = 0m, bool ativo = true) =>
        CriarClienteInternoAsync(limite, saldo, ativo);

    private async Task<Cliente> CriarClienteInternoAsync(decimal limite, decimal saldo, bool ativo)
    {
        var cliente = new Cliente
        {
            Nome = "Cliente Teste",
            CpfCnpj = "529.982.247-25",
            Telefone = "(69) 99999-9999",
            Email = "cliente@teste.com",
            Endereco = "Rua Teste, 100",
            LimiteCredito = limite,
            SaldoDevedor = saldo,
            Ativo = ativo,
            CadastradoEm = DateTime.UtcNow
        };

        Context.Clientes.Add(cliente);
        await UnitOfWork.SalvarAsync();
        return cliente;
    }

    /// <summary>Cria um fornecedor válido (CNPJ válido) para os testes de compra.</summary>
    public async Task<Fornecedor> CriarFornecedorAsync()
    {
        var fornecedor = new Fornecedor
        {
            Nome = "Fornecedor Teste",
            Cnpj = "11.222.333/0001-81",
            InscricaoEstadual = "11004249011400",
            Telefone = "(69) 3333-3333",
            Email = "fornecedor@teste.com",
            Ativo = true
        };

        Context.Fornecedores.Add(fornecedor);
        await UnitOfWork.SalvarAsync();
        return fornecedor;
    }

    public Task<Caixa> AbrirCaixaAsync(int usuarioId, decimal valorInicial = 100m) =>
        Caixas.AbrirCaixaAsync(valorInicial, usuarioId);

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
    }
}
