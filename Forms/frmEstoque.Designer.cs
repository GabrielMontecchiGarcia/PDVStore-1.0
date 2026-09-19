namespace PDVStore.Forms
{
    partial class frmEstoque
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            dgvProdutos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvProdutos.Location = new Point(16, 212);
            dgvProdutos.Name = "dgvProdutos";
            dgvProdutos.RowHeadersWidth = 51;
            dgvProdutos.Size = new Size(720, 266);
            dgvProdutos.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvProdutos.TabIndex = 0;
            dgvProdutos.SelectionChanged += dgvProdutos_SelectionChanged;
            // 
            // cmbTipoMovimento
            // 
            cmbTipoMovimento.FormattingEnabled = true;
            cmbTipoMovimento.Location = new Point(140, 12);
            cmbTipoMovimento.Name = "cmbTipoMovimento";
            cmbTipoMovimento.Size = new Size(139, 28);
            cmbTipoMovimento.TabIndex = 1;
            cmbTipoMovimento.SelectedIndexChanged += cmbTipoMovimento_SelectedIndexChanged;
            // 
            // lblTipoMovimento
            // 
            lblTipoMovimento.AutoSize = true;
            lblTipoMovimento.Location = new Point(12, 15);
            lblTipoMovimento.Name = "lblTipoMovimento";
            lblTipoMovimento.Size = new Size(122, 20);
            lblTipoMovimento.TabIndex = 2;
            lblTipoMovimento.Text = "Tipo movimento:";
            // 
            // lblQuantidade
            // 
            lblQuantidade.AutoSize = true;
            lblQuantidade.Location = new Point(12, 59);
            lblQuantidade.Name = "lblQuantidade";
            lblQuantidade.Size = new Size(90, 20);
            lblQuantidade.TabIndex = 3;
            lblQuantidade.Text = "Quantidade:";
            // 
            // lblBuscar
            // 
            lblBuscar.AutoSize = true;
            lblBuscar.Location = new Point(16, 171);
            lblBuscar.Name = "lblBuscar";
            lblBuscar.Size = new Size(73, 20);
            lblBuscar.TabIndex = 4;
            lblBuscar.Text = "Pesquisar:";
            // 
            // lblMotivo
            // 
            lblMotivo.AutoSize = true;
            lblMotivo.Location = new Point(12, 100);
            lblMotivo.Name = "lblMotivo";
            lblMotivo.Size = new Size(59, 20);
            lblMotivo.TabIndex = 5;
            lblMotivo.Text = "Motivo:";
            // 
            // txtQuantidade
            // 
            txtQuantidade.Location = new Point(142, 59);
            txtQuantidade.Name = "txtQuantidade";
            txtQuantidade.Size = new Size(125, 27);
            txtQuantidade.TabIndex = 6;
            // 
            // txtMotivo
            // 
            txtMotivo.Location = new Point(95, 97);
            txtMotivo.Multiline = true;
            txtMotivo.Name = "txtMotivo";
            txtMotivo.Size = new Size(348, 68);
            txtMotivo.TabIndex = 6;
            // 
            // txtBuscar
            // 
            txtBuscar.Location = new Point(95, 171);
            txtBuscar.Name = "txtBuscar";
            txtBuscar.Size = new Size(348, 27);
            txtBuscar.TabIndex = 6;
            // 
            // btnBuscar
            // 
            btnBuscar.Location = new Point(465, 171);
            btnBuscar.Name = "btnBuscar";
            btnBuscar.Size = new Size(115, 29);
            btnBuscar.TabIndex = 7;
            btnBuscar.Text = "Buscar";
            btnBuscar.UseVisualStyleBackColor = true;
            btnBuscar.Click += btnBuscar_Click;
            // 
            // btnConfirmarMovimento
            // 
            btnConfirmarMovimento.Location = new Point(463, 136);
            btnConfirmarMovimento.Name = "btnConfirmarMovimento";
            btnConfirmarMovimento.Size = new Size(117, 29);
            btnConfirmarMovimento.TabIndex = 8;
            btnConfirmarMovimento.Text = "Confirmar";
            btnConfirmarMovimento.UseVisualStyleBackColor = true;
            btnConfirmarMovimento.Click += btnConfirmarMovimento_ClickAsync;
            // 
            // lblProdutoSelecionado
            // 
            lblProdutoSelecionado.AutoSize = true;
            lblProdutoSelecionado.Location = new Point(16, 494);
            lblProdutoSelecionado.Name = "lblProdutoSelecionado";
            lblProdutoSelecionado.Size = new Size(91, 20);
            lblProdutoSelecionado.TabIndex = 9;
            lblProdutoSelecionado.Text = "Selecionado";
            // 
            // frmEstoque
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(780, 546);
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