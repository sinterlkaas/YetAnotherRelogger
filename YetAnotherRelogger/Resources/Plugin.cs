// VERSION: 0.1.9.1
/* Changelog:
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
using Zeta.Internals;
using Zeta.TreeSharp;
using UIElement = Zeta.Internals.UIElement;

namespace YARPLUGIN
{
    public class YARPLUGIN : IPlugin
    {
        // Plugin version
        public Version Version { get { return new Version(0, 1, 9, 1); } }

        private const bool _debug = true;

        // Compatibility
        private static readonly Regex[] ReCompatibility =
            {
                /* BuddyStats Remote control action */
                new Regex(@"Stop command from BuddyStats", RegexOptions.Compiled), // stop command
                /* Emergency Stop: You need to stash an item but no valid space could be found. Stash is full? Stopping the bot to prevent infinite town-run loop. */
                new Regex(@".+Emergency Stop: .+", RegexOptions.Compiled), // Emergency stop
                /* Atom 2.0.15+ "Take a break" */
                new Regex(@".*Atom.*Will Stop the bot for .+ minutes\.$", RegexOptions.Compiled), // Take a break
                /* RadsAtom "Take a break" */
                new Regex(@"\[RadsAtom\].+ minutes to next break, the break will last for .+ minutes."), 
            };

        // CrashTender
        private static readonly Regex[] ReCrashTender =
            {
                /* Invalid Session */
                new Regex(@"Session is invalid!", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                /* Session expired */
                new Regex(@"Session is expired", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                /* Failed to attach to D3*/
                new Regex(@"Was not able to attach to any running Diablo III process, are you running the bot already\?",RegexOptions.Compiled ), 
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

            Logging.OnLogMessage += new Logging.LogMessageDelegate(Logging_OnLogMessage);
            _bs.Pid = Process.GetCurrentProcess().Id;

            Reset();

            _yarThread = new Thread(YarWorker) {IsBackground = true};
            _yarThread.Start();
            Send("Initialized");
        }

        public void OnShutdown()
        {
            _yarThread.Abort();
            Logging.OnLogMessage -= new Logging.LogMessageDelegate(Logging_OnLogMessage);
        }

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
        }

        public void OnPulse()
        {
            _pulseCheck = true;
            _bs.LastPulse = DateTime.Now.Ticks;
        }
        #endregion

        #region Logging Monitor
        void Logging_OnLogMessage(ReadOnlyCollection<Logging.LogMessage> messages)
        {
            foreach (var lm in messages)
            {
                var msg = lm.Message;
                if (ReCrashTender.Any(re => re.IsMatch(msg)))
                {
                    Send("CrashTender " + ProfileManager.CurrentProfile.Path); // tell relogger to "crash tender" :)
                    break;
                }
                if (!_allPluginsCompiled && FindPluginsCompiled(msg)) continue;
                if (FindStartDelay(msg)) continue;

                if (msg.Equals("Start/Stop Button Clicked!"))
                {
                    Send("UserStop");
                    continue;
                }

                if (ReCompatibility.Any(re => re.IsMatch(msg)))
                {
                    Send("ThirdpartyStop");
                }
                Thread.Sleep(1);
            }
        }
        public bool FindStartDelay(string msg)
        {
            // Waiting #.# seconds before next game...
            var m = new Regex(@"Waiting (.+) seconds before next game...", RegexOptions.Compiled).Match(msg);
            if (m.Success)
            {
                Send("StartDelay " + DateTime.Now.AddSeconds(double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).Ticks);
                return true;
            }
            return false;
        }

        public bool FindPluginsCompiled(string msg)
        {
            var m = new Regex(@"There are \d+ plugins.", RegexOptions.Compiled).Match(msg);
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
                // Handle errors and other strange situations
                ErrorHandling(); 

                _bs.PluginPulse = DateTime.Now.Ticks;
                _bs.IsRunning = BotMain.IsRunning;
                _bs.IsLoadingWorld = ZetaDia.IsLoadingWorld;
                _bs.Coinage = 0;
                try
                {
                    if (ZetaDia.Me != null)
                        _bs.Coinage = ZetaDia.Me.Inventory.Coinage;
                }
                catch (System.Exception ex)
                {
                    _bs.Coinage = -1;
                }

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

                // Send stats
                Send("XML:" + _bs.ToXmlString(), xml:true);
                Thread.Sleep(3000);
            }
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
                    Send("CheckConnection", pause:true);
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
        private void Send(string data, bool pause = false, bool xml = false, int retry = 1, int timeout = 3000)
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
                BotMain.PauseWhile(waitFor, 0, TimeSpan.FromMilliseconds((retry*timeout) + 3000));
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
                            var sw = new StreamWriter(client) {AutoFlush = true};
                            var sr = new StreamReader(client);

                            sw.WriteLine(data);
                            var connectionTime = DateTime.Now;
                            while (client.IsConnected)
                            {
                                if (DateTime.Now.Subtract(connectionTime).TotalSeconds > 10)
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
                                client.Close();
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
                }
                Thread.Sleep(100);
            }
            _recieved = true;
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
                    CharacterSettings.Instance.MonsterPowerLevel = Convert.ToInt32(data.Trim());
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
                ZetaDia.Service.Games.LeaveGame();
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
            _time = DateTime.Now; // used for delay
        }

        private DateTime _time;

        [XmlAttribute("Delay")]
        private string Delay { get; set; }

        [XmlAttribute("Profile")]
        private string Profile { get; set; }

        protected override Composite CreateBehavior()
        {
            return new Action((x) =>
                                  {
                                      if (DateTime.Now.Subtract(_time).TotalSeconds > Convert.ToInt32(Delay))
                                      {
                                          Logging.Write("[YAR Kickstart] Load profile: {0}", Profile);
                                          ProfileManager.Load(Profile);
                                          _isdone = true;
                                      }
                                  });
        }

        private bool _isdone;
        public override bool IsDone
        {
            get { return _isdone; }
        }
        public override void ResetCachedDone()
        {
            _isdone = false;
            base.ResetCachedDone();
        }
    }
 
}
#endregion