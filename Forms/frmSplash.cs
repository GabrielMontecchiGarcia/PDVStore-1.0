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

        // Construtor da tela de splash: prepara a UI e agenda a verificação do sistema.
        // O QUE FAZ: valida o VerificacaoSistemaService via DI, chama BuildUI e associa
        // ao evento Shown a execução assíncrona de ExecutarVerificacaoAsync.
        // POR QUE EXISTE: a verificação só deve rodar quando a janela já estiver
        // visível; centralizar aqui evita iniciar checagens antes de haver UI.
        // QUEM CHAMA: o Program.cs na inicialização da aplicação.
        public frmSplash(VerificacaoSistemaService verificacao)
        {
            _verificacao = verificacao ?? throw new ArgumentNullException(nameof(verificacao));
            BuildUI();
            Shown += async (_, _) => await ExecutarVerificacaoAsync();
        }

        // Monta os controles visuais do splash: título, versão, barra e status.
        // O QUE FAZ: cria e posiciona os rótulos, a barra de progresso e o texto de
        // status, adicionando tudo ao formulário.
        // POR QUE EXISTE: separa a construção visual da lógica de verificação,
        // deixando claro o que é aparência e o que é regra de inicialização.
        // QUEM CHAMA: o construtor de frmSplash.
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

        // Executa as verificações de ambiente e decide se o login pode abrir.
        // O QUE FAZ: define um callback que atualiza barra/status a cada passo, chama
        // VerificacaoSistemaService.VerificarAsync, trata falhas (mensagem e UltimaFalha)
        // e dispara o evento VerificacaoConcluida, fechando o splash.
        // POR QUE EXISTE: valida pré-requisitos (ex.: banco de dados) antes do login —
        // se algo falhar, a aplicação encerra de forma controlada e informa o motivo.
        // DEPENDÊNCIAS: _verificacao e o evento VerificacaoConcluida (consumido no Program).
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