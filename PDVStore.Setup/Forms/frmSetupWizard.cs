using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using PDVStore.Setup.Services;

namespace PDVStore.Setup.Forms;

public enum PassoSetup
{
    BemVindo,
    Requisitos,
    Preparacao,
    Conclusao
}

/// <summary>
/// Assistente guiado de instalação do PDVStore: verifica pré-requisitos, baixa/instala
/// o que faltar, configura o ambiente, prepara o banco e publica o aplicativo.
/// </summary>
public class frmSetupWizard : Form
{
    private readonly SetupContext _ctx = new();

    private PassoSetup _passo = PassoSetup.BemVindo;
    private bool _preparacaoEmAndamento;
    private bool _verificacaoOk;

    private Label _lblTitulo = null!;
    private Label _lblDescricao = null!;
    private Panel _pnlConteudo = null!;
    private ProgressBar _pgb = null!;
    private Label _lblStatus = null!;
    private RichTextBox _txtLog = null!;
    private Button _btnVoltar = null!;
    private Button _btnAvancar = null!;
    private Button _btnFechar = null!;

    // controles da etapa BemVindo
    private TextBox _txtDir = null!;
    private TextBox _txtBanco = null!;

    // controles da etapa Requisitos
    private ListView _lvRequisitos = null!;

    // controles da etapa Conclusão
    private TextBox _txtResumo = null!;
    private CheckBox _ckbAbrir = null!;

    public frmSetupWizard()
    {
        ConstruirUI();
        MostrarPasso(PassoSetup.BemVindo);
    }

