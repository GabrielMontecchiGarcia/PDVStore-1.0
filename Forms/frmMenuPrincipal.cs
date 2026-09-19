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

        // Construtor do menu principal.
        // O QUE FAZ: recebe o IServiceProvider via DI, guarda a referência e chama
        // BuildUI para montar a interface com os atalhos por permissão.
        // POR QUE EXISTE: define o ponto de entrada depois do login e armazena o
        // provider que resolverá as telas de negócio ao clicar em cada botão.
        // QUEM CHAMA: criado pelo escopo da sessão dentro do frmLogin.
        public frmMenuPrincipal(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            BuildUI();
        }

        // Monta a interface principal: saudação, rodapé e botões de atalho.
        // O QUE FAZ: configura título/tamanho da janela, adiciona a mensagem de boas-
        // vindas com o nome do usuário logado, o painel de atalhos (FlowLayoutPanel)
        // preenchido conforme a permissão e o botão "Sair".
        // POR QUE EXISTE: separa a "construção visual" da "lógica de navegação",
        // facilitando entender como o menu é composto.
        // DEPENDÊNCIAS: Session.CurrentUser e AdicionarBotoesPorPermissao.
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

            AdicionarBotoesPorPermissao(menu);

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

        // Exibe os botões conforme o papel do usuário logado:
        //  - Administrador: acesso total a todas as telas.
        //  - Operador (Caixa): apenas o PDV.
        //  - Estoquista: apenas o cadastro de produtos.
        // Exibe os botões do menu conforme o papel (permissão) do usuário logado.
        // O QUE FAZ: lê Session.CurrentUser.Permissao e monta a lista de atalhos:
        // Administrador vê tudo; Estoquista só Produtos; Operador (default) só o PDV.
        // POR QUE EXISTE: aplica a regra de negócio de controle de acesso por papel,
        // garantindo que cada usuário veja apenas as telas que pode usar.
        // DEPENDÊNCIAS: Session/TipoPermissao e AddMenuButton; chamado por BuildUI.
        private void AdicionarBotoesPorPermissao(FlowLayoutPanel menu)
        {
            var permissao = Session.CurrentUser?.Permissao ?? TipoPermissao.Operador;

            switch (permissao)
            {
                case TipoPermissao.Administrador:
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
                    break;

                case TipoPermissao.Estoquista:
                    AddMenuButton(menu, "Produtos", OnProdutos);
                    break;

                default: // Operador (Caixa)
                    AddMenuButton(menu, "Vender (PDV)", OnVender);
                    break;
            }
        }

        // Cria um botão de atalho com estilo padrão e o liga a um handler.
        // O QUE FAZ: instancia um Button com texto, tamanho e cores, associa o evento
        // Click ao handler recebido e adiciona o botão ao painel de menu.
        // POR QUE EXISTE: evita duplicação de código ao montar dezenas de atalhos,
        // padronizando o visual e o comportamento de todos os botões do menu.
        // QUEM CHAMA: AdicionarBotoesPorPermissao.
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

        // Handler do atalho "Vender (PDV)": abre a tela de ponto de venda.
        // POR QUE EXISTE: é a tela principal do operador de caixa.
        // DEPENDÊNCIAS: delega para OpenForm<frmPDV>.
        private void OnVender(object? sender, EventArgs e) => OpenForm<frmPDV>();
        // Handler do atalho "Dashboard": abre o painel de indicadores gerenciais.
        // POR QUE EXISTE: dá ao administrador uma visão rápida das vendas/estoque.
        // DEPENDÊNCIAS: delega para OpenForm<frmDashboard>.
        private void OnDashboard(object? sender, EventArgs e) => OpenForm<frmDashboard>();
        // Handler do atalho "Produtos": abre o gerenciamento de produtos.
        // POR QUE EXISTE: permite cadastrar/editar produtos e preços.
        // DEPENDÊNCIAS: delega para OpenForm<frmGerenciarProdutos>.
        private void OnProdutos(object? sender, EventArgs e) => OpenForm<frmGerenciarProdutos>();
        // Handler do atalho "Estoque": abre a tela de movimentação de estoque.
        // POR QUE EXISTE: registrar entradas/saídas e ajustar quantidades.
        // DEPENDÊNCIAS: delega para OpenForm<frmEstoque>.
        private void OnEstoque(object? sender, EventArgs e) => OpenForm<frmEstoque>();
        // Handler do atalho "Clientes": abre a gestão de clientes.
        // POR QUE EXISTE: cadastrar clientes e controlar saldo fiado/limite.
        // DEPENDÊNCIAS: delega para OpenForm<frmClientes>.
        private void OnClientes(object? sender, EventArgs e) => OpenForm<frmClientes>();
        // Handler do atalho "Fornecedores": abre a gestão de fornecedores.
        // POR QUE EXISTE: manter os parceiros usados nas compras de mercadoria.
        // DEPENDÊNCIAS: delega para OpenForm<frmFornecedores>.
        private void OnFornecedores(object? sender, EventArgs e) => OpenForm<frmFornecedores>();
        // Handler do atalho "Compras": abre a tela de compras/entrada de mercadoria.
        // POR QUE EXISTE: registrar aquisições que alimentam o estoque.
        // DEPENDÊNCIAS: delega para OpenForm<frmCompras>.
        private void OnCompras(object? sender, EventArgs e) => OpenForm<frmCompras>();
        // Handler do atalho "Abrir/Fechar Caixa": abre a gestão do caixa.
        // POR QUE EXISTE: sem caixa aberto o PDV bloqueia as vendas.
        // DEPENDÊNCIAS: delega para OpenForm<frmCaixa>.
        private void OnCaixa(object? sender, EventArgs e) => OpenForm<frmCaixa>();
        // Handler do atalho "Usuários": abre a gestão de usuários.
        // POR QUE EXISTE: administrar contas, permissões e fotos dos operadores.
        // DEPENDÊNCIAS: delega para OpenForm<frmGerenciarUsuarios>.
        private void OnUsuarios(object? sender, EventArgs e) => OpenForm<frmGerenciarUsuarios>();
        // Handler do atalho "Sobre": abre a tela de informações da aplicação.
        // POR QUE EXISTE: exibir versão/créditos para suporte e identificação.
        // DEPENDÊNCIAS: delega para OpenForm<frmSobre>.
        private void OnSobre(object? sender, EventArgs e) => OpenForm<frmSobre>();

        // Abre qualquer tela (T : Form) resolvida pelo container DI, em modo modal.
        // O QUE FAZ: busca o formulário em _serviceProvider.GetRequiredService<T>,
        // centraliza na tela e exibe com ShowDialog; erros ao resolver são mostrados.
        // POR QUE EXISTE: evita repetir o padrão "resolver + centralizar + abrir" em
        // cada handler de atalho e propaga o mesmo contexto de banco da sessão.
        // DEPENDÊNCIAS: _serviceProvider; chamado pelos handlers OnVender/OnProdutos etc.
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