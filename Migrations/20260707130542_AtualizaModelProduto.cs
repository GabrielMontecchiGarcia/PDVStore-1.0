using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDVStore.Migrations
{
    /// <inheritdoc />
    public partial class AtualizaModelProduto : Migration
    {
        // ==========================================================================
        // MIGRAÇÃO "AtualizaModelProduto" — MÉTODO Up():
        //
        // Evolui a tabela Produtos, adicionando campos que o domínio exige para um
        // catálogo de produtos mais completo:
        //
        //   - Ativo        : controle lógico (desativa um produto sem apagá-lo);
        //   - Categoria    : agrupa produtos (alimentos, bebidas, etc.);
        //   - CodigoBarras : código de barras lido pelo scanner do caixa;
        //   - Descricao    : texto livre descrevendo o produto;
        //   - EstoqueAtual : estoque "na prática", atualizado pelas movimentações.
        // ==========================================================================
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Produtos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Produtos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoBarras",
                table: "Produtos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Produtos",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstoqueAtual",
                table: "Produtos",
type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        // ==========================================================================
        // MIGRAÇÃO "AtualizaModelProduto" — MÉTODO Down():
        //
        // Desfaz a evolução: remove as colunas adicionadas no Up(), fazendo a
        // tabela Produtos voltar ao formato anterior (sem Ativo, Categoria,
        // CodigoBarras, Descricao e EstoqueAtual).
        // ==========================================================================
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "CodigoBarras",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "EstoqueAtual",
                table: "Produtos");
        }
    }
}
