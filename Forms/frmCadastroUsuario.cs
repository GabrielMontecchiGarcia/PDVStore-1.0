using PDVStore.Data;
using PDVStore.Models;
using Serilog;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public partial class frmCadastroUsuario : Form
    {
        private readonly PDVContext _context;
        private readonly ErrorProvider _errorProvider = new();
        private string? _caminhoFotoSelecionada;
        private UsuarioCaixa? _usuarioEmEdicao;

        private sealed class OpcaoPermissao
        {
            public string Descricao { get; init; } = "";
            public TipoPermissao Valor { get; init; }
        }

        // CONSTRUTOR - MODO "NOVO USUÁRIO"
        // O que faz: abre o formulário limpo para criar um usuário em vez de editá-lo.
        // Por que existe: com dois construtores, a mesma tela serve tanto para cadastrar
        //   quanto para editar; aqui _usuarioEmEdicao fica null, indicando inclusão.
        // Dependências: recebe o PDVContext (EF Core) para salvar no banco e inicia a UI
        //   via CarregarImagemPadrao() e PreencherOpcoesPermissao().
        public frmCadastroUsuario(PDVContext context)
        {
            InitializeComponent();
            _context = context;
            this.Text = "Cadastrar Novo Usuário";
            CarregarImagemPadrao();
            PreencherOpcoesPermissao();
            cboPermissao.SelectedValue = TipoPermissao.Operador; // Padrão: Operador (Caixa)
        }

        // CONSTRUTOR - MODO "EDIÇÃO"
        // O que faz: carrega os dados de um usuário existente na tela para alterá-lo.
        // Por que existe: o nome é bloqueado (não pode ser trocado) e a permissão
        //   só muda se o operador logado for Administrador (regra de negócio).
        // Dependências: recebe PDVContext e o UsuarioCaixa a editar; chama
        //   PreencherOpcoesPermissao() e PreencherDadosParaEdicao().
        public frmCadastroUsuario(PDVContext context, UsuarioCaixa usuario)
        {
            InitializeComponent();
            _context = context;
            _usuarioEmEdicao = usuario;
            this.Text = $"Editar Usuário - {usuario.Nome}";
            PreencherOpcoesPermissao();
            PreencherDadosParaEdicao();
        }

        // O que faz: preenche o ComboBox cboPermissao com os três perfis possíveis.
        // Por que existe: o usuário precisa escolher entre Operador, Estoquista e
        //   Administrador; cada perfil libera telas diferentes do PDV.
        // Dependências: usa o enum TipoPermissao (PDVStore.Models); nada de banco aqui,
        //   apenas monta o DataSource exibido no formulário.
        private void PreencherOpcoesPermissao()
        {
            cboPermissao.DataSource = new[]
            {
                new OpcaoPermissao { Descricao = "Operador (Caixa) - apenas vendas (PDV)", Valor = TipoPermissao.Operador },
                new OpcaoPermissao { Descricao = "Estoquista - apenas cadastro de produtos", Valor = TipoPermissao.Estoquista },
                new OpcaoPermissao { Descricao = "Administrador - acesso total", Valor = TipoPermissao.Administrador }
            };
            cboPermissao.DisplayMember = nameof(OpcaoPermissao.Descricao);
            cboPermissao.ValueMember = nameof(OpcaoPermissao.Valor);
        }

        // O que faz: exibe a imagem padrão de usuário no PictureBox picFoto.
        // Por que existe: ao cadastrar um usuário novo, a foto pode ficar vazia; uma
        //   imagem padrão melhora a apresentação visual (UX).
        // Dependências: tenta ler um PNG em Resources/Images; se não existir, usa o
        //   recurso embutido Properties.Resources.user_default; em caso de erro zera a foto.
        private void CarregarImagemPadrao()
        {
            try
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var imagePath = Path.Combine(appDir, "Resources", "Images", "user_default.png");

                if (File.Exists(imagePath))
                    picFoto.Image = Image.FromFile(imagePath);
                else
                    picFoto.Image = Properties.Resources.user_default;
            }
            catch
            {
                picFoto.Image = null;
            }
        }

        // O que faz: copia os dados do usuário em edição para os campos da tela.
        // Por que existe: permite visualizar e ajustar permissão/foto sem digitar tudo
        //   de novo; o nome fica desabilitado por regra de negócio (identidade do login).
        // Dependências: lê _usuarioEmEdicao (set pelo construtor de edição) e o arquivo
        //   de foto salvo em disco (UsuarioCaixa.FotoPath).
        private void PreencherDadosParaEdicao()
        {
            if (_usuarioEmEdicao == null) return;

            txtNome.Text = _usuarioEmEdicao.Nome;
            txtNome.Enabled = false; // Não permitir alterar nome

            cboPermissao.SelectedValue = _usuarioEmEdicao.Permissao;

            if (!string.IsNullOrEmpty(_usuarioEmEdicao.FotoPath) && File.Exists(_usuarioEmEdicao.FotoPath))
            {
                picFoto.Image = Image.FromFile(_usuarioEmEdicao.FotoPath);
            }
        }

        // EVENTO - botão "Escolher Foto"
        // O que faz: abre um OpenFileDialog e carrega a imagem escolhida no picFoto.
        // Por que existe: dá ao usuário a opção de personalizar a foto no cadastro (UX);
        //   valida que o arquivo é imagem e reduz dimensões maiores que 800px (memória).
        // Dependências: guarda o caminho em _caminhoFotoSelecionada, usado depois por
        //   SalvarFotoUsuario() quando o formulário é salvo.
        private void btnEscolherFoto_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Imagens|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Selecionar Foto do Usuário"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _caminhoFotoSelecionada = ofd.FileName;
                try
                {
                    // Dispose previous image to free memory and avoid file locks
                    if (picFoto.Image != null)
                    {
                        var old = picFoto.Image;
                        picFoto.Image = null;
                        old.Dispose();
                    }

                    // Load image from stream (validates image data) and clone to avoid locking file
                    using var fs = new FileStream(_caminhoFotoSelecionada, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var img = Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: true);

                    // If image is very large, downscale to limit memory usage
                    const int MaxDimension = 800;
                    int width = img.Width;
                    int height = img.Height;

                    if (width > MaxDimension || height > MaxDimension)
                    {
                        double scale = Math.Min((double)MaxDimension / width, (double)MaxDimension / height);
                        int newW = Math.Max(1, (int)(width * scale));
                        int newH = Math.Max(1, (int)(height * scale));

                        var resized = new Bitmap(newW, newH);
                        using (var g = Graphics.FromImage(resized))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(img, 0, 0, newW, newH);
                        }

                        picFoto.Image = resized;
                    }
                    else
                    {
                        picFoto.Image = new Bitmap(img);
                    }
                }
                catch (OutOfMemoryException)
                {
                    MessageBox.Show("Arquivo não é uma imagem válida ou imagem muito grande.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao carregar a imagem: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ADAPTADOR DO DESIGNER
        // O que faz: encaminha o clique para btnEscolherFoto_Click (o handler original).
        // Por que existe: o Visual Studio, em alguns casos, registra no .Designer.cs o
        //   primeiro handler e depois gera uma cópia "_1"; manter este adaptador evita
        //   quebrar o vínculo do designer sem duplicar a lógica de carregar a imagem.
        // Dependências: chamado pelo Windows Forms; delega para o handler principal.
        private void btnEscolherFoto_Click_1(object sender, EventArgs e) => btnEscolherFoto_Click(sender, e);

        // EVENTO - botão "Salvar" (async)
        // O que faz: valida nome/senha/confirmação, cria ou atualiza o UsuarioCaixa e
        //   persiste no banco (SaveChangesAsync).
        // Por que existe: centraliza a regra de cadastro — nome único, senha mínima de
        //   6 caracteres e troca de permissão apenas se o logado for Administrador.
        // Dependências: usa PDVContext (EF Core), Session.CurrentUser (usuário logado),
        //   SalvarFotoUsuario(), Serilog (Log) e _errorProvider para destacar erros.
        private async void btnSalvar_Click(object sender, EventArgs e)
        {
            _errorProvider.Clear();

            try
            {
                if (string.IsNullOrWhiteSpace(txtNome.Text))
                {
                    _errorProvider.SetError(txtNome, "Nome do usuário é obrigatório!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtSenha.Text) || txtSenha.Text.Length < 6)
                {
                    _errorProvider.SetError(txtSenha, "Senha deve ter no mínimo 6 caracteres!");
                    return;
                }

                if (txtSenha.Text != txtConfirmaSenha.Text)
                {
                    _errorProvider.SetError(txtConfirmaSenha, "As senhas não coincidem!");
                    return;
                }

                UsuarioCaixa usuario;

                var permissaoSelecionada = cboPermissao.SelectedValue is TipoPermissao p
                    ? p
                    : TipoPermissao.Operador;

                if (_usuarioEmEdicao == null) // NOVO USUÁRIO
                {
                    if (_context.Usuarios.Any(u => u.Nome == txtNome.Text.Trim()))
                    {
                        _errorProvider.SetError(txtNome, "Este nome de usuário já está em uso!");
                        return;
                    }

                    usuario = new UsuarioCaixa
                    {
                        Nome = txtNome.Text.Trim(),
                        Permissao = permissaoSelecionada
                    };

                    _context.Usuarios.Add(usuario);
                }
                else // EDIÇÃO
                {
                    usuario = _usuarioEmEdicao;
                    // Atualiza permissão apenas se o usuário logado for Admin
                    if (PDVStore.Models.Session.CurrentUser?.EhAdmin() == true)
                    {
                        usuario.Permissao = permissaoSelecionada;
                    }
                }

                usuario.SetSenha(txtSenha.Text);
                usuario.SetFotoPath(SalvarFotoUsuario());

                await _context.SaveChangesAsync();

                Log.Information(_usuarioEmEdicao == null ?
                    "Novo usuário cadastrado: {Nome} | Permissão: {Permissao}" :
                    "Usuário atualizado: {Nome}", usuario.Nome, usuario.Permissao);

                MessageBox.Show("Usuário salvo com sucesso!", "Sucesso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao salvar usuário");
                MessageBox.Show($"Erro ao salvar usuário: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // O que faz: copia a foto selecionada para a pasta Resources/Users e retorna o caminho.
        // Por que existe: mantém as fotos dos usuários em um local padronizado do sistema,
        //   evitando perder o arquivo se a origem for removida; sem foto nova, preserva a anterior.
        // Dependências: usa _caminhoFotoSelecionada (set em btnEscolherFoto_Click), o diretório
        //   base do aplicativo e Serilog para registrar falhas sem derrubar o salvamento.
        private string? SalvarFotoUsuario()
        {
            if (string.IsNullOrEmpty(_caminhoFotoSelecionada))
                return _usuarioEmEdicao?.GetFotoPath(); // Mantém foto anterior

            try
            {
                string pastaFotos = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Users");
                Directory.CreateDirectory(pastaFotos);

                string nomeArquivo = $"user_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(_caminhoFotoSelecionada)}";
                string caminhoDestino = Path.Combine(pastaFotos, nomeArquivo);

                File.Copy(_caminhoFotoSelecionada, caminhoDestino, true);
                return caminhoDestino;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Falha ao salvar foto do usuário");
                return _usuarioEmEdicao?.GetFotoPath();
            }
        }

        // EVENTO - botão "Cancelar"
        // O que faz: fecha o formulário descartando qualquer alteração não salva.
        // Por que existe: oferece ao usuário uma saída sem consequências (UX); nada é
        //   gravado, pois o banco só é alterado em btnSalvar_Click.
        // Dependências: nenhuma além do próprio formulário (this.Close).
        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}