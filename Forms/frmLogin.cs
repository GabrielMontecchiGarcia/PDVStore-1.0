using Microsoft.Extensions.DependencyInjection;
using PDVStore.Models;
using PDVStore.Services;
using System;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public partial class frmLogin : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly UsuarioService _usuarioService;
        private IServiceScope? _sessionScope;

        public frmLogin(IServiceProvider serviceProvider, UsuarioService usuarioService)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _usuarioService = usuarioService;
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            var nome = txtUsuario.Text.Trim();
            var senha = txtSenha.Text;

            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(senha))
            {
                MessageBox.Show("Informe usuário e senha.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                var usuario = await _usuarioService.AutenticarAsync(nome, senha);

                if (usuario == null)
                {
                    MessageBox.Show("Credenciais inválidas!", "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!usuario.Ativo)
                {
                    MessageBox.Show("Usuário inativo. Contate o administrador.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Mantém um escopo vivo enquanto o usuário estiver logado,
                // para que as telas resolvidas via DI compartilhem o mesmo PDVContext.
                Session.CurrentUser = usuario;

                _sessionScope?.Dispose();
                _sessionScope = _serviceProvider.CreateScope();

                var frmMenu = _sessionScope.ServiceProvider.GetRequiredService<frmMenuPrincipal>();
                frmMenu.FormClosed += (_, _) =>
                {
                    _sessionScope?.Dispose();
                    _sessionScope = null;
                };

                frmMenu.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao realizar login: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void btnSair_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}