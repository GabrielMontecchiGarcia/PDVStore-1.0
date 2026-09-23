namespace PDVStore.Forms
{
    partial class frmGerenciarUsuarios
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
        // O QUE FAZ: instancia o grid de usuários, os botões (Novo/Editar/Excluir/
        // Atualizar/Buscar) e o campo de pesquisa, definindo posições e eventos.
        // POR QUE EXISTE: é o método gerado pelo WinForms Designer que constrói a UI;
        // não deve ser editado manualmente para não perder alterações do Designer.
        // QUEM CHAMA: o construtor de frmGerenciarUsuarios.
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            dgvUsuarios = new DataGridView();
            btnNovo = new Button();
            btnEditar = new Button();
            btnExcluir = new Button();
            btnAtualizar = new Button();
            lblPesquisar = new Label();
            txtBusca = new TextBox();
            btnBuscarUsuario = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvUsuarios).BeginInit();
            SuspendLayout();
            // 
            // dgvUsuarios
            // 
            dgvUsuarios.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvUsuarios.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvUsuarios.Location = new Point(10, 49);
            dgvUsuarios.Margin = new Padding(3, 2, 3, 2);
            dgvUsuarios.Name = "dgvUsuarios";
            dgvUsuarios.RowHeadersWidth = 51;
            dgvUsuarios.Size = new Size(563, 285);
            dgvUsuarios.TabIndex = 0;
            dgvUsuarios.CellContentClick += dgvUsuarios_CellContentClick;
            // 
            // btnNovo
            // 
            btnNovo.Location = new Point(605, 52);
            btnNovo.Margin = new Padding(3, 2, 3, 2);
            btnNovo.Name = "btnNovo";
            btnNovo.Size = new Size(109, 22);
            btnNovo.TabIndex = 1;
            btnNovo.Text = "Novo cadastro";
            btnNovo.UseVisualStyleBackColor = true;
            btnNovo.Click += btnNovo_Click;
            // 
            // btnEditar
            // 
            btnEditar.Location = new Point(605, 96);
            btnEditar.Margin = new Padding(3, 2, 3, 2);
            btnEditar.Name = "btnEditar";
            btnEditar.Size = new Size(109, 22);
            btnEditar.TabIndex = 2;
            btnEditar.Text = "Editar";
            btnEditar.UseVisualStyleBackColor = true;
            btnEditar.Click += btnEditar_Click;
            // 
            // btnExcluir
            // 
            btnExcluir.Location = new Point(605, 140);
            btnExcluir.Margin = new Padding(3, 2, 3, 2);
            btnExcluir.Name = "btnExcluir";
            btnExcluir.Size = new Size(109, 22);
            btnExcluir.TabIndex = 3;
            btnExcluir.Text = "Excluir";
            btnExcluir.UseVisualStyleBackColor = true;
            btnExcluir.Click += btnExcluir_ClickAsync;
            // 
            // btnAtualizar
            // 
            btnAtualizar.Location = new Point(605, 176);
            btnAtualizar.Margin = new Padding(3, 2, 3, 2);
            btnAtualizar.Name = "btnAtualizar";
            btnAtualizar.Size = new Size(109, 22);
            btnAtualizar.TabIndex = 4;
            btnAtualizar.Text = "Atualizar";
            btnAtualizar.UseVisualStyleBackColor = true;
            btnAtualizar.Click += btnAtualizar_Click;
            // 
            // lblPesquisar
            // 
            lblPesquisar.AutoSize = true;
            lblPesquisar.Location = new Point(14, 11);
            lblPesquisar.Name = "lblPesquisar";
            lblPesquisar.Size = new Size(60, 15);
            lblPesquisar.TabIndex = 5;
            lblPesquisar.Text = "Pesquisar:";
            // 
            // txtBusca
            // 
            txtBusca.Location = new Point(83, 11);
            txtBusca.Margin = new Padding(3, 2, 3, 2);
            txtBusca.Name = "txtBusca";
            txtBusca.Size = new Size(490, 23);
            txtBusca.TabIndex = 6;
            // 
            // btnBuscarUsuario
            // 
            btnBuscarUsuario.Location = new Point(605, 11);
            btnBuscarUsuario.Margin = new Padding(3, 2, 3, 2);
            btnBuscarUsuario.Name = "btnBuscarUsuario";
            btnBuscarUsuario.Size = new Size(109, 22);
            btnBuscarUsuario.TabIndex = 7;
            btnBuscarUsuario.Text = "Buscar";
            btnBuscarUsuario.UseVisualStyleBackColor = true;
            btnBuscarUsuario.Click += btnBuscarUsuario_Click;
            // 
            // frmGerenciarUsuarios
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(776, 375);
            Controls.Add(btnBuscarUsuario);
            Controls.Add(txtBusca);
            Controls.Add(lblPesquisar);
            Controls.Add(btnAtualizar);
            Controls.Add(btnExcluir);
            Controls.Add(btnEditar);
            Controls.Add(btnNovo);
            Controls.Add(dgvUsuarios);
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            MaximumSize = new Size(792, 414);
            MinimumSize = new Size(792, 414);
            Name = "frmGerenciarUsuarios";
            Text = "frmGerenciarUsuarios";
            ((System.ComponentModel.ISupportInitialize)dgvUsuarios).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView dgvUsuarios;
        private Button btnNovo;
        private Button btnEditar;
        private Button btnExcluir;
        private Button btnAtualizar;
        private Label lblPesquisar;
        private TextBox txtBusca;
        private Button btnBuscarUsuario;
    }
}