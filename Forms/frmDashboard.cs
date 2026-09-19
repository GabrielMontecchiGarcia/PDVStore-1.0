using PDVStore.Services;
using PDVStore.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public class frmDashboard : Form
    {
        private readonly DashboardViewModel _viewModel;
        private readonly RelatorioService _relatorioService;

        private DateTimePicker dtpInicio = null!;
        private DateTimePicker dtpFim = null!;
        private Label lblTotal = null!;
        private Label lblQtdVendas = null!;
        private Label lblPagamentos = null!;
        private DataGridView dgvMaisVendidos = null!;
        private DataGridView dgvAlertasEstoque = null!;

        public frmDashboard(DashboardViewModel viewModel, RelatorioService relatorioService)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _relatorioService = relatorioService ?? throw new ArgumentNullException(nameof(relatorioService));
            BuildUI();
            Load += async (_, _) => await CarregarAsync();
        }

        private void BuildUI()
        {
            Text = "Dashboard & Relatórios";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1050, 640);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            var hoje = DateTime.Today;
            dtpInicio = new DateTimePicker { Location = new Point(80, 18), Size = new Size(130, 26), Format = DateTimePickerFormat.Short, Value = new DateTime(hoje.Year, hoje.Month, 1) };
            dtpFim = new DateTimePicker { Location = new Point(300, 18), Size = new Size(130, 26), Format = DateTimePickerFormat.Short, Value = hoje };

            Controls.Add(new Label { Text = "De:", Location = new Point(15, 22), AutoSize = true });
            Controls.Add(dtpInicio);
            Controls.Add(new Label { Text = "Até:", Location = new Point(235, 22), AutoSize = true });
            Controls.Add(dtpFim);

            var btnAtualizar = new Button { Text = "Atualizar", Location = new Point(455, 14), Size = new Size(110, 32), BackColor = Color.ForestGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnExportarPDF = new Button { Text = "Exportar PDF", Location = new Point(575, 14), Size = new Size(120, 32), FlatStyle = FlatStyle.Flat };
            var btnExportarExcel = new Button { Text = "Exportar Excel", Location = new Point(705, 14), Size = new Size(130, 32), FlatStyle = FlatStyle.Flat };

            Controls.AddRange(new Control[] { btnAtualizar, btnExportarPDF, btnExportarExcel });

            btnAtualizar.Click += async (_, _) => await CarregarAsync();
            btnExportarPDF.Click += ExportarPDF;
            btnExportarExcel.Click += ExportarExcel;

            // Totais
            var grpTotais = new GroupBox { Text = "Resumo do período", Location = new Point(15, 62), Size = new Size(1015, 110) };

            lblTotal = new Label { Text = "Total de vendas: R$ 0,00", Location = new Point(15, 28), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.DarkGreen };
            lblQtdVendas = new Label { Text = "Quantidade de vendas: 0", Location = new Point(15, 60), AutoSize = true, Font = new Font("Segoe UI", 11F) };
            lblPagamentos = new Label { Text = "Formas de pagamento: -", Location = new Point(400, 28), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = Color.DimGray };

            grpTotais.Controls.AddRange(new Control[] { lblTotal, lblQtdVendas, lblPagamentos });
            Controls.Add(grpTotais);

            // Itens mais vendidos
            var grpVendidos = new GroupBox { Text = "Itens mais vendidos", Location = new Point(15, 185), Size = new Size(620, 415) };
            dgvMaisVendidos = new DataGridView
            {
                Location = new Point(10, 24),
                Size = new Size(600, 380),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false
            };
            dgvMaisVendidos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Produto", HeaderText = "Produto", DataPropertyName = "NomeProduto", Width = 260 });
            dgvMaisVendidos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", HeaderText = "Código", DataPropertyName = "CodigoBarras", Width = 110 });
            dgvMaisVendidos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qtd", HeaderText = "Qtd", DataPropertyName = "TotalVendido", Width = 60 });
            dgvMaisVendidos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Valor", HeaderText = "Valor", DataPropertyName = "ValorTotalVendido", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            grpVendidos.Controls.Add(dgvMaisVendidos);
            Controls.Add(grpVendidos);

            // Alertas de estoque
            var grpAlertas = new GroupBox { Text = "Alertas de estoque mínimo", Location = new Point(650, 185), Size = new Size(380, 415) };
            dgvAlertasEstoque = new DataGridView
            {
                Location = new Point(10, 24),
                Size = new Size(360, 380),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false
            };
            dgvAlertasEstoque.Columns.Add(new DataGridViewTextBoxColumn { Name = "Produto", HeaderText = "Produto", DataPropertyName = "Nome", Width = 200 });
            dgvAlertasEstoque.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estoque", HeaderText = "Atual", DataPropertyName = "EstoqueAtual", Width = 50 });
            dgvAlertasEstoque.Columns.Add(new DataGridViewTextBoxColumn { Name = "Minimo", HeaderText = "Mín.", DataPropertyName = "EstoqueMinimo", Width = 50 });
            grpAlertas.Controls.Add(dgvAlertasEstoque);
            Controls.Add(grpAlertas);
        }

        private async Task CarregarAsync()
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                var inicio = dtpInicio.Value.Date;
                var fim = dtpFim.Value.Date.AddDays(1).AddSeconds(-1);

                await _viewModel.CarregarDadosAsync(inicio, fim);

                lblTotal.Text = $"Total de vendas: {_viewModel.TotalPeriodo:C2}";
                lblQtdVendas.Text = $"Quantidade de vendas: {_viewModel.QuantidadeVendas}";

                if (_viewModel.PagamentosPorForma.Count == 0)
                {
                    lblPagamentos.Text = "Formas de pagamento: nenhuma venda no período";
                }
                else
                {
                    var partes = _viewModel.PagamentosPorForma.Select(kv =>
                        $"{kv.Key}: {kv.Value.Quantidade} venda(s) - {kv.Value.Total:C2}");
                    lblPagamentos.Text = "Formas de pagamento:\n" + string.Join("\n", partes);
                }

                dgvMaisVendidos.DataSource = _viewModel.ItensMaisVendidos.ToList();
                dgvAlertasEstoque.DataSource = _viewModel.AlertasEstoque.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dashboard: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void ExportarPDF(object? sender, EventArgs e)
        {
            var inicio = dtpInicio.Value.Date;
            var fim = dtpFim.Value.Date.AddDays(1).AddSeconds(-1);

            var vendidos = _relatorioService.GerarRelatorioItensMaisVendidos(inicio, fim);
            var alertas = _relatorioService.GerarRelatorioEstoqueMinimo();

            using var sfd = new SaveFileDialog
            {
                Filter = "PDF|*.pdf",
                FileName = $"Relatorio_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                Title = "Salvar relatório PDF"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var caminho = sfd.FileName;

                if (!caminho.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    caminho += ".pdf";

                _relatorioService.ExportarPDF(vendidos, caminho);
                MessageBox.Show("Relatório exportado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar PDF: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportarExcel(object? sender, EventArgs e)
        {
            var inicio = dtpInicio.Value.Date;
            var fim = dtpFim.Value.Date.AddDays(1).AddSeconds(-1);

            var vendidos = _relatorioService.GerarRelatorioItensMaisVendidos(inicio, fim);
            var alertas = _relatorioService.GerarRelatorioEstoqueMinimo();

            using var sfd = new SaveFileDialog
            {
                Filter = "Excel|*.xlsx",
                FileName = $"Relatorio_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                Title = "Salvar relatório Excel"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var caminho = sfd.FileName;
                if (!caminho.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    caminho += ".xlsx";

                _relatorioService.ExportarExcel(vendidos, caminho);
                _relatorioService.ExportarExcel(alertas, caminho.Replace(".xlsx", "_estoque.xlsx"));

                MessageBox.Show("Relatório exportado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar Excel: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}