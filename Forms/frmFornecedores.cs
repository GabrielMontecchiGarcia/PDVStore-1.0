using PDVStore.Helpers;
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
        private bool _mascaraAplicando;
        private string _cnpjOriginal = string.Empty;
        private string _telefoneOriginal = string.Empty;
        private string _emailOriginal = string.Empty;

        // CONSTRUTOR
        // O que faz: guarda o FornecedorService e monta a interface com BuildUI().
        // Por que existe: fornecedores são a fonte de mercadorias do PDV; a tela permite
        //   cadastrar, editar e desativar sem apagar o histórico de compras.
        // Dependências: recebe FornecedorService por injeção; no Load executa
        //   CarregarAsync() para listar os fornecedores cadastrados.
        public frmFornecedores(FornecedorService fornecedorService)
        {
            _fornecedorService = fornecedorService ?? throw new ArgumentNullException(nameof(fornecedorService));
            BuildUI();
            Load += async (_, _) => await CarregarAsync();
        }

        // O que faz: cria os campos de cadastro, botões de ação e o grid de fornecedores.
        // Por que existe: formulário montado em código (sem .Designer); a disposição dos
        //   controles segue o mesmo padrão visual das outras telas de cadastro.
        // Dependências: cria dgvFornecedores e liga os TextChanged de máscara (CNPJ/
        //   telefone), SelectionChanged do grid e cliques de Salvar/Novo/Desativar.
        private void BuildUI()
        {
            Text = "Fornecedores";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900, 560);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            int y = 16, dy = 42;

            Controls.Add(new Label { Text = "Nome:", Location = new Point(15, y), AutoSize = true });
            txtNome = new TextBox { Location = new Point(150, y - 4), Size = new Size(380, 26) };
            Controls.Add(txtNome);

            Controls.Add(new Label { Text = "CNPJ:", Location = new Point(15, y += dy), AutoSize = true });
            txtCnpj = new TextBox { Location = new Point(150, y - 4), Size = new Size(180, 26) };
            txtCnpj.TextChanged += TxtCnpj_TextChanged;
            Controls.Add(txtCnpj);

            Controls.Add(new Label { Text = "Telefone:", Location = new Point(450, y), AutoSize = true });
            txtTelefone = new TextBox { Location = new Point(560, y - 4), Size = new Size(180, 26) };
            txtTelefone.TextChanged += TxtTelefone_TextChanged;
            Controls.Add(txtTelefone);

            Controls.Add(new Label { Text = "E-mail:", Location = new Point(15, y += dy), AutoSize = true });
            txtEmail = new TextBox { Location = new Point(150, y - 4), Size = new Size(380, 26) };
            Controls.Add(txtEmail);

            btnSalvar = new Button { Text = "Salvar", Location = new Point(150, y + 36), Size = new Size(110, 32), BackColor = Color.ForestGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnNovo = new Button { Text = "Novo", Location = new Point(270, y + 36), Size = new Size(90, 32), FlatStyle = FlatStyle.Flat };
            btnDesativar = new Button { Text = "Desativar", Location = new Point(370, y + 36), Size = new Size(100, 32), BackColor = Color.Firebrick, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            var btnExportarPdf = new Button { Text = "Exportar PDF", Location = new Point(480, y + 36), Size = new Size(120, 32), FlatStyle = FlatStyle.Flat };
            var btnExportarExcel = new Button { Text = "Exportar Excel", Location = new Point(610, y + 36), Size = new Size(120, 32), FlatStyle = FlatStyle.Flat };

            btnSalvar.Click += async (_, _) => await SalvarAsync();
            btnNovo.Click += (_, _) => LimparCampos();
            btnDesativar.Click += async (_, _) => await DesativarAsync();
            btnExportarPdf.Click += (_, _) => ExportadorService.ExportarPdf(dgvFornecedores, "Fornecedores", $"Fornecedores_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
            btnExportarExcel.Click += (_, _) => ExportadorService.ExportarExcel(dgvFornecedores, "Fornecedores", $"Fornecedores_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");

            Controls.AddRange(new Control[] { btnSalvar, btnNovo, btnDesativar, btnExportarPdf, btnExportarExcel });

            dgvFornecedores = new DataGridView
            {
                Location = new Point(15, 250),
                Size = new Size(870, 280),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
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
            foreach (DataGridViewColumn c in dgvFornecedores.Columns) c.FillWeight = Math.Max(50, c.Width);
            dgvFornecedores.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvFornecedores.SelectionChanged += (_, _) => SelecionarFornecedor();
            Controls.Add(dgvFornecedores);
        }

        // O que faz: busca todos os fornecedores e preenche o dgvFornecedores.
        // Por que existe: mantém a lista da tela atualizada; é chamado no Load e após
        //   salvar ou desativar um fornecedor.
        // Dependências: chama FornecedorService.ListarAsync() e liga o resultado ao grid.
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

        // O que faz: ao selecionar uma linha, guarda o fornecedor em _fornecedorSelecionado
        //   e preenche os campos com os dados dele.
        // Por que existe: permite reutilizar a tela para editar e desativar o fornecedor
        //   correto, sem redigitar os dados (regra de UX).
        // Dependências: disparado por dgvFornecedores.SelectionChanged; aplica máscaras
        //   via Mascaras e guarda os valores originais para validação no Salvar.
        private void SelecionarFornecedor()
        {
            if (dgvFornecedores.CurrentRow?.DataBoundItem is Fornecedor f)
            {
                _fornecedorSelecionado = f;
                _cnpjOriginal = f.Cnpj ?? string.Empty;
                _telefoneOriginal = f.Telefone ?? string.Empty;
                _emailOriginal = f.Email ?? string.Empty;
                txtNome.Text = f.Nome;
                txtCnpj.Text = Mascaras.FormatarCpfCnpj(f.Cnpj);
                txtTelefone.Text = Mascaras.FormatarTelefone(f.Telefone);
                txtEmail.Text = f.Email;
            }
        }

        // EVENTO - digitação no campo CNPJ
        // O que faz: aplica a máscara de CNPJ enquanto o usuário digita.
        // Por que existe: mantém o documento padronizado (regra fiscal) e evita salvar
        //   um CNPJ com formatação inconsistente.
        // Dependências: chamado pelo TextChanged; delega a AplicarMascara() com
        //   Mascaras.FormatarCpfCnpj (que também cobre CNPJ).
        private void TxtCnpj_TextChanged(object? sender, EventArgs e) => AplicarMascara(txtCnpj, Mascaras.FormatarCpfCnpj);

        // EVENTO - digitação no campo Telefone
        // O que faz: aplica a máscara de telefone com DDD durante a digitação.
        // Por que existe: padroniza o contato do fornecedor e garante o mínimo de 10
        //   dígitos na validação do salvar.
        // Dependências: chamado pelo TextChanged; delega a AplicarMascara() com
        //   Mascaras.FormatarTelefone (Helpers).
        private void TxtTelefone_TextChanged(object? sender, EventArgs e) => AplicarMascara(txtTelefone, Mascaras.FormatarTelefone);

        // O que faz: aplica o formatador recebido sobre o conteúdo do TextBox.
        // Por que existe: alterar txt.Text dentro de TextChanged dispara o evento de novo;
        //   o flag _mascaraAplicando corta esse loop (técnica didática de máscara).
        // Dependências: genérico — chamado por TxtCnpj_TextChanged e TxtTelefone_TextChanged.
        private void AplicarMascara(TextBox txt, Func<string?, string> formatar)
        {
            if (_mascaraAplicando) return;
            _mascaraAplicando = true;
            txt.Text = formatar(txt.Text);
            txt.SelectionStart = txt.Text.Length;
            _mascaraAplicando = false;
        }

        // O que faz: valida nome, CNPJ, telefone e e-mail e salva o fornecedor.
        // Por que existe: implementa as regras de cadastro — CNPJ obrigatório/válido,
        //   telefone mínimo de 10 dígitos e e-mail válido; revalida só o que mudou.
        // Dependências: usa Mascaras (SomenteDigitos, ValidarCnpj, ValidarEmail) e
        //   FornecedorService.SalvarAsync(); ao final chama CarregarAsync() e LimparCampos().
        private async Task SalvarAsync()
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("Nome do fornecedor é obrigatório.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool editando = _fornecedorSelecionado != null;
            var digitosDoc = Mascaras.SomenteDigitos(txtCnpj.Text);
            var digitosTelefone = Mascaras.SomenteDigitos(txtTelefone.Text);
            string email = txtEmail.Text.Trim();

            if (!editando || digitosDoc != Mascaras.SomenteDigitos(_cnpjOriginal))
            {
                bool documentoValido = digitosDoc.Length == 14 && Mascaras.ValidarCnpj(digitosDoc);
                if (!documentoValido)
                {
                    MessageBox.Show("Informe um CNPJ válido.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                var fornecedor = _fornecedorSelecionado ?? new Fornecedor { Ativo = true };
                fornecedor.Nome = txtNome.Text.Trim();
                fornecedor.Cnpj = Mascaras.FormatarCpfCnpj(txtCnpj.Text);
                fornecedor.Telefone = Mascaras.FormatarTelefone(txtTelefone.Text);
                fornecedor.Email = email;

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

        // O que faz: marca o fornecedor selecionado como inativo no banco.
        // Por que existe: desativar preserva o histórico de compras do fornecedor (não é
        //   uma exclusão física), evitando erros contábeis em NF-e/relatórios.
        // Dependências: chama FornecedorService.AtualizarStatusAsync(id, false); depois
        //   recarrega o grid e limpa os campos.
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

        // O que faz: limpa os campos e zera _fornecedorSelecionado (modo novo).
        // Por que existe: prepara a tela para um novo cadastro; também remove os valores
        //   "originais" usados para decidir o que validar no salvar.
        // Dependências: manipula diretamente os TextBoxes; chamado pelo botão "Novo"
        //   (lambda), por SalvarAsync() e DesativarAsync().
        private void LimparCampos()
        {
            txtNome.Clear();
            txtCnpj.Clear();
            txtTelefone.Clear();
            txtEmail.Clear();
            _fornecedorSelecionado = null;
            _cnpjOriginal = string.Empty;
            _telefoneOriginal = string.Empty;
            _emailOriginal = string.Empty;
            txtNome.Focus();
        }
    }
}