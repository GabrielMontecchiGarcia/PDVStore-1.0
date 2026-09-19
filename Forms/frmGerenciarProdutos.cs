using PDVStore.Models;
using PDVStore.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public class frmGerenciarProdutos : Form
    {
        private readonly EstoqueService _estoqueService;
        private Produto? _produtoSelecionado;

        private DataGridView dgvProdutos = null!;
        private TextBox txtCodigo = null!;
        private TextBox txtNomeProduto = null!;
        private TextBox txtPreco = null!;
        private TextBox txtPrecoCusto = null!;
        private TextBox txtEstoque = null!;
        private TextBox txtEstoqueMinimo = null!;
        private TextBox txtCategoria = null!;
        private TextBox txtDescricao = null!;
        private Button btnSalvar = null!;
        private Button btnNovo = null!;
        private Button btnExcluir = null!;
        private Button btnAtualizarEstoque = null!;
        private Button btnRefresh = null!;

        public frmGerenciarProdutos(EstoqueService estoqueService)
        {
            _estoqueService = estoqueService ?? throw new ArgumentNullException(nameof(estoqueService));
            InitializeComponent();
            ConfigurarGrid();
            Load += async (_, _) => await CarregarProdutosAsync();
        }

        private void InitializeComponent()
        {
            Text = "Gerenciar Produtos";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1180, 560);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            dgvProdutos = new DataGridView { Location = new Point(500, 20), Size = new Size(660, 460), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, ReadOnly = true, AllowUserToAddRows = false, AutoGenerateColumns = false };
            btnRefresh = new Button { Text = "Refresh", Location = new Point(1045, 492), Size = new Size(75, 28) };

            int y = 16;
            int dy = 47;

            var lbl1 = new Label { Text = "Código Barras:", Location = new Point(15, y), AutoSize = true };
            txtCodigo = new TextBox { Location = new Point(140, y - 4), Size = new Size(180, 26) };
            var lbl2 = new Label { Text = "Produto:", Location = new Point(15, y += dy), AutoSize = true };
            txtNomeProduto = new TextBox { Location = new Point(140, y - 4), Size = new Size(330, 26) };
            var lbl3 = new Label { Text = "Preço Venda:", Location = new Point(15, y += dy), AutoSize = true };
            txtPreco = new TextBox { Location = new Point(140, y - 4), Size = new Size(120, 26) };
            var lbl3b = new Label { Text = "Preço Custo:", Location = new Point(15, y += dy), AutoSize = true };
            txtPrecoCusto = new TextBox { Location = new Point(140, y - 4), Size = new Size(120, 26) };
            var lbl4 = new Label { Text = "Estoque:", Location = new Point(15, y += dy), AutoSize = true };
            txtEstoque = new TextBox { Location = new Point(140, y - 4), Size = new Size(120, 26) };
            var lbl4b = new Label { Text = "Estoque Mínimo:", Location = new Point(15, y += dy), AutoSize = true };
            txtEstoqueMinimo = new TextBox { Location = new Point(140, y - 4), Size = new Size(120, 26), Text = "0" };
            var lbl5 = new Label { Text = "Categoria:", Location = new Point(15, y += dy), AutoSize = true };
            txtCategoria = new TextBox { Location = new Point(140, y - 4), Size = new Size(180, 26) };
            var lbl6 = new Label { Text = "Descrição:", Location = new Point(15, y += dy), AutoSize = true };
            txtDescricao = new TextBox { Location = new Point(140, y - 4), Size = new Size(330, 60), Multiline = true };

            btnSalvar = new Button { Text = "Salvar", Location = new Point(140, y + 70), Size = new Size(100, 30), BackColor = Color.ForestGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnNovo = new Button { Text = "Novo", Location = new Point(250, y + 70), Size = new Size(90, 30), FlatStyle = FlatStyle.Flat };
            btnExcluir = new Button { Text = "Excluir", Location = new Point(350, y + 70), Size = new Size(90, 30), BackColor = Color.Firebrick, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAtualizarEstoque = new Button { Text = "Estoque", Location = new Point(140, y + 110), Size = new Size(90, 30), FlatStyle = FlatStyle.Flat };

            var btnExportarPdf = new Button { Text = "Exportar PDF", Location = new Point(250, y + 110), Size = new Size(110, 30), FlatStyle = FlatStyle.Flat };
            var btnExportarExcel = new Button { Text = "Exportar Excel", Location = new Point(372, y + 110), Size = new Size(118, 30), FlatStyle = FlatStyle.Flat };

            btnSalvar.Click += async (_, _) => await SalvarAsync();
            btnNovo.Click += (_, _) => LimparCampos();
            btnExcluir.Click += async (_, _) => await ExcluirAsync();
            btnAtualizarEstoque.Click += (_, _) => MessageBox.Show(
                "Para ajustar estoque, use a tela de Estoque (Entrada/Saída).", "Ajuste de Estoque",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnRefresh.Click += async (_, _) => await CarregarProdutosAsync();
            btnExportarPdf.Click += (_, _) => ExportadorService.ExportarPdf(dgvProdutos, "Gerenciar Produtos", $"Produtos_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
            btnExportarExcel.Click += (_, _) => ExportadorService.ExportarExcel(dgvProdutos, "Gerenciar Produtos", $"Produtos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");

            dgvProdutos.SelectionChanged += (_, _) => SelecionarProduto();

            Controls.AddRange(new Control[] {
                dgvProdutos, btnRefresh,
                lbl1, txtCodigo, lbl2, txtNomeProduto, lbl3, txtPreco, lbl3b, txtPrecoCusto,
                lbl4, txtEstoque, lbl4b, txtEstoqueMinimo, lbl5, txtCategoria, lbl6, txtDescricao,
                btnSalvar, btnNovo, btnExcluir, btnAtualizarEstoque, btnExportarPdf, btnExportarExcel
            });
        }

        private void ConfigurarGrid()
        {
            dgvProdutos.Columns.Clear();
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", Width = 50 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", HeaderText = "Código", DataPropertyName = "CodigoBarras", Width = 110 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "Nome do Produto", DataPropertyName = "Nome", Width = 230 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Preco", HeaderText = "Preço", DataPropertyName = "Preco", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Custo", HeaderText = "Custo", DataPropertyName = "PrecoCusto", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estoque", HeaderText = "Estoque", DataPropertyName = "EstoqueAtual", Width = 70 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "EstoqueMinimo", HeaderText = "Mín.", DataPropertyName = "EstoqueMinimo", Width = 55 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Categoria", HeaderText = "Categoria", DataPropertyName = "Categoria", Width = 100 });
            foreach (DataGridViewColumn c in dgvProdutos.Columns) c.FillWeight = Math.Max(50, c.Width);
            dgvProdutos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private async Task CarregarProdutosAsync()
        {
            try
            {
                var produtos = await _estoqueService.GetAllAsync();
                dgvProdutos.DataSource = produtos.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Falha carregando produtos: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelecionarProduto()
        {
            if (dgvProdutos.CurrentRow?.DataBoundItem is Produto produto)
            {
                _produtoSelecionado = produto;
                PreencherCampos(produto);
            }
        }

        private void PreencherCampos(Produto produto)
        {
            txtCodigo.Text = produto.CodigoBarras;
            txtNomeProduto.Text = produto.Nome;
            txtPreco.Text = produto.Preco.ToString("0.00");
            txtPrecoCusto.Text = produto.PrecoCusto.ToString("0.00");
            txtEstoque.Text = produto.EstoqueAtual.ToString();
            txtEstoqueMinimo.Text = produto.EstoqueMinimo.ToString();
            txtCategoria.Text = produto.Categoria;
            txtDescricao.Text = produto.Descricao;
        }

        private async Task SalvarAsync()
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text))
            {
                MessageBox.Show("Código de barras é obrigatório.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNomeProduto.Text))
            {
                MessageBox.Show("Nome do produto é obrigatório.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtPreco.Text, out decimal preco) || preco < 0)
            {
                MessageBox.Show("Informe um preço de venda válido.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var produto = _produtoSelecionado ?? new Produto { Ativo = true };

                produto.CodigoBarras = txtCodigo.Text.Trim();
                produto.Nome = txtNomeProduto.Text.Trim();
                produto.Preco = preco;
                produto.PrecoCusto = decimal.TryParse(txtPrecoCusto.Text, out decimal custo) ? Math.Max(0, custo) : 0;
                produto.Estoque = int.TryParse(txtEstoque.Text, out int estoque) ? Math.Max(0, estoque) : 0;
                produto.EstoqueMinimo = int.TryParse(txtEstoqueMinimo.Text, out int minimo) ? Math.Max(0, minimo) : 0;
                produto.Categoria = txtCategoria.Text.Trim();
                produto.Descricao = txtDescricao.Text.Trim();

                if (produto.Id == 0)
                    await _estoqueService.AddAsync(produto);
                else
                    await _estoqueService.UpdateAsync(produto);

                MessageBox.Show("Produto salvo com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await CarregarProdutosAsync();
                LimparCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ExcluirAsync()
        {
            if (_produtoSelecionado == null)
            {
                MessageBox.Show("Selecione um produto para excluir.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"Deseja desativar o produto '{_produtoSelecionado.Nome}'?",
                "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            await _estoqueService.DeleteAsync(_produtoSelecionado.Id);
            MessageBox.Show("Produto desativado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await CarregarProdutosAsync();
            LimparCampos();
        }

        private void LimparCampos()
        {
            txtCodigo.Clear();
            txtNomeProduto.Clear();
            txtPreco.Clear();
            txtPrecoCusto.Clear();
            txtEstoque.Clear();
            txtEstoqueMinimo.Text = "0";
            txtCategoria.Clear();
            txtDescricao.Clear();
            _produtoSelecionado = null;
            txtCodigo.Focus();
        }
    }
}