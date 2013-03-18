﻿using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Security.Principal;

using YetAnotherRelogger.Helpers;
using YetAnotherRelogger.Forms;
using YetAnotherRelogger.Helpers.Tools;
using YetAnotherRelogger.Properties;

namespace YetAnotherRelogger
{
    static class Program
    {
        public const string VERSION = "0.2.0.8";
        public const int Sleeptime = 10;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        [STAThread]
        static void Main()
        {
            try
            {
                // Allow only one instance to be run
                if (!SingleInstance.Start())
                {
                    SingleInstance.ShowFirstInstance();
                    return;
                }

                // Run as admin check
                var identity = WindowsIdentity.GetCurrent();
                if (identity != null)
                    IsRunAsAdmin = (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator));

                // Get Commandline args
                CommandLineArgs.Get();

                if (CommandLineArgs.SafeMode)
                {
                    var result = MessageBox.Show("Launching in safe mode!\nThis will reset some features", "YetAnotherRelogger Safe Mode", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                    if (result == DialogResult.Cancel)
                        return;
                }

                // Load settings
                BotSettings.Instance.Load();
                Settings.Default.Reload();

                if (Settings.Default.AutoPosScreens == null ||
                    (Settings.Default.AutoPosScreens != null && Settings.Default.AutoPosScreens.Count == 0))
                    AutoPosition.UpdateScreens();
                if (Settings.Default.D3StarterPath.Equals(string.Empty) || Settings.Default.D3StarterPath.Equals(""))
                    Settings.Default.D3StarterPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "ThirdParty\\D3Starter.exe");

                // Start background threads
                Relogger.Instance.Start();
                Communicator.Instance.Start();

                if (!CommandLineArgs.SafeMode)
                {
                    if (Settings.Default.StatsEnabled)
                        StatsUpdater.Instance.Start();
                    if (Settings.Default.FocusCheck)
                        ForegroundChecker.Instance.Start();
                }
                else
                {
                    Settings.Default.StatsEnabled = false;
                    Settings.Default.FocusCheck = false;
                    AutoPosition.UpdateScreens();
                }


                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Mainform = new MainForm2();
                Application.Run(Mainform);
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteGlobal(ex.ToString());
            }
            // Clean up
            SingleInstance.Stop();
            Settings.Default.Save();
            Logger.Instance.WriteGlobal("Closed!");
            Logger.Instance.ClearBuffer();
        }

        public static MainForm2 Mainform;
        public static bool IsRunAsAdmin;
        public static bool Pause;
    }

    #region SingleInstance
    // http://www.codeproject.com/Articles/32908/C-Single-Instance-App-With-the-Ability-To-Restore
    static public class SingleInstance
    {
        public static readonly int WM_SHOWFIRSTINSTANCE = WinAPI.RegisterWindowMessage("WM_SHOWFIRSTINSTANCE|{0}", ProgramInfo.AssemblyGuid);
        static Mutex mutex;
        static public bool Start()
        {
            bool onlyInstance;
            var mutexName = String.Format("Local\\{0}", ProgramInfo.AssemblyGuid);

            // if you want your app to be limited to a single instance
            // across ALL SESSIONS (multiple users & terminal services), then use the following line instead:
            // string mutexName = String.Format("Global\\{0}", ProgramInfo.AssemblyGuid);

            mutex = new Mutex(true, mutexName, out onlyInstance);
            return onlyInstance;
        }
        static public void ShowFirstInstance()
        {
            WinAPI.PostMessage(
                (IntPtr)WinAPI.HWND_BROADCAST,
                WM_SHOWFIRSTINSTANCE,
                IntPtr.Zero,
                IntPtr.Zero);
        }
        static public void Stop()
        {
            try
            {
                mutex.ReleaseMutex();
            }
            catch (Exception ex)
            {
                DebugHelper.Exception(ex);
            }
            
        }
    }
    #endregion
    #region ProgramInfo
    static public class ProgramInfo
    {
        static public string AssemblyGuid
        {
            get
            {
                var attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
                return attributes.Length == 0 ? String.Empty : ((System.Runtime.InteropServices.GuidAttribute)attributes[0]).Value;
            }
        }
    } 
    #endregion
}
