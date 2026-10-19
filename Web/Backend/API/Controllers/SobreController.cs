using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PDVStore.API.Controllers;

/// <summary>
/// Informações do sistema (equivalente ao frmSobre). Disponível a qualquer
/// usuário autenticado; não expõe segredos nem credenciais.
/// </summary>
[ApiController]
[Route("api/sobre")]
[Authorize]
public class SobreController : ControllerBase
{
    [HttpGet]
    public IActionResult Obter() => Ok(new
    {
        nome = "PDVStore Web",
        versao = typeof(SobreController).Assembly.GetName().Version?.ToString() ?? "1.0.0",
        descricao = "Sistema de Ponto de Venda (PDV) para pequenas lojas e comércios de bairro.",
        tecnologias = new[] { "React", "C# / .NET 8", "SQL Server", "Entity Framework Core" },
        funcionalidades = new[]
        {
            "Vendas no PDV com exigência de caixa aberto",
            "Formas de pagamento: Dinheiro, PIX, Cartão de Crédito, Cartão de Débito e Fiado",
            "Venda fiada com limite de crédito e saldo devedor",
            "Cadastro de produtos com estoque mínimo",
            "Gestão de estoque com entrada/saída e histórico",
            "Fornecedores e compras com entrada automática no estoque",
            "Abertura e fechamento de caixa com sangria",
            "Dashboard e relatórios com exportação PDF/Excel",
            "Usuários e perfis: Administrador, Operador (Caixa) e Estoquista"
        },
        licenca = "MIT",
        autoria = "Projeto acadêmico PDVStore — versão web derivada do sistema Windows de referência"
    });
}
