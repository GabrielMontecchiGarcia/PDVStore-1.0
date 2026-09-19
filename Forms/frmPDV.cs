using PDVLoja.Services;
using PDVStore.Models;
using PDVStore.Services;
using PDVStore.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public class frmPDV : Form
    {
        private readonly VendaService _vendaService;
        private readonly EstoqueService _estoqueService;
        private readonly CaixaService _caixaService;
        private readonly ClienteService _clienteService;

        private readonly PDVViewModel _viewModel = new();

        private DataGridView dgvProdutos = null!;
        private DataGridView dgvItens = null!;
        private TextBox txtBuscaProduto = null!;
        private TextBox txtQuantidade = null!;
        private TextBox txtDesconto = null!;
        private TextBox txtValorRecebido = null!;
        private Label lblValorTroco = null!;
        private Label lblTotal = null!;
        private Label lblUsuarioLogado = null!;
        private Label lblCaixaStatus = null!;
        private ComboBox cmbFormaPagamento = null!;
        private ComboBox cmbCliente = null!;
        private Button btnFinalizar = null!;
        private TextBox txtRecibo = null!;

        private List<Cliente> _clientes = new();
        private List<Produto> _produtos = new();

        private PrintDocument? _printDocument;
        private string? _reciboTextoParaImprimir;

        // Construtor do PDV: injeta os serviços usados em toda a operação de venda.
        // O QUE FAZ: valida e armazena VendaService, EstoqueService, CaixaService e
        // ClienteService; monta a interface via BuildUI e assina o evento Load.
        // POR QUE EXISTE: para a tela funcionar é preciso registrar venda, consultar
        // produtos, validar caixa aberto e listar clientes — as quatro dependências.
        // QUEM CHAMA: resolvido pelo container DI (frmMenuPrincipal.OnVender).
        public frmPDV(VendaService vendaService, EstoqueService estoqueService,
                      CaixaService caixaService, ClienteService clienteService)
        {
            _vendaService = vendaService ?? throw new ArgumentNullException(nameof(vendaService));
            _estoqueService = estoqueService ?? throw new ArgumentNullException(nameof(estoqueService));
            _caixaService = caixaService ?? throw new ArgumentNullException(nameof(caixaService));
            _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));

            BuildUI();
            Load += OnLoad;
        }

        // Carga inicial do PDV quando o formulário é exibido.
        // O QUE FAZ: mostra o operador logado, verifica se há caixa aberto (desabilita
        // a finalização se não houver), carrega produtos, clientes e formas de pagamento.
        // POR QUE EXISTE: garante que a tela já abra com dados e valida a regra de
        // negócio "só vende com caixa aberto".
        // DEPENDÊNCIAS: _caixaService, _estoqueService, _clienteService e Session.
        private async void OnLoad(object? sender, EventArgs e)
        {
            try
            {
                lblUsuarioLogado.Text = $"Operador: {Session.CurrentUser?.Nome ?? "-"}";

                var caixa = await _caixaService.ObterCaixaAbertoAsync();
                if (caixa == null)
                {
                    lblCaixaStatus.Text = "Caixa: fechado. Abra o caixa para vender.";
                    btnFinalizar.Enabled = false;
                }
                else
                {
                    lblCaixaStatus.Text = $"Caixa #{caixa.Id} aberto desde {caixa.Abertura:dd/MM HH:mm}.";
                }

                await CarregarProdutosAsync();
                await CarregarClientesAsync();
                PreencherFormasPagamento();

                if (cmbFormaPagamento.Items.Count > 0)
                    cmbFormaPagamento.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar PDV: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Constrói toda a interface do PDV em código: grids, campos e grupos.
        // O QUE FAZ: cria os painéis Produtos, Carrinho, Pagamento e Recibo, posiciona
        // os controles e associa os eventos (busca, adicionar, remover, finalizar etc.).
        // POR QUE EXISTE: monta a tela dinamicamente para que a lógica fique clara,
        // sem depender de arquivos gerados pelo Designer.
        // DEPENDÊNCIAS: chama ConfigureProdutosGrid e ConfigureItensGrid.
        private void BuildUI()
        {
            Text = "PDV - Ponto de Venda";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            lblUsuarioLogado = new Label { Text = "Operador: -", AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Location = new Point(20, 10) };
            Controls.Add(lblUsuarioLogado);

            lblCaixaStatus = new Label { Text = "Caixa: -", AutoSize = true, Location = new Point(20, 38), ForeColor = Color.DimGray };
            Controls.Add(lblCaixaStatus);

            // ===== Painel Produtos (esquerda) =====
            var grpProdutos = new GroupBox { Text = "Produtos", Location = new Point(20, 70), Size = new Size(610, 560) };

            var lblBusca = new Label { Text = "Buscar:", Location = new Point(12, 28), AutoSize = true };
            txtBuscaProduto = new TextBox { Location = new Point(90, 25), Size = new Size(300, 26) };
            txtBuscaProduto.KeyDown += async (_, args) => { if (args.KeyCode == Keys.Enter) await BuscarAsync(); };

            var btnBuscar = new Button { Text = "Buscar", Location = new Point(400, 24), Size = new Size(90, 28) };
            btnBuscar.Click += async (_, _) => await BuscarAsync();

            var lblQtd = new Label { Text = "Qtd:", Location = new Point(12, 68), AutoSize = true };
            txtQuantidade = new TextBox { Location = new Point(90, 64), Size = new Size(100, 26), Text = "1" };

            var btnAdicionar = new Button { Text = "Adicionar (Enter)", Location = new Point(400, 62), Size = new Size(200, 30), BackColor = Color.ForestGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAdicionar.Click += AdicionarItem;

            dgvProdutos = new DataGridView
            {
                Location = new Point(10, 100),
                Size = new Size(590, 445),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false
            };
            ConfigureProdutosGrid();

            grpProdutos.Controls.AddRange(new Control[] { lblBusca, txtBuscaProduto, btnBuscar, lblQtd, txtQuantidade, btnAdicionar, dgvProdutos });
            Controls.Add(grpProdutos);

            // ===== Painel Carrinho / Pagamento (direita) =====
            var grpCarrinho = new GroupBox { Text = "Carrinho", Location = new Point(650, 70), Size = new Size(700, 350) };

            dgvItens = new DataGridView
            {
                Location = new Point(10, 24),
                Size = new Size(680, 250),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false
            };
            ConfigureItensGrid();

            var btnRemover = new Button { Text = "Remover item", Location = new Point(10, 286), Size = new Size(120, 30), BackColor = Color.Firebrick, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnRemover.Click += RemoverItem;

            var btnLimpar = new Button { Text = "Limpar venda", Location = new Point(140, 286), Size = new Size(120, 30), BackColor = Color.DimGray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLimpar.Click += LimparVenda;

            grpCarrinho.Controls.AddRange(new Control[] { dgvItens, btnRemover, btnLimpar });

            // ===== Pagamento =====
            var grpPagamento = new GroupBox { Text = "Pagamento", Location = new Point(650, 430), Size = new Size(700, 200) };

            var lblForma = new Label { Text = "Forma de pagamento:", Location = new Point(12, 28), AutoSize = true };
            cmbFormaPagamento = new ComboBox { Location = new Point(170, 24), Size = new Size(200, 28), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblCliente = new Label { Text = "Cliente:", Location = new Point(12, 64), AutoSize = true };
            cmbCliente = new ComboBox { Location = new Point(170, 60), Size = new Size(300, 28), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblDesconto = new Label { Text = "Desconto (R$):", Location = new Point(12, 100), AutoSize = true };
            txtDesconto = new TextBox { Location = new Point(170, 96), Size = new Size(120, 26), Text = "0,00" };
            txtDesconto.TextChanged += (_, _) => AtualizarTotais();

            var lblRecebido = new Label { Text = "Valor recebido:", Location = new Point(390, 28), AutoSize = true };
            txtValorRecebido = new TextBox { Location = new Point(510, 24), Size = new Size(160, 26) };
            txtValorRecebido.TextChanged += (_, _) => AtualizarTroco();

            var lblTroco = new Label { Text = "Troco:", Location = new Point(390, 64), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            lblValorTroco = new Label { Text = "R$ 0,00", Location = new Point(510, 60), AutoSize = true, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.DarkRed };

            var lblTotalLabel = new Label { Text = "Total:", Location = new Point(12, 150), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            lblTotal = new Label { Text = "R$ 0,00", Location = new Point(170, 146), AutoSize = true, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.DarkGreen };

            btnFinalizar = new Button { Text = "Finalizar venda", Location = new Point(510, 130), Size = new Size(160, 50), BackColor = Color.ForestGreen, ForeColor = Color.White, Font = new Font("Segoe UI", 12F, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnFinalizar.Click += FinalizarVenda;

            grpPagamento.Controls.AddRange(new Control[]
            {
                lblForma, cmbFormaPagamento, lblCliente, cmbCliente, lblDesconto, txtDesconto,
                lblRecebido, txtValorRecebido, lblTroco, lblValorTroco, lblTotalLabel, lblTotal, btnFinalizar
            });

            Controls.Add(grpCarrinho);
            Controls.Add(grpPagamento);

            // ===== Recibo =====
            var grpRecibo = new GroupBox { Text = "Recibo", Location = new Point(1370, 70), Size = new Size(520, 560) };

            txtRecibo = new TextBox
            {
                Location = new Point(10, 24),
                Size = new Size(500, 520),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false,
                Font = new Font("Courier New", 10F),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            grpRecibo.Controls.Add(txtRecibo);
            Controls.Add(grpRecibo);
        }

        // Configura as colunas do grid de produtos disponíveis para venda.
        // O QUE FAZ: define colunas ligadas ao modelo Produto (Id, código, nome,
        // preço em moeda, estoque) e ativa o preenchimento automático da quantidade.
        // POR QUE EXISTE: mostra ao operador o que pode ser vendido e quanto há em
        // estoque; o evento SelectionChanged dispara PreencherQuantidadePadrao.
        // QUEM CHAMA: BuildUI.
        private void ConfigureProdutosGrid()
        {
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", Width = 50 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", HeaderText = "Código", DataPropertyName = "CodigoBarras", Width = 120 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "Produto", DataPropertyName = "Nome", Width = 260 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Preco",
                HeaderText = "Preço",
                DataPropertyName = "Preco",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estoque", HeaderText = "Estoque", DataPropertyName = "Estoque", Width = 60 });
            foreach (DataGridViewColumn c in dgvProdutos.Columns) c.FillWeight = Math.Max(50, c.Width);
            dgvProdutos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProdutos.SelectionChanged += (_, _) => PreencherQuantidadePadrao();
        }

        // Configura as colunas do grid do carrinho de itens da venda.
        // O QUE FAZ: cria colunas ligadas ao ItemVenda (produto, quantidade, preço
        // unitário e subtotal), com valores monetários formatados.
        // POR QUE EXISTE: o carrinho reflete o PDVViewModel e precisa dessas colunas
        // para exibir corretamente o que será registrado na venda.
        // QUEM CHAMA: BuildUI.
        private void ConfigureItensGrid()
        {
            dgvItens.Columns.Add(new DataGridViewTextBoxColumn { Name = "Produto", HeaderText = "Produto", DataPropertyName = "NomeProduto", Width = 280 });
            dgvItens.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qtd", HeaderText = "Qtd", DataPropertyName = "Quantidade", Width = 70 });
            dgvItens.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Preco",
                HeaderText = "Preço Unit.",
                DataPropertyName = "PrecoUnitario",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });
            dgvItens.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Subtotal",
                HeaderText = "Subtotal",
                DataPropertyName = "Subtotal",
                Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });
            foreach (DataGridViewColumn c in dgvItens.Columns) c.FillWeight = Math.Max(50, c.Width);
            dgvItens.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        // Reinicia a quantidade digitada para 1 ao trocar o produto selecionado.
        // O QUE FAZ: se existe um Produto na linha corrente do grid, define a
        // quantidade padrão como "1".
        // POR QUE EXISTE: facilita a operação — o operador normalmente adiciona 1
        // unidade de cada item, e só ajusta a quantidade quando necessário.
        // QUEM CHAMA: evento dgvProdutos.SelectionChanged.
        private void PreencherQuantidadePadrao()
        {
            if (dgvProdutos.CurrentRow?.DataBoundItem is Produto p)
                txtQuantidade.Text = "1";
        }

        // Efetua a busca de produtos usando o texto digitado no campo de pesquisa.
        // O QUE FAZ: repassa o texto do txtBuscaProduto para CarregarProdutosAsync.
        // POR QUE EXISTE: centraliza o ponto de entrada da pesquisa (acionado pelo
        // botão "Buscar" e pela tecla Enter no campo).
        // DEPENDÊNCIAS: chama CarregarProdutosAsync.
        private async Task BuscarAsync()
        {
            await CarregarProdutosAsync(txtBuscaProduto.Text.Trim());
        }

        // Carrega produtos no grid, listando todos ou filtrando por texto.
        // O QUE FAZ: sem filtro consulta EstoqueService.GetAllAsync; com filtro usa
        // EstoqueService.BuscarAsync. O resultado vira o DataSource do dgvProdutos.
        // POR QUE EXISTE: alimenta a escolha de itens pelo operador e é usada tanto na
        // carga inicial (OnLoad) quanto na busca em tempo real.
        // DEPENDÊNCIAS: _estoqueService; atualiza dgvProdutos e o campo _produtos.
        private async Task CarregarProdutosAsync(string filtro = "")
        {
            _produtos = string.IsNullOrWhiteSpace(filtro)
                ? await _estoqueService.GetAllAsync()
                : await _estoqueService.BuscarAsync(filtro);

            dgvProdutos.DataSource = _produtos;
        }

        // Carrega a lista de clientes no ComboBox, incluindo "Consumidor Final".
        // O QUE FAZ: consulta ClienteService.ListarAsync e preenche o cmbCliente com a
        // opção padrão "— Consumidor Final —" seguida dos nomes (mostrando débito).
        // POR QUE EXISTE: clientes são necessários principalmente para vendas fiadas,
        // mas o combo sempre inicia no consumidor final para vendas comuns.
        // DEPENDÊNCIAS: _clienteService; atualiza cmbCliente.
        private async Task CarregarClientesAsync()
        {
            _clientes = await _clienteService.ListarAsync();
            cmbCliente.Items.Clear();
            cmbCliente.Items.Add("— Consumidor Final —");
            foreach (var c in _clientes)
                cmbCliente.Items.Add($"{c.Nome} {(c.SaldoDevedor > 0 ? $"(débito {c.SaldoDevedor:C2})" : "")}");
            if (cmbCliente.Items.Count > 0)
                cmbCliente.SelectedIndex = 0;
        }

        // Preenche o ComboBox de formas de pagamento disponíveis.
        // O QUE FAZ: adiciona as opções Dinheiro, PIX, Cartão Crédito, Cartão Débito
        // e Fiado ao cmbFormaPagamento.
        // POR QUE EXISTE: fixa as formas aceitas pela regra de negócio do caixa e
        // evita que o operador digite um valor livre inválido.
        // QUEM CHAMA: OnLoad.
        private void PreencherFormasPagamento()
        {
            cmbFormaPagamento.Items.AddRange(new object[]
            {
                "Dinheiro", "PIX", "Cartão Crédito", "Cartão Débito", "Fiado"
            });
        }

        // Retorna o Cliente efetivamente selecionado no combo, ou null para consumidor final.
        // O QUE FAZ: converte o índice do combo na posição da lista _clientes (índice 0
        // é "Consumidor Final", índices 1..n equivalem a _clientes[0..n-1]).
        // POR QUE EXISTE: liga a escolha visual ao objeto de negócio Cliente, necessário
        // nas vendas fiadas e no recibo correspondente.
        // DEPENDÊNCIAS: _clientes e cmbCliente.
        private Cliente? ObterClienteSelecionado()
        {
            int idx = cmbCliente.SelectedIndex;
            return (idx <= 0 || idx > _clientes.Count) ? null : _clientes[idx - 1];
        }

        // Adiciona o produto selecionado ao carrinho na quantidade digitada.
        // O QUE FAZ: valida seleção, quantidade e estoque disponível; se o produto já
        // estiver no carrinho soma a quantidade, senão cria um novo ItemVenda.
        // POR QUE EXISTE: aplica a regra de negócio de venda — não addar quantidade
        // maior que o estoque e junta itens repetidos para um único subtotal.
        // DEPENDÊNCIAS: dgvProdutos, txtQuantidade e o _viewModel; depois RefreshCarrinho.
        private void AdicionarItem(object? sender, EventArgs e)
        {
            if (dgvProdutos.CurrentRow?.DataBoundItem is not Produto produto)
            {
                MessageBox.Show("Selecione um produto na lista.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtQuantidade.Text, out int quantidade) || quantidade <= 0)
            {
                MessageBox.Show("Informe uma quantidade válida.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (quantidade > produto.Estoque)
            {
                MessageBox.Show($"Estoque disponível: {produto.Estoque}.", "Estoque insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existente = _viewModel.Itens.FirstOrDefault(i => i.ProdutoId == produto.Id);
            if (existente != null)
            {
                existente.Quantidade += quantidade;
                existente.Produto = produto;
            }
            else
            {
                _viewModel.Itens.Add(new ItemVenda
                {
                    ProdutoId = produto.Id,
                    Produto = produto,
                    Quantidade = quantidade,
                    PrecoUnitario = produto.Preco
                });
            }

            RefreshCarrinho();
            txtQuantidade.Text = "1";
            txtBuscaProduto.Focus();
        }

        // Atualiza a exibição do carrinho e os totais da venda.
        // O QUE FAZ: reatribui o DataSource do dgvItens com a lista do _viewModel e
        // recalcula os totais (lblTotal e troco).
        // POR QUE EXISTE: após qualquer alteração nos itens (adicionar/remover) o grid
        // precisa ser reexibido para o operador ver o estado atual da venda.
        // QUEM CHAMA: AdicionarItem e RemoverItem.
        private void RefreshCarrinho()
        {
            dgvItens.DataSource = null;
            dgvItens.DataSource = _viewModel.Itens;
            AtualizarTotais();
        }

        // Remove o item selecionado do carrinho de venda.
        // O QUE FAZ: se existe um ItemVenda na linha corrente do dgvItens, remove-o da
        // lista do _viewModel e atualiza o grid.
        // POR QUE EXISTE: permite corrigir a venda antes de finalizar, retirando itens
        // adicionados por engano.
        // DEPENDÊNCIAS: dgvItens, _viewModel e RefreshCarrinho.
        private void RemoverItem(object? sender, EventArgs e)
        {
            if (dgvItens.CurrentRow?.DataBoundItem is ItemVenda item)
            {
                _viewModel.Itens.Remove(item);
                RefreshCarrinho();
            }
        }

        // Esvazia completamente o carrinho e reseta os campos de pagamento.
        // O QUE FAZ: chama _viewModel.Limpar(), zera desconto e valor recebido, limpa o
        // grid e recalcula os totais.
        // POR QUE EXISTE: oferece ao operador a opção de cancelar/descartar toda a venda
        // em andamento e é reutilizada após uma venda concluída.
        // DEPENDÊNCIAS: _viewModel, txtDesconto, txtValorRecebido e AtualizarTotais.
        private void LimparVenda(object? sender, EventArgs e)
        {
            _viewModel.Limpar();
            txtDesconto.Text = "0,00";
            txtValorRecebido.Clear();
            dgvItens.DataSource = null;
            AtualizarTotais();
        }

        // Recalcula o desconto e o total da venda exibidos na tela.
        // O QUE FAZ: interpreta o txtDesconto (nunca negativo), aplica ao _viewModel e
        // atualiza o rótulo do total; depois recalcula o troco.
        // POR QUE EXISTE: o total precisa reagir imediatamente quando o operador digita
        // um desconto — regra comercial comum em PDV.
        // DEPENDÊNCIAS: _viewModel (Total) e AtualizarTroco; disparado pelo TextChanged.
        private void AtualizarTotais()
        {
            _viewModel.Desconto = 0;
            if (decimal.TryParse(txtDesconto.Text, out var desconto))
                _viewModel.Desconto = Math.Max(0, desconto);

            lblTotal.Text = _viewModel.Total.ToString("C2");
            AtualizarTroco();
        }

        // Calcula o troco com base no valor recebido e no total da venda.
        // O QUE FAZ: subtrai o total do _viewModel do valor digitado em txtValorRecebido,
        // guarda em _troco e mostra no rótulo (ou "Valor insuficiente").
        // POR QUE EXISTE: orienta o operador sobre quanto devolver ao cliente e bloqueia
        // visualmente valores recebidos abaixo do total.
        // DEPENDÊNCIAS: txtValorRecebido, _viewModel.Total e lblValorTroco.
        private void AtualizarTroco()
        {
            _troco = 0;
            if (decimal.TryParse(txtValorRecebido.Text, out decimal recebido))
                _troco = recebido - _viewModel.Total;

            lblValorTroco.Text = _troco >= 0 ? _troco.ToString("C2") : "Valor insuficiente";
        }

        private decimal _troco;

        // Gera o texto do recibo de venda a partir da venda registrada e do cliente.
        // O QUE FAZ: monta um StringBuilder com o "cupom" (cabeçalho, itens, desconto,
        // total, forma de pagamento e troco; para fiado inclui linha de assinatura).
        // POR QUE EXISTE: produz o comprovante legível em fonte monoespaçada, usado
        // tanto para exibir na tela quanto para imprimir/salvar.
        // DEPENDÊNCIAS: _viewModel (itens) e Session (operador).
        private string GerarReciboTexto(Venda venda, Cliente? cliente)
        {
            var sb = new StringBuilder();
            sb.AppendLine("===============================================");
            sb.AppendLine("                   RECIBO DE VENDA");
            sb.AppendLine("===============================================");
            sb.AppendLine($"Venda Nº: {venda.Id}");
            sb.AppendLine($"Data: {venda.DataVenda.ToLocalTime():dd/MM/yyyy HH:mm:ss}");
            if (Session.CurrentUser != null)
                sb.AppendLine($"Operador: {Session.CurrentUser.Nome}");
            sb.AppendLine("-----------------------------------------------");
            sb.AppendLine($"{"Produto",-30}{"Qtd",4}{"Valor Unit.",12}");
            sb.AppendLine("-----------------------------------------------");
            foreach (var item in _viewModel.Itens)
            {
                var nome = string.IsNullOrWhiteSpace(item.NomeProduto) ? "Item" : item.NomeProduto;
                sb.AppendLine($"{nome,-30}{item.Quantidade,4}{item.PrecoUnitario,12:C2}");
            }
            sb.AppendLine("-----------------------------------------------");
            if (venda.Desconto > 0)
                sb.AppendLine($"{"Desconto:",-30}{venda.Desconto,16:C2}");
            sb.AppendLine($"{"TOTAL:",-30}{venda.ValorTotal,16:C2}");
            sb.AppendLine("-----------------------------------------------");
            sb.AppendLine($"Forma de pagamento: {venda.FormaPagamento}");
            if (venda.FormaPagamento == "Dinheiro" && _troco > 0)
                sb.AppendLine($"Troco: {_troco:C2}");
            if (venda.FormaPagamento == "Fiado" && cliente != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Cliente: {cliente.Nome}");
                sb.AppendLine();
                sb.AppendLine("_______________________________________________");
                sb.AppendLine("            Assinatura do cliente");
            }
            return sb.ToString();
        }

        // Salva o texto do recibo em um arquivo .txt na pasta "Recibos".
        // O QUE FAZ: garante a existência da pasta (junto ao executável), cria um nome
        // de arquivo com data/hora e grava o texto em UTF-8.
        // POR QUE EXISTE: mantém um histórico físico auditável das vendas na máquina,
        // além do registro no banco de dados.
        // DEPENDÊNCIAS: System.IO; chamado por FinalizarVenda (retorna o caminho).
        private string SalvarRecibo(string texto)
        {
            var pasta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recibos");
            Directory.CreateDirectory(pasta);
            var caminho = Path.Combine(pasta, $"Recibo_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(caminho, texto, Encoding.UTF8);
            return caminho;
        }

        // Prepara o documento de impressão, ligando apenas a página a desenhar.
        // O QUE FAZ: cria um PrintDocument e assina o evento PrintPage com o método
        // que desenha o texto do recibo no papel.
        // POR QUE EXISTE: a impressão no WinForms é dirigida por eventos — é preciso
        // configurar o documento uma única vez e depois disparar .Print().
        // QUEM CHAMA: ImprimirRecibo (somente se _printDocument ainda for nulo).
        private void ConfigurarImpressao()
        {
            _printDocument = new PrintDocument();
            _printDocument.PrintPage += ImprimirRecibo_PrintPage;
        }

        // Desenha cada linha do recibo na página impressa.
        // O QUE FAZ: se há texto a imprimir, divide-o em linhas e usa DrawString com
        // fonte Courier New, avançando a posição Y linha a linha.
        // POR QUE EXISTE: é o handler de PrintPage que transforma o texto em um layout
        // de impressão limpo e legível.
        // QUEM CHAMA: disparado pelo PrintDocument durante .Print().
        private void ImprimirRecibo_PrintPage(object? sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_reciboTextoParaImprimir))
                return;

            using var fonte = new Font("Courier New", 10F);
            var linhas = _reciboTextoParaImprimir.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            float y = e.MarginBounds.Top;
            foreach (var linha in linhas)
            {
                e.Graphics!.DrawString(linha, fonte, Brushes.Black, e.MarginBounds.Left, y);
                y += fonte.GetHeight(e.Graphics);
            }
        }

        // Abre o diálogo de impressão e envia o recibo para a impressora escolhida.
        // O QUE FAZ: garante que o PrintDocument exista, mostra um PrintDialog (com
        // pré-visualização do sistema) e chama _printDocument.Print(); erros são avisados.
        // POR QUE EXISTE: entrega ao operador o controle da impressão (impressora,
        // orientação etc.) antes de gerar o papel.
        // DEPENDÊNCIAS: ConfigurarImpressao e _reciboTextoParaImprimir.
        private void ImprimirRecibo()
        {
            if (_printDocument == null)
                ConfigurarImpressao();

            using var dlg = new PrintDialog { Document = _printDocument, UseEXDialog = true };
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                _printDocument!.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao imprimir recibo: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Finaliza a venda: valida, registra no banco, exibe recibo e limpa o carrinho.
        // O QUE FAZ: valida itens; para fiado exige cliente e confere limite de crédito;
        // para dinheiro exige troco >= 0; monta a Venda, chama VendaService.
        // RegistrarVendaAsync, gera/salva/imprime o recibo e reinicia o PDV.
        // POR QUE EXISTE: é o coração da regra de negócio do caixa — garante que o
        // pagamento é coerente e que o estoque/valor são controlados pelo serviço.
        // DEPENDÊNCIAS: _viewModel, _vendaService, Session; usa GerarReciboTexto,
        // SalvarRecibo, ImprimirRecibo, ObterClienteSelecionado e LimparVenda.
        private async void FinalizarVenda(object? sender, EventArgs e)
        {
            if (_viewModel.Itens.Count == 0)
            {
                MessageBox.Show("Adicione itens à venda.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var formaPagamento = cmbFormaPagamento.Text;
            var usuario = Session.CurrentUser;

            if (formaPagamento == "Fiado")
            {
                var clienteFiado = ObterClienteSelecionado();
                if (clienteFiado == null)
                {
                    MessageBox.Show("Venda fiada exige a seleção de um cliente.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (clienteFiado.LimiteCredito > 0 &&
                    clienteFiado.SaldoDevedor + _viewModel.Total > clienteFiado.LimiteCredito)
                {
                    MessageBox.Show(
                        "Crédito insuficiente para venda fiada.\n\n" +
                        $"Limite de crédito: {clienteFiado.LimiteCredito:C2}\n" +
                        $"Débito atual: {clienteFiado.SaldoDevedor:C2}\n" +
                        $"Valor da venda: {_viewModel.Total:C2}",
                        "Crédito insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (formaPagamento == "Dinheiro" && _troco < 0)
            {
                MessageBox.Show("Valor recebido é insuficiente para completar a venda.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (usuario == null)
            {
                MessageBox.Show("Usuário não autenticado.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Cliente (identificação da venda)
            var cliente = ObterClienteSelecionado();

            btnFinalizar.Enabled = false;
            var previousCursor = Cursor;
            Cursor = Cursors.WaitCursor;

            try
            {
                var venda = new Venda
                {
                    UsuarioCaixaId = usuario.Id,
                    Itens = _viewModel.Itens.Select(i => new ItemVenda
                    {
                        ProdutoId = i.ProdutoId,
                        Quantidade = i.Quantidade,
                        PrecoUnitario = i.PrecoUnitario
                    }).ToList(),
                    Desconto = _viewModel.Desconto,
                    FormaPagamento = formaPagamento,
                    ValorTotal = _viewModel.Total,
                    ClienteId = cliente?.Id
                };

                var vendaRegistrada = await _vendaService.RegistrarVendaAsync(venda);

                string mensagem = $"Venda #{vendaRegistrada.Id} registrada com sucesso!";

                if (formaPagamento == "Dinheiro" && _troco > 0)
                    mensagem += $"\nTroco: {_troco:C2}";

                if (formaPagamento == "Fiado" && cliente != null)
                    mensagem += $"\nCliente: {cliente.Nome}";

                var reciboTexto = GerarReciboTexto(vendaRegistrada, cliente);
                var caminhoRecibo = SalvarRecibo(reciboTexto);
                mensagem += $"\nRecibo salvo em:\n{caminhoRecibo}";

                MessageBox.Show(mensagem, "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtRecibo.Text = reciboTexto;

                if (MessageBox.Show("Imprimir o recibo?", "Impressão", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _reciboTextoParaImprimir = reciboTexto;
                    ImprimirRecibo();
                }

                LimparVenda(null, EventArgs.Empty);
                await CarregarClientesAsync();
            }
            catch (InvalidOperationException invEx)
            {
                MessageBox.Show($"Erro: {invEx.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnFinalizar.Enabled = true;
                Cursor = previousCursor;
            }
        }
    }
}