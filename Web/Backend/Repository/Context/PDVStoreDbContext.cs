using Microsoft.EntityFrameworkCore;
using PDVStore.Domain.Entities;

namespace PDVStore.Repository.Context;

/// <summary>
/// Contexto EF Core da versão web. Replica o PDVContext do sistema Windows:
/// mesmas tabelas (nomes dos DbSets), mesmos relacionamentos/comportamentos de
/// exclusão, mesmos tipos (decimal(18,2), datetime2, nvarchar(max)) e os mesmos
/// seeds (caixa padrão Id=1 e usuário Admin).
///
/// A configuração Fluent API NÃO altera nenhuma regra do modelo original; apenas
/// acrescenta índices de consulta (documentados em Database/README.md).
/// </summary>
public class PDVStoreDbContext : DbContext
{
    public PDVStoreDbContext(DbContextOptions<PDVStoreDbContext> options) : base(options)
    {
    }

    public DbSet<UsuarioCaixa> Usuarios => Set<UsuarioCaixa>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVendas => Set<ItemVenda>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Caixa> Caixas => Set<Caixa>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<ItemCompra> ItensCompras => Set<ItemCompra>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Todas as strings seguem o padrão do EF do projeto original (nvarchar(max)),
        // e todos os valores monetários são decimal(18,2).
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
        {
            property.SetColumnType("datetime2");
        }

        ConfigurarRelacionamentos(modelBuilder);
        ConfigurarIndices(modelBuilder);
        ConfigurarSeeds(modelBuilder);
    }

    /// <summary>
    /// Relacionamentos e comportamentos de exclusão idênticos ao PDVContext original.
    /// </summary>
    private static void ConfigurarRelacionamentos(ModelBuilder modelBuilder)
    {
        // Caixa -> usuário responsável (Restrict: não apaga caixas ao excluir usuário).
        modelBuilder.Entity<Caixa>()
            .HasOne(c => c.UsuarioCaixa)
            .WithMany()
            .HasForeignKey(c => c.UsuarioCaixaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Venda -> Caixa (vendas legadas com CaixaId = 1 apontam para o caixa default).
        modelBuilder.Entity<Venda>()
            .HasOne(v => v.Caixa)
            .WithMany(c => c.Vendas)
            .HasForeignKey(v => v.CaixaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Venda -> Cliente (SetNull: cliente pode ser removido sem apagar a venda).
        modelBuilder.Entity<Venda>()
            .HasOne(v => v.Cliente)
            .WithMany()
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);

        // Venda -> Usuário operador.
        modelBuilder.Entity<Venda>()
            .HasOne(v => v.UsuarioCaixa)
            .WithMany()
            .HasForeignKey(v => v.UsuarioCaixaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Itens da venda.
        modelBuilder.Entity<ItemVenda>()
            .HasOne(i => i.Produto)
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ItemVenda>()
            .HasOne(i => i.Venda)
            .WithMany(v => v.Itens)
            .HasForeignKey(i => i.VendaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Movimentações de estoque.
        modelBuilder.Entity<MovimentacaoEstoque>()
            .HasOne(m => m.Produto)
            .WithMany()
            .HasForeignKey(m => m.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MovimentacaoEstoque>()
            .HasOne(m => m.Usuario)
            .WithMany()
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Compras e itens.
        modelBuilder.Entity<Compra>()
            .HasOne(c => c.Fornecedor)
            .WithMany(f => f.Compras)
            .HasForeignKey(c => c.FornecedorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Compra>()
            .HasOne(c => c.UsuarioCaixa)
            .WithMany()
            .HasForeignKey(c => c.UsuarioCaixaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ItemCompra>()
            .HasOne(i => i.Compra)
            .WithMany(c => c.Itens)
            .HasForeignKey(i => i.CompraId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ItemCompra>()
            .HasOne(i => i.Produto)
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// Índices adicionais de consulta. Não são únicos, portanto não introduzem
    /// novas restrições de negócio (apenas desempenho em relatórios por período).
    ///
    /// Observação: as colunas de texto permanecem nvarchar(max), exatamente como
    /// no banco original. Por isso NÃO são indexadas (o SQL Server não indexa
    /// nvarchar(max) diretamente) e as buscas por nome usam LIKE '%termo%', que
    /// também não se beneficiaria de um índice B-tree comum.
    /// </summary>
    private static void ConfigurarIndices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Venda>().HasIndex(v => v.DataVenda);
        modelBuilder.Entity<Compra>().HasIndex(c => c.DataCompra);
        modelBuilder.Entity<MovimentacaoEstoque>().HasIndex(m => m.DataMovimentacao);
        modelBuilder.Entity<MovimentacaoEstoque>().HasIndex(m => new { m.ProdutoId, m.DataMovimentacao });
    }

    /// <summary>
    /// Seeds idênticos ao projeto original: caixa padrão 1 (fechado) e usuário
    /// Admin (senha inicial admin123) para o primeiro acesso.
    /// </summary>
    private static void ConfigurarSeeds(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Caixa>().HasData(
            new Caixa
            {
                Id = 1,
                UsuarioCaixaId = 1,
                Abertura = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
                ValorInicial = 0,
                Status = StatusCaixa.Fechado
            });

        modelBuilder.Entity<UsuarioCaixa>().HasData(
            new UsuarioCaixa
            {
                Id = 1,
                Nome = "Admin",
                SenhaHash = "$2a$11$ZBR/L6.DiCjgBWjvnJOsMeVfeHtigReLCpU15E8R8FK/Kgn8n1nZa",
                Permissao = TipoPermissao.Administrador,
                CreatedAt = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
                Ativo = true
            });
    }
}
