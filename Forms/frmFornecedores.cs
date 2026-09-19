using PDVStore.Models;
using PDVStore.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public class frmFornecedores : Form
    {
        private readonly FornecedorService _fornecedorService;
        private Fornecedor? _fornecedorSelecionado;

        private DataGridView dgvFornecedores = null!;
        private TextBox txtNome = null!;
        private TextBox txtCnpj = null!;
        private TextBox txtTelefone = null!;
        private TextBox txtEmail = null!;
        private Button btnSalvar = null!;
        private Button btnNovo = null!;
        private Button btnDesativar = null!;

        public frmFornecedores(FornecedorService fornecedorService)
        {
            _fornecedorService = fornecedorService ?? throw new ArgumentNullException(nameof(fornecedorService));
            BuildUI();
            Load += async (_, _) => await CarregarAsync();
        }

        private void BuildUI()
        {
            Text = "Fornecedores";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900, 470);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            int y = 16, dy = 42;

            Controls.Add(new Label { Text = "Nome:", Location = new Point(15, y), AutoSize = true });
            txtNome = new TextBox { Location = new Point(150, y - 4), Size = new Size(380, 26) };
            Controls.Add(txtNome);

            Controls.Add(new Label { Text = "CNPJ:", Location = new Point(15, y += dy), AutoSize = true });
            txtCnpj = new TextBox { Location = new Point(150, y - 4), Size = new Size(180, 26) };
            Controls.Add(txtCnpj);

            Controls.Add(new Label { Text = "Telefone:", Location = new Point(450, y), AutoSize = true });
            txtTelefone = new TextBox { Location = new Point(560, y - 4), Size = new Size(180, 26) };
            Controls.Add(txtTelefone);

            Controls.Add(new Label { Text = "E-mail:", Location = new Point(15, y += dy), AutoSize = true });
            txtEmail = new TextBox { Location = new Point(150, y - 4), Size = new Size(380, 26) };
            Controls.Add(txtEmail);

            btnSalvar = new Button { Text = "Salvar", Location = new Point(150, y + 36), Size = new Size(110, 32), BackColor = Color.ForestGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnNovo = new Button { Text = "Novo", Location = new Point(270, y + 36), Size = new Size(90, 32), FlatStyle = FlatStyle.Flat };
            btnDesativar = new Button { Text = "Desativar", Location = new Point(370, y + 36), Size = new Size(100, 32), BackColor = Color.Firebrick, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            btnSalvar.Click += async (_, _) => await SalvarAsync();
            btnNovo.Click += (_, _) => LimparCampos();
            btnDesativar.Click += async (_, _) => await DesativarAsync();

            Controls.AddRange(new Control[] { btnSalvar, btnNovo, btnDesativar });

            dgvFornecedores = new DataGridView
            {
                Location = new Point(15, 250),
                Size = new Size(870, 200),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvFornecedores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", Width = 50 });
            dgvFornecedores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "Nome", DataPropertyName = "Nome", Width = 260 });
            dgvFornecedores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cnpj", HeaderText = "CNPJ", DataPropertyName = "Cnpj", Width = 150 });
            dgvFornecedores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tel", HeaderText = "Telefone", DataPropertyName = "Telefone", Width = 130 });
            dgvFornecedores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "E-mail", DataPropertyName = "Email", Width = 200 });
            dgvFornecedores.SelectionChanged += (_, _) => SelecionarFornecedor();
            Controls.Add(dgvFornecedores);
        }

        private async Task CarregarAsync()
        {
            try
            {
                var fornecedores = await _fornecedorService.ListarAsync();
                dgvFornecedores.DataSource = fornecedores.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar fornecedores: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelecionarFornecedor()
        {
            if (dgvFornecedores.CurrentRow?.DataBoundItem is Fornecedor f)
            {
                _fornecedorSelecionado = f;
                txtNome.Text = f.Nome;
                txtCnpj.Text = f.Cnpj;
                txtTelefone.Text = f.Telefone;
                txtEmail.Text = f.Email;
            }
        }

        private async Task SalvarAsync()
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("Nome do fornecedor é obrigatório.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var fornecedor = _fornecedorSelecionado ?? new Fornecedor { Ativo = true };
                fornecedor.Nome = txtNome.Text.Trim();
                fornecedor.Cnpj = txtCnpj.Text.Trim();
                fornecedor.Telefone = txtTelefone.Text.Trim();
                fornecedor.Email = txtEmail.Text.Trim();

                await _fornecedorService.SalvarAsync(fornecedor);

                MessageBox.Show("Fornecedor salvo com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (_fornecedorSelecionado == null)
            {
                MessageBox.Show("Selecione um fornecedor.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await _fornecedorService.AtualizarStatusAsync(_fornecedorSelecionado.Id, false);
            MessageBox.Show("Fornecedor desativado.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await CarregarAsync();
            LimparCampos();
        }

        private void LimparCampos()
        {
            txtNome.Clear();
            txtCnpj.Clear();
            txtTelefone.Clear();
            txtEmail.Clear();
            _fornecedorSelecionado = null;
            txtNome.Focus();
        }
    }
}