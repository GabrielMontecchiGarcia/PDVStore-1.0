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

        // Construtor para Novo Usuário
        public frmCadastroUsuario(PDVContext context)
        {
            InitializeComponent();
            _context = context;
            this.Text = "Cadastrar Novo Usuário";
            CarregarImagemPadrao();
            PreencherOpcoesPermissao();
            cboPermissao.SelectedValue = TipoPermissao.Operador; // Padrão: Operador (Caixa)
        }

        // Construtor para Edição
        public frmCadastroUsuario(PDVContext context, UsuarioCaixa usuario)
        {
            InitializeComponent();
            _context = context;
            _usuarioEmEdicao = usuario;
            this.Text = $"Editar Usuário - {usuario.Nome}";
            PreencherOpcoesPermissao();
            PreencherDadosParaEdicao();
        }

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

        // Designer may reference btnEscolherFoto_Click_1; provide a small adapter.
        private void btnEscolherFoto_Click_1(object sender, EventArgs e) => btnEscolherFoto_Click(sender, e);

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

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}