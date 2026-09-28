using System;
using System.Collections.Generic;
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
/// Assistente guiado de instalação do PDVStore: baixa o código do GitHub, verifica e instala os
/// pré-requisitos faltantes, recria a instância do banco, restaura as dependências e publica
/// o aplicativo, mostrando cada etapa ao usuário.
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
    private CheckBox _ckbRecriar = null!;

    // controles da etapa Requisitos
    private ListView _lvRequisitos = null!;

    // controles da etapa Preparacao
    private ListView _lvEtapas = null!;
    private List<Etapa> _etapas = null!;

    // controles da etapa Conclusão
    private TextBox _txtResumo = null!;
    private CheckBox _ckbAbrir = null!;

    // Construtor do formulário assistente: monta a interface (ConstruirUI) e exibe a etapa
    // inicial (BemVindo), deixando o fluxo guiado acontecer a partir da interação do usuário.
    public frmSetupWizard()
    {
        ConstruirUI();
        MostrarPasso(PassoSetup.BemVindo);
    }

    // Cria e posiciona todos os controles fixos da janela (títulos, barra de progresso, log,
    // botões Voltar/Avançar/Fechar) e registra os eventos de clique.
    // POR QUE em um método separado: mantém o construtor leve e organiza a criação da UI em um
    // único local, facilitando a leitura didática do que compõe a tela.
    private void ConstruirUI()
    {
        Text = "PDV Store - Instalação";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(900, 700);
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
            Size = new Size(850, 24),
            ForeColor = Color.DimGray,
            Text = "Verifica requisitos, prepara o ambiente e instala o sistema."
        };

        _pnlConteudo = new Panel
        {
            Location = new Point(20, 92),
            Size = new Size(860, 380),
            BackColor = Color.White
        };

        _pgb = new ProgressBar
        {
            Location = new Point(20, 482),
            Size = new Size(700, 20)
        };

        _lblStatus = new Label
        {
            Location = new Point(20, 508),
            Size = new Size(860, 20),
            ForeColor = Color.DimGray
        };

        _txtLog = new RichTextBox
        {
            Location = new Point(20, 534),
            Size = new Size(860, 124),
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
            Location = new Point(600, 662),
            Size = new Size(88, 30)
        };
        _btnAvancar = new Button
        {
            Text = "Avançar >",
            Location = new Point(696, 662),
            Size = new Size(96, 30)
        };
        _btnFechar = new Button
        {
            Text = "Cancelar",
            Location = new Point(800, 662),
            Size = new Size(80, 30),
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

    // Alterna o assistente para a etapa solicitada: limpa o painel de conteúdo, monta a tela
    // daquela etapa e atualiza textos, estado dos botões e barra de progresso.
    private void MostrarPasso(PassoSetup passo)
    {
        _passo = passo;
        _pnlConteudo.Controls.Clear();
        _lvEtapas = null!;
        _etapas = null!;

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

        _pgb.Value = passo == PassoSetup.Conclusao ? PreparadorAmbiente.PassosTotais : 0;
        _lblStatus.Text = "";
    }

    private string Descricao(PassoSetup passo) => passo switch
    {
        PassoSetup.BemVindo => "Informe as preferências de instalação e prossiga para a verificação dos pré-requisitos.",
        PassoSetup.Requisitos => "Resultado da verificação. Clique em \"Instalar\" para baixar o código do GitHub e preparar tudo.",
        PassoSetup.Preparacao => "Baixando o código, instalando pré-requisitos, recriando o banco e publicando o aplicativo. Aguarde...",
        _ => "Instalação concluída. Confira o resumo abaixo."
    };

    // O rótulo do botão muda conforme a etapa para deixar a próxima ação evidente.
    private string BotaoAvancarTexto(PassoSetup passo) => passo switch
    {
        PassoSetup.BemVindo => "Verificar >",
        PassoSetup.Requisitos => "Instalar >",
        PassoSetup.Preparacao => "Avançar >",
        _ => "Concluir"
    };

    // A tela de Requisitos NUNCA bloqueia o avanço. As etapas 2 a 4 existem justamente para
    // instalar o que estiver ausente, então travar aqui deixaria o instalador impossível de usar
    // justamente na máquina que mais precisa dele (SDK, LocalDB ou dotnet-ef faltando).
    // A tela apenas informa o que foi encontrado e o que será instalado; nenhuma checagem impede
    // seguir. Se algo for irrecuperável, a etapa 1 (download do código) ou a 10 (publicação)
    // abortam com a causa no log.
    private bool AvancarHabilitado(PassoSetup passo) => true;

    // Adiciona uma linha ao log, com segurança para chamadas vindas de threads de segundo plano.
    private void Log(string texto)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => Log(texto)));
            return;
        }
        _txtLog.AppendText(texto + Environment.NewLine);
    }

    // Atualiza o label de status, com o mesmo tratamento thread-safe do Log.
    private void SetStatus(string texto)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetStatus(texto)));
            return;
        }
        _lblStatus.Text = texto;
    }

    // ========================= ETAPAS DA PREPARAÇÃO =========================

    // Monta o checklist com as 10 etapas, todas ainda pendentes. A lista é criada uma única vez:
    // o instalador altera o status dos mesmos objetos e a tela os redesenha.
    private void ConstruirPreparacao()
    {
        _etapas = PreparadorAmbiente.MontarEtapas();

        _lvEtapas = new ListView
        {
            Location = new Point(0, 0),
            Size = new Size(858, 376),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            BorderStyle = BorderStyle.FixedSingle
        };
        _lvEtapas.Columns.Add("#", 40);
        _lvEtapas.Columns.Add("Etapa", 250);
        _lvEtapas.Columns.Add("Situação", 130);
        _lvEtapas.Columns.Add("Detalhe", 430);

        _pnlConteudo.Controls.Add(_lvEtapas);
        RenderizarEtapas();
    }

    // Copia o estado da etapa informada pelo instalador para a lista usada pelo checklist.
    //
    // POR QUE necessário: a tela cria a lista dela própria em ConstruirPreparacao
    // (PreparadorAmbiente.MontarEtapas), e o ExecutarAsync rastreia uma lista DIFERENTE, com
    // objetos próprios. Como o estado só é alterado nos objetos do ExecutarAsync, o
    // RenderizarEtapas redesenhava uma lista que ninguém mudava e o checklist ficava com as
    // 10 etapas em "Pendente" durante a instalação inteira — o mesmo vale para o resumo final,
    // que também lê esta lista. A correspondência é pelo Numero da etapa.
    private void SincronizarEtapa(Etapa origem)
    {
        if (_etapas == null)
            return;

        var alvo = _etapas.FirstOrDefault(e => e.Numero == origem.Numero);
        if (alvo == null)
            return;

        alvo.Status = origem.Status;
        alvo.Detalhe = origem.Detalhe;
    }

    // Redesenha o checklist a partir do estado atual das etapas. Chamado pelo instalador sempre
    // que uma etapa muda de status, então a lista reflete o progresso em tempo real.
    private void RenderizarEtapas()
    {
        if (_lvEtapas == null || _etapas == null)
            return;

        void Redesenhar()
        {
            _lvEtapas.BeginUpdate();
            _lvEtapas.Items.Clear();
            foreach (var e in _etapas)
            {
                var item = new ListViewItem(e.Numero.ToString("00"));
                item.SubItems.Add(e.Titulo);
                item.SubItems.Add(TextoStatus(e.Status));
                item.SubItems.Add(e.Detalhe);
                item.ForeColor = CorStatus(e.Status);
                _lvEtapas.Items.Add(item);
            }
            _lvEtapas.EndUpdate();

            var concluidas = _etapas.Count(x =>
                x.Status == StatusEtapa.Concluido || x.Status == StatusEtapa.Ignorado);
            _pgb.Maximum = PreparadorAmbiente.PassosTotais;
            _pgb.Value = Math.Min(concluidas, _pgb.Maximum);
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(Redesenhar));
            return;
        }
        Redesenhar();
    }

    private static string TextoStatus(StatusEtapa status) => status switch
    {
        StatusEtapa.Pendente => "Pendente",
        StatusEtapa.EmAndamento => "Em andamento",
        StatusEtapa.Concluido => "OK",
        StatusEtapa.Ignorado => "Ignorado",
        StatusEtapa.Falhou => "FALHOU",
        _ => ""
    };

    private static Color CorStatus(StatusEtapa status) => status switch
    {
        StatusEtapa.Pendente => Color.DimGray,
        StatusEtapa.EmAndamento => Color.RoyalBlue,
        StatusEtapa.Concluido => Color.DarkGreen,
        StatusEtapa.Ignorado => Color.DarkOrange,
        StatusEtapa.Falhou => Color.Firebrick,
        _ => Color.Black
    };

    // ========================= CONSTRUÇÃO DAS TELAS =========================

    // Etapa BemVindo: pasta de instalação, nome do banco, a opção de recriar o banco e um quadro
    // explicando o que o instalador vai fazer e de onde vem o código.
    private void ConstruirBemVindo()
    {
        var lblDir = new Label { Text = "Pasta de instalação (criada automaticamente)", Location = new Point(0, 6), Size = new Size(430, 20) };
        _txtDir = new TextBox { Location = new Point(0, 30), Size = new Size(700, 26), Text = _ctx.DirInstalacao };
        var btnProcurar = new Button { Text = "...", Location = new Point(708, 29), Size = new Size(36, 26) };
        btnProcurar.Click += (_, _) =>
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Selecione a pasta onde o PDVStore será instalado.",
                SelectedPath = Directory.Exists(_txtDir.Text) ? _txtDir.Text : string.Empty
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
                _txtDir.Text = fbd.SelectedPath;
        };

        var lblBanco = new Label { Text = "Nome do banco de dados (LocalDB)", Location = new Point(0, 66), Size = new Size(400, 20) };
        _txtBanco = new TextBox { Location = new Point(0, 90), Size = new Size(300, 26), Text = _ctx.NomeBanco };

        // Marcado por padrão: instalação limpa e previsível. Desmarcado, o instalador reaproveita
        // a instância existente e preserva os dados.
        _ckbRecriar = new CheckBox
        {
            Location = new Point(0, 122),
            Size = new Size(560, 22),
            Text = "Recriar a instância do banco do zero (apaga os dados existentes)",
            Checked = _ctx.RecriarBanco
        };

        var lblFonte = new Label
        {
            Location = new Point(0, 156),
            Size = new Size(858, 30),
            ForeColor = Color.DimGray,
            Text = $"Código-fonte: {InstaladorInfo.RepoPagina}  (ZIP do repositório; Git só como plano B)"
        };

        var lblCs = new Label
        {
            Location = new Point(0, 194),
            Size = new Size(858, 180),
            ForeColor = Color.DimGray,
            Text =
                "Este instalador irá, de forma automática:\r\n" +
                "  • Baixar o projeto completo do GitHub, incluindo todas as dependências NuGet;\r\n" +
                "  • Verificar .NET SDK 8+, SQL Server Express LocalDB e a ferramenta dotnet-ef;\r\n" +
                "  • Baixar e instalar automaticamente qualquer pré-requisito ausente;\r\n" +
                "  • Configurar o PATH do usuário e a instância MSSQL LocalDB;\r\n" +
                "  • Recriar a instância do banco, aplicando as migrations;\r\n" +
                "  • Publicar o aplicativo na pasta escolhida e criar o atalho na área de trabalho.\r\n\r\n" +
                $"Connection string: {InstaladorInfo.ConexaoPadrao(_ctx.NomeBanco)}\r\n" +
                $"O código baixado fica em: {Path.Combine(_ctx.DirInstalacao, InstaladorInfo.PastaFonteRelativa)}"
        };

        _pnlConteudo.Controls.AddRange(new Control[]
        {
            lblDir, _txtDir, btnProcurar,
            lblBanco, _txtBanco,
            _ckbRecriar, lblFonte, lblCs
        });
    }

    // Etapa Requisitos: ListView com o resultado da verificação, colorida por status. Requisitos
    // opcionais aparecem em cinza, pois não bloqueiam a instalação.
    private void ConstruirRequisitos()
    {
        var lblResumo = new Label
        {
            Location = new Point(0, 2),
            Size = new Size(858, 22),
            ForeColor = Color.DimGray,
            Text = _verificacaoOk
                ? "Pré-requisitos obrigatórios atendidos. Prossiga para baixar e instalar."
                : "Alguns pré-requisitos obrigatórios estão ausentes e serão instalados na próxima etapa."
        };

        _lvRequisitos = new ListView
        {
            Location = new Point(0, 30),
            Size = new Size(858, 344),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            BorderStyle = BorderStyle.FixedSingle
        };
        _lvRequisitos.Columns.Add("Pré-requisito", 210);
        _lvRequisitos.Columns.Add("Situação", 130);
        _lvRequisitos.Columns.Add("Detalhe", 510);
        _lvRequisitos.BeginUpdate();

        foreach (var r in _ctx.Requisitos)
        {
            var instalado = r.Status == StatusRequisito.Instalado;
            var item = new ListViewItem(r.Nome);
            item.SubItems.Add(instalado ? "Instalado" : r.Opcional ? "Ausente (opcional)" : "Ausente");
            item.SubItems.Add(r.Detalhe.Replace(Environment.NewLine, " "));
            item.ForeColor = instalado ? Color.DarkGreen : r.Opcional ? Color.DimGray : Color.OrangeRed;
            _lvRequisitos.Items.Add(item);
        }
        _lvRequisitos.EndUpdate();

        _pnlConteudo.Controls.AddRange(new Control[] { lblResumo, _lvRequisitos });
    }

    // Etapa Conclusão: resumo da instalação e a opção de abrir o PDVStore.
    private void ConstruirConclusao()
    {
        _txtResumo = new TextBox
        {
            Location = new Point(0, 0),
            Size = new Size(858, 300),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(250, 250, 250)
        };

        _ckbAbrir = new CheckBox
        {
            Location = new Point(0, 310),
            Size = new Size(400, 24),
            Text = "Abrir o PDVStore ao concluir",
            Checked = true
        };

        _pnlConteudo.Controls.AddRange(new Control[] { _txtResumo, _ckbAbrir });
        _txtResumo.Text = MontarResumo();
    }

    // Gera o resumo final: destino, origem do código, banco, resultado de cada etapa e as
    // credenciais iniciais. As etapas vêm do checklist já preenchido durante a preparação, o que
    // evita repetir a verificação dos pré-requisitos.
    private string MontarResumo()
    {
        var linhas = new List<string>
        {
            "===== RESUMO DA INSTALAÇÃO =====",
            "",
            "Destino:        " + _ctx.DirInstalacao,
            "Código-fonte:   " + _ctx.PastaFonte + "  (branch " + _ctx.Branch + ")",
            "Banco:          " + _ctx.NomeBanco,
            "Connection:     " + _ctx.ConnectionString,
            "Banco recriado: " + (_ctx.RecriarBanco ? "sim" : "não (instância existente aproveitada)"),
            ""
        };

        if (_etapas != null)
        {
            linhas.Add("Etapas:");
            foreach (var e in _etapas)
                linhas.Add(string.Format("  [{0:00}] {1,-42} {2}", e.Numero, e.Titulo, TextoStatus(e.Status)));

            var falhas = _etapas.Where(e => e.Status == StatusEtapa.Falhou).ToList();
            if (falhas.Count > 0)
            {
                linhas.Add("");
                linhas.Add("Etapas com falha:");
                foreach (var e in falhas)
                    linhas.Add("  - " + e.Titulo + ": " + e.Detalhe);
            }

            var ignoradas = _etapas.Where(e => e.Status == StatusEtapa.Ignorado).ToList();
            if (ignoradas.Count > 0)
            {
                linhas.Add("");
                linhas.Add("Etapas ignoradas (o sistema continua funcionando):");
                foreach (var e in ignoradas)
                    linhas.Add("  - " + e.Titulo + ": " + e.Detalhe);
            }
        }

        linhas.Add("");
        linhas.Add("Executável: " + (_ctx.CaminhoExeApp ?? "não publicado"));
        linhas.Add("");
        linhas.Add("Credenciais iniciais: usuário 'Admin' / senha 'admin123' (altere após o 1º login).");
        return string.Join(Environment.NewLine, linhas);
    }

    // ========================= FLUXO =========================

    // Núcleo do fluxo do assistente: decide o que fazer quando o botão "Avançar" é clicado,
    // dependendo da etapa atual.
    private async Task AvancarAsync()
    {
        switch (_passo)
        {
            case PassoSetup.BemVindo:
                if (!ColetarPreferencias())
                    return;
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
                    SetStatus("Falha durante a instalação.");
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

    // Lê os campos da tela de boas-vindas para o contexto, validando o destino antes de criar
    // qualquer pasta. A validação testa gravação de verdade, porque uma pasta pode existir e
    // ainda assim negar escrita (por exemplo, C:\Program Files sem elevação).
    private bool ColetarPreferencias()
    {
        _ctx.DirInstalacao = _txtDir.Text.Trim();
        _ctx.NomeBanco = string.IsNullOrWhiteSpace(_txtBanco.Text.Trim())
            ? InstaladorInfo.BancoPadrao
            : _txtBanco.Text.Trim();
        _ctx.RecriarBanco = _ckbRecriar.Checked;

        if (_ctx.TryValidarDestino(out var erro))
            return true;

        MessageBox.Show(this, erro, "PDV Store - Instalação",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        _txtDir.Focus();
        _txtDir.SelectAll();
        return false;
    }

    // Executa a verificação de pré-requisitos em segundo plano, desabilita a interface durante o
    // processo e, ao final, navega para a etapa de Requisitos.
    private async Task ExecutarVerificacaoAsync()
    {
        _btnAvancar.Enabled = false;
        _btnFechar.Enabled = false;
        _lblStatus.Text = "Verificando pré-requisitos...";
        Cursor = Cursors.WaitCursor;

        try
        {
            await VerificadorRequisitos.CarregarAsync(_ctx, s => SetStatus("Verificação: " + s));
            _verificacaoOk = VerificadorRequisitos.TudoPronto(_ctx);
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

    // Executa as 10 etapas da instalação, atualizando o checklist, a barra de progresso e o log.
    // O botão fica desabilitado durante a execução para o usuário não interromper um download
    // ou uma instalação no meio.
    private async Task ExecutarPreparacaoAsync()
    {
        _preparacaoEmAndamento = true;
        _btnVoltar.Visible = false;
        _btnAvancar.Enabled = false;
        _btnFechar.Enabled = false;
        _txtLog.Visible = true;
        SetStatus("Instalando... aguarde.");

        try
        {
            _pgb.Minimum = 0;
            _pgb.Maximum = PreparadorAmbiente.PassosTotais;
            _pgb.Value = 0;

            await PreparadorAmbiente.ExecutarAsync(
                _ctx,
                etapa =>
                {
                    SincronizarEtapa(etapa);
                    RenderizarEtapas();
                    SetStatus($"Etapa {etapa.Numero} de {PreparadorAmbiente.PassosTotais}: {TextoStatus(etapa.Status).ToLower()}");
                },
                Log,
                System.Threading.CancellationToken.None);

            SetStatus("Instalação concluída.");
        }
        finally
        {
            _preparacaoEmAndamento = false;
            _btnFechar.Enabled = true;
            _btnAvancar.Enabled = true;
        }
    }

    // Abre o executável publicado. O fallback para bin\Debug cobre o caso em que a publicação
    // não ocorreu (ex.: o dotnet publish falhou), usando o build de desenvolvimento.
    private void AbrirAplicativo()
    {
        if (string.IsNullOrEmpty(_ctx.CaminhoExeApp) || !File.Exists(_ctx.CaminhoExeApp))
        {
            var proj = _ctx.LocalizarProjeto();
            var candidato = string.IsNullOrEmpty(proj)
                ? _ctx.CaminhoExe
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
