using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDVStore.Migrations
{
    /// <inheritdoc />
    public partial class start : Migration
    {
        // ==========================================================================
        // MIGRAÇÃO "start" — MÉTODO Up():
        //
        // Este é o método que EXECUTA as mudanças no banco (Up = "aplica").
        // Aqui criamos o banco de dados "do zero", com a estrutura inicial do PDV:
        //
        //   1. Tabela FormaPagamentos -> formas de pagamento (Dinheiro/Cartão/Pix...)
        //   2. Tabela Produtos        -> produtos com Nome, Preco e Estoque
        //   3. Tabela UsuarioCaixa    -> usuários do caixa (SenhaHash e Permissao)
        //   4. Tabela Vendas          -> vendas registradas (Data, Total, FormaPagamentoId)
        //   5. Tabela ItemVenda       -> itens de cada venda (FK VendaId -> Vendas)
        //
        // Também inserimos um usuário padrão "Admin" (seed) e criamos o índice
        // IX_ItemVenda_VendaId para acelerar a consulta de itens de cada venda.
        // ==========================================================================
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FormaPagamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormaPagamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(nullable: false),
                    Preco = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estoque = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioCaixa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(nullable: false),
                    SenhaHash = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Permissao = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioCaixa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vendas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FormaPagamentoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemVenda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Produto = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemVenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemVenda_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "UsuarioCaixa",
                columns: new[] { "Id", "CreatedAt", "Nome", "Permissao", "SenhaHash" },
                values: new object[] { 1, new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Admin", 1, "$2a$11$abcdefghijklmnopqrstuv" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemVenda_VendaId",
                table: "ItemVenda",
                column: "VendaId");
        }

        // ==========================================================================
        // MIGRAÇÃO "start" — MÉTODO Down():
        //
        // O Down() é o oposto do Up(): ele DESFAZ as mudanças. Aqui removemos
        // todos os objetos criados no Up(), derrubando as tabelas do banco.
        // A ordem importa: como ItemVenda depende de Vendas, ela é removida antes,
        // para não deixar referências pendentes.
        // ==========================================================================
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormaPagamentos");

            migrationBuilder.DropTable(
                name: "ItemVenda");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "UsuarioCaixa");

            migrationBuilder.DropTable(
                name: "Vendas");
        }
    }
}
