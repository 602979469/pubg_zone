using System;
using System.Linq;
using System.Windows.Forms;

namespace PubgZoneHelper;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Any(arg => arg.Equals("--diagnose", StringComparison.OrdinalIgnoreCase)))
        {
            Diagnostics.Write();
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
