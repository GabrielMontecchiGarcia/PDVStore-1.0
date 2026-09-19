using PDVStore.Models;
using PDVStore.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public class frmCaixa : Form
    {
        private readonly CaixaService _caixaService;

        private Label lblStatus = null!;
        private Label lblDetalhes = null!;
        private Button btnAbrir = null!;
        private Button btnSangria = null!;
        private Button btnFechar = null!;
        private DataGridView dgvCaixas = null!;

        public frmCaixa(CaixaService caixaService)
        {
            _caixaService = caixaService ?? throw new ArgumentNullException(nameof(caixaService));
            BuildUI();
            Load += async (_, _) => await AtualizarAsync();
        }

        private void BuildUI()
        {
            Text = "Abertura / Fechamento de Caixa";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(820, 420);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            var grpOperacao = new GroupBox { Text = "Operação do caixa", Location = new Point(15, 15), Size = new Size(790, 170) };

            lblStatus = new Label
            {
                Text = "Caixa: verificando...",
                Location = new Point(15, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.DimGray
            };

            lblDetalhes = new Label
            {
                Text = "",
                Location = new Point(15, 56),
                AutoSize = true,
                ForeColor = Color.DimGray
            };

            btnAbrir = new Button { Text = "Abrir caixa", Location = new Point(15, 105), Size = new Size(140, 44), BackColor = Color.ForestGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSangria = new Button { Text = "Sangria", Location = new Point(165, 105), Size = new Size(120, 44), BackColor = Color.DarkOrange, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnFechar = new Button { Text = "Fechar caixa", Location = new Point(295, 105), Size = new Size(140, 44), BackColor = Color.Firebrick, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            grpOperacao.Controls.AddRange(new Control[] { lblStatus, lblDetalhes, btnAbrir, btnSangria, btnFechar });
            Controls.Add(grpOperacao);

            var grpHistorico = new GroupBox { Text = "Histórico", Location = new Point(15, 195), Size = new Size(790, 205) };

            dgvCaixas = new DataGridView
            {
                Location = new Point(10, 24),
                Size = new Size(770, 170),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvCaixas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "#", DataPropertyName = "Id", Width = 40 });
            dgvCaixas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", Width = 80 });
            dgvCaixas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Abertura", HeaderText = "Abertura", DataPropertyName = "Abertura", Width = 140, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
            dgvCaixas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fechamento", HeaderText = "Fechamento", DataPropertyName = "Fechamento", Width = 140, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
            dgvCaixas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Inicial", HeaderText = "Inicial", DataPropertyName = "ValorInicial", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgvCaixas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Final", HeaderText = "Final", DataPropertyName = "ValorFinal", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgvCaixas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sangria", HeaderText = "Sangria", DataPropertyName = "Sangria", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });

            grpHistorico.Controls.Add(dgvCaixas);
            Controls.Add(grpHistorico);

            btnAbrir.Click += async (_, _) => await AbrirCaixaAsync();
            btnSangria.Click += async (_, _) => await SangriaAsync();
            btnFechar.Click += async (_, _) => await FecharCaixaAsync();
        }

        private async Task AtualizarAsync()
        {
            var caixa = await _caixaService.ObterCaixaAbertoAsync();

            if (caixa == null)
            {
                lblStatus.Text = "Caixa: fechado";
                lblStatus.ForeColor = Color.Firebrick;
                lblDetalhes.Text = "Abra o caixa para liberar as vendas do PDV.";
                btnAbrir.Enabled = true;
                btnSangria.Enabled = false;
                btnFechar.Enabled = false;
            }
            else
            {
                lblStatus.Text = $"Caixa #{caixa.Id}: aberto";
                lblStatus.ForeColor = Color.ForestGreen;
                lblDetalhes.Text = $"Aberto em {caixa.Abertura:dd/MM/yyyy HH:mm} por {caixa.UsuarioCaixa?.Nome ?? "-"} " +
                                   $"| Inicial: {caixa.ValorInicial:C2} | Sangria: {caixa.Sangria:C2}";
                btnAbrir.Enabled = false;
                btnSangria.Enabled = true;
                btnFechar.Enabled = true;
            }

            dgvCaixas.DataSource = (await _caixaService.ListarAsync()).ToList();
        }

        private async Task AbrirCaixaAsync()
        {
            if (Models.Session.CurrentUser == null)
            {
                MessageBox.Show("Usuário não autenticado.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var valor = Helpers.PromptDialog.AskDecimal("Abrir Caixa", "Valor inicial (fundo de troco):", 0);
            if (!valor.HasValue) return;

            try
            {
                var caixa = await _caixaService.AbrirCaixaAsync(valor.Value, Models.Session.CurrentUser.Id);
                MessageBox.Show($"Caixa #{caixa.Id} aberto com {valor.Value:C2}.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await AtualizarAsync();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task SangriaAsync()
        {
            var caixa = await _caixaService.ObterCaixaAbertoAsync();
            if (caixa == null) return;

            var valor = Helpers.PromptDialog.AskDecimal("Sangria", "Valor a retirar do caixa:", 0);
            if (!valor.HasValue || valor.Value <= 0) return;

            var ok = await _caixaService.RegistrarSangriaAsync(caixa.Id, valor.Value);
            if (ok)
            {
                MessageBox.Show($"Sangria de {valor.Value:C2} registrada.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await AtualizarAsync();
            }
            else
            {
                MessageBox.Show("Não foi possível registrar a sangria.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task FecharCaixaAsync()
        {
            var caixa = await _caixaService.ObterCaixaAbertoAsync();
            if (caixa == null) return;

            if (MessageBox.Show("Fechar o caixa atual?", "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var ok = await _caixaService.FecharCaixaAsync(caixa.Id);
            if (ok)
            {
                var atualizado = await _caixaService.ListarAsync();
                var fechado = atualizado.FirstOrDefault(c => c.Id == caixa.Id);
                MessageBox.Show(fechado?.ValorFinal != null
                    ? $"Caixa fechado. Valor final: {fechado.ValorFinal:C2}"
                    : "Caixa fechado.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await AtualizarAsync();
            }
            else
            {
                MessageBox.Show("Não foi possível fechar o caixa.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}