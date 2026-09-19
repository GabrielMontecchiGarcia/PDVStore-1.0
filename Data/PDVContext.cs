using Microsoft.EntityFrameworkCore;
using PDVStore.Models;

namespace PDVStore.Data
{
    public class PDVContext : DbContext
    {
        public PDVContext(DbContextOptions<PDVContext> options) : base(options)
        {
        }

        public DbSet<UsuarioCaixa> Usuarios { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Venda> Vendas { get; set; }
        public DbSet<ItemVenda> ItensVendas { get; set; }
        public DbSet<MovimentacaoEstoque> MovimentacoesEstoque { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Caixa> Caixas { get; set; }
        public DbSet<Fornecedor> Fornecedores { get; set; }
        public DbSet<Compra> Compras { get; set; }
        public DbSet<ItemCompra> ItensCompras { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Caixa
            modelBuilder.Entity<Caixa>()
                .HasOne(c => c.UsuarioCaixa)
                .WithMany()
                .HasForeignKey(c => c.UsuarioCaixaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Venda -> Caixa (manter valores antigos com CaixaId = 1 apontam para caixa default)
            modelBuilder.Entity<Venda>()
                .HasOne(v => v.Caixa)
                .WithMany(c => c.Vendas)
                .HasForeignKey(v => v.CaixaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Venda>()
                .HasOne(v => v.Cliente)
                .WithMany()
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Venda>()
                .HasOne(v => v.UsuarioCaixa)
                .WithMany()
                .HasForeignKey(v => v.UsuarioCaixaId)
                .OnDelete(DeleteBehavior.Restrict);

            // ItemVenda
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

            // MovimentacaoEstoque
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

            // Compra / ItemCompra
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

            // Caixa default (id=1) para vendas legadas com CaixaId=1
            modelBuilder.Entity<Caixa>().HasData(
                new Caixa
                {
                    Id = 1,
                    UsuarioCaixaId = 1,
                    Abertura = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
                    ValorInicial = 0,
                    Status = "Fechado"
                });

            // Usuário Admin padrão (senha inicial: admin123)
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
}