using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using OfficeOpenXml;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PDVStore.Services
{
    /// <summary>
    /// Exporta o conteúdo atual de qualquer DataGridView para PDF (iText) ou
    /// Excel (EPPlus), exibindo o diálogo de salvamento.
    /// </summary>
    public static class ExportadorService
    {
        public static void ExportarPdf(DataGridView grid, string titulo, string nomeArquivo)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Arquivo PDF|*.pdf",
                FileName = nomeArquivo,
                Title = $"Exportar {titulo} em PDF"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            var caminho = sfd.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                ? sfd.FileName
                : sfd.FileName + ".pdf";

            try
            {
                CriarPdf(grid, titulo, caminho);
                MessageBox.Show("PDF exportado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar PDF: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void ExportarExcel(DataGridView grid, string titulo, string nomeArquivo)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Planilha Excel|*.xlsx",
                FileName = nomeArquivo,
                Title = $"Exportar {titulo} em Excel"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            var caminho = sfd.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? sfd.FileName
                : sfd.FileName + ".xlsx";

            try
            {
                CriarExcel(grid, titulo, caminho);
                MessageBox.Show("Planilha Excel exportada com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar Excel: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static System.Collections.Generic.List<DataGridViewColumn> ColunasExportaveis(DataGridView grid) =>
            grid.Columns
                .Cast<DataGridViewColumn>()
                .Where(c => c.Visible && c is not DataGridViewImageColumn && c is not DataGridViewButtonColumn && c is not DataGridViewCheckBoxColumn)
                .ToList();

        private static void CriarPdf(DataGridView grid, string titulo, string caminho)
        {
            using var writer = new PdfWriter(caminho);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf);

            doc.Add(new Paragraph(titulo).SetFontSize(18).SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(20));

            var colunas = ColunasExportaveis(grid);
            if (colunas.Count == 0) return;

            var table = new Table(UnitValue.CreatePercentArray(colunas.Select(_ => 1f).ToArray()))
                .SetWidth(UnitValue.CreatePercentValue(100));

            foreach (var c in colunas)
                table.AddHeaderCell(new Cell().Add(new Paragraph(c.HeaderText ?? "")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));

            foreach (DataGridViewRow row in grid.Rows)
            {
                foreach (var c in colunas)
                {
                    var valor = row.Cells[c.Index].Value?.ToString() ?? "";
                    table.AddCell(new Cell().Add(new Paragraph(valor)));
                }
            }

            doc.Add(table);

            doc.Add(new Paragraph($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetMarginTop(30));
        }

        private static void CriarExcel(DataGridView grid, string titulo, string caminho)
        {
            ExcelPackage.License.SetNonCommercialPersonal("PDVStore");

            var colunas = ColunasExportaveis(grid);
            using var package = new ExcelPackage(new FileInfo(caminho));
            var ws = package.Workbook.Worksheets.Add(titulo.Length > 31 ? titulo.Substring(0, 31) : titulo);

            for (int i = 0; i < colunas.Count; i++)
                ws.Cells[1, i + 1].Value = colunas[i].HeaderText;

            int linha = 2;
            foreach (DataGridViewRow dr in grid.Rows)
            {
                for (int i = 0; i < colunas.Count; i++)
                    ws.Cells[linha, i + 1].Value = dr.Cells[colunas[i].Index].Value;
                linha++;
            }

            if (colunas.Count > 0)
                ws.Cells[1, 1, linha - 1, colunas.Count].AutoFitColumns();

            package.Save();
        }
    }
}