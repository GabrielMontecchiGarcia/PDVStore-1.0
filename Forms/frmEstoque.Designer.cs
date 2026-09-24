namespace PDVStore.Forms
{
    partial class frmEstoque
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // Libera os recursos gerenciados do formulário ao ser fechado/descartado.
        // O QUE FAZ: quando disposing é true, descarta o container "components" (que
        // guarda os controles criados pelo Designer) e chama a implementação base.
        // POR QUE EXISTE: evita vazamento de memória/recursos GDI; é sobrescrito para
        // incluir os componentes específicos desta tela na limpeza.
        // QUEM CHAMA: o runtime do WinForms ao fechar/descartar o formulário.
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        // Inicializa e configura todos os controles da tela (código gerado).
        // O QUE FAZ: instancia o grid, o combo de movimento, os campos e botões, define
        // posições/tamanhos e associa os eventos de seleção e clique.
        // POR QUE EXISTE: é o método gerado pelo WinForms Designer que constrói a UI;
        // não deve ser editado manualmente para não perder alterações do Designer.
        // QUEM CHAMA: o construtor de frmEstoque.
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            dgvProdutos = new DataGridView();
            cmbTipoMovimento = new ComboBox();
            lblTipoMovimento = new Label();
            lblQuantidade = new Label();
            lblBuscar = new Label();
            lblMotivo = new Label();
            txtQuantidade = new TextBox();
            txtMotivo = new TextBox();
            txtBuscar = new TextBox();
            btnBuscar = new Button();
            btnConfirmarMovimento = new Button();
            lblProdutoSelecionado = new Label();
            ((System.ComponentModel.ISupportInitialize)dgvProdutos).BeginInit();
            SuspendLayout();
            // 
            // dgvProdutos
            // 
            dgvProdutos.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvProdutos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvProdutos.Location = new Point(14, 159);
            dgvProdutos.Margin = new Padding(3, 2, 3, 2);
            dgvProdutos.Name = "dgvProdutos";
            dgvProdutos.RowHeadersWidth = 51;
            dgvProdutos.Size = new Size(630, 200);
            dgvProdutos.TabIndex = 0;
            dgvProdutos.SelectionChanged += dgvProdutos_SelectionChanged;
            // 
            // cmbTipoMovimento
            // 
            cmbTipoMovimento.FormattingEnabled = true;
            cmbTipoMovimento.Location = new Point(122, 9);
            cmbTipoMovimento.Margin = new Padding(3, 2, 3, 2);
            cmbTipoMovimento.Name = "cmbTipoMovimento";
            cmbTipoMovimento.Size = new Size(122, 23);
            cmbTipoMovimento.TabIndex = 1;
            cmbTipoMovimento.SelectedIndexChanged += cmbTipoMovimento_SelectedIndexChanged;
            // 
            // lblTipoMovimento
            // 
            lblTipoMovimento.AutoSize = true;
            lblTipoMovimento.Location = new Point(10, 11);
            lblTipoMovimento.Name = "lblTipoMovimento";
            lblTipoMovimento.Size = new Size(99, 15);
            lblTipoMovimento.TabIndex = 2;
            lblTipoMovimento.Text = "Tipo movimento:";
            // 
            // lblQuantidade
            // 
            lblQuantidade.AutoSize = true;
            lblQuantidade.Location = new Point(10, 44);
            lblQuantidade.Name = "lblQuantidade";
            lblQuantidade.Size = new Size(72, 15);
            lblQuantidade.TabIndex = 3;
            lblQuantidade.Text = "Quantidade:";
            // 
            // lblBuscar
            // 
            lblBuscar.AutoSize = true;
            lblBuscar.Location = new Point(14, 128);
            lblBuscar.Name = "lblBuscar";
            lblBuscar.Size = new Size(60, 15);
            lblBuscar.TabIndex = 4;
            lblBuscar.Text = "Pesquisar:";
            // 
            // lblMotivo
            // 
            lblMotivo.AutoSize = true;
            lblMotivo.Location = new Point(10, 75);
            lblMotivo.Name = "lblMotivo";
            lblMotivo.Size = new Size(48, 15);
            lblMotivo.TabIndex = 5;
            lblMotivo.Text = "Motivo:";
            // 
            // txtQuantidade
            // 
            txtQuantidade.Location = new Point(124, 44);
            txtQuantidade.Margin = new Padding(3, 2, 3, 2);
            txtQuantidade.Name = "txtQuantidade";
            txtQuantidade.Size = new Size(110, 23);
            txtQuantidade.TabIndex = 6;
            // 
            // txtMotivo
            // 
            txtMotivo.Location = new Point(83, 73);
            txtMotivo.Margin = new Padding(3, 2, 3, 2);
            txtMotivo.Multiline = true;
            txtMotivo.Name = "txtMotivo";
            txtMotivo.Size = new Size(305, 52);
            txtMotivo.TabIndex = 6;
            // 
            // txtBuscar
            // 
            txtBuscar.Location = new Point(83, 128);
            txtBuscar.Margin = new Padding(3, 2, 3, 2);
            txtBuscar.Name = "txtBuscar";
            txtBuscar.Size = new Size(305, 23);
            txtBuscar.TabIndex = 6;
            // 
            // btnBuscar
            // 
            btnBuscar.Location = new Point(394, 128);
            btnBuscar.Margin = new Padding(3, 2, 3, 2);
            btnBuscar.Name = "btnBuscar";
            btnBuscar.Size = new Size(102, 23);
            btnBuscar.TabIndex = 7;
            btnBuscar.Text = "Buscar";
            btnBuscar.UseVisualStyleBackColor = true;
            btnBuscar.Click += btnBuscar_Click;
            // 
            // btnConfirmarMovimento
            // 
            btnConfirmarMovimento.Location = new Point(394, 94);
            btnConfirmarMovimento.Margin = new Padding(3, 2, 3, 2);
            btnConfirmarMovimento.Name = "btnConfirmarMovimento";
            btnConfirmarMovimento.Size = new Size(102, 30);
            btnConfirmarMovimento.TabIndex = 8;
            btnConfirmarMovimento.Text = "Confirmar";
            btnConfirmarMovimento.UseVisualStyleBackColor = true;
            btnConfirmarMovimento.Click += btnConfirmarMovimento_ClickAsync;
            // 
            // lblProdutoSelecionado
            // 
            lblProdutoSelecionado.AutoSize = true;
            lblProdutoSelecionado.Location = new Point(14, 370);
            lblProdutoSelecionado.Name = "lblProdutoSelecionado";
            lblProdutoSelecionado.Size = new Size(71, 15);
            lblProdutoSelecionado.TabIndex = 9;
            lblProdutoSelecionado.Text = "Selecionado";
            // 
            // frmEstoque
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(682, 410);
            Controls.Add(lblProdutoSelecionado);
            Controls.Add(btnConfirmarMovimento);
            Controls.Add(btnBuscar);
            Controls.Add(txtBuscar);
            Controls.Add(txtMotivo);
            Controls.Add(txtQuantidade);
            Controls.Add(lblMotivo);
            Controls.Add(lblBuscar);
            Controls.Add(lblQuantidade);
            Controls.Add(lblTipoMovimento);
            Controls.Add(cmbTipoMovimento);
            Controls.Add(dgvProdutos);
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            MaximumSize = new Size(698, 449);
            MinimumSize = new Size(698, 449);
            Name = "frmEstoque";
            Text = "frmEstoque";
            ((System.ComponentModel.ISupportInitialize)dgvProdutos).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView dgvProdutos;
        private ComboBox cmbTipoMovimento;
        private Label lblTipoMovimento;
        private Label lblQuantidade;
        private Label lblBuscar;
        private Label lblMotivo;
        private TextBox txtQuantidade;
        private TextBox txtMotivo;
        private TextBox txtBuscar;
        private Button btnBuscar;
        private Button btnConfirmarMovimento;
        private Label lblProdutoSelecionado;
    }
}