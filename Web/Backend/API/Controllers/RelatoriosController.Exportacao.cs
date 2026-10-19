using Microsoft.AspNetCore.Mvc;
using PDVStore.API.Common;
using PDVStore.Application.Mappings;
using PDVStore.DTOs;

namespace PDVStore.API.Controllers;

/// <summary>
/// Exportação de relatórios em Excel/PDF (parte complementar do controller).
/// </summary>
public partial class RelatoriosController
{
    /// <summary>
    /// Exporta um relatório em Excel ou PDF.
    /// Tipos: itens-mais-vendidos, estoque-minimo, produtos, clientes,
    /// fornecedores, usuarios, vendas, compras. Formatos: excel, pdf.
    /// </summary>
    [HttpGet("exportar/{tipo}/{formato}")]
    public async Task<IActionResult> Exportar(
        string tipo,
        string formato,
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        [FromQuery] bool apenasAtivos = true,
        CancellationToken ct = default)
    {
        // Estoquista só pode exportar o cadastro de produtos (sua única tela).
        if (!User.IsInRole("Administrador") &&
            !(User.IsInRole("Estoquista") && tipo.Equals("produtos", StringComparison.OrdinalIgnoreCase)))
        {
            return Forbid();
        }

        var (inicioUtc, fimUtc) = _periodo.Converter(inicio, fim);

        byte[] arquivo;
        string titulo;

        switch (tipo.ToLowerInvariant())
        {
            case "itens-mais-vendidos":
                titulo = "Itens Mais Vendidos";
                var itens = await _relatorios.ItensMaisVendidosAsync(inicioUtc, fimUtc, 10, ct);
                arquivo = Gerar(itens, titulo, formato);
                break;

            case "estoque-minimo":
                titulo = "Estoque Mínimo";
                var minimos = (await _relatorios.EstoqueMinimoAsync(ct)).Select(p => p.ToDto()).ToList();
                arquivo = Gerar(minimos, titulo, formato);
                break;

            case "produtos":
                titulo = "Produtos";
                var produtos = (await _produtos.ListarAsync(apenasAtivos, null, ct)).Select(p => p.ToDto()).ToList();
                arquivo = Gerar(produtos, titulo, formato);
                break;

            case "clientes":
                titulo = "Clientes";
                var clientes = (await _clientes.ListarAsync(apenasAtivos, null, ct)).Select(c => c.ToDto()).ToList();
                arquivo = Gerar(clientes, titulo, formato);
                break;

            case "fornecedores":
                titulo = "Fornecedores";
                var fornecedores = (await _fornecedores.ListarAsync(apenasAtivos, null, ct)).Select(f => f.ToDto()).ToList();
                arquivo = Gerar(fornecedores, titulo, formato);
                break;

            case "usuarios":
                titulo = "Usuários";
                var usuarios = (await _usuarios.ListarAsync(false, null, ct)).Select(u => u.ToDto()).ToList();
                arquivo = Gerar(usuarios, titulo, formato);
                break;

            case "vendas":
                titulo = "Vendas";
                var vendas = (await _vendas.ListarPorPeriodoAsync(inicioUtc, fimUtc, null, ct))
                    .Select(v => new
                    {
                        v.Id,
                        Data = v.DataVenda,
                        Cliente = v.Cliente?.Nome ?? "Não identificado",
                        Operador = v.UsuarioCaixa?.Nome,
                        v.FormaPagamento,
                        v.Status,
                        v.ValorTotal,
                        v.Desconto
                    }).ToList();
                arquivo = Gerar(vendas, titulo, formato);
                break;

            case "compras":
                titulo = "Compras";
                var compras = (await _compras.ListarAsync(inicioUtc, fimUtc, ct))
                    .Select(c => new
                    {
                        c.Id,
                        c.DataCompra,
                        Fornecedor = c.Fornecedor?.Nome,
                        c.NumeroNota,
                        c.Status,
                        c.ValorTotal
                    }).ToList();
                arquivo = Gerar(compras, titulo, formato);
                break;

            default:
                return BadRequest(new ErroResposta
                {
                    Status = StatusCodes.Status400BadRequest,
                    Mensagem = "Tipo de relatório inválido.",
                    TraceId = HttpContext.TraceIdentifier
                });
        }

        var extensao = formato.Equals("pdf", StringComparison.OrdinalIgnoreCase) ? "pdf" : "xlsx";
        var contentType = extensao == "pdf"
            ? "application/pdf"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        return File(arquivo, contentType, $"{titulo.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.{extensao}");
    }

    /// <summary>Gera o arquivo no formato solicitado (padrão: Excel).</summary>
    private byte[] Gerar<T>(IEnumerable<T> dados, string titulo, string formato) where T : class =>
        formato.Equals("pdf", StringComparison.OrdinalIgnoreCase)
            ? _exportacao.GerarPdf(dados, titulo)
            : _exportacao.GerarExcel(dados, titulo);
}
