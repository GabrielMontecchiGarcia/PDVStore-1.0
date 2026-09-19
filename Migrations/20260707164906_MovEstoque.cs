using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDVStore.Migrations
{
    /// <inheritdoc />
    public partial class MovEstoque : Migration
    {
        // ==========================================================================
        // MIGRAÇÃO "MovEstoque" — MÉTODO Up():
        //
        // Cria a tabela MovimentacoesEstoque, que registra TODAS as entradas e
        // saídas de estoque (vendas, compras, ajustes). Com isso, o saldo do produto
        // (EstoqueAtual) pode ser calculado somando o histórico de movimentações.
        //
        //   1. A tabela guarda: ProdutoId (qual produto), Tipo (Entrada/Saída),
        //      Quantidade, PrecoUnitario, DataMovimentacao, UsuarioId (quem fez),
        //      Motivo e ReferenciaVendaId (opcional, ligando à venda de origem).
        //   2. FKs: ProdutoId -> Produtos e UsuarioId -> UsuarioCaixa, com índices
        //      para acelerar a busca de movimentações por produto/usuário.
        // ==========================================================================
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MovimentacoesEstoque",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<string>(nullable: false),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DataMovimentacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Motivo = table.Column<string>(nullable: true),
                    ReferenciaVendaId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentacoesEstoque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentacoesEstoque_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovimentacoesEstoque_UsuarioCaixa_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "UsuarioCaixa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesEstoque_ProdutoId",
                table: "MovimentacoesEstoque",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesEstoque_UsuarioId",
                table: "MovimentacoesEstoque",
                column: "UsuarioId");
        }

        // ==========================================================================
        // MIGRAÇÃO "MovEstoque" — MÉTODO Down():
        //
        // Desfaz o que o Up() criou: derruba a tabela MovimentacoesEstoque,
        // apagando juntos suas FKs e índices (que acompanham a tabela).
        // ==========================================================================
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimentacoesEstoque");
        }
    }
}
