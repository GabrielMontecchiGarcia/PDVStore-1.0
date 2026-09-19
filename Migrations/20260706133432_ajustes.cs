using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDVStore.Migrations
{
    /// <inheritdoc />
    public partial class ajustes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Total",
                table: "Vendas",
                newName: "ValorTotal");

            migrationBuilder.RenameColumn(
                name: "FormaPagamentoId",
                table: "Vendas",
                newName: "UsuarioCaixaId");

            migrationBuilder.RenameColumn(
                name: "Data",
                table: "Vendas",
                newName: "DataVenda");

            migrationBuilder.RenameColumn(
                name: "Produto",
                table: "ItemVenda",
                newName: "ProdutoId");

            migrationBuilder.AddColumn<int>(
                name: "CaixaId",
                table: "Vendas",
type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Desconto",
                table: "Vendas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FormaPagamento",
                table: "Vendas",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PixTxId",
                table: "Vendas",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Vendas",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_UsuarioCaixaId",
                table: "Vendas",
                column: "UsuarioCaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemVenda_ProdutoId",
                table: "ItemVenda",
                column: "ProdutoId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemVenda_Produtos_ProdutoId",
                table: "ItemVenda",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendas_UsuarioCaixa_UsuarioCaixaId",
                table: "Vendas",
                column: "UsuarioCaixaId",
                principalTable: "UsuarioCaixa",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemVenda_Produtos_ProdutoId",
                table: "ItemVenda");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendas_UsuarioCaixa_UsuarioCaixaId",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_UsuarioCaixaId",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_ItemVenda_ProdutoId",
                table: "ItemVenda");

            migrationBuilder.DropColumn(
                name: "CaixaId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "Desconto",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "FormaPagamento",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "PixTxId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Vendas");

            migrationBuilder.RenameColumn(
                name: "ValorTotal",
                table: "Vendas",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "UsuarioCaixaId",
                table: "Vendas",
                newName: "FormaPagamentoId");

            migrationBuilder.RenameColumn(
                name: "DataVenda",
                table: "Vendas",
                newName: "Data");

            migrationBuilder.RenameColumn(
                name: "ProdutoId",
                table: "ItemVenda",
                newName: "Produto");
        }
    }
}
