using PDVStore.Models;
using PDVStore.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public partial class frmEstoque : Form
    {
        private readonly EstoqueService _estoqueService;
        private Produto? _produtoSelecionado;

        // CONSTRUTOR
        // O que faz: armazena o EstoqueService e configura o formulário.
        // Por que existe: o frmEstoque tem arquivo .Designer; aqui apenas guardamos o
        //   serviço e delegamos a configuração inicial para ConfigurarFormulario().
        // Dependências: recebe EstoqueService por injeção (Services).
        public frmEstoque(EstoqueService estoqueService)
        {
            _estoqueService = estoqueService ?? throw new ArgumentNullException(nameof(estoqueService));
            InitializeComponent();
            ConfigurarFormulario();
        }

        // EVENTO - Load do formulário (async)
        // O que faz: carrega a lista de produtos assim que a janela é exibida.
        // Por que existe: o operador deve ver o estoque atual imediatamente ao abrir a
        //   tela; é disparado uma única vez pelo Windows Forms.
        // Dependências: registrado em ConfigurarFormulario() (Load += Form_Load);
        //   chama CarregarProdutosAsync() sem filtro (lista completa).
        private async void Form_Load(object? sender, EventArgs e)
        {
            await CarregarProdutosAsync();
        }

        // O que faz: busca produtos (todos ou por filtro de busca) e liga no dgvProdutos.
        // Por que existe: centraliza a atualização do grid — usada no Load, na busca e
        //   após registrar um movimento (mantendo o filtro digitado pelo usuário).
        // Dependências: usa EstoqueService.GetAllAsync() ou BuscarAsync(filtro);
        //   alimenta dgvProdutos e trata erros com MessageBox.
        private async Task CarregarProdutosAsync(string filtro = "")
        {
            try
            {
                var produtos = string.IsNullOrWhiteSpace(filtro)
                    ? await _estoqueService.GetAllAsync()
                    : await _estoqueService.BuscarAsync(filtro);

                dgvProdutos.DataSource = produtos.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar estoque: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // O que faz: ajusta título, combobox de tipo de movimento e colunas do grid.
        // Por que existe: algumas configurações não cabem no .Designer (carregar no Load,
        //   evitar colunas duplicadas e estilizar a grade) — centraliza-as aqui.
        // Dependências: registra Form_Load e adiciona os botões de exportar PDF/Excel;
        //   é chamado pelo construtor.
        private void ConfigurarFormulario()
        {
            this.Text = "Gestão de Estoque - Entrada / Saída";
            this.StartPosition = FormStartPosition.CenterScreen;

            Load += Form_Load;

            if (cmbTipoMovimento.Items.Count == 0)
                cmbTipoMovimento.Items.AddRange(new string[] { "Entrada", "Saída" });
            cmbTipoMovimento.SelectedIndex = 0;

            dgvProdutos.AutoGenerateColumns = false;

            // Evita duplicar colunas se a configuração rodar mais de uma vez
            if (dgvProdutos.Columns.Count == 0)
            {
                dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", Width = 70 });
                dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", HeaderText = "Código", DataPropertyName = "CodigoBarras", Width = 130 });
                dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "Produto", DataPropertyName = "Nome", Width = 280 });
                dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "EstoqueAtual", HeaderText = "Estoque Atual", DataPropertyName = "EstoqueAtual", Width = 120 });
                dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Preco", HeaderText = "Preço", DataPropertyName = "Preco", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            }
            foreach (DataGridViewColumn c in dgvProdutos.Columns) c.FillWeight = Math.Max(50, c.Width);
            dgvProdutos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var btnExportarPdf = new Button { Text = "Exportar PDF", Location = new Point(542, 9), Size = new Size(110, 29), FlatStyle = FlatStyle.Flat };
            var btnExportarExcel = new Button { Text = "Exportar Excel", Location = new Point(542, 39), Size = new Size(110, 29), FlatStyle = FlatStyle.Flat };
            btnExportarPdf.Click += (_, _) => ExportadorService.ExportarPdf(dgvProdutos, "Gestão de Estoque", $"Estoque_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
            btnExportarExcel.Click += (_, _) => ExportadorService.ExportarExcel(dgvProdutos, "Gestão de Estoque", $"Estoque_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            this.Controls.Add(btnExportarPdf);
            this.Controls.Add(btnExportarExcel);
        }

        // EVENTO - botão "Buscar" (async)
        // O que faz: recarrega o grid aplicando o texto digitado em txtBuscar.
        // Por que existe: permite localizar um produto pelo nome/código rapidamente,
        //   especialmente quando o cadastro é grande (regra de UX).
        // Dependências: chama CarregarProdutosAsync(txtBuscar.Text.Trim()).
        private async void btnBuscar_Click(object sender, EventArgs e)
        {
            await CarregarProdutosAsync(txtBuscar.Text.Trim());
        }

        // EVENTO - troca do tipo de movimento
        // O que faz: muda a cor do botão de confirmar entre verde (Entrada) e vermelho (Saída).
        // Por que existe: dá feedback visual imediato sobre a operação que será executada,
        //   evitando que o operador confunda entrada com saída (regra de UX).
        // Dependências: lê cmbTipoMovimento.Text e ajusta btnConfirmarMovimento.BackColor.
        private void cmbTipoMovimento_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnConfirmarMovimento.BackColor = cmbTipoMovimento.Text == "Entrada"
                ? System.Drawing.Color.DarkGreen
                : System.Drawing.Color.DarkRed;
        }

        // EVENTO - seleção de produto no grid
        // O que faz: guarda o produto escolhido em _produtoSelecionado e mostra o nome
        //   com o estoque atual no lblProdutoSelecionado.
        // Por que existe: o movimento precisa saber sobre qual produto agir; sem seleção,
        //   o botão "Confirmar" avisaria que faltou escolher (regra de segurança da operação).
        // Dependências: lê dgvProdutos.CurrentRow e atualiza o label e o campo privado.
        private void dgvProdutos_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvProdutos.CurrentRow?.DataBoundItem is Produto produto)
            {
                _produtoSelecionado = produto;
                lblProdutoSelecionado.Text = $"Produto Selecionado: {produto.Nome} (Estoque: {produto.EstoqueAtual})";
            }
        }

        // EVENTO - botão "Confirmar movimento" (async)
        // O que faz: valida produto e quantidade e chama o serviço de entrada/saída.
        // Por que existe: registra a movimentação de estoque escolhendo Entrada (compra/
        //   devolução) ou Saída (venda/ajuste), sempre com motivo para auditoria.
        // Dependências: usa EstoqueService.AdicionarEstoqueAsync() ou BaixarEstoqueAsync() e
        //   depois CarregarProdutosAsync() para refletir a nova quantidade no grid.
        private async void btnConfirmarMovimento_ClickAsync(object sender, EventArgs e)
        {
            if (_produtoSelecionado == null)
            {
                MessageBox.Show("Selecione um produto na lista.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtQuantidade.Text, out int quantidade) || quantidade <= 0)
            {
                MessageBox.Show("Informe uma quantidade válida.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string tipo = cmbTipoMovimento.Text;
            string motivo = txtMotivo.Text.Trim();

            try
            {
                bool sucesso;
                if (tipo == "Entrada")
                {
                    sucesso = await _estoqueService.AdicionarEstoqueAsync(_produtoSelecionado.Id, quantidade,
                        string.IsNullOrWhiteSpace(motivo) ? "Entrada manual" : motivo);
                }
                else
                {
                    sucesso = await _estoqueService.BaixarEstoqueAsync(_produtoSelecionado.Id, quantidade,
                        string.IsNullOrWhiteSpace(motivo) ? "Saída manual" : motivo);
                }

                if (sucesso)
                {
                    MessageBox.Show($"{quantidade} unidade(s) de {tipo} realizada com sucesso!", $"{tipo} Confirmada",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await CarregarProdutosAsync(txtBuscar.Text.Trim());
                    txtQuantidade.Clear();
                    txtMotivo.Clear();
                }
                else
                {
                    MessageBox.Show("Não foi possível realizar o movimento. Verifique a quantidade disponível.",
                        "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao realizar movimento: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}