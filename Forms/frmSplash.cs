using PDVStore.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    /// <summary>
    /// Splash de inicialização: executa a verificação do sistema e, ao concluir,
    /// dispara um evento para o Program abrir a tela de login.
    /// </summary>
    public class frmSplash : Form
    {
        private readonly VerificacaoSistemaService _verificacao;

        private Label lblStatus = null!;
        private ProgressBar pgbProgresso = null!;
        private int _passosConcluidos;
        private VerificacaoItem? _ultimoItem;

        public event EventHandler<bool>? VerificacaoConcluida;
        public string? UltimaFalha { get; private set; }

        public frmSplash(VerificacaoSistemaService verificacao)
        {
            _verificacao = verificacao ?? throw new ArgumentNullException(nameof(verificacao));
            BuildUI();
            Shown += async (_, _) => await ExecutarVerificacaoAsync();
        }

        private void BuildUI()
        {
            Text = "PDV Store";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(480, 230);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            var lblTitulo = new Label
            {
                Text = "PDV Store",
                Location = new Point(20, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 26F, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };

            var lblSub = new Label
            {
                Text = "Verificação do sistema antes do login",
                Location = new Point(22, 68),
                AutoSize = true,
                ForeColor = Color.DimGray
            };

            var lblVersao = new Label
            {
                Text = "v1.0",
                Location = new Point(405, 24),
                AutoSize = true,
                ForeColor = Color.DimGray
            };

            pgbProgresso = new ProgressBar
            {
                Location = new Point(20, 110),
                Size = new Size(440, 22),
                Minimum = 0,
                Maximum = 100,
                Style = ProgressBarStyle.Blocks
            };

            lblStatus = new Label
            {
                Location = new Point(20, 146),
                Size = new Size(440, 60),
                ForeColor = Color.DimGray,
                Text = "Iniciando verificações...",
                AutoEllipsis = true
            };

            Controls.AddRange(new Control[] { lblTitulo, lblSub, lblVersao, pgbProgresso, lblStatus });
        }

        private async Task ExecutarVerificacaoAsync()
        {
            Action<VerificacaoItem> onPasso = item =>
            {
                _ultimoItem = item;
                _passosConcluidos++;
                pgbProgresso.Value = Math.Min(100,
                    (int)(_passosConcluidos * 100d / VerificacaoSistemaService.TotalPassos));

                lblStatus.Text = item.Sucesso
                    ? $"{item.Descricao}: {item.Mensagem}"
                    : $"{item.Descricao}: falha detectada.";
            };

            bool ok = false;
            try
            {
                ok = await _verificacao.VerificarAsync(onPasso);
                if (ok)
                {
                    lblStatus.Text = "Sistema pronto. Abrindo login...";
                    pgbProgresso.Value = 100;
                }
            }
            catch (Exception ex)
            {
                _ultimoItem = new VerificacaoItem
                {
                    Descricao = "Verificação do sistema",
                    Sucesso = false,
                    Mensagem = ex.Message
                };
                ok = false;
            }

            if (!ok)
            {
                UltimaFalha = _ultimoItem?.Mensagem ?? "Falha desconhecida na verificação.";
                string detalhe = _ultimoItem != null
                    ? $"{_ultimoItem.Descricao}: {UltimaFalha}"
                    : UltimaFalha;

                MessageBox.Show(
                    $"{detalhe}\n\nA aplicação será encerrada.",
                    "PDV Store - Falha na verificação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            VerificacaoConcluida?.Invoke(this, ok);
            Close();
        }
    }
}