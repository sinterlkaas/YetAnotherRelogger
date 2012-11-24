using System;
using System.Drawing;
using System.Text.RegularExpressions;
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
        [XmlIgnore] public DateTime LoginTime { get; set; }
        [XmlIgnore] public bool FoundLoginTime { get; set; }
        
        // Demonbuddy
        public string Location { get; set; }
        public string Key { get; set; }
        public string CombatRoutine { get; set; }
        public bool NoFlash { get; set; }
        public bool AutoUpdate { get; set; }
        public bool NoUpdate { get; set; }
        public int Priority { get; set; }

        // Affinity
        // If CpuCount does not match current machines CpuCount,
        // the affinity is set to all processor
        public int CpuCount { get; set; }
        public int ProcessorAffinity { get; set; }

        [XmlIgnore] public int AllProcessors
        {
            get
            {
                int intProcessorAffinity = 0;
                for (int i = 0; i < Environment.ProcessorCount; i++)
                    intProcessorAffinity |= (1 << i);
                return intProcessorAffinity;
            }
        }
        

        // Position
        public bool ManualPosSize { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        [XmlIgnore] public Rectangle AutoPos;

        public bool ForceEnableAllPlugins { get; set; }

        public DemonbuddyClass()
        {
            CpuCount = Environment.ProcessorCount;
            ProcessorAffinity = AllProcessors;
        }


        public bool IsInitialized
        {
            get
            {
                if (!Parent.AntiIdle.IsInitialized && General.DateSubtract(Parent.AntiIdle.InitTime) > 180)
                {
                    Parent.AntiIdle.FailedInitCount++;
                    if (Parent.AntiIdle.FailedInitCount > 3)
                    {
                        Logger.Instance.Write(Parent, "Demonbuddy:{0}: Failed to initialize more than 3 times", Parent.Demonbuddy.Proc.Id);
                        Parent.Stop();
                    }
                    else
                    {
                        Logger.Instance.Write(Parent, "Demonbuddy:{0}: Failed to initialize", Parent.Demonbuddy.Proc.Id);
                        Parent.Demonbuddy.Stop(true);
                        Thread.Sleep(2000);
                        Parent.Demonbuddy.Start();
                    }
                    return false;
                }
                return Parent.AntiIdle.IsInitialized;
            }
        }
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

            // Get Last login time and kill old session
            if (GetLastLoginTime) BuddyAuth.Instance.KillSession(Parent);

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
            else if (!noprofile)
                Logger.Instance.Write("Warning: Launching Demonbuddy without a starting profile (Add a profile to the profilescheduler for this bot)");

            if (NoFlash) arguments += " -noflash";
            if (AutoUpdate) arguments += " -autoupdate";
            if (NoUpdate) arguments += " -noupdate";

            if (ForceEnableAllPlugins)
                arguments += " -YarEnableAll";

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
               
                
                // Set affinity
                if (CpuCount != Environment.ProcessorCount)
                {
                    ProcessorAffinity = AllProcessors; // set it to all ones
                    CpuCount = Environment.ProcessorCount;
                }
                Proc.ProcessorAffinity = (IntPtr)ProcessorAffinity;


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

        private bool GetLastLoginTime
        {
            get
            {
                // No info to get from any process
                if (Proc == null) return false;

                // get log dir
                var logdir = Path.Combine(Path.GetDirectoryName(Location), "Logs");
                if (logdir.Length == 0 || !Directory.Exists(logdir))
                { // Failed to get log dir so exit here
                    Logger.Instance.Write(Parent, "Demonbuddy:{0}: Failed to find logdir", Proc.Id);
                    return false;
                }
                // get log file
                var logfile = string.Empty;
                var success = false;
                var starttime = Proc.StartTime;
                // Loop a few times if log is not found on first attempt and add a minute for each loop
                for (int i = 0; i <= 3; i++)
                {
                    // Test if logfile exists for current process starttime + 1 minute
                    logfile = string.Format("{0}\\{1} {2}.txt", logdir, Proc.Id, starttime.AddMinutes(i).ToString("yyyy-MM-dd HH.mm"));
                    if (File.Exists(logfile))
                    {
                        success = true;
                        break;
                    }
                }
                
                if (success)
                {
                    Logger.Instance.Write(Parent, "Demonbuddy:{0}: Found matching log: {1}", Proc.Id, logfile);

                    // Read Log file
                    // [11:03:21.173 N] Logging in...
                    try
                    {
                        using (var fs = new FileStream(logfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            var reader = new StreamReader(fs);
                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();
                                if (line == null) continue;
                                var m = new Regex(@"^\[([0-9]{2}:[0-9]{2}:[0-9]{2}.[0-9]{3}) .\] Logging in\.\.\.$", RegexOptions.Compiled).Match(line);
                                if (m.Success)
                                {
                                    LoginTime = DateTime.Parse(string.Format("{0:yyyy-MM-dd} {1}", starttime.ToUniversalTime(), TimeSpan.Parse(m.Groups[1].Value)));
                                    Debug.WriteLine("Found login time: {0}", LoginTime);
                                    return true;
                                }
                                Thread.Sleep(200); // Be nice for CPU
                            }
                            Logger.Instance.Write(Parent, "Demonbuddy:{0}: Failed to find login time", Proc.Id);
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Write(Parent, "Demonbuddy:{0}: Error accured while reading log: {1}", Proc.Id, ex.ToString());
                    }
                }
                // Else print error + return false
                Logger.Instance.Write(Parent, "Demonbuddy:{0}: Failed to find matching log", Proc.Id);
                return false;
            }
        }
    }
}
