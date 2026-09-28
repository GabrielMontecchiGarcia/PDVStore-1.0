using System;
using System.Drawing;
using System.Windows.Forms;

namespace PDVStore.Helpers
{
    // Aplica a identidade visual oficial do Senac aos formulários do sistema.
    // O QUE FAZ: percorre os controles de um formulário já construído (Designer ou
    //   código) e ajusta cores, fontes de botões/cabeçalhos/grids para a paleta
    //   institucional, SEM alterar posição, tamanho, âncoras ou qualquer outra
    //   configuração estrutural definida originalmente na tela.
    // POR QUE EXISTE: padronizar visualmente todas as telas do PDV com as cores
    //   institucionais do Senac (Manual da Marca), sem precisar reescrever o
    //   layout de cada formulário.
    // QUEM CHAMA: o construtor de cada formulário, logo após montar seus controles
    //   (InitializeComponent() ou BuildUI()). O frmCaixa não chama este método a
    //   pedido explícito: sua estrutura e configurações originais (tamanho fixo,
    //   não expansível) devem permanecer exatamente como estão.
    public static class Tema
    {
        // Cromias institucionais do Senac (Manual da Marca):
        //   Azul Senac    - Pantone 288C - #004A8D
        //   Laranja Senac - Pantone 144C - #F7941D
        //   Laranja-claro - Pantone 144C 55% - #FDC180
        public static readonly Color AzulSenac = ColorTranslator.FromHtml("#004A8D");
        public static readonly Color AzulSenacEscuro = ColorTranslator.FromHtml("#00365F");
        public static readonly Color LaranjaSenac = ColorTranslator.FromHtml("#F7941D");
        public static readonly Color LaranjaClaro = ColorTranslator.FromHtml("#FDC180");
        public static readonly Color Fundo = ColorTranslator.FromHtml("#FAF3E0");
        public static readonly Color Borda = ColorTranslator.FromHtml("#D9D9D9");
        public static readonly Color TextoClaro = Color.White;
        public static readonly Color TextoEscuro = ColorTranslator.FromHtml("#2E2E2E");

        // Palavras que identificam ações secundárias/neutras (Sair, Cancelar, Fechar,
        // Voltar, Limpar) para não pintá-las de laranja junto com as ações principais.
        private static readonly string[] AcoesSecundarias =
        {
            "sair", "cancelar", "fechar", "voltar", "limpar", "desativar"
        };

        /// <summary>
        /// Aplica a paleta de cores do Senac ao formulário e a todos os seus
        /// controles (recursivamente), preservando 100% da estrutura, tamanho,
        /// posição e demais propriedades já configuradas.
        /// </summary>
        public static void Aplicar(Form form)
        {
            if (form == null) return;

            form.BackColor = Fundo;

            AplicarControles(form.Controls);
        }

        private static void AplicarControles(Control.ControlCollection controles)
        {
            foreach (Control c in controles)
            {
                switch (c)
                {
                    case Panel p when p.Dock == DockStyle.Top:
                        p.BackColor = AzulSenac;
                        ColorirComoCabecalho(p);
                        break;

                    case Button b:
                        EstilizarBotao(b);
                        break;

                    case DataGridView dgv:
                        EstilizarGrid(dgv);
                        break;

                    case GroupBox gb:
                        gb.ForeColor = AzulSenacEscuro;
                        break;

                    case TabControl tab:
                        tab.BackColor = Fundo;
                        break;

                    case Label lbl when !EhFilhoDeCabecalho(lbl):
                        lbl.ForeColor = TextoEscuro;
                        break;
                }

                if (c.HasChildren)
                {
                    AplicarControles(c.Controls);
                }
            }
        }

        private static bool EhFilhoDeCabecalho(Control c)
            => c.Parent is Panel pai && pai.Dock == DockStyle.Top && pai.BackColor == AzulSenac;

        private static void ColorirComoCabecalho(Panel painel)
        {
            foreach (Control filho in painel.Controls)
            {
                if (filho is Label lbl)
                {
                    lbl.ForeColor = TextoClaro;
                    lbl.Font = new Font(lbl.Font, FontStyle.Bold);
                }
            }
        }

        private static void EstilizarBotao(Button b)
        {
            var texto = (b.Text ?? string.Empty).ToLowerInvariant();
            bool secundario = false;
            foreach (var palavra in AcoesSecundarias)
            {
                if (texto.Contains(palavra))
                {
                    secundario = true;
                    break;
                }
            }

            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;

            if (secundario)
            {
                b.BackColor = ColorTranslator.FromHtml("#7A7A7A");
                b.ForeColor = TextoClaro;
                b.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#8F8F8F");
            }
            else
            {
                b.BackColor = LaranjaSenac;
                b.ForeColor = TextoClaro;
                b.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#FFA83D");
            }
        }

        private static void EstilizarGrid(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.BorderStyle = BorderStyle.None;
            dgv.BackgroundColor = Color.White;
            dgv.GridColor = Borda;

            dgv.ColumnHeadersDefaultCellStyle.BackColor = AzulSenac;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextoClaro;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = AzulSenac;
            dgv.ColumnHeadersHeight = 32;

            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.ForeColor = TextoEscuro;
            dgv.DefaultCellStyle.SelectionBackColor = LaranjaSenac;
            dgv.DefaultCellStyle.SelectionForeColor = TextoClaro;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FCEBD8");
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = LaranjaSenac;
            dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextoClaro;
        }
    }
}
