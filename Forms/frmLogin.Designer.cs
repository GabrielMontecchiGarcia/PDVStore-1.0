namespace PDVStore.Forms
{
    partial class frmLogin
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
            btnLogin = new Button();
            btnSair = new Button();
            lblUsuario = new Label();
            lblSenha = new Label();
            txtSenha = new TextBox();
            txtUsuario = new TextBox();
            SuspendLayout();
            // 
            // btnLogin
            // 
            btnLogin.Location = new Point(208, 119);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(94, 29);
            btnLogin.TabIndex = 0;
            btnLogin.Text = "Acessar";
            btnLogin.UseVisualStyleBackColor = true;
            btnLogin.Click += btnLogin_Click;
            // 
            // btnSair
            // 
            btnSair.Location = new Point(100, 119);
            btnSair.Name = "btnSair";
            btnSair.Size = new Size(94, 29);
            btnSair.TabIndex = 9;
            btnSair.Text = "Sair";
            btnSair.UseVisualStyleBackColor = true;
            btnSair.Click += btnSair_Click;
            // 
            // lblUsuario
            // 
            lblUsuario.AutoSize = true;
            lblUsuario.Location = new Point(26, 24);
            lblUsuario.Name = "lblUsuario";
            lblUsuario.Size = new Size(80, 20);
            lblUsuario.TabIndex = 1;
            lblUsuario.Text = "Usuário(a):";
            // 
            // lblSenha
            // 
            lblSenha.AutoSize = true;
            lblSenha.Location = new Point(30, 71);
            lblSenha.Name = "lblSenha";
            lblSenha.Size = new Size(52, 20);
            lblSenha.TabIndex = 2;
            lblSenha.Text = "Senha:";
            // 
            // txtSenha
            // 
            txtSenha.Location = new Point(112, 71);
            txtSenha.Name = "txtSenha";
            txtSenha.PasswordChar = '*';
            txtSenha.Size = new Size(190, 27);
            txtSenha.TabIndex = 3;
            // 
            // txtUsuario
            // 
            txtUsuario.Location = new Point(112, 21);
            txtUsuario.Name = "txtUsuario";
            txtUsuario.Size = new Size(190, 27);
            txtUsuario.TabIndex = 4;
            // 
            // frmLogin
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(398, 252);
            Controls.Add(txtUsuario);
            Controls.Add(txtSenha);
            Controls.Add(lblSenha);
            Controls.Add(lblUsuario);
            Controls.Add(btnLogin);
            Controls.Add(btnSair);
            Name = "frmLogin";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PDV Store - Login";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnLogin;
        private Button btnSair;
        private Label lblUsuario;
        private Label lblSenha;
        private TextBox txtSenha;
        private TextBox txtUsuario;
    }
}