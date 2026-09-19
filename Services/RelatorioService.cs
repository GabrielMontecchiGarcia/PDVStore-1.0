using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using PDVStore.Data;
using PDVStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PDVStore.Services
{
    public class RelatorioService
    {
        private readonly PDVContext _context;

        public RelatorioService(PDVContext context)
        {
            _context = context;
        }

        public List<ItemRelatorio> GerarRelatorioItensMaisVendidos(DateTime inicio, DateTime fim, int topN = 10)
        {
            var query = (from iv in _context.ItensVendas
                         join p in _context.Produtos on iv.ProdutoId equals p.Id
                         join v in _context.Vendas on iv.VendaId equals v.Id
                         where v.Status == "Concluida" && v.DataVenda >= inicio && v.DataVenda <= fim
                         group new { iv, p } by p into g
                         select new ItemRelatorio
                         {
                             NomeProduto = g.Key.Nome,
                             CodigoBarras = g.Key.CodigoBarras,
                             TotalVendido = g.Sum(x => x.iv.Quantidade),
                             ValorTotalVendido = g.Sum(x => x.iv.Quantidade * x.iv.PrecoUnitario),
                             EstoqueAtual = g.Key.Estoque,
                             EstoqueMinimo = g.Key.EstoqueMinimo,
                             StatusMinimo = g.Key.Estoque <= g.Key.EstoqueMinimo ? "Baixo" : "OK"
                         })
                        .OrderByDescending(ir => ir.TotalVendido)
                        .Take(topN)
                        .ToList();

            return query;
        }

        public List<Produto> GerarRelatorioEstoqueMinimo()
        {
            return _context.Produtos
                .AsNoTracking()
                .Where(p => p.Ativo && p.Estoque <= p.EstoqueMinimo)
                .OrderBy(p => p.Nome)
                .ToList();
        }

        /// <summary>
        /// Exporta relatório para PDF usando iText 9.x.
        /// </summary>
        public void ExportarPDF<T>(List<T> dados, string caminho) where T : class
        {
            using (var writer = new PdfWriter(caminho))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                document.Add(new Paragraph("Relatório PDV")
                    .SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20));

                if (dados == null || !dados.Any())
                {
                    document.Add(new Paragraph("Nenhum dado encontrado para o período informado.")
                        .SetFontSize(12));
                    return;
                }

                if (typeof(T) == typeof(ItemRelatorio))
                {
                    var table = new Table(UnitValue.CreatePercentArray(new float[] { 40, 18, 18, 12, 12 }))
                        .SetWidth(UnitValue.CreatePercentValue(100));

                    var headerColor = ColorConstants.LIGHT_GRAY;

                    table.AddHeaderCell(new Cell().Add(new Paragraph("Produto")).SetBackgroundColor(headerColor));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Qtde Vendida")).SetBackgroundColor(headerColor).SetTextAlignment(TextAlignment.CENTER));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Valor Vendido")).SetBackgroundColor(headerColor).SetTextAlignment(TextAlignment.CENTER));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Estoque Atual")).SetBackgroundColor(headerColor).SetTextAlignment(TextAlignment.CENTER));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Status")).SetBackgroundColor(headerColor).SetTextAlignment(TextAlignment.CENTER));

                    foreach (var item in dados.Cast<ItemRelatorio>())
                    {
                        table.AddCell(new Cell().Add(new Paragraph(item.NomeProduto)));
                        table.AddCell(new Cell().Add(new Paragraph(item.TotalVendido.ToString())).SetTextAlignment(TextAlignment.CENTER));
                        table.AddCell(new Cell().Add(new Paragraph(item.ValorTotalVendido.ToString("C2"))).SetTextAlignment(TextAlignment.CENTER));
                        table.AddCell(new Cell().Add(new Paragraph(item.EstoqueAtual.ToString())).SetTextAlignment(TextAlignment.CENTER));

                        var statusCell = new Cell().Add(new Paragraph(item.StatusMinimo)).SetTextAlignment(TextAlignment.CENTER);
                        if (item.StatusMinimo == "Baixo")
                            statusCell.SetFontColor(ColorConstants.RED);
                        table.AddCell(statusCell);
                    }

                    document.Add(table);
                }
                else if (typeof(T) == typeof(Produto))
                {
                    var table = new Table(UnitValue.CreatePercentArray(new float[] { 50, 12, 18, 20 }))
                        .SetWidth(UnitValue.CreatePercentValue(100));

                    var headerColor = ColorConstants.LIGHT_GRAY;

                    table.AddHeaderCell(new Cell().Add(new Paragraph("Produto")).SetBackgroundColor(headerColor));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Código")).SetBackgroundColor(headerColor));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Estoque Atual")).SetBackgroundColor(headerColor).SetTextAlignment(TextAlignment.CENTER));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Status")).SetBackgroundColor(headerColor).SetTextAlignment(TextAlignment.CENTER));

                    foreach (var produto in dados.Cast<Produto>())
                    {
                        bool baixo = produto.Estoque <= produto.EstoqueMinimo;
                        table.AddCell(new Cell().Add(new Paragraph(produto.Nome)));
                        table.AddCell(new Cell().Add(new Paragraph(produto.CodigoBarras ?? "-")));
                        table.AddCell(new Cell().Add(new Paragraph(produto.Estoque.ToString())).SetTextAlignment(TextAlignment.CENTER));

                        var statusCell = new Cell().Add(new Paragraph(baixo ? "BAIXO" : "OK"))
                            .SetTextAlignment(TextAlignment.CENTER);
                        if (baixo)
                            statusCell.SetFontColor(ColorConstants.RED);
                        table.AddCell(statusCell);
                    }

                    document.Add(table);
                }
                else
                {
                    document.Add(new Paragraph($"Relatório de {typeof(T).Name} - {dados.Count} registros"));
                }

                document.Add(new Paragraph($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginTop(30));
            }
        }

        /// <summary>
        /// Exporta uma lista de objetos para planilha Excel (EPPlus 8).
        /// </summary>
        public void ExportarExcel<T>(List<T> dados, string caminho) where T : class
        {
            // Requerido pelo EPPlus 8 (licença não-comercial para desenvolvimento).
            ExcelPackage.License.SetNonCommercialPersonal("PDVStore");

            using (var package = new ExcelPackage(new FileInfo(caminho)))
            {
                var ws = package.Workbook.Worksheets.Add("Relatorio");
                ws.Cells[1, 1].Value = "Relatório Gerado em:";
                ws.Cells[1, 2].Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                if (dados != null && dados.Any())
                {
                    var properties = typeof(T).GetProperties();
                    int row = 3;

                    for (int col = 0; col < properties.Length; col++)
                        ws.Cells[row, col + 1].Value = properties[col].Name;

                    row++;
                    foreach (var item in dados)
                    {
                        for (int col = 0; col < properties.Length; col++)
                        {
                            var valor = properties[col].GetValue(item);
                            ws.Cells[row, col + 1].Value = valor?.ToString();
                        }
                        row++;
                    }

                    ws.Cells[3, 1, row - 1, properties.Length].AutoFitColumns();
                }

                package.Save();
            }
        }

        public async Task<int> GetTotalVendasAsync()
        {
            return await _context.Vendas.CountAsync();
        }
    }

    public class ItemRelatorio
    {
        public string NomeProduto { get; set; }
        public string? CodigoBarras { get; set; }
        public int TotalVendido { get; set; }
        public decimal ValorTotalVendido { get; set; }
        public int EstoqueAtual { get; set; }
        public int EstoqueMinimo { get; set; }
        public string StatusMinimo { get; set; }
    }
}