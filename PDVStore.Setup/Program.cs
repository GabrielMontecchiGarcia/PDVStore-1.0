using System;
using System.Windows.Forms;
using PDVStore.Setup.Forms;

namespace PDVStore.Setup;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new frmSetupWizard());
    }
}