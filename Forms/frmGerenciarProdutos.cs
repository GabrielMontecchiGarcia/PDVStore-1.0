using PDVStore.Models;
using PDVStore.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public class frmGerenciarProdutos : Form
    {
        private readonly EstoqueService _estoqueService;
        private Produto? _produtoSelecionado;

        private DataGridView dgvProdutos = null!;
        private TextBox txtCodigo = null!;
        private TextBox txtNomeProduto = null!;
        private TextBox txtPreco = null!;
        private TextBox txtPrecoCusto = null!;
        private TextBox txtEstoque = null!;
        private TextBox txtEstoqueMinimo = null!;
        private TextBox txtCategoria = null!;
        private TextBox txtDescricao = null!;
        private Button btnSalvar = null!;
        private Button btnNovo = null!;
        private Button btnExcluir = null!;
        private Button btnAtualizarEstoque = null!;
        private Button btnRefresh = null!;

        // Construtor da tela de gerenciamento de produtos.
        // O QUE FAZ: recebe o EstoqueService via Injeção de Dependência (DI), valida a
        // dependência e prepara a interface (InitializeComponent + ConfigurarGrid).
        // POR QUE EXISTE: centraliza a montagem do formulário e dispara o carregamento
        // inicial dos produtos quando o evento Load ocorre (veja o lambda registrado).
        // QUEM CHAMA: resolvido pelo container DI (frmMenuPrincipal.OpenForm<T>).
        public frmGerenciarProdutos(EstoqueService estoqueService)
        {
            _estoqueService = estoqueService ?? throw new ArgumentNullException(nameof(estoqueService));
            MontarInterface();
            ConfigurarGrid();
            Load += async (_, _) => await CarregarProdutosAsync();
        }

        // Constrói manualmente todos os controles da tela (grid, campos, botões) em código.
        // O QUE FAZ: define títulos, posições, tamanhos e associa os eventos dos botões
        // aos métodos correspondentes (SalvarAsync, LimparCampos, ExcluirAsync, etc.).
        // POR QUE EXISTE: mantém a interface do projeto 100% pronta em tempo de execução,
        // sem depender do Designer do WinForms, facilitando o estudo do código.
        // DEPENDÊNCIAS: usa o ExportadorService (PDF/Excel) nos botões de exportação.
        private void MontarInterface()
        {
            Text = "Gerenciar Produtos";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1200, 580);
            MaximumSize = new Size(1200, 580);
            MinimumSize = new Size(1200, 580);
            MaximizeBox = false;
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            dgvProdutos = new DataGridView { Location = new Point(500, 20), Size = new Size(660, 460), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, ReadOnly = true, AllowUserToAddRows = false, AutoGenerateColumns = false };
            btnRefresh = new Button { Text = "Refresh", Location = new Point(1045, 492), Size = new Size(75, 28) };

            int y = 16;
            int dy = 47;

            var lbl1 = new Label { Text = "Código Barras:", Location = new Point(15, y), AutoSize = true };
            txtCodigo = new TextBox { Location = new Point(140, y - 4), Size = new Size(180, 26) };
            var lbl2 = new Label { Text = "Produto:", Location = new Point(15, y += dy), AutoSize = true };
            txtNomeProduto = new TextBox { Location = new Point(140, y - 4), Size = new Size(330, 26) };
            var lbl3 = new Label { Text = "Preço Venda:", Location = new Point(15, y += dy), AutoSize = true };
            txtPreco = new TextBox { Location = new Point(140, y - 4), Size = new Size(120, 26) };
            var lbl3b = new Label { Text = "Preço Custo:", Location = new Point(15, y += dy), AutoSize = true };
            txtPrecoCusto = new TextBox { Location = new Point(140, y - 4), Size = new Size(120, 26) };
            var lbl4 = new Label { Text = "Estoque:", Location = new Point(15, y += dy), AutoSize = true };
            txtEstoque = new TextBox { Location = new Point(140, y - 4), Size = new Size(120, 26) };
            var lbl4b = new Label { Text = "Estoque Mínimo:", Location = new Point(15, y += dy), AutoSize = true };
            txtEstoqueMinimo = new TextBox { Location = new Point(140, y - 4), Size = new Size(120, 26), Text = "0" };
            var lbl5 = new Label { Text = "Categoria:", Location = new Point(15, y += dy), AutoSize = true };
            txtCategoria = new TextBox { Location = new Point(140, y - 4), Size = new Size(180, 26) };
            var lbl6 = new Label { Text = "Descrição:", Location = new Point(15, y += dy), AutoSize = true };
            txtDescricao = new TextBox { Location = new Point(140, y - 4), Size = new Size(330, 60), Multiline = true };

            btnSalvar = new Button { Text = "Salvar", Location = new Point(140, y + 70), Size = new Size(100, 30), BackColor = Color.ForestGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnNovo = new Button { Text = "Novo", Location = new Point(250, y + 70), Size = new Size(90, 30), FlatStyle = FlatStyle.Flat };
            btnExcluir = new Button { Text = "Excluir", Location = new Point(350, y + 70), Size = new Size(90, 30), BackColor = Color.Firebrick, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAtualizarEstoque = new Button { Text = "Estoque", Location = new Point(140, y + 110), Size = new Size(90, 30), FlatStyle = FlatStyle.Flat };

            var btnExportarPdf = new Button { Text = "Exportar PDF", Location = new Point(250, y + 110), Size = new Size(110, 30), FlatStyle = FlatStyle.Flat };
            var btnExportarExcel = new Button { Text = "Exportar Excel", Location = new Point(372, y + 110), Size = new Size(118, 30), FlatStyle = FlatStyle.Flat };

            btnSalvar.Click += async (_, _) => await SalvarAsync();
            btnNovo.Click += (_, _) => LimparCampos();
            btnExcluir.Click += async (_, _) => await ExcluirAsync();
            btnAtualizarEstoque.Click += (_, _) => MessageBox.Show(
                "Para ajustar estoque, use a tela de Estoque (Entrada/Saída).", "Ajuste de Estoque",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnRefresh.Click += async (_, _) => await CarregarProdutosAsync();
            btnExportarPdf.Click += (_, _) => ExportadorService.ExportarPdf(dgvProdutos, "Gerenciar Produtos", $"Produtos_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
            btnExportarExcel.Click += (_, _) => ExportadorService.ExportarExcel(dgvProdutos, "Gerenciar Produtos", $"Produtos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");

            dgvProdutos.SelectionChanged += (_, _) => SelecionarProduto();

            Controls.AddRange(new Control[] {
                dgvProdutos, btnRefresh,
                lbl1, txtCodigo, lbl2, txtNomeProduto, lbl3, txtPreco, lbl3b, txtPrecoCusto,
                lbl4, txtEstoque, lbl4b, txtEstoqueMinimo, lbl5, txtCategoria, lbl6, txtDescricao,
                btnSalvar, btnNovo, btnExcluir, btnAtualizarEstoque, btnExportarPdf, btnExportarExcel
            });
        }

        // Configura as colunas exibidas no DataGridView de produtos.
        // O QUE FAZ: limpa as colunas existentes e cria cada coluna ligada a uma
        // propriedade do modelo Produto (DataPropertyName), com formatos de moeda
        // para preço/custo e larguras adequadas.
        // POR QUE EXISTE: o grid é somente leitura e usa DataSource; sem essa ligação
        // as colunas não saberiam quais valores do produto exibir.
        // QUEM CHAMA: o construtor da tela.
        private void ConfigurarGrid()
        {
            dgvProdutos.Columns.Clear();
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", Width = 50 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", HeaderText = "Código", DataPropertyName = "CodigoBarras", Width = 110 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "Nome do Produto", DataPropertyName = "Nome", Width = 230 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Preco", HeaderText = "Preço", DataPropertyName = "Preco", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Custo", HeaderText = "Custo", DataPropertyName = "PrecoCusto", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estoque", HeaderText = "Estoque", DataPropertyName = "EstoqueAtual", Width = 70 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "EstoqueMinimo", HeaderText = "Mín.", DataPropertyName = "EstoqueMinimo", Width = 55 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Categoria", HeaderText = "Categoria", DataPropertyName = "Categoria", Width = 100 });
            foreach (DataGridViewColumn c in dgvProdutos.Columns) c.FillWeight = Math.Max(50, c.Width);
            dgvProdutos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        // Carrega (ou recarrega) a lista de produtos no DataGridView.
        // O QUE FAZ: consulta todos os produtos via EstoqueService.GetAllAsync e os
        // atribui como DataSource do grid.
        // POR QUE EXISTE: é o método central de atualização da listagem, chamado no
        // Load do formulário, no Refresh e após salvar/excluir.
        // DEPENDÊNCIAS: _estoqueService; atualiza o controle dgvProdutos.
        private async Task CarregarProdutosAsync()
        {
            try
            {
                var produtos = await _estoqueService.GetAllAsync();
                dgvProdutos.DataSource = produtos.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Falha carregando produtos: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Reage à mudança de linha selecionada no grid de produtos.
        // O QUE FAZ: se a linha atual corresponde a um Produto, guarda esse objeto em
        // _produtoSelecionado e chama PreencherCampos para enviar os dados aos TextBoxes.
        // POR QUE EXISTE: permite ao usuário visualizar/editar o produto clicando na
        // grade, e informa qual registro será alterado ou excluído.
        // QUEM CHAMA: o evento dgvProdutos.SelectionChanged.
        private void SelecionarProduto()
        {
            if (dgvProdutos.CurrentRow?.DataBoundItem is Produto produto)
            {
                _produtoSelecionado = produto;
                PreencherCampos(produto);
            }
        }

        // Copia as propriedades de um Produto para os campos de edição da tela.
        // O QUE FAZ: preenche os TextBoxes (código, nome, preço, custo, estoque,
        // estoque mínimo, categoria e descrição) a partir do objeto recebido.
        // POR QUE EXISTE: dá feedback visual do produto selecionado e permite alterá-lo.
        // QUEM CHAMA: SelecionarProduto (evento de seleção do grid).
        private void PreencherCampos(Produto produto)
        {
            txtCodigo.Text = produto.CodigoBarras;
            txtNomeProduto.Text = produto.Nome;
            txtPreco.Text = produto.Preco.ToString("0.00");
            txtPrecoCusto.Text = produto.PrecoCusto.ToString("0.00");
            txtEstoque.Text = produto.EstoqueAtual.ToString();
            txtEstoqueMinimo.Text = produto.EstoqueMinimo.ToString();
            txtCategoria.Text = produto.Categoria;
            txtDescricao.Text = produto.Descricao;
        }

        // Valida e salva (cria ou atualiza) um produto no banco de dados.
        // O QUE FAZ: valida os campos obrigatórios (código, nome, preço), monta um objeto
        // Produto e decide entre AddAsync (Id == 0, novo) ou UpdateAsync (edição).
        // POR QUE EXISTE: é a regra de negócio principal da tela — garantir que só
        // dados coerentes cheguem ao EstoqueService.
        // DEPENDÊNCIAS: _estoqueService; atualiza dgvProdutos e limpa os campos ao final.
        private async Task SalvarAsync()
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text))
            {
                MessageBox.Show("Código de barras é obrigatório.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNomeProduto.Text))
            {
                MessageBox.Show("Nome do produto é obrigatório.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtPreco.Text, out decimal preco) || preco < 0)
            {
                MessageBox.Show("Informe um preço de venda válido.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var produto = _produtoSelecionado ?? new Produto { Ativo = true };

                produto.CodigoBarras = txtCodigo.Text.Trim();
                produto.Nome = txtNomeProduto.Text.Trim();
                produto.Preco = preco;
                produto.PrecoCusto = decimal.TryParse(txtPrecoCusto.Text, out decimal custo) ? Math.Max(0, custo) : 0;
                produto.Estoque = int.TryParse(txtEstoque.Text, out int estoque) ? Math.Max(0, estoque) : 0;
                produto.EstoqueMinimo = int.TryParse(txtEstoqueMinimo.Text, out int minimo) ? Math.Max(0, minimo) : 0;
                produto.Categoria = txtCategoria.Text.Trim();
                produto.Descricao = txtDescricao.Text.Trim();

                if (produto.Id == 0)
                    await _estoqueService.AddAsync(produto);
                else
                    await _estoqueService.UpdateAsync(produto);

                MessageBox.Show("Produto salvo com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await CarregarProdutosAsync();
                LimparCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Desativa (excluição lógica) um produto escolhido na grade.
        // O QUE FAZ: exige um produto selecionado, pede confirmação e chama
        // EstoqueService.DeleteAsync; depois recarrega a lista e limpa os campos.
        // POR QUE EXISTE: evita exclusão física/irreversível — o produto apenas
        // deixa de aparecer, preservando o histórico de vendas e movimentações.
        // QUEM CHAMA: botão "Excluir" (btnExcluir.Click).
        private async Task ExcluirAsync()
        {
            if (_produtoSelecionado == null)
            {
                MessageBox.Show("Selecione um produto para excluir.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"Deseja desativar o produto '{_produtoSelecionado.Nome}'?",
                "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            await _estoqueService.DeleteAsync(_produtoSelecionado.Id);
            MessageBox.Show("Produto desativado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await CarregarProdutosAsync();
            LimparCampos();
        }

        // Limpa todos os campos do formulário e desfaz a seleção atual.
        // O QUE FAZ: esvazia os TextBoxes, zera o _produtoSelecionado e devolve o foco
        // ao campo de código para a próxima digitação.
        // POR QUE EXISTE: prepara a tela para um novo cadastro e evita que dados de um
        // registro anterior sejam salvos por engano.
        // QUEM CHAMA: botão "Novo" e o final de SalvarAsync/ExcluirAsync.
        private void LimparCampos()
        {
            txtCodigo.Clear();
            txtNomeProduto.Clear();
            txtPreco.Clear();
            txtPrecoCusto.Clear();
            txtEstoque.Clear();
            txtEstoqueMinimo.Text = "0";
            txtCategoria.Clear();
            txtDescricao.Clear();
            _produtoSelecionado = null;
            txtCodigo.Focus();
        }
    }
}