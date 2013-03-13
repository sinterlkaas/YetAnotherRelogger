// VERSION: 0.2.0.7
/* Changelog:
 * VERSION: 0.2.0.6
 * Added 1 second pulse timer to reduce CPU utilization
 * VERSION: 0.2.0.6
 * Added: Support for: Take A Break by Ghaleon
 * VERSION: 0.2.0.5
 * Improved: Log scanning
 * VERSION: 0.1.9.1
 * Added: Monsterpower
 * Added: Support for RadsAtom
 * VERSION: 0.1.8.4
 * Changed: Delay between stats reports to yar from 1 second to 3 seconds
 * Added: Some delay in possible intensive loops (make it nicer for CPU)
 * VERSION: 0.1.8.2
 * Added: Crashtender now uses Kickstart profile
 * VERSION: 0.1.8.1
 * Added: Kickstart custom profiletag
 * VERSION: 0.1.7.7
 * improved profile loading
 * VERSION: 0.1.7.6
 * Added: Support for Atom 2.0.15+ "Take a break"
 * VERSION: 0.1.7.2
 * Added: Sends Coinage to YAR, will be reset after 2 mins of no gold change
 * VERSION: 0.1.7.1
 * Added: Demonbuddy invalid/expired sessions detection
 * Added: Failed to attach detection
 * Improved AntiIdle system a bit
 * VERSION: 0.0.0.6
 * Fixed: DateTime issues for non-english windows
 * VERSION: 0.0.0.5
 * Main app update
 * VERSION: 0.0.0.4
 * Added: Force enable all plugins
 * Added: Support for Giles Emergency stop
 * Added: Support for BuddyStats stop
 * Changed: Version now matches YAR main app
 * VERSION: 0.0.0.1
 * Initial realease
 */
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.IO.Pipes;
using System.IO;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Settings;
using Zeta.TreeSharp;
using UIElement = Zeta.Internals.UIElement;

namespace YARPLUGIN
{
    public class YARPLUGIN : IPlugin
    {
        // Plugin version
        public Version Version { get { return new Version(0, 2, 0, 7); } }

        private const bool _debug = true;

        // Compatibility
        private static readonly Regex[] ReCompatibility =
            {
                /* BuddyStats Remote control action */
                new Regex(@"Stop command from BuddyStats"), // stop command
                /* Emergency Stop: You need to stash an item but no valid space could be found. Stash is full? Stopping the bot to prevent infinite town-run loop. */
                new Regex(@".+Emergency Stop: .+"), // Emergency stop
                /* Atom 2.0.15+ "Take a break" */
                new Regex(@".*Atom.*Will Stop the bot for .+ minutes\.$"), // Take a break
                /* RadsAtom "Take a break" */
                new Regex(@"\[RadsAtom\].+ minutes to next break, the break will last for .+ minutes."), 
                /* Take A Break by Ghaleon */
                new Regex(@"\[TakeABreak.*\] It's time to take a break.*"), 
            };

        // CrashTender
        private static readonly Regex[] ReCrashTender =
            {
                /* Invalid Session */
                new Regex(@"Session is invalid!", RegexOptions.IgnoreCase),
                /* Session expired */
                new Regex(@"Session is expired", RegexOptions.IgnoreCase),
                /* Failed to attach to D3*/
                new Regex(@"Was not able to attach to any running Diablo III process, are you running the bot already\?"), 
            };

        public class BotStats
        {
            public int Pid;
            public long LastRun;
            public long LastPulse;
            public long PluginPulse;
            public long LastGame;
            public bool IsPaused;
            public bool IsRunning;
            public bool IsInGame;
            public bool IsLoadingWorld;
            public int Coinage;
        }
        #region Plugin information
        public string Author { get { return "sinterlkaas"; } }
        public string Description { get { return "Communication plugin for YetAnotherRelogger"; } }
        public string Name { get { return "YAR Comms"; } }
        public bool Equals(IPlugin other)
        {
            return (other.Name == Name) && (other.Version == Version);
        }
        #endregion

