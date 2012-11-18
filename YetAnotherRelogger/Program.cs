using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Security.Principal;

using YetAnotherRelogger.Helpers;
using YetAnotherRelogger.Forms;
using YetAnotherRelogger.Properties;

namespace YetAnotherRelogger
{
    static class Program
    {
        public const string VERSION = "0.1.7.2";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        [STAThread]
        static void Main()
        {
            // Run as admin check
            IsRunAsAdmin = (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator));
            
            // Load settings
            BotSettings.Instance.Load();
            Settings.Default.Reload();

            if (Settings.Default.AutoPosScreens == null || (Settings.Default.AutoPosScreens != null && Settings.Default.AutoPosScreens.Count == 0))
                AutoPosition.UpdateScreens();
            if (Settings.Default.D3StarterPath.Equals(string.Empty) || Settings.Default.D3StarterPath.Equals(""))
                Settings.Default.D3StarterPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "ThirdParty\\D3Starter.exe");

            // Start background thread
            Relogger.Instance.Start();
            
            if (Settings.Default.FocusCheck)
                ForegroundChecker.Instance.Start();
            
            var comms = new Communicator();
            comms.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Mainform = new MainForm2();
            Application.Run(Mainform);

            Settings.Default.Save();
            Logger.Instance.WriteGlobal("Closed!");
            Logger.Instance.ClearBuffer();
        }

        public static MainForm2 Mainform;
        public static bool IsRunAsAdmin;
        public static bool Pause;
    }
}
