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
        // Exporta o conteúdo de um DataGridView para um arquivo PDF. Fluxo: abre o
        // SaveFileDialog (filtro *.pdf) e, se o usuário confirmar, chama CriarPdf
        // para gerar o arquivo. Trata exceções exibindo MessageBox de erro, ou
        // sucesso em caso de êxito. Usado pelas telas que possuem botão
        // "Exportar PDF". Depende da biblioteca iText (PdfWriter/PdfDocument) e
        // do helper ColunasExportaveis para definir quais colunas entram.
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

        // Exporta o conteúdo de um DataGridView para uma planilha Excel (.xlsx),
        // seguindo o mesmo padrão do PDF: SaveFileDialog, chamada a CriarExcel e
        // mensagens de sucesso/erro via MessageBox. Usado pelas telas com botão
        // "Exportar Excel". Depende da biblioteca EPPlus (ExcelPackage) e do
        // helper ColunasExportaveis. Não altera os dados do grid (somente leitura).
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

        // Retorna somente as colunas que DEVEM ser exportadas: exclui colunas
        // invisíveis e colunas de interação (imagem, botão e checkbox, ex.:
        // "Deletar"/"Excluir"). É a regra comum aos PDFs e Excels, garantindo que
        // o arquivo exportado contenha apenas dados úteis. Depende do grid
        // passado pelo chamador (ExportarPdf/ExportarExcel).
        private static System.Collections.Generic.List<DataGridViewColumn> ColunasExportaveis(DataGridView grid) =>
            grid.Columns
                .Cast<DataGridViewColumn>()
                .Where(c => c.Visible && c is not DataGridViewImageColumn && c is not DataGridViewButtonColumn && c is not DataGridViewCheckBoxColumn)
                .ToList();

        // Gera o documento PDF em si usando iText: cria PdfWriter/PdfDocument/Document,
        // adiciona o título centralizado, constrói uma tabela com as colunas
        // exportáveis e as linhas do grid, calcula o rodapé "Gerado em" e salva
        // no caminho. Se não houver colunas exportáveis, encerra sem erro. É
        // chamado SOMENTE por ExportarPdf após o usuário escolher o local.
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

        // Gera a planilha Excel usando EPPlus: define a licença (NonCommercialPersonal,
        // exigência da biblioteca), cria uma planilha (nome limitado a 31
        // caracteres, restrição do Excel), escreve o cabeçalho na linha 1 e os
        // valores das linhas a partir da linha 2, ajusta a largura das colunas
        // (AutoFitColumns) e salva o arquivo. Chamado somente por ExportarExcel.
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