        public Window DisplayWindow { get { return null; } }
        private bool _allPluginsCompiled;
        private Thread _yarThread;
        private Thread _scanlogthread;
        private BotStats _bs;
        private bool _pulseFix;

        public static void Log(string str, params object[] args)
        {
            Logging.Write("[YetAnotherRelogger] " + str, args);
        }
        public static void LogException(Exception ex)
        {
            Logging.Write("[YetAnotherRelogger] Error: {0}", ex.Message);
            if (_debug) Logging.Write("[YetAnotherRelogger] Error: {0}", ex.StackTrace);
        }

        #region Plugin Events
        public void OnInitialize()
        {
            _bs = new BotStats();

            lmd = new Logging.LogMessageDelegate(Logging_OnLogMessage);
            Logging.OnLogMessage += lmd;
            _bs.Pid = Process.GetCurrentProcess().Id;

            Reset();

            _yarThread = new Thread(YarWorker) { IsBackground = true };
            _yarThread.Start();

            Send("Initialized");
        }

        public void OnShutdown()
        {
            _yarThread.Abort();
            Logging.OnLogMessage -= new Logging.LogMessageDelegate(Logging_OnLogMessage);
        }


        Logging.LogMessageDelegate lmd;
        public void OnEnabled()
        {


            if (_yarThread == null || (_yarThread != null && !_yarThread.IsAlive))
            {
                _yarThread = new Thread(YarWorker) { IsBackground = true };
                _yarThread.Start();
            }
            Send("NewMonsterPowerLevel", true); // Request Monsterpower level
            Reset();
        }

        public void OnDisabled()
        {
            Log("Disabled!");

            // Pulsefix disabled plugin
            if (_pulseFix)
            {
                _pulseFix = false;
                return; // Stop here to prevent Thread abort
            }
            // user disabled plugin abort Thread
            _yarThread.Abort();

            if (lmd != null)
                Logging.OnLogMessage -= lmd;
        }

        private Stopwatch pulseTimer = new Stopwatch();

        public void OnPulse()
        {
            
            _pulseCheck = true;
            _bs.LastPulse = DateTime.Now.Ticks;

            _bs.PluginPulse = DateTime.Now.Ticks;
            _bs.IsRunning = BotMain.IsRunning;

            if (BotMain.IsPaused || BotMain.IsPausedForStateExecution)
            {
                _bs.IsPaused = true;
            }
            else if (BotMain.IsRunning)
            {
                _bs.IsPaused = false;
                _bs.LastRun = DateTime.Now.Ticks;
            }
            else
                _bs.IsPaused = false;

            if (!pulseTimer.IsRunning)
            {
                pulseTimer.Start();
                return;
            }

            if (pulseTimer.ElapsedMilliseconds > 1000)
                pulseTimer.Restart();

            if (!ZetaDia.IsInGame || ZetaDia.Me == null || !ZetaDia.Me.IsValid || ZetaDia.IsLoadingWorld)
            {
                return;
            }

            // Handle errors and other strange situations
            //ErrorHandling();

            // Trinity Pause support
            //TrinityPauseHandling();

            // in-game / character data 
            _bs.IsLoadingWorld = ZetaDia.IsLoadingWorld;
            _bs.Coinage = 0;
            try
            {
                if (ZetaDia.Me != null && ZetaDia.Me.IsValid)
                    _bs.Coinage = ZetaDia.Me.Inventory.Coinage;
            }
            catch
            {
                _bs.Coinage = -1;
            }

            if (ZetaDia.IsInGame)
            {
                _bs.LastGame = DateTime.Now.Ticks;
                _bs.IsInGame = true;
            }
            else
            {
                if (_bs.IsInGame)
                {
                    Send("GameLeft", true);
                    Send("NewMonsterPowerLevel", true); // Request Monsterpower level
                }
                _bs.IsInGame = false;
            }
        }
        #endregion

