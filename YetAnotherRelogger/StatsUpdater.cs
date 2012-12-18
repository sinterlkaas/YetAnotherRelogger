using System;
using System.Diagnostics;
using System.Threading;
using YetAnotherRelogger.Helpers;
using YetAnotherRelogger.Helpers.Stats;

namespace YetAnotherRelogger
{
    public sealed class StatsUpdater
    {
        #region singleton
        static readonly StatsUpdater instance = new StatsUpdater();

        static StatsUpdater()
        {
        }

        StatsUpdater()
        {
        }

        public static StatsUpdater Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion
        private Thread _statsUpdater;
        public void Start()
        {
            _statsUpdater = new Thread(new ThreadStart(StatsUpdaterWorker)) { IsBackground = true };
            _statsUpdater.Start();
        }

        public void Stop()
        {
            _statsUpdater.Abort();
        }

        public void StatsUpdaterWorker()
        {
            var usages = new CpuRamUsage();
            var cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var TotalRam = new PerformanceCounter("Memory", "Available MBytes");
            while (true)
            {
                // Update Cpu/Ram Usage
                usages.Update();

                double diabloCpuUsage = 0;
                long diabloRamUsage = 0;
                int diabloCount = 0;
                double demonbuddyCpuUsage = 0;
                long demonbuddyRamUsage = 0;
                int demonbuddyCount = 0;
                
                foreach (var bot in BotSettings.Instance.Bots)
                {
                    // Update bot uptime
                    bot.RunningTime = bot.IsRunning ? DateTime.Now.Subtract(bot.StartTime).ToString(@"hh\hmm\mss\s") : "";


                    // Calculate total Cpu/Ram usage for Diablo
                    if (bot.Diablo.IsRunning)
                    {
                        try
                        {
                            var usage = usages.GetUsageById(bot.Diablo.Proc.Id);
                            diabloCpuUsage += usage.Cpu;
                            diabloRamUsage += usage.Memory;
                            diabloCount++;
                        }
                        catch
                        {}
                    }
                    // Calculate total Cpu/Ram usage for Demonbuddy
                    if (bot.Demonbuddy.IsRunning)
                    {
                        try
                        {
                            var usage = usages.GetUsageById(bot.Demonbuddy.Proc.Id);
                            demonbuddyCpuUsage += usage.Cpu;
                            demonbuddyRamUsage += usage.Memory;
                            demonbuddyCount++;
                        }
                        catch
                        {}
                    }

                    
                }
                // Update Stats label on mainform
                updateMainformStatsLabel(
                    string.Format("Cpu Usage : Diablo[{0}] {1}%, Demonbuddy[{2}] {3}%, Total {4}%" + Environment.NewLine + 
                                  "Ram Usage : Diablo[{0}] {5}Gb, Demonbuddy[{2}] {6}Mb, Total {7}Gb",
                                  diabloCount, Math.Round(diabloCpuUsage,1), demonbuddyCount, Math.Round(demonbuddyCpuUsage,1), Math.Round(diabloCpuUsage+demonbuddyCpuUsage,1),
                                  Math.Round(diabloRamUsage / Math.Pow(2 , 30), 2), Math.Round(demonbuddyRamUsage / Math.Pow(2 , 20),2), Math.Round((diabloRamUsage / Math.Pow(2,30)) + (demonbuddyRamUsage / Math.Pow(2,30)), 2)));
                Thread.Sleep(500);
            }
        }

        private void updateMainformStatsLabel(string text)
        {
            if (Program.Mainform != null && Program.Mainform.labelStats != null)
            {
                try
                {
                    Program.Mainform.Invoke(new Action(() => Program.Mainform.labelStats.Text = text));
                }
                catch
                {
                    // Failed! do nothing
                }
            }
        }
    }
}
