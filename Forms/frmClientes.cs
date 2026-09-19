using PDVStore.Helpers;
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
        private Button btnEditar = null!;
        private Button btnDesativar = null!;
        private Button btnReceber = null!;
        private bool _mascaraAplicando;
        private string _docOriginal = string.Empty;
        private string _telefoneOriginal = string.Empty;
        private string _emailOriginal = string.Empty;

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
            ClientSize = new Size(1200, 600);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            int y = 16, dy = 40;

            Controls.Add(new Label { Text = "Nome:", Location = new Point(15, y), AutoSize = true });
            txtNome = new TextBox { Location = new Point(130, y - 4), Size = new Size(280, 26) };
            Controls.Add(txtNome);

            Controls.Add(new Label { Text = "CPF/CNPJ:", Location = new Point(15, y += dy), AutoSize = true });
            txtCpfCnpj = new TextBox { Location = new Point(130, y - 4), Size = new Size(180, 26) };
            txtCpfCnpj.TextChanged += TxtCpfCnpj_TextChanged;
            Controls.Add(txtCpfCnpj);

            Controls.Add(new Label { Text = "Telefone:", Location = new Point(430, y), AutoSize = true });
            txtTelefone = new TextBox { Location = new Point(540, y - 4), Size = new Size(180, 26) };
            txtTelefone.TextChanged += TxtTelefone_TextChanged;
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
            btnEditar = new Button { Text = "Editar", Location = new Point(620, y + 36), Size = new Size(90, 32), FlatStyle = FlatStyle.Flat };
            btnDesativar = new Button { Text = "Desativar", Location = new Point(370, y + 36), Size = new Size(100, 32), BackColor = Color.Firebrick, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnReceber = new Button { Text = "Receber débito", Location = new Point(480, y + 36), Size = new Size(130, 32), BackColor = Color.DarkOrange, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            var btnExportarPdf = new Button { Text = "Exportar PDF", Location = new Point(730, y + 36), Size = new Size(120, 32), FlatStyle = FlatStyle.Flat };
            var btnExportarExcel = new Button { Text = "Exportar Excel", Location = new Point(850, y + 36), Size = new Size(120, 32), FlatStyle = FlatStyle.Flat };

            btnSalvar.Click += async (_, _) => await SalvarAsync();
            btnNovo.Click += (_, _) => LimparCampos();
            btnEditar.Click += EditarCliente;
            btnDesativar.Click += async (_, _) => await DesativarAsync();
            btnReceber.Click += async (_, _) => await ReceberAsync();
            btnExportarPdf.Click += (_, _) => ExportadorService.ExportarPdf(dgvClientes, "Clientes", $"Clientes_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
            btnExportarExcel.Click += (_, _) => ExportadorService.ExportarExcel(dgvClientes, "Clientes", $"Clientes_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");

            Controls.AddRange(new Control[] { btnSalvar, btnNovo, btnEditar, btnDesativar, btnReceber, btnExportarPdf, btnExportarExcel });

            dgvClientes = new DataGridView
            {
                Location = new Point(15, 280),
                Size = new Size(1170, 310),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", Width = 50 });
            dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "Nome", DataPropertyName = "Nome", Width = 220 });
            dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Doc", HeaderText = "CPF/CNPJ", DataPropertyName = "CpfCnpj", Width = 130 });
            dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tel", HeaderText = "Telefone", DataPropertyName = "Telefone", Width = 120 });
            dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "E-mail", DataPropertyName = "Email", Width = 210 });
            dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Limite", HeaderText = "Limite", DataPropertyName = "LimiteCredito", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Saldo", HeaderText = "Saldo Devedor", DataPropertyName = "SaldoDevedor", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            foreach (DataGridViewColumn c in dgvClientes.Columns) c.FillWeight = Math.Max(50, c.Width);
            dgvClientes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
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
            _docOriginal = c.CpfCnpj ?? string.Empty;
            _telefoneOriginal = c.Telefone ?? string.Empty;
            _emailOriginal = c.Email ?? string.Empty;
            txtNome.Text = c.Nome;
            txtCpfCnpj.Text = Mascaras.FormatarCpfCnpj(c.CpfCnpj);
            txtTelefone.Text = Mascaras.FormatarTelefone(c.Telefone);
            txtEmail.Text = c.Email;
            txtEndereco.Text = c.Endereco;
            txtLimite.Text = c.LimiteCredito.ToString("0.00");
        }

        private void TxtCpfCnpj_TextChanged(object? sender, EventArgs e) => AplicarMascara(txtCpfCnpj, Mascaras.FormatarCpfCnpj);

        private void TxtTelefone_TextChanged(object? sender, EventArgs e) => AplicarMascara(txtTelefone, Mascaras.FormatarTelefone);

        private void AplicarMascara(TextBox txt, Func<string?, string> formatar)
        {
            if (_mascaraAplicando) return;
            _mascaraAplicando = true;
            txt.Text = formatar(txt.Text);
            txt.SelectionStart = txt.Text.Length;
            _mascaraAplicando = false;
        }

        private void EditarCliente(object? sender, EventArgs e)
        {
            if (dgvClientes.CurrentRow?.DataBoundItem is not Cliente c)
            {
                MessageBox.Show("Selecione um cliente para editar.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _clienteSelecionado = c;
            PreencherCampos(c);
            txtNome.Focus();
        }

        private async Task SalvarAsync()
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("Nome do cliente é obrigatório.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool editando = _clienteSelecionado != null;
            var digitosDoc = Mascaras.SomenteDigitos(txtCpfCnpj.Text);
            var digitosTelefone = Mascaras.SomenteDigitos(txtTelefone.Text);
            string email = txtEmail.Text.Trim();

            if (!editando || digitosDoc != Mascaras.SomenteDigitos(_docOriginal))
            {
                bool documentoValido = digitosDoc.Length == 11 ? Mascaras.ValidarCpf(digitosDoc)
                                   : digitosDoc.Length == 14 ? Mascaras.ValidarCnpj(digitosDoc)
                                   : false;
                if (!documentoValido)
                {
                    MessageBox.Show("Informe um CPF ou CNPJ válido.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (!editando || digitosTelefone != Mascaras.SomenteDigitos(_telefoneOriginal))
            {
                if (digitosTelefone.Length < 10)
                {
                    MessageBox.Show("Informe um telefone válido com DDD (mínimo 10 dígitos).", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (!editando || !string.Equals(email, _emailOriginal.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                if (!Mascaras.ValidarEmail(email))
                {
                    MessageBox.Show("Informe um e-mail válido.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                var cliente = _clienteSelecionado ?? new Cliente { Ativo = true };
                cliente.Nome = txtNome.Text.Trim();
                cliente.CpfCnpj = Mascaras.FormatarCpfCnpj(txtCpfCnpj.Text);
                cliente.Telefone = Mascaras.FormatarTelefone(txtTelefone.Text);
                cliente.Email = email;
                cliente.Endereco = txtEndereco.Text.Trim();
                cliente.LimiteCredito = Mascaras.ParseDecimal(txtLimite.Text);

                await _clienteService.SalvarAsync(cliente);

                MessageBox.Show($"Cliente salvo com sucesso!\nLimite de crédito: {cliente.LimiteCredito:C2}",
                    "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            var saldoDevedor = _clienteSelecionado.SaldoDevedor;

            var valor = Helpers.PromptDialog.AskDecimal("Receber Fiado",
                $"Cliente: {_clienteSelecionado.Nome}\nDébito atual: {saldoDevedor:C2}\nValor a receber (parcial ou total):",
                saldoDevedor);

            if (!valor.HasValue)
                return;

            decimal pagamento = valor.Value;
            if (pagamento <= 0)
            {
                MessageBox.Show("Informe um valor maior que zero.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (pagamento > saldoDevedor)
            {
                var opcao = MessageBox.Show(
                    $"O valor informado ({pagamento:C2}) é maior que o débito do cliente ({saldoDevedor:C2}).\n\n" +
                    "Deseja registrar apenas o valor total devido, quitando o débito?",
                    "Valor acima do débito",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (opcao != DialogResult.Yes)
                    return;

                pagamento = saldoDevedor;
            }

            await _clienteService.ReceberFiadoAsync(_clienteSelecionado.Id, pagamento);

            if (pagamento >= saldoDevedor)
            {
                MessageBox.Show($"Débito de {_clienteSelecionado.Nome} quitado ({pagamento:C2})!",
                    "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Pagamento parcial de {pagamento:C2} registrado.\nSaldo restante: {saldoDevedor - pagamento:C2}",
                    "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            await CarregarAsync();
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
            _docOriginal = string.Empty;
            _telefoneOriginal = string.Empty;
            _emailOriginal = string.Empty;
            txtNome.Focus();
        }
    }
}