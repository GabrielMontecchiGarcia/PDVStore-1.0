using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDVStore.Migrations
{
    /// <inheritdoc />
    public partial class novojuste : Migration
    {
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
