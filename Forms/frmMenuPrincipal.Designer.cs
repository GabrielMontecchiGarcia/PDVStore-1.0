namespace PDVStore.Forms
{
    partial class frmMenuPrincipal
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

        // Inicializa as propriedades básicas do formulário (código gerado).
        // O QUE FAZ: cria o container de componentes, define o modo de escala automática,
        // o tamanho do cliente e o texto da janela.
        // POR QUE EXISTE: é o método gerado pelo WinForms Designer; a maior parte da UI
        // do menu é construída em código por BuildUI, mas o Designer ainda exige este método.
        // QUEM CHAMA: o construtor de frmMenuPrincipal.
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Text = "frmMenuPrincipal";
        }

        #endregion
    }
}