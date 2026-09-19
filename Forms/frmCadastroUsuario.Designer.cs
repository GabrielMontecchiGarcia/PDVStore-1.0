namespace PDVStore.Forms
{
    partial class frmCadastroUsuario
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
            txtNome = new TextBox();
            label1 = new Label();
            txtSenha = new TextBox();
            lblConfirma = new Label();
            picFoto = new PictureBox();
            lblNome = new Label();
            txtConfirmaSenha = new TextBox();
            btnSalvar = new Button();
            btnEscolherFoto = new Button();
            btnCancelar = new Button();
            openFileDialog1 = new OpenFileDialog();
            lblPermissao = new Label();
            cboPermissao = new ComboBox();
            ((System.ComponentModel.ISupportInitialize)picFoto).BeginInit();
            SuspendLayout();
            // 
            // txtNome
            // 
            txtNome.Location = new Point(252, 25);
            txtNome.Margin = new Padding(3, 2, 3, 2);
            txtNome.Name = "txtNome";
            txtNome.Size = new Size(261, 23);
            txtNome.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(175, 52);
            label1.Name = "label1";
            label1.Size = new Size(42, 15);
            label1.TabIndex = 2;
            label1.Text = "Senha:";
            // 
            // txtSenha
            // 
            txtSenha.Location = new Point(252, 52);
            txtSenha.Margin = new Padding(3, 2, 3, 2);
            txtSenha.Name = "txtSenha";
            txtSenha.PasswordChar = '*';
            txtSenha.Size = new Size(160, 23);
            txtSenha.TabIndex = 3;
            // 
            // lblConfirma
            // 
            lblConfirma.AutoSize = true;
            lblConfirma.Location = new Point(175, 87);
            lblConfirma.Name = "lblConfirma";
            lblConfirma.Size = new Size(64, 15);
            lblConfirma.TabIndex = 5;
            lblConfirma.Text = "Confirmar:";
            // 
            // picFoto
            // 
            picFoto.BorderStyle = BorderStyle.Fixed3D;
            picFoto.Image = Properties.Resources.user_default;
            picFoto.Location = new Point(19, 22);
            picFoto.Margin = new Padding(3, 2, 3, 2);
            picFoto.Name = "picFoto";
            picFoto.Size = new Size(129, 109);
            picFoto.SizeMode = PictureBoxSizeMode.StretchImage;
            picFoto.TabIndex = 6;
            picFoto.TabStop = false;
            // 
            // lblNome
            // 
            lblNome.AutoSize = true;
            lblNome.Location = new Point(175, 27);
            lblNome.Name = "lblNome";
            lblNome.Size = new Size(43, 15);
            lblNome.TabIndex = 7;
            lblNome.Text = "Nome:";
            // 
            // txtConfirmaSenha
            // 
            txtConfirmaSenha.Location = new Point(252, 85);
            txtConfirmaSenha.Margin = new Padding(3, 2, 3, 2);
            txtConfirmaSenha.Name = "txtConfirmaSenha";
            txtConfirmaSenha.PasswordChar = '*';
            txtConfirmaSenha.Size = new Size(160, 23);
            txtConfirmaSenha.TabIndex = 8;
            // 
            // btnSalvar
            // 
            btnSalvar.Location = new Point(252, 135);
            btnSalvar.Margin = new Padding(3, 2, 3, 2);
            btnSalvar.Name = "btnSalvar";
            btnSalvar.Size = new Size(129, 22);
            btnSalvar.TabIndex = 9;
            btnSalvar.Text = "Cadastrar usuário";
            btnSalvar.UseVisualStyleBackColor = true;
            btnSalvar.Click += btnSalvar_Click;
            // 
            // btnEscolherFoto
            // 
            btnEscolherFoto.Location = new Point(19, 135);
            btnEscolherFoto.Margin = new Padding(3, 2, 3, 2);
            btnEscolherFoto.Name = "btnEscolherFoto";
            btnEscolherFoto.Size = new Size(129, 22);
            btnEscolherFoto.TabIndex = 10;
            btnEscolherFoto.Text = "Escolher foto";
            btnEscolherFoto.UseVisualStyleBackColor = true;
            btnEscolherFoto.Click += btnEscolherFoto_Click_1;
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(252, 161);
            btnCancelar.Margin = new Padding(3, 2, 3, 2);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(129, 22);
            btnCancelar.TabIndex = 11;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // lblPermissao
            // 
            lblPermissao.AutoSize = true;
            lblPermissao.Location = new Point(175, 112);
            lblPermissao.Name = "lblPermissao";
            lblPermissao.Size = new Size(39, 15);
            lblPermissao.TabIndex = 13;
            lblPermissao.Text = "Papel:";
            // 
            // cboPermissao
            // 
            cboPermissao.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPermissao.Location = new Point(252, 110);
            cboPermissao.Margin = new Padding(3, 2, 3, 2);
            cboPermissao.Name = "cboPermissao";
            cboPermissao.Size = new Size(261, 23);
            cboPermissao.TabIndex = 14;
            // 
            // frmCadastroUsuario
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(534, 207);
            Controls.Add(cboPermissao);
            Controls.Add(lblPermissao);
            Controls.Add(btnCancelar);
            Controls.Add(btnEscolherFoto);
            Controls.Add(btnSalvar);
            Controls.Add(txtConfirmaSenha);
            Controls.Add(lblNome);
            Controls.Add(picFoto);
            Controls.Add(lblConfirma);
            Controls.Add(txtSenha);
            Controls.Add(label1);
            Controls.Add(txtNome);
            Margin = new Padding(3, 2, 3, 2);
            Name = "frmCadastroUsuario";
            Text = "frmCadastroUsuario";
            ((System.ComponentModel.ISupportInitialize)picFoto).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtNome;
        private TextBox txtSenha;
        private Label lblConfirma;
        private PictureBox picFoto;
        private Label lblNome;
        private TextBox txtConfirmaSenha;
        private Button btnSalvar;
        private Button btnEscolherFoto;
        private Button btnCancelar;
        private OpenFileDialog openFileDialog1;
        private Label lblPermissao;
        private ComboBox cboPermissao;
    }
}