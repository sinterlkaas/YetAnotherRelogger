using System;
using System.Threading;
using YetAnotherRelogger.Helpers;

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
            while (true)
            {
                foreach (var bot in BotSettings.Instance.Bots)
                {
                    // Update bot uptime
                    bot.RunningTime = bot.IsRunning ? DateTime.Now.Subtract(bot.StartTime).ToString(@"hh\hmm\mss\s") : "";
                }
                Thread.Sleep(1000);
            }
        }
    }
}
