using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Xml.Serialization;
using YetAnotherRelogger.Helpers.Bot;
using YetAnotherRelogger.Helpers.Tools;
using YetAnotherRelogger.Properties;

namespace YetAnotherRelogger.Helpers
{
    public class Communicator
    {
        Thread _threadWorker;
        public void Start()
        {
            _threadWorker = new Thread(Worker) {IsBackground = true};
            _threadWorker.Start();
        }

        public void Worker()
        {
            while (true)
            {
                try
                {
                    var serverStream = new NamedPipeServerStream("YetAnotherRelogger", PipeDirection.InOut, 100, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

                    serverStream.WaitForConnection();

                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        using (var pipeClientConnection = (NamedPipeServerStream)state)
                        {
                            var handleClient = new HandleClient(pipeClientConnection);
                            handleClient.Start();
                        }
                    }, serverStream);
                }
                catch (Exception ex)
                {
                    Logger.Instance.WriteGlobal(ex.Message);
                    Thread.Sleep(1000);
                }
                Thread.Sleep(100);
            }
        }

        class HandleClient : IDisposable
        {
            private  StreamReader _reader;
            private  StreamWriter _writer;
            private  NamedPipeServerStream _stream;

            public HandleClient(NamedPipeServerStream stream)
            {
                _stream = stream;
                _reader = new StreamReader(stream);
                _writer = new StreamWriter(stream) { AutoFlush = true };
            }

