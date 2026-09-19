using System;
using System.Windows.Forms;
using PDVStore.Setup.Forms;

namespace PDVStore.Setup;

internal static class Program
{
    // Ponto de entrada do instalador: é aqui que o Windows começa a executar o PDVStore.Setup.
    // O atributo [STAThread] indica que o thread principal deve usar o modelo STA (Single-Threaded
    // Apartment), exigência obrigatória de qualquer aplicação Windows Forms — sem ele os controles
    // como Button e TextBox podem se comportar de forma imprevisível.
    // Dependências: inicializa o visual moderno (EnableVisualStyles), adota texto compatível com
    // versões antigas e abre a janela principal do assistente (frmSetupWizard).
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new frmSetupWizard());
    }
}