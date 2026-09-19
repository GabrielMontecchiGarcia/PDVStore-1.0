using Microsoft.Extensions.DependencyInjection;
using PDVStore.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public partial class frmMenuPrincipal : Form
    {
        private readonly IServiceProvider _serviceProvider;

        public frmMenuPrincipal(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "PDV Store - Menu Principal";
            StartPosition = FormStartPosition.CenterScreen;
            // AutoSize giraria apenas o formulário; deixamos dimensão fixa
            ClientSize = new Size(760, 460);
            BackColor = Color.White;

            Font = new Font("Segoe UI", 11F);

            var lblBemVindo = new Label
            {
                Text = $"Bem-vindo(a), {Session.CurrentUser?.Nome ?? "usuário"}",
                AutoSize = true,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Location = new Point(24, 18)
            };
            Controls.Add(lblBemVindo);

            var lblCaixa = new Label
            {
                Text = "Caixa: consultar em Abertura de Caixa.",
                AutoSize = true,
                Location = new Point(26, 60),
                ForeColor = Color.DimGray
            };
            Controls.Add(lblCaixa);

            var menu = new FlowLayoutPanel
            {
                Location = new Point(24, 96),
                Size = new Size(712, 320),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            AddMenuButton(menu, "Vender (PDV)", OnVender);
            AddMenuButton(menu, "Dashboard", OnDashboard);
            AddMenuButton(menu, "Produtos", OnProdutos);
            AddMenuButton(menu, "Estoque", OnEstoque);
            AddMenuButton(menu, "Clientes", OnClientes);
            AddMenuButton(menu, "Fornecedores", OnFornecedores);
            AddMenuButton(menu, "Compras", OnCompras);
            AddMenuButton(menu, "Abrir/Fechar Caixa", OnCaixa);
            AddMenuButton(menu, "Usuários", OnUsuarios);
            AddMenuButton(menu, "Sobre", OnSobre);

            Controls.Add(menu);

            var btnSair = new Button
            {
                Text = "Sair",
                Size = new Size(120, 40),
                Location = new Point(614, 404),
                BackColor = Color.Firebrick,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSair.Click += (_, _) => Close();
            Controls.Add(btnSair);
        }

        private static void AddMenuButton(FlowLayoutPanel menu, string text, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(160, 64),
                Margin = new Padding(0, 0, 12, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            btn.Click += onClick;
            menu.Controls.Add(btn);
        }

        private void OnVender(object? sender, EventArgs e) => OpenForm<frmPDV>();
        private void OnDashboard(object? sender, EventArgs e) => OpenForm<frmDashboard>();
        private void OnProdutos(object? sender, EventArgs e) => OpenForm<frmGerenciarProdutos>();
        private void OnEstoque(object? sender, EventArgs e) => OpenForm<frmEstoque>();
        private void OnClientes(object? sender, EventArgs e) => OpenForm<frmClientes>();
        private void OnFornecedores(object? sender, EventArgs e) => OpenForm<frmFornecedores>();
        private void OnCompras(object? sender, EventArgs e) => OpenForm<frmCompras>();
        private void OnCaixa(object? sender, EventArgs e) => OpenForm<frmCaixa>();
        private void OnUsuarios(object? sender, EventArgs e) => OpenForm<frmGerenciarUsuarios>();
        private void OnSobre(object? sender, EventArgs e) => OpenForm<frmSobre>();

        private void OpenForm<T>() where T : Form
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                using var frm = _serviceProvider.GetRequiredService<T>();
                frm.StartPosition = FormStartPosition.CenterScreen;
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir a tela: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
}