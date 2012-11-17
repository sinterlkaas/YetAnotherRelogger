using System;
using System.Drawing;
using System.Threading;
using System.Xml.Serialization;
using System.Diagnostics;
using System.IO;
using YetAnotherRelogger.Helpers.Tools;

namespace YetAnotherRelogger.Helpers.Bot
{
    public class DemonbuddyClass
    {
        [XmlIgnore] public BotClass Parent { get; set; }

        [XmlIgnore] public Process Proc;
        [XmlIgnore] private bool _isStopped;
        [XmlIgnore] public bool IsRunning { get {  return (Proc != null && !Proc.HasExited && !_isStopped); } }

        [XmlIgnore] public  IntPtr MainWindowHandle;

        // Buddy Auth
        public string BuddyAuthUsername { get; set; }
        public string BuddyAuthPassword { get; set; }
        
        // Demonbuddy
        public string Location { get; set; }
        public string Key { get; set; }
        public string CombatRoutine { get; set; }
        public bool NoFlash { get; set; }
        public bool AutoUpdate { get; set; }
        public bool NoUpdate { get; set; }
        public int Priority { get; set; }
        

        // Position
        public bool ManualPosSize { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        [XmlIgnore] public Rectangle AutoPos;

        public bool ForceEnableAllPlugins { get; set; }

        private DateTime _lastRepsonse;
        public void CrashCheck()
        {
            if (Proc.HasExited)
                return;

            if (CrashChecker.IsResponding(MainWindowHandle))
                _lastRepsonse = DateTime.Now;

            if (DateTime.Now.Subtract(_lastRepsonse).TotalSeconds > 30)
            {
                Logger.Instance.Write(Parent, "Demonbuddy:{0}: Is unresponsive for more than 30 seconds", Proc.Id);
                Logger.Instance.Write(Parent, "Demonbuddy:{0}: Killing process", Proc.Id);
                try
                {
                    if (Proc != null && !Proc.HasExited)
                        Proc.Kill();
                }
                catch (Exception ex)
                {
                    Logger.Instance.Write(Parent, "Failed to kill process: {0}", ex.Message);
                }
            }
        }

        public void Start(bool noprofile = false)
        {
            if (!Parent.IsStarted || !Parent.Diablo.IsRunning)
                return;
            if (!File.Exists(Location))
            {
                Logger.Instance.Write("File not found: {0}", Location);
                return;
            }
            _isStopped = false;

            // Reset AntiIdle;
            Parent.AntiIdle.Reset(true);

            var arguments = "-pid=" + Parent.Diablo.Proc.Id;
            arguments += " -key=" + Key;
            arguments += " -autostart";
            arguments += string.Format(" -routine=\"{0}\"", CombatRoutine);
            arguments += string.Format(" -bnetaccount=\"{0}\"", Parent.Diablo.Username);
            arguments += string.Format(" -bnetpassword=\"{0}\"", Parent.Diablo.Password);

            if (Parent.ProfileSchedule.Profiles.Count > 0 && !noprofile)
            {
                var profilepath = Parent.ProfileSchedule.GetProfile;
                if (File.Exists(profilepath))
                    arguments += string.Format(" -profile=\"{0}\"", profilepath);
            }
            else
                Logger.Instance.Write("Warning: Launching Demonbuddy without a starting profile (Add a profile to the profilescheduler for this bot)");

            if (NoFlash) arguments += " -noflash";
            if (AutoUpdate) arguments += " -autoupdate";
            if (NoUpdate) arguments += " -noupdate";

            if (ForceEnableAllPlugins)
                arguments += " -YarEnableAll";
            //arguments += " -YARENABLE";

            Debug.WriteLine("DB Arguments: {0}", arguments);

            var p = new ProcessStartInfo(Location, arguments) {WorkingDirectory = Path.GetDirectoryName(Location)};
            
            // Check/Install latest Communicator plugin
            var plugin = string.Format("{0}\\Plugins\\YAR\\Plugin.cs", p.WorkingDirectory);
            if (!PluginVersionCheck.Check(plugin)) PluginVersionCheck.Install(plugin);


            try // Try to start Demonbuddy
            {
                Parent.Status = "Starting Demonbuddy"; // Update Status
                Proc = Process.Start(p);

                if (Program.IsRunAsAdmin)
                    Proc.PriorityClass = General.GetPriorityClass(Priority);
                else
                    Logger.Instance.Write(Parent, "Failed to change priority (No admin rights)");

                Logger.Instance.Write(Parent, "Demonbuddy:{0}: Waiting for process to become ready", Proc.Id);
                if (!Proc.WaitForInputIdle(30000))
                {
                    Logger.Instance.Write(Parent, "Demonbuddy:{0}: Failed to start!", Proc.Id);
                    Parent.Restart();
                    return;
                }

                if (_isStopped) return;
                
            }
            catch (Exception ex)
            {
                Logger.Instance.Write(ex.ToString());
                Parent.Stop();
            }

            var timeout = DateTime.Now;
            while (!FindMainWindow())
            {
                if (General.DateSubtract(timeout) > 30)
                {
                    MainWindowHandle = Proc.MainWindowHandle;
                    break;
                }
                Thread.Sleep(500);
            }

            // Window postion & resizing
            if (ManualPosSize)
                AutoPosition.ManualPositionWindow(MainWindowHandle, X, Y, W, H, Parent);

            Logger.Instance.Write(Parent, "Demonbuddy:{0}: Process is ready", Proc.Id);
        }

        private bool FindMainWindow()
        {
            var handle = FindWindow.EqualsWindowCaption("Demonbuddy", Proc.Id);
            if (handle != IntPtr.Zero)
            {
                MainWindowHandle = handle;
                Logger.Instance.Write(Parent, "Found Demonbuddy: MainWindow ({0})", handle);
                return true;
            }
            handle = FindWindow.EqualsWindowCaption("Demonbuddy - BETA", Proc.Id);
            if (handle != IntPtr.Zero)
            {
                MainWindowHandle = handle;
                Logger.Instance.Write(Parent, "Found Demonbuddy - BETA: MainWindow ({0})", handle);
                return true;
            }
            return false;
        }

        public void Stop(bool force = false)
        {
            _isStopped = true;

            if (Proc == null || Proc.HasExited) return;

            // Force close
            if (force)
            {
                Logger.Instance.Write(Parent, "Demonbuddy:{0}: Forced to close!", Proc.Id);
                Proc.Kill();
                return;
            }

            if (Parent.Diablo.Proc == null || Parent.Diablo.Proc.HasExited)
            {
                Logger.Instance.Write(Parent, "Demonbuddy:{0}: Waiting to close", Proc.Id);
                Proc.WaitForExit(5000);
                if (Proc == null || Proc.HasExited) 
                {
                    Logger.Instance.Write(Parent, "Demonbuddy:{0}: Closed.", Proc.Id);
                    return;
                }
            }

            if (Proc.HasExited)
                Logger.Instance.Write(Parent, "Demonbuddy:{0}: Closed.", Proc.Id);
            else
            {
                Logger.Instance.Write(Parent, "Demonbuddy:{0}: Failed to close! kill process", Proc.Id);
                Proc.Kill();
            }
        }

        public void CrashTender()
        {
            Logger.Instance.Write(Parent, "CrashTender: Stopping Demonbuddy:{0}", Proc.Id);
            Stop(true); // Force DB to stop
            Logger.Instance.Write(Parent, "CrashTender: Starting Demonbuddy without a starting profile");
            Start(noprofile:true);
        }
    }
}
