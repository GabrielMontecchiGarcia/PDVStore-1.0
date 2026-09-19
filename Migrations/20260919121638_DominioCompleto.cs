using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDVStore.Migrations
{
    /// <inheritdoc />
    public partial class DominioCompleto : Migration
    {
        // ==========================================================================
        // MIGRAÇÃO "DominioCompleto" — MÉTODO Up():
        //
        // Esta é a maior evolução do projeto: reorganiza o banco para o DOMÍNIO
        // COMPLETO do PDV (clientes, caixa aberto/fechado, compras e fornecedores).
        //
        // 1. Refatora o modelo base:
        //    - UsuarioCaixa -> Usuarios; ItemVenda -> ItensVendas;
        //    - Produtos.EstoqueAtual -> EstoqueMinimo; PKs e índices renomeados.
        // 2. Remoções: a tabela FormaPagamentos (pagamento virou texto em Vendas).
        // 3. Novas colunas: Vendas.ClienteId (quem comprou) e Produtos.PrecoCusto
        //    (para calcular margem); MovimentacoesEstoque.UsuarioId vira nullable.
        // 4. Novas tabelas:
        //    - Caixas: abertura, fechamento, valores e estado do caixa;
        //    - Clientes: dados do cliente, limite de crédito e SaldoDevedor;
        //    - Fornecedores + Compras + ItensCompras: reposição de estoque.
        // 5. Seeds (caixa fechado inicial e Admin atualizado), índices e FKs
        //    unindo todo o novo domínio.
        // ==========================================================================
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemVenda_Produtos_ProdutoId",
                table: "ItemVenda");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemVenda_Vendas_VendaId",
                table: "ItemVenda");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimentacoesEstoque_Produtos_ProdutoId",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimentacoesEstoque_UsuarioCaixa_UsuarioId",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendas_UsuarioCaixa_UsuarioCaixaId",
                table: "Vendas");

            migrationBuilder.DropTable(
                name: "FormaPagamentos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsuarioCaixa",
                table: "UsuarioCaixa");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemVenda",
                table: "ItemVenda");

            migrationBuilder.RenameTable(
                name: "UsuarioCaixa",
                newName: "Usuarios");

            migrationBuilder.RenameTable(
                name: "ItemVenda",
                newName: "ItensVendas");

            migrationBuilder.RenameColumn(
                name: "EstoqueAtual",
                table: "Produtos",
                newName: "EstoqueMinimo");

            migrationBuilder.RenameIndex(
                name: "IX_ItemVenda_VendaId",
                table: "ItensVendas",
                newName: "IX_ItensVendas_VendaId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemVenda_ProdutoId",
                table: "ItensVendas",
                newName: "IX_ItensVendas_ProdutoId");

            migrationBuilder.AddColumn<int>(
                name: "ClienteId",
                table: "Vendas",
type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecoCusto",
                table: "Produtos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "MovimentacoesEstoque",
type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItensVendas",
                table: "ItensVendas",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Caixas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioCaixaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Abertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Fechamento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValorInicial = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorFinal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Sangria = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caixas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Caixas_Usuarios_UsuarioCaixaId",
                        column: x => x.UsuarioCaixaId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(nullable: false),
                    CpfCnpj = table.Column<string>(nullable: true),
                    Telefone = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    Endereco = table.Column<string>(nullable: true),
                    CadastradoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    LimiteCredito = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaldoDevedor = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fornecedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(nullable: false),
                    Cnpj = table.Column<string>(nullable: true),
                    Telefone = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fornecedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Compras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FornecedorId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioCaixaId = table.Column<int>(type: "INTEGER", nullable: false),
                    DataCompra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumeroNota = table.Column<string>(nullable: true),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compras_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Compras_Usuarios_UsuarioCaixaId",
                        column: x => x.UsuarioCaixaId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensCompras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompraId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecoCusto = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensCompras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensCompras_Compras_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensCompras_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Caixas",
                columns: new[] { "Id", "Abertura", "Fechamento", "Sangria", "Status", "UsuarioCaixaId", "ValorFinal", "ValorInicial" },
                values: new object[] { 1, new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Fechado", 1, null, 0m });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Permissao", "SenhaHash" },
                values: new object[] { 2, "$2a$11$ZBR/L6.DiCjgBWjvnJOsMeVfeHtigReLCpU15E8R8FK/Kgn8n1nZa" });

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_CaixaId",
                table: "Vendas",
                column: "CaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_ClienteId",
                table: "Vendas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_UsuarioCaixaId",
                table: "Caixas",
                column: "UsuarioCaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_FornecedorId",
                table: "Compras",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_UsuarioCaixaId",
                table: "Compras",
                column: "UsuarioCaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensCompras_CompraId",
                table: "ItensCompras",
                column: "CompraId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensCompras_ProdutoId",
                table: "ItensCompras",
                column: "ProdutoId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItensVendas_Produtos_ProdutoId",
                table: "ItensVendas",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ItensVendas_Vendas_VendaId",
                table: "ItensVendas",
                column: "VendaId",
                principalTable: "Vendas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimentacoesEstoque_Produtos_ProdutoId",
                table: "MovimentacoesEstoque",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimentacoesEstoque_Usuarios_UsuarioId",
                table: "MovimentacoesEstoque",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendas_Caixas_CaixaId",
                table: "Vendas",
                column: "CaixaId",
                principalTable: "Caixas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendas_Clientes_ClienteId",
                table: "Vendas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendas_Usuarios_UsuarioCaixaId",
                table: "Vendas",
                column: "UsuarioCaixaId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        // ==========================================================================
        // MIGRAÇÃO "DominioCompleto" — MÉTODO Down():
        //
        // Reverte a evolução completa: remove as FKs e tabelas novas (Caixas,
        // Clientes, Fornecedores, Compras, ItensCompras), volta as colunas
        // (ClienteId, PrecoCusto, UsuarioId não-nullable), renomeia tabelas/
        // colunas/índices de volta (Usuarios -> UsuarioCaixa, ItensVendas ->
        // ItemVenda, etc.) e restaura a tabela FormaPagamentos com o seed do
        // Admin original.
        // ==========================================================================
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItensVendas_Produtos_ProdutoId",
                table: "ItensVendas");

            migrationBuilder.DropForeignKey(
                name: "FK_ItensVendas_Vendas_VendaId",
                table: "ItensVendas");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimentacoesEstoque_Produtos_ProdutoId",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimentacoesEstoque_Usuarios_UsuarioId",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendas_Caixas_CaixaId",
                table: "Vendas");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendas_Clientes_ClienteId",
                table: "Vendas");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendas_Usuarios_UsuarioCaixaId",
                table: "Vendas");

            migrationBuilder.DropTable(
                name: "Caixas");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "ItensCompras");

            migrationBuilder.DropTable(
                name: "Compras");

            migrationBuilder.DropTable(
                name: "Fornecedores");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_CaixaId",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_ClienteId",
                table: "Vendas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItensVendas",
                table: "ItensVendas");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "PrecoCusto",
                table: "Produtos");

            migrationBuilder.RenameTable(
                name: "Usuarios",
                newName: "UsuarioCaixa");

            migrationBuilder.RenameTable(
                name: "ItensVendas",
                newName: "ItemVenda");

            migrationBuilder.RenameColumn(
                name: "EstoqueMinimo",
                table: "Produtos",
                newName: "EstoqueAtual");

            migrationBuilder.RenameIndex(
                name: "IX_ItensVendas_VendaId",
                table: "ItemVenda",
                newName: "IX_ItemVenda_VendaId");

            migrationBuilder.RenameIndex(
                name: "IX_ItensVendas_ProdutoId",
                table: "ItemVenda",
                newName: "IX_ItemVenda_ProdutoId");

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "MovimentacoesEstoque",
type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsuarioCaixa",
                table: "UsuarioCaixa",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemVenda",
                table: "ItemVenda",
                column: "Id");

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

            migrationBuilder.UpdateData(
                table: "UsuarioCaixa",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Permissao", "SenhaHash" },
                values: new object[] { 1, "$2a$11$abcdefghijklmnopqrstuv" });

            migrationBuilder.AddForeignKey(
                name: "FK_ItemVenda_Produtos_ProdutoId",
                table: "ItemVenda",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemVenda_Vendas_VendaId",
                table: "ItemVenda",
                column: "VendaId",
                principalTable: "Vendas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimentacoesEstoque_Produtos_ProdutoId",
                table: "MovimentacoesEstoque",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimentacoesEstoque_UsuarioCaixa_UsuarioId",
                table: "MovimentacoesEstoque",
                column: "UsuarioId",
                principalTable: "UsuarioCaixa",
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
    }
}
