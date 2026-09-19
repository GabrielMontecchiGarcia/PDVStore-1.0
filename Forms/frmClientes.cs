using PDVStore.Models;
using PDVStore.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public class frmClientes : Form
    {
        private readonly ClienteService _clienteService;
        private Cliente? _clienteSelecionado;

        private DataGridView dgvClientes = null!;
        private TextBox txtNome = null!;
        private TextBox txtCpfCnpj = null!;
        private TextBox txtTelefone = null!;
        private TextBox txtEmail = null!;
        private TextBox txtEndereco = null!;
        private TextBox txtLimite = null!;
        private Button btnSalvar = null!;
        private Button btnNovo = null!;
        private Button btnDesativar = null!;
        private Button btnReceber = null!;

        public frmClientes(ClienteService clienteService)
        {
            _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
            BuildUI();
            Load += async (_, _) => await CarregarAsync();
        }

        private void BuildUI()
        {
            Text = "Clientes (Fiado / Caderneta)";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1000, 520);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            int y = 16, dy = 40;

            Controls.Add(new Label { Text = "Nome:", Location = new Point(15, y), AutoSize = true });
            txtNome = new TextBox { Location = new Point(130, y - 4), Size = new Size(280, 26) };
            Controls.Add(txtNome);

            Controls.Add(new Label { Text = "CPF/CNPJ:", Location = new Point(15, y += dy), AutoSize = true });
            txtCpfCnpj = new TextBox { Location = new Point(130, y - 4), Size = new Size(180, 26) };
            Controls.Add(txtCpfCnpj);

            Controls.Add(new Label { Text = "Telefone:", Location = new Point(430, y), AutoSize = true });
            txtTelefone = new TextBox { Location = new Point(540, y - 4), Size = new Size(180, 26) };
            Controls.Add(txtTelefone);

            Controls.Add(new Label { Text = "E-mail:", Location = new Point(15, y += dy), AutoSize = true });
            txtEmail = new TextBox { Location = new Point(130, y - 4), Size = new Size(280, 26) };
            Controls.Add(txtEmail);

            Controls.Add(new Label { Text = "Endereço:", Location = new Point(430, y), AutoSize = true });
            txtEndereco = new TextBox { Location = new Point(540, y - 4), Size = new Size(280, 26) };
            Controls.Add(txtEndereco);

            Controls.Add(new Label { Text = "Limite crédito (R$):", Location = new Point(15, y += dy), AutoSize = true });
            txtLimite = new TextBox { Location = new Point(150, y - 4), Size = new Size(120, 26), Text = "0" };
            Controls.Add(txtLimite);

            btnSalvar = new Button { Text = "Salvar", Location = new Point(150, y + 36), Size = new Size(110, 32), BackColor = Color.ForestGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnNovo = new Button { Text = "Novo", Location = new Point(270, y + 36), Size = new Size(90, 32), FlatStyle = FlatStyle.Flat };
            btnDesativar = new Button { Text = "Desativar", Location = new Point(370, y + 36), Size = new Size(100, 32), BackColor = Color.Firebrick, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnReceber = new Button { Text = "Receber débito", Location = new Point(480, y + 36), Size = new Size(130, 32), BackColor = Color.DarkOrange, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            btnSalvar.Click += async (_, _) => await SalvarAsync();
            btnNovo.Click += (_, _) => LimparCampos();
            btnDesativar.Click += async (_, _) => await DesativarAsync();
            btnReceber.Click += async (_, _) => await ReceberAsync();

            Controls.AddRange(new Control[] { btnSalvar, btnNovo, btnDesativar, btnReceber });

            dgvClientes = new DataGridView
            {
                Location = new Point(15, 280),
                Size = new Size(970, 220),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", Width = 50 });
            dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "Nome", DataPropertyName = "Nome", Width = 220 });
            dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Doc", HeaderText = "CPF/CNPJ", DataPropertyName = "CpfCnpj", Width = 130 });
            dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tel", HeaderText = "Telefone", DataPropertyName = "Telefone", Width = 120 });
            dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Limite", HeaderText = "Limite", DataPropertyName = "LimiteCredito", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Saldo", HeaderText = "Saldo Devedor", DataPropertyName = "SaldoDevedor", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgvClientes.SelectionChanged += (_, _) => SelecionarCliente();
            Controls.Add(dgvClientes);
        }

        private async Task CarregarAsync()
        {
            try
            {
                var clientes = await _clienteService.ListarAsync();
                dgvClientes.DataSource = clientes.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar clientes: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelecionarCliente()
        {
            if (dgvClientes.CurrentRow?.DataBoundItem is Cliente c)
            {
                _clienteSelecionado = c;
                PreencherCampos(c);
            }
        }

        private void PreencherCampos(Cliente c)
        {
            txtNome.Text = c.Nome;
            txtCpfCnpj.Text = c.CpfCnpj;
            txtTelefone.Text = c.Telefone;
            txtEmail.Text = c.Email;
            txtEndereco.Text = c.Endereco;
            txtLimite.Text = c.LimiteCredito.ToString("0.00");
        }

        private async Task SalvarAsync()
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("Nome do cliente é obrigatório.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var cliente = _clienteSelecionado ?? new Cliente { Ativo = true };
                cliente.Nome = txtNome.Text.Trim();
                cliente.CpfCnpj = txtCpfCnpj.Text.Trim();
                cliente.Telefone = txtTelefone.Text.Trim();
                cliente.Email = txtEmail.Text.Trim();
                cliente.Endereco = txtEndereco.Text.Trim();
                cliente.LimiteCredito = decimal.TryParse(txtLimite.Text, out decimal limite) ? limite : 0;

                await _clienteService.SalvarAsync(cliente);

                MessageBox.Show("Cliente salvo com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await CarregarAsync();
                LimparCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DesativarAsync()
        {
            if (_clienteSelecionado == null)
            {
                MessageBox.Show("Selecione um cliente.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await _clienteService.AtualizarStatusAsync(_clienteSelecionado.Id, false);
            MessageBox.Show("Cliente desativado.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await CarregarAsync();
            LimparCampos();
        }

        private async Task ReceberAsync()
        {
            if (_clienteSelecionado == null)
            {
                MessageBox.Show("Selecione um cliente com débito.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_clienteSelecionado.SaldoDevedor <= 0)
            {
                MessageBox.Show("Este cliente não possui débito em aberto.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var valor = Helpers.PromptDialog.AskDecimal("Receber Fiado",
                $"Cliente: {_clienteSelecionado.Nome}\nDébito atual: {_clienteSelecionado.SaldoDevedor:C2}\nValor a receber:",
                _clienteSelecionado.SaldoDevedor);

            if (valor.HasValue && valor.Value > 0)
            {
                await _clienteService.ReceberFiadoAsync(_clienteSelecionado.Id, valor.Value);
                MessageBox.Show($"Pagamento de {valor.Value:C2} registrado!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await CarregarAsync();
            }
        }

        private void LimparCampos()
        {
            txtNome.Clear();
            txtCpfCnpj.Clear();
            txtTelefone.Clear();
            txtEmail.Clear();
            txtEndereco.Clear();
            txtLimite.Text = "0";
            _clienteSelecionado = null;
            txtNome.Focus();
        }
    }
}