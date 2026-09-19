using System;
using System.Drawing;
using System.Windows.Forms;

namespace PDVStore.Helpers
{
    /// <summary>Caixas de entrada simples para valores e textos (sem depender do Microsoft.VisualBasic).</summary>
    public static class PromptDialog
    {
        public static decimal? AskDecimal(string titulo, string mensagem, decimal valorInicial = 0)
        {
            using var form = new Form
            {
                Text = titulo,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ClientSize = new Size(340, 120),
                MaximizeBox = false,
                MinimizeBox = false
            };
            form.Font = new Font("Segoe UI", 10F);

            var lbl = new Label { Text = mensagem, Location = new Point(12, 10), AutoSize = true };
            var txt = new TextBox { Location = new Point(12, 36), Size = new Size(312, 26), Text = valorInicial.ToString("0.00") };
            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(150, 76), Size = new Size(84, 30) };
            var btnCancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(240, 76), Size = new Size(84, 30) };

            form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            return form.ShowDialog() == DialogResult.OK && decimal.TryParse(txt.Text, out decimal valor) && valor >= 0
                ? valor
                : (decimal?)null;
        }

        public static string? AskText(string titulo, string mensagem, string valorInicial = "")
        {
            using var form = new Form
            {
                Text = titulo,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ClientSize = new Size(340, 120),
                MaximizeBox = false,
                MinimizeBox = false
            };
            form.Font = new Font("Segoe UI", 10F);

            var lbl = new Label { Text = mensagem, Location = new Point(12, 10), AutoSize = true };
            var txt = new TextBox { Location = new Point(12, 36), Size = new Size(312, 26), Text = valorInicial };
            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(150, 76), Size = new Size(84, 30) };
            var btnCancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(240, 76), Size = new Size(84, 30) };

            form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            return form.ShowDialog() == DialogResult.OK ? txt.Text : null;
        }
    }
}