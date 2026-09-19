using PDVLoja.Services;
using PDVStore.Models;
using PDVStore.Services;
using PDVStore.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public class frmPDV : Form
    {
        private readonly VendaService _vendaService;
        private readonly EstoqueService _estoqueService;
        private readonly CaixaService _caixaService;
        private readonly ClienteService _clienteService;

        private readonly PDVViewModel _viewModel = new();

        private DataGridView dgvProdutos = null!;
        private DataGridView dgvItens = null!;
        private TextBox txtBuscaProduto = null!;
        private TextBox txtQuantidade = null!;
        private TextBox txtDesconto = null!;
        private TextBox txtValorRecebido = null!;
        private Label lblValorTroco = null!;
        private Label lblTotal = null!;
        private Label lblUsuarioLogado = null!;
        private Label lblCaixaStatus = null!;
        private ComboBox cmbFormaPagamento = null!;
        private ComboBox cmbCliente = null!;
        private Button btnFinalizar = null!;

        private List<Cliente> _clientes = new();
        private List<Produto> _produtos = new();

        public frmPDV(VendaService vendaService, EstoqueService estoqueService,
                      CaixaService caixaService, ClienteService clienteService)
        {
            _vendaService = vendaService ?? throw new ArgumentNullException(nameof(vendaService));
            _estoqueService = estoqueService ?? throw new ArgumentNullException(nameof(estoqueService));
            _caixaService = caixaService ?? throw new ArgumentNullException(nameof(caixaService));
            _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));

            BuildUI();
            Load += OnLoad;
        }

        private async void OnLoad(object? sender, EventArgs e)
        {
            try
            {
                lblUsuarioLogado.Text = $"Operador: {Session.CurrentUser?.Nome ?? "-"}";

                var caixa = await _caixaService.ObterCaixaAbertoAsync();
                if (caixa == null)
                {
                    lblCaixaStatus.Text = "Caixa: fechado. Abra o caixa para vender.";
                    btnFinalizar.Enabled = false;
                }
                else
                {
                    lblCaixaStatus.Text = $"Caixa #{caixa.Id} aberto desde {caixa.Abertura:dd/MM HH:mm}.";
                }

                await CarregarProdutosAsync();
                await CarregarClientesAsync();
                PreencherFormasPagamento();

                if (cmbFormaPagamento.Items.Count > 0)
                    cmbFormaPagamento.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar PDV: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BuildUI()
        {
            Text = "PDV - Ponto de Venda";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            lblUsuarioLogado = new Label { Text = "Operador: -", AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Location = new Point(20, 10) };
            Controls.Add(lblUsuarioLogado);

            lblCaixaStatus = new Label { Text = "Caixa: -", AutoSize = true, Location = new Point(20, 38), ForeColor = Color.DimGray };
            Controls.Add(lblCaixaStatus);

            // ===== Painel Produtos (esquerda) =====
            var grpProdutos = new GroupBox { Text = "Produtos", Location = new Point(20, 70), Size = new Size(610, 560) };

            var lblBusca = new Label { Text = "Buscar:", Location = new Point(12, 28), AutoSize = true };
            txtBuscaProduto = new TextBox { Location = new Point(90, 25), Size = new Size(300, 26) };
            txtBuscaProduto.KeyDown += async (_, args) => { if (args.KeyCode == Keys.Enter) await BuscarAsync(); };

            var btnBuscar = new Button { Text = "Buscar", Location = new Point(400, 24), Size = new Size(90, 28) };
            btnBuscar.Click += async (_, _) => await BuscarAsync();

            var lblQtd = new Label { Text = "Qtd:", Location = new Point(12, 68), AutoSize = true };
            txtQuantidade = new TextBox { Location = new Point(90, 64), Size = new Size(100, 26), Text = "1" };

            var btnAdicionar = new Button { Text = "Adicionar (Enter)", Location = new Point(400, 62), Size = new Size(200, 30), BackColor = Color.ForestGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAdicionar.Click += AdicionarItem;

            dgvProdutos = new DataGridView
            {
                Location = new Point(10, 100),
                Size = new Size(590, 445),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false
            };
            ConfigureProdutosGrid();

            grpProdutos.Controls.AddRange(new Control[] { lblBusca, txtBuscaProduto, btnBuscar, lblQtd, txtQuantidade, btnAdicionar, dgvProdutos });
            Controls.Add(grpProdutos);

            // ===== Painel Carrinho / Pagamento (direita) =====
            var grpCarrinho = new GroupBox { Text = "Carrinho", Location = new Point(650, 70), Size = new Size(700, 350) };

            dgvItens = new DataGridView
            {
                Location = new Point(10, 24),
                Size = new Size(680, 250),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false
            };
            ConfigureItensGrid();

            var btnRemover = new Button { Text = "Remover item", Location = new Point(10, 286), Size = new Size(120, 30), BackColor = Color.Firebrick, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnRemover.Click += RemoverItem;

            var btnLimpar = new Button { Text = "Limpar venda", Location = new Point(140, 286), Size = new Size(120, 30), BackColor = Color.DimGray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLimpar.Click += LimparVenda;

            grpCarrinho.Controls.AddRange(new Control[] { dgvItens, btnRemover, btnLimpar });

            // ===== Pagamento =====
            var grpPagamento = new GroupBox { Text = "Pagamento", Location = new Point(650, 430), Size = new Size(700, 200) };

            var lblForma = new Label { Text = "Forma de pagamento:", Location = new Point(12, 28), AutoSize = true };
            cmbFormaPagamento = new ComboBox { Location = new Point(170, 24), Size = new Size(200, 28), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblCliente = new Label { Text = "Cliente:", Location = new Point(12, 64), AutoSize = true };
            cmbCliente = new ComboBox { Location = new Point(170, 60), Size = new Size(300, 28), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblDesconto = new Label { Text = "Desconto (R$):", Location = new Point(12, 100), AutoSize = true };
            txtDesconto = new TextBox { Location = new Point(170, 96), Size = new Size(120, 26), Text = "0,00" };
            txtDesconto.TextChanged += (_, _) => AtualizarTotais();

            var lblRecebido = new Label { Text = "Valor recebido:", Location = new Point(390, 28), AutoSize = true };
            txtValorRecebido = new TextBox { Location = new Point(510, 24), Size = new Size(160, 26) };
            txtValorRecebido.TextChanged += (_, _) => AtualizarTroco();

            var lblTroco = new Label { Text = "Troco:", Location = new Point(390, 64), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            lblValorTroco = new Label { Text = "R$ 0,00", Location = new Point(510, 60), AutoSize = true, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.DarkRed };

            var lblTotalLabel = new Label { Text = "Total:", Location = new Point(12, 150), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            lblTotal = new Label { Text = "R$ 0,00", Location = new Point(170, 146), AutoSize = true, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.DarkGreen };

            btnFinalizar = new Button { Text = "Finalizar venda", Location = new Point(510, 130), Size = new Size(160, 50), BackColor = Color.ForestGreen, ForeColor = Color.White, Font = new Font("Segoe UI", 12F, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnFinalizar.Click += FinalizarVenda;

            grpPagamento.Controls.AddRange(new Control[]
            {
                lblForma, cmbFormaPagamento, lblCliente, cmbCliente, lblDesconto, txtDesconto,
                lblRecebido, txtValorRecebido, lblTroco, lblValorTroco, lblTotalLabel, lblTotal, btnFinalizar
            });

            Controls.Add(grpCarrinho);
            Controls.Add(grpPagamento);
        }

        private void ConfigureProdutosGrid()
        {
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", Width = 50 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", HeaderText = "Código", DataPropertyName = "CodigoBarras", Width = 120 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "Produto", DataPropertyName = "Nome", Width = 260 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Preco",
                HeaderText = "Preço",
                DataPropertyName = "Preco",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estoque", HeaderText = "Estoque", DataPropertyName = "Estoque", Width = 60 });
            foreach (DataGridViewColumn c in dgvProdutos.Columns) c.FillWeight = Math.Max(50, c.Width);
            dgvProdutos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProdutos.SelectionChanged += (_, _) => PreencherQuantidadePadrao();
        }

        private void ConfigureItensGrid()
        {
            dgvItens.Columns.Add(new DataGridViewTextBoxColumn { Name = "Produto", HeaderText = "Produto", DataPropertyName = "NomeProduto", Width = 280 });
            dgvItens.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qtd", HeaderText = "Qtd", DataPropertyName = "Quantidade", Width = 70 });
            dgvItens.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Preco",
                HeaderText = "Preço Unit.",
                DataPropertyName = "PrecoUnitario",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });
            dgvItens.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Subtotal",
                HeaderText = "Subtotal",
                DataPropertyName = "Subtotal",
                Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });
            foreach (DataGridViewColumn c in dgvItens.Columns) c.FillWeight = Math.Max(50, c.Width);
            dgvItens.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void PreencherQuantidadePadrao()
        {
            if (dgvProdutos.CurrentRow?.DataBoundItem is Produto p)
                txtQuantidade.Text = "1";
        }

        private async Task BuscarAsync()
        {
            await CarregarProdutosAsync(txtBuscaProduto.Text.Trim());
        }

        private async Task CarregarProdutosAsync(string filtro = "")
        {
            _produtos = string.IsNullOrWhiteSpace(filtro)
                ? await _estoqueService.GetAllAsync()
                : await _estoqueService.BuscarAsync(filtro);

            dgvProdutos.DataSource = _produtos;
        }

        private async Task CarregarClientesAsync()
        {
            _clientes = await _clienteService.ListarAsync();
            cmbCliente.Items.Clear();
            cmbCliente.Items.Add("— Consumidor Final —");
            foreach (var c in _clientes)
                cmbCliente.Items.Add($"{c.Nome} {(c.SaldoDevedor > 0 ? $"(débito {c.SaldoDevedor:C2})" : "")}");
            if (cmbCliente.Items.Count > 0)
                cmbCliente.SelectedIndex = 0;
        }

        private void PreencherFormasPagamento()
        {
            cmbFormaPagamento.Items.AddRange(new object[]
            {
                "Dinheiro", "PIX", "Cartão Crédito", "Cartão Débito", "Fiado"
            });
        }

        private Cliente? ObterClienteSelecionado()
        {
            int idx = cmbCliente.SelectedIndex;
            return (idx <= 0 || idx > _clientes.Count) ? null : _clientes[idx - 1];
        }

        private void AdicionarItem(object? sender, EventArgs e)
        {
            if (dgvProdutos.CurrentRow?.DataBoundItem is not Produto produto)
            {
                MessageBox.Show("Selecione um produto na lista.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtQuantidade.Text, out int quantidade) || quantidade <= 0)
            {
                MessageBox.Show("Informe uma quantidade válida.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (quantidade > produto.Estoque)
            {
                MessageBox.Show($"Estoque disponível: {produto.Estoque}.", "Estoque insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existente = _viewModel.Itens.FirstOrDefault(i => i.ProdutoId == produto.Id);
            if (existente != null)
            {
                existente.Quantidade += quantidade;
                existente.Produto = produto;
            }
            else
            {
                _viewModel.Itens.Add(new ItemVenda
                {
                    ProdutoId = produto.Id,
                    Produto = produto,
                    Quantidade = quantidade,
                    PrecoUnitario = produto.Preco
                });
            }

            RefreshCarrinho();
            txtQuantidade.Text = "1";
            txtBuscaProduto.Focus();
        }

        private void RefreshCarrinho()
        {
            dgvItens.DataSource = null;
            dgvItens.DataSource = _viewModel.Itens;
            AtualizarTotais();
        }

        private void RemoverItem(object? sender, EventArgs e)
        {
            if (dgvItens.CurrentRow?.DataBoundItem is ItemVenda item)
            {
                _viewModel.Itens.Remove(item);
                RefreshCarrinho();
            }
        }

        private void LimparVenda(object? sender, EventArgs e)
        {
            _viewModel.Limpar();
            txtDesconto.Text = "0,00";
            txtValorRecebido.Clear();
            dgvItens.DataSource = null;
            AtualizarTotais();
        }

        private void AtualizarTotais()
        {
            _viewModel.Desconto = 0;
            if (decimal.TryParse(txtDesconto.Text, out var desconto))
                _viewModel.Desconto = Math.Max(0, desconto);

            lblTotal.Text = _viewModel.Total.ToString("C2");
            AtualizarTroco();
        }

        private void AtualizarTroco()
        {
            _troco = 0;
            if (decimal.TryParse(txtValorRecebido.Text, out decimal recebido))
                _troco = recebido - _viewModel.Total;

            lblValorTroco.Text = _troco >= 0 ? _troco.ToString("C2") : "Valor insuficiente";
        }

        private decimal _troco;

        private async void FinalizarVenda(object? sender, EventArgs e)
        {
            if (_viewModel.Itens.Count == 0)
            {
                MessageBox.Show("Adicione itens à venda.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var formaPagamento = cmbFormaPagamento.Text;
            var usuario = Session.CurrentUser;

            if (formaPagamento == "Fiado")
            {
                var clienteFiado = ObterClienteSelecionado();
                if (clienteFiado == null)
                {
                    MessageBox.Show("Venda fiada exige a seleção de um cliente.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (clienteFiado.LimiteCredito > 0 &&
                    clienteFiado.SaldoDevedor + _viewModel.Total > clienteFiado.LimiteCredito)
                {
                    MessageBox.Show(
                        "Crédito insuficiente para venda fiada.\n\n" +
                        $"Limite de crédito: {clienteFiado.LimiteCredito:C2}\n" +
                        $"Débito atual: {clienteFiado.SaldoDevedor:C2}\n" +
                        $"Valor da venda: {_viewModel.Total:C2}",
                        "Crédito insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (formaPagamento == "Dinheiro" && _troco < 0)
            {
                MessageBox.Show("Valor recebido é insuficiente para completar a venda.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (usuario == null)
            {
                MessageBox.Show("Usuário não autenticado.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Cliente (identificação da venda)
            var cliente = ObterClienteSelecionado();

            btnFinalizar.Enabled = false;
            var previousCursor = Cursor;
            Cursor = Cursors.WaitCursor;

            try
            {
                var venda = new Venda
                {
                    UsuarioCaixaId = usuario.Id,
                    Itens = _viewModel.Itens.Select(i => new ItemVenda
                    {
                        ProdutoId = i.ProdutoId,
                        Quantidade = i.Quantidade,
                        PrecoUnitario = i.PrecoUnitario
                    }).ToList(),
                    Desconto = _viewModel.Desconto,
                    FormaPagamento = formaPagamento,
                    ValorTotal = _viewModel.Total,
                    ClienteId = cliente?.Id
                };

                var vendaRegistrada = await _vendaService.RegistrarVendaAsync(venda);

                string mensagem = $"Venda #{vendaRegistrada.Id} registrada com sucesso!";

                if (formaPagamento == "Dinheiro" && _troco > 0)
                    mensagem += $"\nTroco: {_troco:C2}";

                if (formaPagamento == "Fiado" && cliente != null)
                    mensagem += $"\nCliente: {cliente.Nome}";

                MessageBox.Show(mensagem, "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LimparVenda(null, EventArgs.Empty);
                await CarregarClientesAsync();
            }
            catch (InvalidOperationException invEx)
            {
                MessageBox.Show($"Erro: {invEx.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnFinalizar.Enabled = true;
                Cursor = previousCursor;
            }
        }
    }
}