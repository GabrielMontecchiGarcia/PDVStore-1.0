using PDVStore.Models;
using PDVStore.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public partial class frmEstoque : Form
    {
        private readonly EstoqueService _estoqueService;
        private Produto? _produtoSelecionado;

        public frmEstoque(EstoqueService estoqueService)
        {
            _estoqueService = estoqueService ?? throw new ArgumentNullException(nameof(estoqueService));
            InitializeComponent();
            ConfigurarFormulario();
        }

        private async void Form_Load(object? sender, EventArgs e)
        {
            await CarregarProdutosAsync();
        }

        private async Task CarregarProdutosAsync(string filtro = "")
        {
            try
            {
                var produtos = string.IsNullOrWhiteSpace(filtro)
                    ? await _estoqueService.GetAllAsync()
                    : await _estoqueService.BuscarAsync(filtro);

                dgvProdutos.DataSource = produtos.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar estoque: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigurarFormulario()
        {
            this.Text = "Gestão de Estoque - Entrada / Saída";
            this.StartPosition = FormStartPosition.CenterScreen;

            Load += Form_Load;

            if (cmbTipoMovimento.Items.Count == 0)
                cmbTipoMovimento.Items.AddRange(new string[] { "Entrada", "Saída" });
            cmbTipoMovimento.SelectedIndex = 0;

            dgvProdutos.AutoGenerateColumns = false;

            // Evita duplicar colunas se a configuração rodar mais de uma vez
            if (dgvProdutos.Columns.Count == 0)
            {
                dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", Width = 70 });
                dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", HeaderText = "Código", DataPropertyName = "CodigoBarras", Width = 130 });
                dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "Produto", DataPropertyName = "Nome", Width = 280 });
                dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "EstoqueAtual", HeaderText = "Estoque Atual", DataPropertyName = "EstoqueAtual", Width = 120 });
                dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Preco", HeaderText = "Preço", DataPropertyName = "Preco", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            }
            foreach (DataGridViewColumn c in dgvProdutos.Columns) c.FillWeight = Math.Max(50, c.Width);
            dgvProdutos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var btnExportarPdf = new Button { Text = "Exportar PDF", Location = new Point(590, 136), Size = new Size(110, 29), FlatStyle = FlatStyle.Flat };
            var btnExportarExcel = new Button { Text = "Exportar Excel", Location = new Point(590, 171), Size = new Size(110, 29), FlatStyle = FlatStyle.Flat };
            btnExportarPdf.Click += (_, _) => ExportadorService.ExportarPdf(dgvProdutos, "Gestão de Estoque", $"Estoque_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
            btnExportarExcel.Click += (_, _) => ExportadorService.ExportarExcel(dgvProdutos, "Gestão de Estoque", $"Estoque_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            this.Controls.Add(btnExportarPdf);
            this.Controls.Add(btnExportarExcel);
        }

        private async void btnBuscar_Click(object sender, EventArgs e)
        {
            await CarregarProdutosAsync(txtBuscar.Text.Trim());
        }

        private void cmbTipoMovimento_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnConfirmarMovimento.BackColor = cmbTipoMovimento.Text == "Entrada"
                ? System.Drawing.Color.DarkGreen
                : System.Drawing.Color.DarkRed;
        }

        private void dgvProdutos_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvProdutos.CurrentRow?.DataBoundItem is Produto produto)
            {
                _produtoSelecionado = produto;
                lblProdutoSelecionado.Text = $"Produto Selecionado: {produto.Nome} (Estoque: {produto.EstoqueAtual})";
            }
        }

        private async void btnConfirmarMovimento_ClickAsync(object sender, EventArgs e)
        {
            if (_produtoSelecionado == null)
            {
                MessageBox.Show("Selecione um produto na lista.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtQuantidade.Text, out int quantidade) || quantidade <= 0)
            {
                MessageBox.Show("Informe uma quantidade válida.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string tipo = cmbTipoMovimento.Text;
            string motivo = txtMotivo.Text.Trim();

            try
            {
                bool sucesso;
                if (tipo == "Entrada")
                {
                    sucesso = await _estoqueService.AdicionarEstoqueAsync(_produtoSelecionado.Id, quantidade,
                        string.IsNullOrWhiteSpace(motivo) ? "Entrada manual" : motivo);
                }
                else
                {
                    sucesso = await _estoqueService.BaixarEstoqueAsync(_produtoSelecionado.Id, quantidade,
                        string.IsNullOrWhiteSpace(motivo) ? "Saída manual" : motivo);
                }

                if (sucesso)
                {
                    MessageBox.Show($"{quantidade} unidade(s) de {tipo} realizada com sucesso!", $"{tipo} Confirmada",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await CarregarProdutosAsync(txtBuscar.Text.Trim());
                    txtQuantidade.Clear();
                    txtMotivo.Clear();
                }
                else
                {
                    MessageBox.Show("Não foi possível realizar o movimento. Verifique a quantidade disponível.",
                        "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao realizar movimento: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}