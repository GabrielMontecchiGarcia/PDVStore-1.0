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

        // Construtor da tela de login.
        // O QUE FAZ: recebe o IServiceProvider (para criar escopos e resolver o menu
        // principal via DI) e o UsuarioService (responsável por autenticar usuários).
        // POR QUE EXISTE: injeta as dependências usadas no fluxo de autenticação, para
        // que esta tela não precise instanciar serviços diretamente.
        // QUEM CHAMA: resolvido pelo container DI no Program.cs.
        public frmLogin(IServiceProvider serviceProvider, UsuarioService usuarioService)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _usuarioService = usuarioService;
        }

        // Handler do botão "Acessar": autentica o usuário e abre o menu principal.
        // O QUE FAZ: valida os campos, chama UsuarioService.AutenticarAsync e verifica
        // se o usuário está ativo; se OK, cria um escopo DI para a sessão, registra o
        // usuário em Session.CurrentUser e abre o frmMenuPrincipal.
        // POR QUE EXISTE: é a porta de entrada do sistema e aplica as regras de acesso
        // (credenciais inválidas e usuário inativo são bloqueados). O escopo é mantido
        // vivo enquanto o menu estiver aberto, para compartilhar o mesmo PDVContext.
        // DEPENDÊNCIAS: _usuarioService, Session (estado global) e _serviceProvider.
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

        // Handler do botão "Sair": encerra a aplicação.
        // O QUE FAZ: chama Application.Exit, fechando todos os formulários e o processo.
        // POR QUE EXISTE: oferece ao usuário uma saída limpa da tela de login.
        // QUEM CHAMA: registrado no Designer (btnSair.Click).
        private void btnSair_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}