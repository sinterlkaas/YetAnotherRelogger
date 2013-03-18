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
        [XmlIgnore] private Process _proc;
        [XmlIgnore] public Process Proc
        {
            get { return _proc; }
            set
            {
                if (value != null)
                    Parent.DemonbuddyPid = value.Id.ToString();
                _proc = value;
            }
        }

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

        [XmlIgnore] private bool _crashTenderRestart;

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
                // If diablo is no longer running close db and return false
                if (!Parent.Diablo.IsRunning)
                {
                    Parent.Demonbuddy.Stop(true);
                    return false;
                }

                if ((!Parent.AntiIdle.IsInitialized && General.DateSubtract(Parent.AntiIdle.InitTime) > 180) || !IsRunning)
                {
                    Parent.AntiIdle.FailedInitCount++;
                    if (Parent.AntiIdle.FailedInitCount >= (Parent.AntiIdle.InitAttempts > 0 ? 1 : 3))
                    {
                        Parent.AntiIdle.InitAttempts++;
                        Logger.Instance.Write(Parent, "Demonbuddy:{0}: Failed to initialize more than 3 times", Parent.Demonbuddy.Proc.Id);
                        Parent.Standby();
                    }
                    else
                    {
                        Logger.Instance.Write(Parent, "Demonbuddy:{0}: Failed to initialize {1}/3", Parent.Demonbuddy.Proc.Id, Parent.AntiIdle.FailedInitCount);
                        Parent.Demonbuddy.Stop(true);
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
                    Logger.Instance.Write(Parent, "Failed to kill process", ex.Message);
                    DebugHelper.Exception(ex);
                }
            }
        }

        public void Start(bool noprofile = false, string profilepath = null, bool crashtenderstart = false)
        {
            if (!Parent.IsStarted || !Parent.Diablo.IsRunning || (_crashTenderRestart && !crashtenderstart)) return;
            if (!File.Exists(Location))
            {
                Logger.Instance.Write("File not found: {0}", Location);
                return;
            }

            while (Parent.IsStarted && Parent.Diablo.IsRunning)
            {
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

                if (profilepath != null)
                {
                    // Check if current profile path is Kickstart
                    var file = Path.GetFileName(profilepath);
                    if (file == null || (file.Equals("YAR_Kickstart.xml") || file.Equals("YAR_TMP_Kickstart.xml")))
                        profilepath = Parent.ProfileSchedule.Current.Location;

                    var profile = new Profile() {Location = profilepath};
                    var path = ProfileKickstart.GenerateKickstart(profile);
                    arguments += string.Format(" -profile=\"{0}\"", path);
                }
                else if (Parent.ProfileSchedule.Profiles.Count > 0 && !noprofile)
                {
                    var path = Parent.ProfileSchedule.GetProfile;
                    if (File.Exists(path))
                        arguments += string.Format(" -profile=\"{0}\"", path);
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
                p = UserAccount.ImpersonateStartInfo(p, Parent);

                // Check/Install latest Communicator plugin
                var plugin = string.Format("{0}\\Plugins\\YAR\\Plugin.cs", p.WorkingDirectory);
                if (!PluginVersionCheck.Check(plugin)) PluginVersionCheck.Install(plugin);


                DateTime timeout;
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
                    Proc.ProcessorAffinity = (IntPtr) ProcessorAffinity;


                    Logger.Instance.Write(Parent, "Demonbuddy:{0}: Waiting for process to become ready", Proc.Id);

                    timeout = DateTime.Now;
                    while (true)
                    {
                        if (General.DateSubtract(timeout) > 30)
                        {
                            Logger.Instance.Write(Parent, "Demonbuddy:{0}: Failed to start!", Proc.Id);
                            Parent.Restart();
                            return;
                        }
                        Thread.Sleep(500);
                        try
                        {
                            Proc.Refresh();
                            if (Proc.WaitForInputIdle(100) || CrashChecker.IsResponding(MainWindowHandle))
                                break;
                        }
                        catch
                        {
                        }
                    }

                    if (_isStopped) return;

                }
                catch (Exception ex)
                {
                    DebugHelper.Exception(ex);
                    Parent.Stop();
                }

                timeout = DateTime.Now;
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

                // Wait for demonbuddy to be Initialized (this means we are logged in)
                // If we don't wait here the Region changeing for diablo fails!
                Logger.Instance.Write(Parent, "Demonbuddy:{0}: Waiting for demonbuddy to log into Diablo", Proc.Id);
                while (!IsInitialized && !_isStopped)
                    Thread.Sleep(1000);
                // We made to many attempts break here
                if (Parent.AntiIdle.FailedInitCount > 3) break;
                if (!Parent.AntiIdle.IsInitialized) continue; // Retry

                // We are ready to go
                Logger.Instance.Write(Parent, "Demonbuddy:{0}: Initialized! We are ready to go", Proc.Id);
                Parent.AntiIdle.FailedInitCount = 0; // only reset counter
                break;
            } // while (Parent.IsStarted && Parent.Diablo.IsRunning)
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
                Proc.WaitForExit(60000);
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
                //Proc.Kill();
            }
        }

        public void CrashTender(string profilepath = null)
        {
            _crashTenderRestart = true;
            Logger.Instance.Write(Parent, "CrashTender: Stopping Demonbuddy:{0}", Proc.Id);
            Stop(true); // Force DB to stop
            Logger.Instance.Write(Parent, "CrashTender: Starting Demonbuddy without a starting profile");


            if (profilepath != null)
                Start(profilepath:profilepath, crashtenderstart: true);
            else
                Start(noprofile: true, crashtenderstart: true);
            _crashTenderRestart = false;
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
                            var time = new TimeSpan();
                            bool Logging = false;
                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();
                                if (line == null) continue;

                                if (Logging && line.Contains("Attached to Diablo III with pid"))
                                {
                                    LoginTime =
                                        DateTime.Parse(string.Format("{0:yyyy-MM-dd} {1}",
                                                                     starttime.ToUniversalTime(),
                                                                     time));
                                    Logger.Instance.Write("Found login time: {0}", LoginTime);
                                    return true;
                                }
                                var m = new Regex(@"^\[(.+) .\] Logging in\.\.\.$",
                                              RegexOptions.Compiled).Match(line);
                                if (m.Success)
                                {
                                    time = TimeSpan.Parse(m.Groups[1].Value);
                                    Logging = true;
                                }

                                Thread.Sleep(5); // Be nice for CPU
                            }
                            Logger.Instance.Write(Parent, "Demonbuddy:{0}: Failed to find login time", Proc.Id);
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Write(Parent, "Demonbuddy:{0}: Error accured while reading log", Proc.Id);
                        DebugHelper.Exception(ex);
                    }
                }
                // Else print error + return false
                Logger.Instance.Write(Parent, "Demonbuddy:{0}: Failed to find matching log", Proc.Id);
                return false;
            }
        }
    }
}
