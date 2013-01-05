using System;

namespace YetAnotherRelogger.Helpers.Bot
{
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

        public void Reset()
        {
            LastGame = PluginPulse = LastPulse = LastRun = DateTime.Now.Ticks;
            IsPaused = IsRunning = IsInGame = false;
            Coinage = 0;
        }
    }

    public class ChartStats
    {
        public ChartStats()
        {
            GoldPerHour = new Gold();
        }
        public Gold GoldPerHour;
        
        public class Gold
        {
            public DateTime StartTime;
            public DateTime LastUpdate;
            public long StartCoinage;
            public long LastGain;
            public long LastCoinage;
        }
    }
}