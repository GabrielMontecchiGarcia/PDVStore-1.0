using System.Reflection;
using ClosedXML.Excel;
using PDVStore.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PDVStore.Infrastructure.Exportacao;

/// <summary>
/// Exportação de relatórios em Excel (ClosedXML) e PDF (QuestPDF).
/// Substitui EPPlus/iText do projeto Windows mantendo a mesma finalidade:
/// gerar planilha com cabeçalhos/valores e PDF tabular com título e data.
/// Os nomes das colunas vêm das propriedades públicas do DTO.
/// </summary>
public class ExportacaoService : IExportacaoService
{
    static ExportacaoService()
    {
        // QuestPDF exige a definição de licença; Community atende este projeto.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>Gera a planilha Excel com data de geração, cabeçalhos e dados.</summary>
    public byte[] GerarExcel<T>(IEnumerable<T> dados, string titulo) where T : class
    {
        var lista = dados?.ToList() ?? new List<T>();
        var propriedades = ObterPropriedades<T>();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(NomeAbaValido(titulo));

        // Cabeçalho de contexto, igual ao padrão do RelatorioService original.
        ws.Cell(1, 1).Value = "Relatório Gerado em:";
        ws.Cell(1, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        ws.Cell(2, 1).Value = titulo;
        ws.Cell(2, 1).Style.Font.Bold = true;

        for (int col = 0; col < propriedades.Length; col++)
        {
            ws.Cell(3, col + 1).Value = propriedades[col].Name;
            ws.Cell(3, col + 1).Style.Font.Bold = true;
            ws.Cell(3, col + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int linha = 4;
        foreach (var item in lista)
        {
            for (int col = 0; col < propriedades.Length; col++)
            {
                var valor = propriedades[col].GetValue(item);
                ws.Cell(linha, col + 1).Value = valor switch
                {
                    null => string.Empty,
                    DateTime dt => dt.ToString("dd/MM/yyyy HH:mm:ss"),
                    decimal d => d,
                    _ => valor.ToString()
                };
            }
            linha++;
        }

        if (propriedades.Length > 0)
            ws.Columns(1, propriedades.Length).AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>Gera o PDF tabular (A4 paisagem) com título, cabeçalho e rodapé.</summary>
    public byte[] GerarPdf<T>(IEnumerable<T> dados, string titulo) where T : class
    {
        var lista = dados?.ToList() ?? new List<T>();
        var propriedades = ObterPropriedades<T>();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(t => t.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Text(titulo).FontSize(16).Bold().AlignCenter();
                    col.Item().PaddingTop(4).Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                        .FontSize(8).AlignRight();
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in propriedades)
                            columns.RelativeColumn();
                    });

                    // Cabeçalho
                    table.Header(header =>
                    {
                        foreach (var prop in propriedades)
                        {
                            header.Cell()
                                .Background(Colors.Grey.Lighten2)
                                .BorderBottom(1)
                                .Padding(3)
                                .Text(prop.Name).Bold();
                        }
                    });

                    // Linhas
                    foreach (var item in lista)
                    {
                        foreach (var prop in propriedades)
                        {
                            var valor = prop.GetValue(item);
                            var texto = valor switch
                            {
                                null => string.Empty,
                                DateTime dt => dt.ToString("dd/MM/yyyy HH:mm:ss"),
                                decimal d => d.ToString("N2"),
                                _ => valor.ToString() ?? string.Empty
                            };

                            table.Cell().BorderBottom(0.5f).Padding(3).Text(texto);
                        }
                    }
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static PropertyInfo[] ObterPropriedades<T>() where T : class =>
        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();

    /// <summary>O Excel limita o nome da aba a 31 caracteres.</summary>
    private static string NomeAbaValido(string titulo) =>
        string.IsNullOrWhiteSpace(titulo) ? "Relatorio" :
        (titulo.Length > 31 ? titulo[..31] : titulo)
            .Replace(":", "-").Replace("/", "-").Replace("\\", "-");
}