            public void Start()
            {
                var isXml = false;
                var xml = string.Empty;
                try
                {
                    while (_stream.IsConnected)
                    {
                        var temp = _reader.ReadLine();
                        if (temp == null)
                        {
                            Thread.Sleep(5);
                            continue;
                        }
                        if (temp.Equals("END"))
                        {
                            HandleXml(xml);
                        }

                        if (temp.StartsWith("XML:"))
                        {
                            temp = temp.Substring(4);
                            isXml = true;
                        }

                        if (isXml)
                        {
                            xml += temp + "\n";
                        }
                        else
                            HandleMsg(temp);
                        Thread.Sleep(5);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                if (_stream != null)
                    _stream.Dispose();
            }

            private void HandleXml(string data)
            {
                BotStats stats;
                //Debug.WriteLine(data);
                var xml = new XmlSerializer(typeof (BotStats));
                using (var stringReader = new StringReader(data))
                {
                    stats = xml.Deserialize(stringReader) as BotStats;
                }
                
                if (stats != null)
                {
                    try
                    {
                        var bot = BotSettings.Instance.Bots.FirstOrDefault(b => (b != null && b.Demonbuddy != null && b.Demonbuddy.Proc != null) && b.Demonbuddy.Proc.Id == stats.Pid);
                        if (bot != null)
                        {
                            if (bot.AntiIdle.Stats == null) bot.AntiIdle.Stats = new BotStats();

                            bot.AntiIdle.UpdateCoinage(stats.Coinage);
                            bot.AntiIdle.Stats = stats;
                            bot.AntiIdle.LastStats = DateTime.Now;
                            Send(bot.AntiIdle.Reply());
                            return;
                        }

                        Logger.Instance.WriteGlobal("Could not find a matching bot for Demonbuddy:{0}", stats.Pid);
                        return;
                    }
                    catch (Exception ex)
                    {
                        Send("Internal server error: " + ex.Message);
                        Logger.Instance.WriteGlobal(ex.ToString());
                        return;
                    }
                }
                Send("Roger!");
            }

            private void HandleMsg(string msg)
            {
                // Message Example:
                // PID:CMD DATA
                // 1234:GameLeft 25-09-1985 18:27:00
                Debug.WriteLine("Recieved: " + msg);

                try
                {
                    var pid = msg.Split(':')[0];
                    var cmd = msg.Substring(pid.Length + 1).Split(' ')[0];
                    int x;
                    msg = msg.Substring(((x = pid.Length + cmd.Length + 2) >= msg.Length ? 0 : x));

                    var b = BotSettings.Instance.Bots.FirstOrDefault(f => (f.Demonbuddy != null && f.Demonbuddy.Proc != null) && f.Demonbuddy.Proc.Id == Convert.ToInt32(pid));
                    if (b == null)
                    {
                        Send("Error: Unknown process");
                        return;
                    }

                    switch (cmd)
                    {
                        case"Initialized":
                            b.AntiIdle.Stats = new BotStats
                                          {
                                              LastGame = DateTime.Now.Ticks,
                                              LastPulse = DateTime.Now.Ticks,
                                              PluginPulse = DateTime.Now.Ticks,
                                              LastRun = DateTime.Now.Ticks
                                          };
                            b.AntiIdle.LastStats = DateTime.Now;
                            b.AntiIdle.State = IdleState.CheckIdle;
                            b.AntiIdle.IsInitialized = true;
                            Send("Roger!");
                            break;
                        case "GameLeft":
                            b.ProfileSchedule.Count++;
                            if (b.ProfileSchedule.Current.Runs > 0)
                                Logger.Instance.Write(b, "Runs completed ({0}/{1})", b.ProfileSchedule.Count, b.ProfileSchedule.MaxRuns);
                            else
                                Logger.Instance.Write(b, "Runs completed {0}", b.ProfileSchedule.Count);

                            if (b.ProfileSchedule.IsDone)
                            {
                                var newprofile = b.ProfileSchedule.GetProfile;
                                Logger.Instance.Write("Next profile: {0}", newprofile);
                                Send("LoadProfile " + newprofile);
                            }
                            else
                                Send("Roger!");
                            break;
                        
                        case "UserStop":
                            b.Status = string.Format("User Stop: {0:d-m H:M:s}", DateTime.Now);
                            b.AntiIdle.State = IdleState.UserStop;
                            Logger.Instance.Write(b, "Demonbuddy stopped by user");
                            Send("Roger!");
                            break;
                        case "StartDelay":
                            var delay = new DateTime(long.Parse(msg));
                            b.AntiIdle.StartDelay = delay.AddSeconds(10);
                            b.AntiIdle.State = IdleState.StartDelay;
                            Send("Roger!");
                            break;
                        // Giles Compatibility
                        case "ThirdpartyStop":
                            b.Status = string.Format("Thirdparty Stop: {0:d-m H:M:s}", DateTime.Now);
                            b.AntiIdle.State = IdleState.UserStop;
                            Logger.Instance.Write(b, "Demonbuddy stopped by Thirdparty");
                            Send("Roger!");
                            break;
                        case "GilesPause":
                            b.AntiIdle.State = IdleState.UserPause;
                            Send("Roger!");
                            break;
                        case "AllCompiled":
                            Send(b.Demonbuddy.ForceEnableAllPlugins ? "ForceEnableAll" : "ForceEnableYar");
                            break;
                        case "CrashTender":
                            if (Settings.Default.UseKickstart && File.Exists(msg))
                                b.Demonbuddy.CrashTender(msg);
                            else
                                b.Demonbuddy.CrashTender();
                            Send("Roger!");
                            break;
                        case "CheckConnection":
                            ConnectionCheck.CheckValidConnection(silent: true);
                            Send("Roger!");
                            break;
                        // Unknown command reply
                        default:
                            Send("Unknown command!");
                            Logger.Instance.WriteGlobal("Unknown command recieved: " + msg);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Send("Internal server error: " + ex.Message);
                    Logger.Instance.WriteGlobal(ex.ToString());
                }
            }

            private void Send(string msg)
            {
                try
                {
                    _writer.WriteLine(msg);
                }
                catch (Exception ex)
                {
                    Logger.Instance.WriteGlobal(ex.ToString());
                }
            }

            public void Dispose()
            {
                //Free managed resources
                if (_reader != null)
                {
                    _reader.Dispose();
                    _reader = null;
                }
                if (_writer != null)
                {
                    _writer.Dispose();
                    _writer = null;
                }
                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }
            }
        }
    }
}
