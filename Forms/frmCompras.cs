using PDVStore.Models;
using PDVStore.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public class frmCompras : Form
    {
        private readonly CompraService _compraService;
        private readonly FornecedorService _fornecedorService;
        private readonly EstoqueService _estoqueService;

        private DataGridView dgvCompras = null!;
        private DataGridView dgvItens = null!;
        private ComboBox cmbFornecedor = null!;
        private ComboBox cmbProduto = null!;
        private TextBox txtNota = null!;
        private TextBox txtQtd = null!;
        private TextBox txtCusto = null!;
        private Label lblTotal = null!;
        private Button btnAddItem = null!;
        private Button btnSalvar = null!;
        private Button btnCancelarCompra = null!;

        private List<Fornecedor> _fornecedores = new();
        private List<Produto> _produtos = new();
        private readonly List<ItemCompra> _itens = new();

        public frmCompras(CompraService compraService, FornecedorService fornecedorService, EstoqueService estoqueService)
        {
            _compraService = compraService ?? throw new ArgumentNullException(nameof(compraService));
            _fornecedorService = fornecedorService ?? throw new ArgumentNullException(nameof(fornecedorService));
            _estoqueService = estoqueService ?? throw new ArgumentNullException(nameof(estoqueService));
            BuildUI();
            Load += async (_, _) => await CarregarAsync();
        }

        private void BuildUI()
        {
            Text = "Compras / Entrada de Mercadorias";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1050, 640);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            // Painel de registro (topo)
            var grpRegistro = new GroupBox { Text = "Nova compra", Location = new Point(15, 10), Size = new Size(1005, 210) };

            var lblForne = new Label { Text = "Fornecedor:", Location = new Point(12, 30), AutoSize = true };
            cmbFornecedor = new ComboBox { Location = new Point(130, 26), Size = new Size(320, 28), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblNota = new Label { Text = "Nº nota:", Location = new Point(12, 68), AutoSize = true };
            txtNota = new TextBox { Location = new Point(130, 64), Size = new Size(180, 26) };

            var lblProduto = new Label { Text = "Produto:", Location = new Point(480, 30), AutoSize = true };
            cmbProduto = new ComboBox { Location = new Point(600, 26), Size = new Size(320, 28), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblQtd = new Label { Text = "Qtd:", Location = new Point(480, 68), AutoSize = true };
            txtQtd = new TextBox { Location = new Point(600, 64), Size = new Size(80, 26), Text = "1" };

            var lblCusto = new Label { Text = "Custo unit.:", Location = new Point(700, 68), AutoSize = true };
            txtCusto = new TextBox { Location = new Point(820, 64), Size = new Size(100, 26) };

            btnAddItem = new Button { Text = "Adicionar item", Location = new Point(600, 110), Size = new Size(140, 32), BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            dgvItens = new DataGridView
            {
                Location = new Point(12, 110),
                Size = new Size(560, 90),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false
            };
            dgvItens.Columns.Add(new DataGridViewTextBoxColumn { Name = "Produto", HeaderText = "Produto", DataPropertyName = "Produto.Nome", Width = 240 });
            dgvItens.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qtd", HeaderText = "Qtd", DataPropertyName = "Quantidade", Width = 60 });
            dgvItens.Columns.Add(new DataGridViewTextBoxColumn { Name = "Custo", HeaderText = "Custo", DataPropertyName = "PrecoCusto", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgvItens.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subtotal", HeaderText = "Subtotal", DataPropertyName = "Subtotal", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });

            var lblTotalLabel = new Label { Text = "Total:", Location = new Point(790, 148), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            lblTotal = new Label { Text = "R$ 0,00", Location = new Point(850, 145), AutoSize = true, Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.DarkGreen };

            btnSalvar = new Button { Text = "Salvar compra", Location = new Point(820, 166), Size = new Size(160, 36), BackColor = Color.ForestGreen, ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), FlatStyle = FlatStyle.Flat };

            grpRegistro.Controls.AddRange(new Control[] {
                lblForne, cmbFornecedor, lblNota, txtNota, lblProduto, cmbProduto, lblQtd, txtQtd, lblCusto, txtCusto,
                btnAddItem, dgvItens, lblTotalLabel, lblTotal, btnSalvar
            });
            Controls.Add(grpRegistro);

            btnAddItem.Click += AddItem;
            btnSalvar.Click += async (_, _) => await SalvarAsync();

            // Listagem de compras
            var grpLista = new GroupBox { Text = "Compras registradas", Location = new Point(15, 230), Size = new Size(1005, 360) };

            dgvCompras = new DataGridView
            {
                Location = new Point(10, 30),
                Size = new Size(985, 260),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Nº", DataPropertyName = "Id", Width = 50 });
            dgvCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "Data", HeaderText = "Data", DataPropertyName = "DataCompra", Width = 130, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
            dgvCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fornecedor", HeaderText = "Fornecedor", DataPropertyName = "Fornecedor.Nome", Width = 260 });
            dgvCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nota", HeaderText = "Nº Nota", DataPropertyName = "NumeroNota", Width = 100 });
            dgvCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "Total", DataPropertyName = "ValorTotal", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgvCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", Width = 90 });

            btnCancelarCompra = new Button { Text = "Cancelar compra selecionada", Location = new Point(10, 310), Size = new Size(220, 34), BackColor = Color.Firebrick, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            grpLista.Controls.AddRange(new Control[] { dgvCompras, btnCancelarCompra });
            Controls.Add(grpLista);

            btnCancelarCompra.Click += async (_, _) => await CancelarAsync();
        }

        private async Task CarregarAsync()
        {
            try
            {
                _fornecedores = await _fornecedorService.ListarAsync();
                _produtos = await _estoqueService.GetAllAsync();

                cmbFornecedor.Items.Clear();
                foreach (var f in _fornecedores)
                    cmbFornecedor.Items.Add(f.Nome);

                cmbProduto.Items.Clear();
                foreach (var p in _produtos)
                    cmbProduto.Items.Add($"{p.Nome} (estoque: {p.EstoqueAtual})");

                if (cmbFornecedor.Items.Count > 0) cmbFornecedor.SelectedIndex = 0;
                if (cmbProduto.Items.Count > 0) cmbProduto.SelectedIndex = 0;

                dgvCompras.DataSource = await _compraService.ListarAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar compras: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddItem(object? sender, EventArgs e)
        {
            if (cmbProduto.SelectedIndex < 0) return;

            var produto = _produtos[cmbProduto.SelectedIndex];

            if (!int.TryParse(txtQtd.Text, out int qtd) || qtd <= 0)
            {
                MessageBox.Show("Informe uma quantidade válida.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtCusto.Text, out decimal custo) || custo <= 0)
            {
                MessageBox.Show("Informe um preço de custo válido.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existente = _itens.FirstOrDefault(i => i.ProdutoId == produto.Id);
            if (existente != null)
            {
                existente.Quantidade += qtd;
                existente.Produto = produto;
            }
            else
            {
                _itens.Add(new ItemCompra
                {
                    ProdutoId = produto.Id,
                    Produto = produto,
                    Quantidade = qtd,
                    PrecoCusto = custo
                });
            }

            RefreshItens();
            txtQtd.Text = "1";
            txtCusto.Clear();
        }

        private void RefreshItens()
        {
            dgvItens.DataSource = null;
            dgvItens.DataSource = _itens.ToList();
            lblTotal.Text = _itens.Sum(i => i.Subtotal).ToString("C2");
        }

        private async Task SalvarAsync()
        {
            if (_itens.Count == 0)
            {
                MessageBox.Show("Adicione pelo menos um item à compra.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbFornecedor.SelectedIndex < 0)
            {
                MessageBox.Show("Selecione um fornecedor.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var compra = new Compra
            {
                FornecedorId = _fornecedores[cmbFornecedor.SelectedIndex].Id,
                NumeroNota = txtNota.Text.Trim(),
                UsuarioCaixaId = Session.CurrentUser?.Id ?? 0,
                Itens = _itens.Select(i => new ItemCompra
                {
                    ProdutoId = i.ProdutoId,
                    Quantidade = i.Quantidade,
                    PrecoCusto = i.PrecoCusto
                }).ToList()
            };

            btnSalvar.Enabled = false;
            try
            {
                var registrada = await _compraService.RegistrarCompraAsync(compra);
                MessageBox.Show($"Compra #{registrada.Id} registrada! Estoque atualizado.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _itens.Clear();
                RefreshItens();
                dgvCompras.DataSource = await _compraService.ListarAsync();
                await CarregarProdutosProcessoAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSalvar.Enabled = true;
            }
        }

        private async Task CarregarProdutosProcessoAsync()
        {
            _produtos = await _estoqueService.GetAllAsync();
            cmbProduto.Items.Clear();
            foreach (var p in _produtos)
                cmbProduto.Items.Add($"{p.Nome} (estoque: {p.EstoqueAtual})");
            if (cmbProduto.Items.Count > 0) cmbProduto.SelectedIndex = 0;
        }

        private async Task CancelarAsync()
        {
            if (dgvCompras.CurrentRow?.DataBoundItem is not Compra compra || compra.Status == "Cancelada")
            {
                MessageBox.Show("Selecione uma compra ativa.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"Cancelar a compra #{compra.Id}? O estoque será estornado.",
                "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var ok = await _compraService.CancelarCompraAsync(compra.Id);
            if (ok)
            {
                MessageBox.Show("Compra cancelada e estoque estornado.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                dgvCompras.DataSource = await _compraService.ListarAsync();
            }
            else
            {
                MessageBox.Show("Não foi possível cancelar a compra.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}