        #region Logging Monitor

        void Logging_OnLogMessage(ReadOnlyCollection<Logging.LogMessage> messages)
        {
            try
            {
                // Create new log buffer
                if (_logBuffer == null)
                    _logBuffer = messages.ToArray();
                else
                {
                    // Append to existing log buffer
                    lock (_logBuffer)
                    {
                        var newbuffer = _logBuffer.Concat(messages.ToArray()).ToArray();
                        _logBuffer = newbuffer;
                    }
                }

                // Start Scan log thread with lower priority
                if (_scanlogthread == null || (_scanlogthread != null && !_scanlogthread.IsAlive))
                {
                    try
                    {
                        ThreadPriority priority;
                        switch (Process.GetCurrentProcess().PriorityClass)
                        {
                            case ProcessPriorityClass.High:
                                priority = ThreadPriority.AboveNormal;
                                break;
                            case ProcessPriorityClass.AboveNormal:
                                priority = ThreadPriority.Normal;
                                break;
                            default:
                                priority = ThreadPriority.BelowNormal;
                                break;
                        }

                        _scanlogthread = new Thread(ScanLogWorker)
                                             {
                                                 Priority = priority,
                                                 IsBackground = true
                                             };
                        _scanlogthread.Start();
                    }
                    catch (Exception ex)
                    {
                        Log("Failed to assign priority to scanlogthread:");
                        LogException(ex);

                        Log("Starting scanlogthread with same priority");
                        _scanlogthread = new Thread(ScanLogWorker) { IsBackground = true };
                        _scanlogthread.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }
        private Logging.LogMessage[] _logBuffer;
        private void ScanLogWorker()
        {
            // Keep Thread alive while log buffer is not empty
            while (_logBuffer != null)
            {
                try
                {
                    var duration = DateTime.Now;
                    Logging.LogMessage[] buffer;
                    // Lock buffer and copy to local variable for scanning
                    lock (_logBuffer)
                    {
                        buffer = new Logging.LogMessage[_logBuffer.Length + 1]; // set log new local log buffer size
                        _logBuffer.CopyTo(buffer, 0); // copy to local
                        _logBuffer = null; // clear buffer
                    }
                    var count = 0; // Scan counter
                    var breakloop = false;
                    // Scan log items
                    foreach (var lm in buffer.Where(x => x != null))
                    {
                        count++; // add to counter
                        var msg = lm.Message;
                        // Log level specific scanning to prevent uneeded cpu usage
                        switch (lm.Level)
                        {
                            case LogLevel.Diagnostic:
                                if (!_allPluginsCompiled && FindPluginsCompiled(msg)) continue; // Find all plugins compiled line
                                // Find Start stop button click
                                if (msg.Equals("Start/Stop Button Clicked!") && !BotMain.IsRunning)
                                {
                                    Send("UserStop");
                                }
                                break; // case end
                            default:
                                if (!ZetaDia.IsInGame && FindStartDelay(msg)) continue; // Find new start delay

                                // Crash Tender check
                                if (ReCrashTender.Any(re => re.IsMatch(msg)))
                                {
                                    Send("CrashTender " + ProfileManager.CurrentProfile.Path); // tell relogger to "crash tender" :)
                                    breakloop = true; // break out of loop
                                    break; // case end here!
                                }
                                // YAR compatibility with other plugins
                                if (ReCompatibility.Any(re => re.IsMatch(msg)))
                                    Send("ThirdpartyStop");
                                break; // case end
                        }
                        if (breakloop) break; // Check if we need to break out of loop
                    }

                    if (count > 1) Logging.WriteDiagnostic("[YetAnotherRelogger] Scanned {0} log items in {1}ms", count, DateTime.Now.Subtract(duration).TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }
        }

        public bool FindStartDelay(string msg)
        {
            // Waiting #.# seconds before next game...
            var m = new Regex(@"Waiting (.+) seconds before next game...").Match(msg);
            if (m.Success)
            {
                Send("StartDelay " + DateTime.Now.AddSeconds(double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).Ticks);
                return true;
            }
            return false;
        }

        public bool FindPluginsCompiled(string msg)
        {
            var m = new Regex(@"There are \d+ plugins.").Match(msg);
            if (m.Success)
            {
                _allPluginsCompiled = true;
                Send("AllCompiled"); // tell relogger about all plugin compile so the relogger can tell what to do next
                return true;
            }
            return false;
        }
        #endregion

        #region Events
        // Nothing here :)
        #endregion

        #region yarWorker
        public void YarWorker()
        {
            while (true)
            {
                _bs.PluginPulse = DateTime.Now.Ticks;
                _bs.IsRunning = BotMain.IsRunning;

                // Send stats
                Send("XML:" + _bs.ToXmlString(), xml: true);
                Thread.Sleep(3000);
            }
        }
        #endregion

        #region Handle Trinity Pause
        private bool _trinityIsPaused;
        private void TrinityPauseHandling()
        {
            if (!_trinityIsPaused && TrinitySupport.IsPaused)
            {
                Send("TrinityPause");
                _trinityIsPaused = true;
            }
            else if (!BotMain.IsPaused && !BotMain.IsPausedForStateExecution)
                _trinityIsPaused = false;
        }
        #endregion

        #region Handle Errors and strange situations

        private bool handlederror;
        private void ErrorHandling()
        {
            if (ErrorDialog.IsVisible)
            { // Check if Demonbuddy found errordialog
                if (!handlederror)
                {
                    Send("CheckConnection", pause: true);
                    handlederror = true;
                }
                else
                {
                    handlederror = false;
                    ErrorDialog.Click();
                    bootTo();
                }
            }
            else if (UIElementTester.isValid(_UIElement.errordialog_okbutton))
            { // Demonbuddy failed to find error dialog use static hash to find the OK button
                Send("CheckConnection", pause: true);
                UIElement.FromHash(_UIElement.errordialog_okbutton).Click();
                bootTo();
            }
            else
            {
                handlederror = false;
                if (UIElementTester.isValid(_UIElement.loginscreen_username))
                { // We are at loginscreen
                    Send("CheckConnection", pause: true);
                }
            }

        }

        // Detect if we are booted to login screen or character selection screen
        private void bootTo()
        {
            var timeout = DateTime.Now;
            while (DateTime.Now.Subtract(timeout).TotalSeconds <= 15)
            {
                BotMain.PauseFor(TimeSpan.FromMilliseconds(600));
                if (UIElementTester.isValid(_UIElement.startresume_button))
                    break;
                if (UIElementTester.isValid(_UIElement.loginscreen_username))
                { // We are at loginscreen
                    Send("CheckConnection", pause: true);
                    break;
                }
                Thread.Sleep(500);
            }
        }
        #endregion

        #region PipeClientSend
        private bool Send(string data, bool pause = false, bool xml = false, int retry = 1, int timeout = 3000)
        {

            var success = false;
            var tries = 0;

            if (!xml)
                data = _bs.Pid + ":" + data;
            else
                data += "\nEND";

            // Pause bot
            if (pause)
            {
                _recieved = false;
                Func<bool> waitFor = Recieved;
                BotMain.PauseWhile(waitFor, 0, TimeSpan.FromMilliseconds((retry * timeout) + 3000));
            }
            while (!success && tries < retry)
            {
                try
                {
                    tries++;
                    using (var client = new NamedPipeClientStream(".", "YetAnotherRelogger"))
                    {
                        client.Connect(timeout);
                        if (client.IsConnected)
                        {
                            var sw = new StreamWriter(client) { AutoFlush = true };
                            var sr = new StreamReader(client);

                            sw.WriteLine(data);
                            var connectionTime = DateTime.Now;
                            while (!success && client.IsConnected)
                            {
                                if (DateTime.Now.Subtract(connectionTime).TotalSeconds > 3)
                                {
                                    client.Close();
                                    break;
                                }

                                var temp = sr.ReadLine();
                                if (temp == null)
                                {
                                    Thread.Sleep(10);
                                    continue;
                                }

                                HandleResponse(temp);
                                success = true;
                            }
                        }
                        else
                        {
                            // Failed to connect
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex);
                    OnShutdown();
                }
                Thread.Sleep(100);
            }
            _recieved = true;
            return success;
        }
        #endregion

        #region HandleResponse
        void HandleResponse(string data)
        {
            string cmd = data.Split(' ')[0];
            if (data.Split(' ').Count() > 1)
                data = data.Substring(cmd.Length + 1);
            switch (cmd)
            {
                case "Restart":
                    Log("Restarting bot");
                    BotMain.Stop();
                    Thread.Sleep(1000);
                    BotMain.Start();
                    Reset();
                    break;
                case "LoadProfile":
                    LoadProfile(data);
                    break;
                case "MonsterPower":
                    var powerlevel = Convert.ToInt32(data.Trim());
                    Log("Recieved MonsterPowerLevel: {0}", powerlevel);
                    if (powerlevel >= 0)
                        CharacterSettings.Instance.MonsterPowerLevel = powerlevel;
                    break;
                case "ForceEnableAll":
                    ForceEnableAllPlugins();
                    break;
                case "ForceEnableYar":
                    ForceEnableYar();
                    break;
                case "FixPulse":
                    FixPulse();
                    break;
                case "Roger!":
                case "Unknown command!":
                    break;
                default:
                    Log("Unknown response! \"{0} {1}\"", cmd, data);
                    break;
            }
            _recieved = true;
        }

        #region ForceEnable Plugin(s)
        private void ForceEnableYar()
        {
            // Check if plugin is enabled
            var plugin = PluginManager.Plugins.FirstOrDefault(x => x.Plugin.Name.Equals(Name));
            if (plugin == null || (plugin.Enabled)) return;

            Log("Force enable plugin");
            var plugins = PluginManager.GetEnabledPlugins().ToList();
            plugins.Add(Name);
            PluginManager.SetEnabledPlugins(plugins.ToArray());
        }

        private void ForceEnableAllPlugins()
        {
            PluginContainer test;
            DateTime limit;
            foreach (var plugin in PluginManager.Plugins)
            {
                try
                {
                    Log("Force enable: \"{0}\"", plugin.Plugin.Name);
                    plugin.Enabled = true;
                    limit = DateTime.Now;
                    while ((test = PluginManager.Plugins.FirstOrDefault(x => x.Plugin.Name.Equals(plugin.Plugin.Name))) != null && !test.Enabled)
                    {
                        if (DateTime.Now.Subtract(limit).TotalSeconds > 5)
                        {
                            Log("Failed to enable: Timeout ({0} seconds) \"{1}\"", DateTime.Now.Subtract(limit).TotalSeconds, plugin.Plugin.Name);
                            break;
                        }
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Log("Failed to enable: \"{0}\"", plugin.Plugin.Name);
                    LogException(ex);
                }
            }
        }
        #endregion

        #region FixPulse

        private bool _pulseCheck;
        private void FixPulse()
        {
            DateTime timeout;
            Log("############## Pulse Fix ##############");
            // Check if plugin is enabled
            var plugin = PluginManager.Plugins.FirstOrDefault(x => x.Plugin.Name.Equals(Name));
            if (plugin != null && plugin.Enabled)
            {
                Log("PulseFix: Plugin is already enabled -> Disable it for now");
                _pulseFix = true; // Prevent our thread from begin aborted
                plugin.Enabled = false;
                timeout = DateTime.Now;
                while (plugin.Enabled)
                {
                    if (DateTime.Now.Subtract(timeout).TotalSeconds > 10)
                    {
                        Log("PulseFix: Failed to disable plugin");
                        Application.Current.Shutdown(0);
                        return;
                    }
                    Thread.Sleep(100);
                }
            }
            else
                Log("PulseFix: Plugin is not enabled!");

            // Force enable yar plugin
            ForceEnableYar();

            var attempt = 0;
            while (!BotMain.IsRunning)
            {
                attempt++;
                if (attempt >= 4)
                {
                    Log("PulseFix: Fix attempts failed, closing demonbuddy!");
                    Application.Current.Shutdown();
                }
                if (BotMain.BotThread == null)
                {
                    Log("PulseFix: Mainbot thread is not running");
                    Log("PulseFix: Force start bot");
                    BotMain.Start();
                }
                else if (BotMain.BotThread != null)
                {
                    if (BotMain.IsPaused || BotMain.IsPausedForStateExecution)
                        Log("PulseFix: DB is Paused!");
                    Log("PulseFix: Force stop bot");
                    BotMain.BotThread.Abort();
                    Thread.Sleep(1000);
                    Log("PulseFix: Force start bot");
                    BotMain.Start();
                }
                Thread.Sleep(1000);
            }

            // Check if we get a pulse within 10 seconds
            Log("PulseFix: Waiting for first pulse");
            _pulseCheck = false;
            timeout = DateTime.Now;
            while (!_pulseCheck)
            {
                if (DateTime.Now.Subtract(timeout).TotalSeconds > 10)
                {
                    Log("PulseFix: Failed to recieve a pulse within 10 seconds");
                    Application.Current.Shutdown();
                    break;
                }
                Thread.Sleep(100);
            }
            Log("############## End Fix ##############");
        }
        #endregion

        bool _recieved;
        bool Recieved()
        {
            return _recieved;
        }
        #endregion

        void Reset()
        {
            _bs.LastPulse = DateTime.Now.Ticks;
            _bs.LastRun = DateTime.Now.Ticks;
            _bs.LastGame = DateTime.Now.Ticks;
        }

        private void LoadProfile(string profile)
        {
            BotMain.Stop(false, "YetAnotherRelogger -> Load new profile");
            if (ZetaDia.IsInGame)
            {
                ZetaDia.Service.Party.LeaveGame();
                while (ZetaDia.IsInGame)
                    Thread.Sleep(1000);
            }

            Thread.Sleep(2000);
            Log("Loading profile: {0}", profile);
            ProfileManager.Load(profile.Trim());
            Thread.Sleep(5000);
            BotMain.Start();
        }
    }

    #region ElementTester
    public static class _UIElement
    {
        public static ulong leavegame_cancel = 0x3B55BA1E41247F50,
        loginscreen_username = 0xDE8625FCCFFDFC28,
        loginscreen_password = 0xBA2D3316B4BB4104,
        loginscreen_loginbutton = 0x50893593B5DB22A9,
        startresume_button = 0x51A3923949DC80B7,
        errordialog_okbutton = 0xB4433DA3F648A992;
    }
    public static class UIElementTester
    {

        /// <summary>
        /// UIElement validation check
        /// </summary>
        /// <param name="hash">UIElement hash to check</param>
        /// <param name="isEnabled">should be enabled</param>
        /// <param name="isVisible">should be visible</param>
        /// <param name="bisValid">should be a valid UIElement</param>
        /// <returns>true if all requirements are valid</returns>
        public static bool isValid(ulong hash, bool isEnabled = true, bool isVisible = true, bool bisValid = true)
        {
            try
            {
                if (!UIElement.IsValidElement(hash))
                    return false;
                else
                {
                    var element = UIElement.FromHash(hash);

                    if ((isEnabled && !element.IsEnabled) || (!isEnabled && element.IsEnabled))
                        return false;
                    if ((isVisible && !element.IsVisible) || (!isVisible && element.IsVisible))
                        return false;
                    if ((bisValid && !element.IsValid) || (!bisValid && element.IsValid))
                        return false;

                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
    #endregion

    #region XmlTools
    public static class XmlTools
    {
        public static string ToXmlString<T>(this T input)
        {
            using (var writer = new StringWriter())
            {
                input.ToXml(writer);
                return writer.ToString();
            }
        }
        public static void ToXml<T>(this T objectToSerialize, Stream stream)
        {
            new XmlSerializer(typeof(T)).Serialize(stream, objectToSerialize);
        }

        public static void ToXml<T>(this T objectToSerialize, StringWriter writer)
        {
            new XmlSerializer(typeof(T)).Serialize(writer, objectToSerialize);
        }
    }
    #endregion


}
#region Kickstart Custom Profile Behavior tag
namespace YARPLUGIN
{
    using Zeta.XmlEngine;
    using Action = Zeta.TreeSharp.Action;


    [XmlElement("Kickstart")]
    public class Kickstart : ProfileBehavior
    {
        public Kickstart()
        {
            //r_time = DateTime.Now; // used for delay
        }

        private DateTime _time;

        [XmlAttribute("Delay")]
        private string Delay { get; set; }

        [XmlAttribute("Profile")]
        private string Profile { get; set; }

        public override void OnStart()
        {
            _time = DateTime.Now;
            Logging.Write("YAR Kickstarter: Profile set to {0}", Profile);
            Logging.Write("YAR Kickstarter: Delay set to {0}", Delay);
        }

        protected override Composite CreateBehavior()
        {
            Sequence s =
            new Sequence(
                new Action(ret => Logging.Write("[YAR Kickstart] Load profile: {0}", Profile)),
                new Action(ret => ProfileManager.Load(Profile))
            );
            return s;
        }

        private bool _isdone;
        public override bool IsDone
        {
            get { return _isdone; }
        }
        public override void ResetCachedDone()
        {
            _isdone = false;
            //base.ResetCachedDone();
        }
    }

}
#endregion

#region Trinity Support
namespace YARPLUGIN
{
    public static class TrinitySupport
    {
        private static bool _failed;
        private static Type _gilesTrinityType;
        public static bool Initialized { get; private set; }

        public static void Initialize()
        {
            Initialized = true;
            YARPLUGIN.Log("Initialize Trinity Support");
            var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name.ToLower().StartsWith("trinity"));
            if (asm != null)
            {
                try
                {
                    _gilesTrinityType = asm.GetType("GilesTrinity.GilesTrinity");
                    _failed = false;
                    return;
                }
                catch (Exception ex)
                {
                    YARPLUGIN.Log("Failed to initialize Trinity Support:");
                    YARPLUGIN.LogException(ex);
                }
            }
            else
            {
                YARPLUGIN.Log("Trinity is not installed");
            }
            _failed = true;
        }

        public static bool IsEnabled
        {
            get
            {
                var plugin = PluginManager.Plugins.FirstOrDefault(p => p.Plugin.Name.Equals("Trinity"));
                return (plugin != null && plugin.Enabled);
            }
        }

        private static bool bDontMoveMeIAmDoingShit
        {
            get
            {
                try
                {
                    return (bool)_gilesTrinityType.GetField("bDontMoveMeIAmDoingShit", BindingFlags.Static).GetValue(null);
                }
                catch (Exception ex)
                {
                    YARPLUGIN.Log("Failed to get Trinity info:");
                    YARPLUGIN.LogException(ex);
                    return false;
                }
            }
        }
        private static bool bMainBotPaused
        {
            get
            {
                try
                {
                    return (bool)_gilesTrinityType.GetField("bMainBotPaused", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                }
                catch (Exception ex)
                {
                    YARPLUGIN.Log("Failed to get Trinity info:");
                    YARPLUGIN.LogException(ex);
                    return false;
                }
            }
        }
        public static bool IsPaused
        {
            get
            {
                if (!Initialized) Initialize();
                return !_failed && bMainBotPaused;
            }
        }
        public static bool IsBusy
        {
            get
            {
                if (!Initialized) Initialize();
                return !_failed && bDontMoveMeIAmDoingShit;
            }
        }
    }
}
#endregion