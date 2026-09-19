using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDVStore.Migrations
{
    /// <inheritdoc />
    public partial class novojuste : Migration
    {
        // ==========================================================================
        // MIGRAÇÃO "novojuste" — MÉTODO Up():
        //
        // Ajusta a tabela UsuarioCaixa para o controle de operação do caixa:
        //
        //   1. Adiciona coluna Ativo   : permite "desativar" um usuário sem apagá-lo,
        //      preservando o histórico (vendas/movimentações) já registrado.
        //   2. Adiciona coluna FotoPath : caminho do arquivo de foto do usuário.
        //   3. UpdateData: marca o usuário Admin (Id = 1) como ativo, garantindo que
        //      o seed continue válido depois da inclusão da coluna Ativo.
        // ==========================================================================
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "UsuarioCaixa",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FotoPath",
                table: "UsuarioCaixa",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "UsuarioCaixa",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Ativo", "FotoPath" },
                values: new object[] { true, null });
        }

        // ==========================================================================
        // MIGRAÇÃO "novojuste" — MÉTODO Down():
        //
        // Remove as colunas Ativo e FotoPath da tabela UsuarioCaixa, voltando ao
        // modelo anterior (sem o controle lógico de usuário ativo e sem a foto).
        // ==========================================================================
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "UsuarioCaixa");

            migrationBuilder.DropColumn(
                name: "FotoPath",
                table: "UsuarioCaixa");
        }
    }
}
