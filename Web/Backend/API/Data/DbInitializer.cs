using PDVStore.Domain.Entities;
using PDVStore.Repository.Context;

namespace PDVStore.API.Data;

/// <summary>
/// Popula dados de demonstração APENAS quando o provider InMemory está ativo
/// (Database:Provider=InMemory). Não é executado no SQL Server, preservando a
/// base real. Útil para avaliar a interface e validar os fluxos sem instalar banco.
/// </summary>
public static class DbInitializer
{
    public static async Task SemearAsync(PDVStoreDbContext db)
    {
        // Produtos de exemplo (somente se ainda não houver nenhum).
        if (!db.Produtos.Any())
        {
            db.Produtos.AddRange(
                new Produto
                {
                    Nome = "Café Torrado 500g",
                    CodigoBarras = "7891000100101",
                    Preco = 18.90m,
                    PrecoCusto = 12.50m,
                    Estoque = 20,
                    EstoqueMinimo = 5,
                    Categoria = "Mercearia",
                    Descricao = "Café torrado e moído, embalagem de 500g",
                    Ativo = true
                },
                new Produto
                {
                    Nome = "Arroz Branco 5kg",
                    CodigoBarras = "7891000100102",
                    Preco = 28.50m,
                    PrecoCusto = 21.00m,
                    Estoque = 15,
                    EstoqueMinimo = 4,
                    Categoria = "Mercearia",
                    Descricao = "Arroz branco tipo 1, pacote de 5kg",
                    Ativo = true
                },
                new Produto
                {
                    Nome = "Leite Integral 1L",
                    CodigoBarras = "7891000100103",
                    Preco = 6.49m,
                    PrecoCusto = 4.80m,
                    Estoque = 30,
                    EstoqueMinimo = 10,
                    Categoria = "Laticínios",
                    Descricao = "Leite integral UHT, caixa de 1 litro",
                    Ativo = true
                },
                new Produto
                {
                    Nome = "Sabão em Pó 1kg",
                    CodigoBarras = "7891000100104",
                    Preco = 14.90m,
                    PrecoCusto = 9.90m,
                    Estoque = 3,
                    EstoqueMinimo = 5,
                    Categoria = "Limpeza",
                    Descricao = "Sabão em pó para lavagem de roupas, 1kg",
                    Ativo = true
                },
                new Produto
                {
                    Nome = "Refrigerante 2L",
                    CodigoBarras = "7891000100105",
                    Preco = 9.99m,
                    PrecoCusto = 6.50m,
                    Estoque = 12,
                    EstoqueMinimo = 6,
                    Categoria = "Bebidas",
                    Descricao = "Refrigerante de cola, garrafa de 2 litros",
                    Ativo = true
                });
        }

        if (!db.Clientes.Any())
        {
            db.Clientes.AddRange(
                new Cliente
                {
                    Nome = "Maria Silva",
                    CpfCnpj = "529.982.247-25",
                    Telefone = "(69) 99999-1111",
                    Email = "maria.silva@exemplo.com",
                    Endereco = "Rua das Flores, 100 - Ji-Paraná/RO",
                    LimiteCredito = 300m,
                    SaldoDevedor = 0m,
                    Ativo = true,
                    CadastradoEm = DateTime.UtcNow
                },
                new Cliente
                {
                    Nome = "Comércio Bom Preço",
                    CpfCnpj = "11.222.333/0001-81",
                    Telefone = "(69) 3333-2222",
                    Email = "contato@bompreco.com",
                    Endereco = "Av. Brasil, 500 - Ji-Paraná/RO",
                    LimiteCredito = 1000m,
                    SaldoDevedor = 0m,
                    Ativo = true,
                    CadastradoEm = DateTime.UtcNow
                });
        }

        if (!db.Fornecedores.Any())
        {
            db.Fornecedores.Add(new Fornecedor
            {
                Nome = "Distribuidora Central",
                Cnpj = "11.222.333/0001-81",
                InscricaoEstadual = "11004249011401",
                Telefone = "(69) 3333-4444",
                Email = "compras@central.com",
                Ativo = true
            });
        }

        await db.SaveChangesAsync();
    }
}
