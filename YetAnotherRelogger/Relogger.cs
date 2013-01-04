using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;

using YetAnotherRelogger.Helpers;
using YetAnotherRelogger.Helpers.Bot;
using YetAnotherRelogger.Helpers.Tools;
using YetAnotherRelogger.Properties;

namespace YetAnotherRelogger
{
    public sealed class Relogger
    {
        #region singleton
        static readonly Relogger instance = new Relogger();

        static Relogger()
        {
        }

        Relogger()
        {
        }

        public static Relogger Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion

        private bool _isStopped;
        Thread _threadRelogger;
        public void Start()
        {
            _isStopped = false;
            _threadRelogger = new Thread(new ThreadStart(ReloggerWorker)) {IsBackground = true};
            _threadRelogger.Start();
        }

        
        public void Stop()
        {
            _isStopped = true;
            _threadRelogger.Abort();
        }

        public BotClass CurrentBot;
        private bool _autoStartDone;
        private void ReloggerWorker()
        {
            // Check if we are launched by windows RUN
            if (CommandLineArgs.WindowsAutoStart && !_autoStartDone)
            {
                _autoStartDone = true;
                Logger.Instance.WriteGlobal("Windows auto start delaying with {0} seconds", Settings.Default.StartDelay);
                Thread.Sleep((int)Settings.Default.StartDelay*1000);
                foreach (var bot in BotSettings.Instance.Bots.Where(c => c.IsEnabled))
                {
                    bot.AntiIdle.Reset(freshstart: true); // Reset AntiIdle
                    bot.IsStarted = true;
                    bot.Status = "Auto Start...";
                }
            }
            // Check if we are launched with the autostart
            if (CommandLineArgs.AutoStart && !_autoStartDone)
            {
                _autoStartDone = true;
                foreach (var bot in BotSettings.Instance.Bots.Where(c => c.IsEnabled))
                {
                    bot.AntiIdle.Reset(freshstart: true); // Reset AntiIdle
                    bot.IsStarted = true;
                    bot.Status = "Auto Start...";
                }
            }

            Logger.Instance.WriteGlobal("Relogger Thread Starting!");
            while (true)
            {
                try
                {
                    // Paused
                    if (Program.Pause)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    // Check / validate internet connection
                    if (!ConnectionCheck.IsConnected || !ConnectionCheck.ValidConnection)
                    {
                        Debug.WriteLine("Internet validation failed looping until success");
                        Thread.Sleep(1000);
                        continue;
                    }

                    foreach (var bot in BotSettings.Instance.Bots.Where(bot => bot != null))
                    {
                        if (Program.Pause) break;

                        var time = DateTime.Now; // set current time to calculate sleep time at end of loop
                        CurrentBot = bot;
                        //Debug.WriteLine(bot.Name + ":" + ":" + bot.IsRunning);
                        //Debug.WriteLine("State=" + bot.AntiIdle.State);
                        if (bot.IsRunning && bot.IsStarted && !bot.Week.ShouldRun(bot.IsRunning))
                        {
                            // We need to stop
                            Logger.Instance.Write("We are scheduled to stop");
                            bot.Week.NextSchedule(true);
                            bot.IsRunning = false;
                            bot.Diablo.Stop();
                            bot.Demonbuddy.Stop();
                            bot.Status = "Scheduled stop!";
                        }
                        else if (!bot.IsRunning && bot.IsStarted && bot.Week.ShouldRun(bot.IsRunning))
                        {
                            // we need to start
                            Logger.Instance.Write("We are scheduled to start");
                            bot.Week.NextSchedule(false);
                            bot.IsRunning = true;
                            bot.StartTime = DateTime.Now;
                            StartBoth(bot);
                        }
                        else if (!bot.IsStandby && bot.IsRunning)
                        {
                            // Check if process is responding
                            bot.Diablo.CrashCheck();
                            if (bot.AntiIdle.IsInitialized)
                                bot.Demonbuddy.CrashCheck();

                            if (!bot.Diablo.IsRunning)
                            {
                                Logger.Instance.Write("Diablo:{0}: Process is not running", bot.Diablo.Proc.Id);
                                if (bot.Demonbuddy.IsRunning)
                                {
                                    Logger.Instance.Write("Demonbuddy:{0}: Closing db", bot.Demonbuddy.Proc.Id);
                                    bot.Demonbuddy.Stop();
                                }
                                StartBoth(bot);
                            }
                            else if (!bot.Demonbuddy.IsRunning)
                            {
                                Logger.Instance.Write("Demonbuddy:{0}: Process is not running", bot.Demonbuddy.Proc.Id);
                                bot.Demonbuddy.Start();
                            }
                            else if (bot.AntiIdle.State != IdleState.Initialize && General.DateSubtract(bot.AntiIdle.LastStats) > 120)
                            {
                                Logger.Instance.Write("We did not recieve any stats during 120 seconds!");
                                bot.Restart();
                            }
                            else if (bot.AntiIdle.IsInitialized)
                            {
                                if (bot.ProfileSchedule.IsDone)
                                {
                                    Logger.Instance.Write("Profile: \"{0}\" Finished!", bot.ProfileSchedule.Current.Name);
                                    bot.AntiIdle.State = IdleState.NewProfile;
                                }
                            }

                        } // else if (bot.isRunning)
                        else
                            bot.StartTime = DateTime.Now;
                        
                        // calculate sleeptime
                        var sleep = (int) (Program.Sleeptime - DateTime.Now.Subtract(time).TotalMilliseconds);
                        if (sleep > 0) Thread.Sleep(sleep);
                    }
                } // try
                catch (InvalidOperationException)
                { // Catch error when bot is edited while in a loop
                    //Logger.Instance.WriteGlobal(iox.Message);
                    continue;
                }
                catch (Exception ex)
                {
                    if (_isStopped) return;
                    Logger.Instance.WriteGlobal("Relogger Crashed! with message {0}", ex.Message);
                    Logger.Instance.WriteGlobal(ex.StackTrace);
                    Logger.Instance.WriteGlobal("Waiting 10 seconds and try again!");
                    Thread.Sleep(10000);
                    continue;
                }
                Thread.Sleep(1000);
            } // while
        } // private void reloggerWorker()

        private bool StartBoth(BotClass bot)
        {
            bot.Diablo.Start();
            if (!bot.Diablo.IsRunning) return false;

            bot.Demonbuddy.Start();
            if (!bot.Demonbuddy.IsRunning) return false;

            bot.Status = "Monitoring";
            return true;
        }
    }
}
