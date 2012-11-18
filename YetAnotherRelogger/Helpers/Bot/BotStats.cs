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
}