using PDVStore.Helpers;
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
        private Panel pnlGraficoProdutos = null!;
        private Panel pnlGraficoVendas = null!;

        // CONSTRUTOR
        // O que faz: injeta o DashboardViewModel e o RelatorioService e monta a interface.
        // Por que existe: separa a UI (esta forma) da lógica de agregação (ViewModel), o
        //   que torna o carregamento assíncrono e as exportações mais testáveis.
        // Dependências: recebe DashboardViewModel e RelatorioService; no Load dispara
        //   CarregarAsync() para apresentar os dados do período vigente.
        public frmDashboard(DashboardViewModel viewModel, RelatorioService relatorioService)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _relatorioService = relatorioService ?? throw new ArgumentNullException(nameof(relatorioService));
            BuildUI();
            Load += async (_, _) => await CarregarAsync();
        }

        // O que faz: cria os filtros de data, totais, grids e painéis de gráfico em código.
        // Por que existe: o dashboard resume o desempenho do PDV (faturamento, itens mais
        //   vendidos, alertas de estoque) numa única tela; montagem 100% em código.
        // Dependências: cria dgvMaisVendidos, dgvAlertasEstoque, os dois Painel com eventos
        //   Paint (PnlGraficoProdutos_Paint, PnlGraficoVendas_Paint) e os botões de exportar.
        private void BuildUI()
        {
            Text = "Dashboard & Relatórios";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1350, 900);
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
            var grpTotais = new GroupBox { Text = "Resumo do período", Location = new Point(15, 62), Size = new Size(1290, 110) };

            lblTotal = new Label { Text = "Total de vendas: R$ 0,00", Location = new Point(15, 28), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.DarkGreen };
            lblQtdVendas = new Label { Text = "Quantidade de vendas: 0", Location = new Point(15, 60), AutoSize = true, Font = new Font("Segoe UI", 11F) };
            lblPagamentos = new Label { Text = "Formas de pagamento: -", Location = new Point(400, 28), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = Color.DimGray };

            grpTotais.Controls.AddRange(new Control[] { lblTotal, lblQtdVendas, lblPagamentos });
            Controls.Add(grpTotais);

            // Itens mais vendidos
            var grpVendidos = new GroupBox { Text = "Itens mais vendidos", Location = new Point(15, 185), Size = new Size(640, 430), Anchor = AnchorStyles.Top | AnchorStyles.Left };
            dgvMaisVendidos = new DataGridView
            {
                Location = new Point(10, 24),
                Size = new Size(620, 395),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false
            };
            dgvMaisVendidos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Produto", HeaderText = "Produto", DataPropertyName = "NomeProduto", Width = 260 });
            dgvMaisVendidos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", HeaderText = "Código", DataPropertyName = "CodigoBarras", Width = 110 });
            dgvMaisVendidos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qtd", HeaderText = "Qtd", DataPropertyName = "TotalVendido", Width = 60 });
            dgvMaisVendidos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Valor", HeaderText = "Valor", DataPropertyName = "ValorTotalVendido", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            foreach (DataGridViewColumn c in dgvMaisVendidos.Columns) c.FillWeight = Math.Max(50, c.Width);
            dgvMaisVendidos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grpVendidos.Controls.Add(dgvMaisVendidos);
            Controls.Add(grpVendidos);

            // Alertas de estoque
            var grpAlertas = new GroupBox { Text = "Alertas de estoque mínimo", Location = new Point(665, 185), Size = new Size(330, 430), Anchor = AnchorStyles.Top | AnchorStyles.Left };
            dgvAlertasEstoque = new DataGridView
            {
                Location = new Point(10, 24),
                Size = new Size(310, 395),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false
            };
            dgvAlertasEstoque.Columns.Add(new DataGridViewTextBoxColumn { Name = "Produto", HeaderText = "Produto", DataPropertyName = "Nome", Width = 200 });
            dgvAlertasEstoque.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estoque", HeaderText = "Atual", DataPropertyName = "EstoqueAtual", Width = 50 });
            dgvAlertasEstoque.Columns.Add(new DataGridViewTextBoxColumn { Name = "Minimo", HeaderText = "Mín.", DataPropertyName = "EstoqueMinimo", Width = 50 });
            foreach (DataGridViewColumn c in dgvAlertasEstoque.Columns) c.FillWeight = Math.Max(50, c.Width);
            dgvAlertasEstoque.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grpAlertas.Controls.Add(dgvAlertasEstoque);
            Controls.Add(grpAlertas);

            // Gráfico: entradas e saídas de produtos
            var grpGraficoProdutos = new GroupBox { Text = "Entradas e Saídas de Produtos", Location = new Point(15, 625), Size = new Size(645, 235), Anchor = AnchorStyles.Top | AnchorStyles.Left };
            pnlGraficoProdutos = new Panel { Location = new Point(9, 22), Size = new Size(626, 203), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            pnlGraficoProdutos.Paint += PnlGraficoProdutos_Paint;
            grpGraficoProdutos.Controls.Add(pnlGraficoProdutos);
            Controls.Add(grpGraficoProdutos);

            // Gráfico: vendas por dia
            var grpGraficoVendas = new GroupBox { Text = "Vendas por Dia (R$)", Location = new Point(665, 625), Size = new Size(640, 235), Anchor = AnchorStyles.Top | AnchorStyles.Left };
            pnlGraficoVendas = new Panel { Location = new Point(9, 22), Size = new Size(621, 203), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            pnlGraficoVendas.Paint += PnlGraficoVendas_Paint;
            grpGraficoVendas.Controls.Add(pnlGraficoVendas);
            Controls.Add(grpGraficoVendas);
        }

        // EVENTO - repintura do painel de entradas/saídas
        // O que faz: desenha o gráfico de barras comparando entradas e saídas do período.
        // Por que existe: dá uma leitura visual rápida do giro de estoque (entradas de
        //   compras versus saídas de vendas) nas datas informadas.
        // Dependências: lê _viewModel.MovimentacoesPorDia e usa o helper
        //   GraficoDeBarras.Desenhar(); disparado pelo Windows Forms no Paint.
        private void PnlGraficoProdutos_Paint(object? sender, PaintEventArgs e)
        {
            var dados = _viewModel.MovimentacoesPorDia;
            var rotulos = dados.Select(d => d.Dia.ToString("dd/MM")).ToArray();
            var series = new List<GraficoDeBarras.Serie>
            {
                new() { Nome = "Entradas", Cor = Color.ForestGreen, Valores = dados.Select(d => (decimal)d.Entradas).ToArray() },
                new() { Nome = "Saídas", Cor = Color.Firebrick, Valores = dados.Select(d => (decimal)d.Saidas).ToArray() }
            };
            GraficoDeBarras.Desenhar(e.Graphics, pnlGraficoProdutos.ClientRectangle, "Entradas vs Saídas de Produtos", rotulos, series);
        }

        // EVENTO - repintura do painel de vendas por dia
        // O que faz: desenha o gráfico de barras com o faturamento (R$) de cada dia.
        // Por que existe: mostra a evolução diária das vendas, útil para identificar
        //   picos e baixas no período selecionado (análise gerencial).
        // Dependências: lê _viewModel.VendasPorDia e usa o helper GraficoDeBarras.Desenhar();
        //   disparado pelo Windows Forms no Paint.
        private void PnlGraficoVendas_Paint(object? sender, PaintEventArgs e)
        {
            var dados = _viewModel.VendasPorDia;
            var rotulos = dados.Select(d => d.Dia.ToString("dd/MM")).ToArray();
            var series = new List<GraficoDeBarras.Serie>
            {
                new() { Nome = "Vendas (R$)", Cor = Color.RoyalBlue, Valores = dados.Select(d => d.TotalVendas).ToArray() }
            };
            GraficoDeBarras.Desenhar(e.Graphics, pnlGraficoVendas.ClientRectangle, "Vendas por Dia", rotulos, series);
        }

        // O que faz: consulta o ViewModel com as datas escolhidas e atualiza toda a tela.
        // Por que existe: é o "motor" do dashboard — recalcula totais, quantidades, formas
        //   de pagamento, itens mais vendidos, alertas e força a repintura dos gráficos.
        // Dependências: usa DashboardViewModel.CarregarDadosAsync() (inicio/fim) e atualiza
        //   lblTotal, lblQtdVendas, lblPagamentos, dgvMaisVendidos, dgvAlertasEstoque e painéis.
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

                pnlGraficoProdutos.Invalidate();
                pnlGraficoVendas.Invalidate();
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

        // EVENTO - botão "Exportar PDF"
        // O que faz: gera relatórios (itens mais vendidos + estoque mínimo) em PDF.
        // Por que existe: o lojista precisa de relatórios imprimíveis para arquivo e para
        //   análise fora do sistema; o SaveFileDialog permite escolher o destino.
        // Dependências: usa RelatorioService.GerarRelatorioItensMaisVendidos (com as datas
        //   dos filtros), GerarRelatorioEstoqueMinimo() e ExportarPDF().
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

        // EVENTO - botão "Exportar Excel"
        // O que faz: gera relatórios em planilha Excel (itens vendidos + estoque mínimo).
        // Por que existe: a planilha permite ao gestor filtrar/analisar os dados com
        //   maior liberdade do que o PDF (versátil para o dia a dia do negócio).
        // Dependências: gera os mesmos relatórios do PDF via RelatorioService e chama
        //   ExportarExcel() (estoque sai em arquivo separado "_estoque.xlsx").
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