    private void ConstruirUI()
    {
        Text = "PDV Store - Instalação";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(880, 610);
        Font = new Font("Segoe UI", 9.75F);
        BackColor = Color.White;

        _lblTitulo = new Label
        {
            Text = "Instalador do PDV Store",
            Location = new Point(24, 16),
            AutoSize = true,
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            ForeColor = Color.DarkGreen
        };

        _lblDescricao = new Label
        {
            Location = new Point(26, 56),
            Size = new Size(830, 24),
            ForeColor = Color.DimGray,
            Text = "Verifica requisitos, prepara o ambiente e instala o sistema."
        };

        _pnlConteudo = new Panel
        {
            Location = new Point(20, 92),
            Size = new Size(840, 330),
            BackColor = Color.White
        };

        _pgb = new ProgressBar
        {
            Location = new Point(20, 432),
            Size = new Size(700, 20)
        };

        _lblStatus = new Label
        {
            Location = new Point(20, 458),
            Size = new Size(840, 20),
            ForeColor = Color.DimGray
        };

        _txtLog = new RichTextBox
        {
            Location = new Point(20, 486),
            Size = new Size(840, 58),
            ReadOnly = true,
            Multiline = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            WordWrap = false,
            BackColor = Color.FromArgb(250, 250, 250),
            Font = new Font("Consolas", 8.5F),
            Visible = false
        };

        _btnVoltar = new Button
        {
            Text = "Voltar",
            Location = new Point(580, 558),
            Size = new Size(88, 36)
        };
        _btnAvancar = new Button
        {
            Text = "Avançar >",
            Location = new Point(676, 558),
            Size = new Size(96, 36)
        };
        _btnFechar = new Button
        {
            Text = "Cancelar",
            Location = new Point(780, 558),
            Size = new Size(90, 36),
            DialogResult = DialogResult.Cancel
        };

        _btnVoltar.Click += (_, _) => MostrarPasso(_passo - 1);
        _btnAvancar.Click += async (_, _) => await AvancarAsync();
        _btnFechar.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            _lblTitulo, _lblDescricao, _pnlConteudo,
            _pgb, _lblStatus, _txtLog,
            _btnVoltar, _btnAvancar, _btnFechar
        });
    }

    private void MostrarPasso(PassoSetup passo)
    {
        _passo = passo;
        _pnlConteudo.Controls.Clear();

        switch (passo)
        {
            case PassoSetup.BemVindo:
                ConstruirBemVindo();
                break;
            case PassoSetup.Requisitos:
                ConstruirRequisitos();
                break;
            case PassoSetup.Preparacao:
                ConstruirPreparacao();
                break;
            case PassoSetup.Conclusao:
                ConstruirConclusao();
                break;
        }

        _lblDescricao.Text = Descricao(passo);
        _txtLog.Visible = passo == PassoSetup.Preparacao || passo == PassoSetup.Conclusao;

        _btnVoltar.Visible = passo > PassoSetup.BemVindo && !_preparacaoEmAndamento;
        _btnAvancar.Text = BotaoAvancarTexto(passo);
        _btnAvancar.Enabled = !_preparacaoEmAndamento && AvancarHabilitado(passo);
        _btnFechar.Text = passo == PassoSetup.BemVindo ? "Cancelar" : "Fechar";

        _pgb.Value = passo == PassoSetup.Conclusao ? 100 : 0;
        _lblStatus.Text = "";
    }

    private string Descricao(PassoSetup passo) => passo switch
    {
        PassoSetup.BemVindo => "Informe as preferências de instalação e prossiga para a verificação dos pré-requisitos.",
        PassoSetup.Requisitos => "Resultado da verificação. Clique em \"Preparar e instalar\" para resolver o que estiver ausente.",
        PassoSetup.Preparacao => "Instalando pré-requisitos, configurando o ambiente e publicando o aplicativo. Aguarde...",
        _ => "Instalação concluída. Confira o resumo abaixo."
    };

    private string BotaoAvancarTexto(PassoSetup passo) => passo switch
    {
        PassoSetup.BemVindo => "Verificar pré-requisitos >",
        PassoSetup.Requisitos => "Preparar e instalar >",
        PassoSetup.Preparacao => "Avançar >",
        _ => "Concluir"
    };

    private bool AvancarHabilitado(PassoSetup passo) => passo switch
    {
        PassoSetup.BemVindo => true,
        PassoSetup.Requisitos => _verificacaoOk,
        _ => true
    };

    private void Log(string texto)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => Log(texto)));
            return;
        }
        _txtLog.AppendText(texto + Environment.NewLine);
    }

    private void SetStatus(string texto)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetStatus(texto)));
            return;
        }
        _lblStatus.Text = texto;
    }

    // ========================= CONSTRUÇÃO DAS ETAPAS =========================

    private void ConstruirBemVindo()
    {
        var lblDir = new Label { Text = "Diretório de instalação (publicação)", Location = new Point(0, 6), Size = new Size(400, 20) };
        _txtDir = new TextBox { Location = new Point(0, 30), Size = new Size(680, 26), Text = _ctx.DirInstalacao };
        var btnProcurar = new Button { Text = "...", Location = new Point(688, 29), Size = new Size(36, 26) };
        btnProcurar.Click += (_, _) =>
        {
            using var fbd = new FolderBrowserDialog { Description = "Selecione a pasta onde o PDVStore será instalado." };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _txtDir.Text = fbd.SelectedPath;
                _ctx.DirInstalacao = fbd.SelectedPath;
            }
        };

        var lblBanco = new Label { Text = "Nome do banco de dados (LocalDB)", Location = new Point(0, 78), Size = new Size(400, 20) };
        _txtBanco = new TextBox { Location = new Point(0, 102), Size = new Size(300, 26), Text = _ctx.NomeBanco };

        var lblCs = new Label
        {
            Location = new Point(0, 150),
            Size = new Size(830, 120),
            ForeColor = Color.DimGray,
            Text =
                "Este instalador irá:\r\n" +
                "  • Verificar .NET SDK 8+, SQL Server Express LocalDB e a ferramenta dotnet-ef;\r\n" +
                "  • Baixar e instalar automaticamente qualquer pré-requisito ausente;\r\n" +
                "  • Configurar o PATH do usuário e a instância MS SQL LocalDB;\r\n" +
                "  • Aplicar as migrações do banco e publicar o aplicativo na pasta escolhida.\r\n\r\n" +
                $"Connection string: {InstaladorInfo.ConnectionStringPadrao(_ctx.NomeBanco)}"
        };

        _pnlConteudo.Controls.AddRange(new Control[] { lblDir, _txtDir, btnProcurar, lblBanco, _txtBanco, lblCs });
    }

    private void ConstruirRequisitos()
    {
        var lblResumo = new Label
        {
            Location = new Point(0, 2),
            Size = new Size(830, 22),
            ForeColor = Color.DimGray,
            Text = _verificacaoOk
                ? "Todos os pré-requisitos foram encontrados."
                : "Alguns pré-requisitos estão ausentes e serão instalados na próxima etapa."
        };

        _lvRequisitos = new ListView
        {
            Location = new Point(0, 30),
            Size = new Size(838, 296),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            BorderStyle = BorderStyle.FixedSingle
        };
        _lvRequisitos.Columns.Add("Pré-requisito", 230);
        _lvRequisitos.Columns.Add("Situação", 140);
        _lvRequisitos.Columns.Add("Detalhe", 466);
        _lvRequisitos.BeginUpdate();

        foreach (var r in _ctx.Requisitos)
        {
            var item = new ListViewItem(r.Nome);
            item.SubItems.Add(r.Status == StatusRequisito.Instalado ? "Instalado" : "Ausente");
            item.SubItems.Add(r.Detalhe.Replace(Environment.NewLine, " "));
            item.ForeColor = r.Status == StatusRequisito.Instalado ? Color.DarkGreen : Color.OrangeRed;
            _lvRequisitos.Items.Add(item);
        }
        _lvRequisitos.EndUpdate();

        _pnlConteudo.Controls.AddRange(new Control[] { lblResumo, _lvRequisitos });
    }

    private void ConstruirPreparacao()
    {
        var lbl = new Label
        {
            Location = new Point(0, 2),
            Size = new Size(830, 40),
            ForeColor = Color.DimGray,
            Text =
                "A preparação pode solicitar confirmação do UAC (para o LocalDB) e pode levar\nalguns minutos na primeira execução (download do SDK, do LocalDB e restauração do NuGet)."
        };
        _pnlConteudo.Controls.Add(lbl);
    }

    private void ConstruirConclusao()
    {
        _txtResumo = new TextBox
        {
            Location = new Point(0, 0),
            Size = new Size(838, 260),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(250, 250, 250)
        };

        _ckbAbrir = new CheckBox
        {
            Location = new Point(0, 272),
            Size = new Size(400, 24),
            Text = "Abrir o PDVStore ao concluir"
        };

        _pnlConteudo.Controls.AddRange(new Control[] { _txtResumo, _ckbAbrir });
        _txtResumo.Text = MontarResumo();
    }

    private string MontarResumo()
    {
        var linhas = new System.Collections.Generic.List<string>
        {
            "===== RESUMO DA INSTALAÇÃO =====",
            "",
            "Pré-requisitos:"
        };
        foreach (var r in _ctx.Requisitos)
            linhas.Add(string.Format("  {0,-35} {1}", r.Nome, r.Status == StatusRequisito.Instalado ? "OK" : "Ausente"));

        linhas.Add("");
        linhas.Add("Banco de dados: " + _ctx.NomeBanco + "  (" + _ctx.ConnectionString + ")");

        var proj = _ctx.LocalizarProjeto();
        linhas.Add("Projeto localizado: " + (string.IsNullOrEmpty(proj) ? "não" : proj));

        if (!string.IsNullOrEmpty(_ctx.CaminhoExeApp) && File.Exists(_ctx.CaminhoExeApp))
            linhas.Add("Aplicativo instalado em: " + _ctx.CaminhoExeApp);
        else
            linhas.Add("Executável: " + (_ctx.CaminhoExeApp ?? "não publicado"));

        linhas.Add("");
        linhas.Add("Credenciais iniciais: usuário 'Admin' / senha 'admin123' (altere após o 1º login).");
        return string.Join(Environment.NewLine, linhas);
    }

    // ========================= FLUXO =========================

    private async Task AvancarAsync()
    {
        switch (_passo)
        {
            case PassoSetup.BemVindo:
                _ctx.DirInstalacao = _txtDir.Text.Trim();
                _ctx.NomeBanco = string.IsNullOrWhiteSpace(_txtBanco.Text.Trim()) ? "PDV_StoreDB" : _txtBanco.Text.Trim();
                await ExecutarVerificacaoAsync();
                break;

            case PassoSetup.Requisitos:
                MostrarPasso(PassoSetup.Preparacao);
                try
                {
                    await ExecutarPreparacaoAsync();
                }
                catch (Exception ex)
                {
                    Log("ERRO: " + ex.Message);
                    SetStatus("Falha durante a preparação.");
                }
                break;

            case PassoSetup.Preparacao:
                MostrarPasso(PassoSetup.Conclusao);
                break;

            case PassoSetup.Conclusao:
                if (_ckbAbrir.Checked)
                    AbrirAplicativo();
                Close();
                break;
        }
    }

    private async Task ExecutarVerificacaoAsync()
    {
        _btnAvancar.Enabled = false;
        _btnFechar.Enabled = false;
        _lblStatus.Text = "Verificando pré-requisitos...";
        Cursor = Cursors.WaitCursor;

        try
        {
            await VerificadorRequisitos.CarregarAsync(_ctx, s => SetStatus("Verificação: " + s));
            _verificacaoOk = _ctx.Requisitos.All(r => r.Status == StatusRequisito.Instalado);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Falha na verificação de pré-requisitos:\n" + ex.Message,
                "PDV Store - Instalação", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _verificacaoOk = false;
        }
        finally
        {
            Cursor = Cursors.Default;
            _btnAvancar.Enabled = true;
            _btnFechar.Enabled = true;
        }

        MostrarPasso(PassoSetup.Requisitos);
    }

    private async Task ExecutarPreparacaoAsync()
    {
        _preparacaoEmAndamento = true;
        _btnVoltar.Visible = false;
        _btnAvancar.Enabled = false;
        _btnFechar.Enabled = false;
        _txtLog.Visible = true;
        SetStatus("Preparando o ambiente... aguarde.");

        try
        {
            _pgb.Minimum = 0;
            _pgb.Maximum = 7;
            _pgb.Value = 0;

            await PreparadorAmbiente.ExecutarAsync(
                _ctx,
                passo => { _pgb.Value = passo; SetStatus($"Etapa {passo} de 7 concluída."); },
                Log,
                System.Threading.CancellationToken.None);

            // reavalia os requisitos após a preparação (atualiza status do resumo)
            try { await VerificadorRequisitos.CarregarAsync(_ctx); } catch { }
            _verificacaoOk = _ctx.Requisitos.All(r => r.Status == StatusRequisito.Instalado);
            _pgb.Value = 7;
            SetStatus("Preparação concluída.");
        }
        finally
        {
            _preparacaoEmAndamento = false;
            _btnFechar.Enabled = true;
            _btnAvancar.Enabled = true;
        }
    }

    private void AbrirAplicativo()
    {
        if (string.IsNullOrEmpty(_ctx.CaminhoExeApp) || !File.Exists(_ctx.CaminhoExeApp))
        {
            var proj = _ctx.LocalizarProjeto();
            var candidato = string.IsNullOrEmpty(proj)
                ? _ctx.CaminhoExeApp
                : Path.Combine(Path.GetDirectoryName(proj)!, "bin", "Debug", "net8.0-windows7.0", "PDVStore.exe");

            if (!string.IsNullOrEmpty(candidato) && File.Exists(candidato))
                _ctx.CaminhoExeApp = candidato;
        }

        if (!string.IsNullOrEmpty(_ctx.CaminhoExeApp) && File.Exists(_ctx.CaminhoExeApp))
        {
            Process.Start(new ProcessStartInfo(_ctx.CaminhoExeApp) { UseShellExecute = true });
        }
        else
        {
            MessageBox.Show(this,
                "Não foi possível localizar o executável do PDVStore para abrir.",
                "PDV Store", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}