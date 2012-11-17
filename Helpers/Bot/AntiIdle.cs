using System;
using System.Diagnostics;
using YetAnotherRelogger.Helpers.Tools;

namespace YetAnotherRelogger.Helpers.Bot
{
    public class AntiIdleClass
    {
        public BotClass Parent;
        public BotStats Stats;
        public DateTime LastStats;
        public DateTime StartDelay;
        public double Delayed;
        public IdleState State;
        public int Failed;
        public DateTime TimeFailedStartDelay;
        public int FailedStartDelay;
        public bool IsInitialized;
        public int FixAttempts;

        private DateTime _lastIdleAction;

        public AntiIdleClass()
        {
            FixAttempts = 0;
            Stats = new BotStats();
        }

        public string Reply()
        {
            switch (State)
            {
                case IdleState.Initialize:
                        // TODO: MAX INIT TIME
                    break;
                case IdleState.StartDelay:
                    if (Stats.IsInGame || Stats.IsLoadingWorld)
                    {
                        State = IdleState.CheckIdle;
                    }
                    else if (General.DateSubtract(StartDelay) > 0)
                    {
                        if (FailedStartDelay > 3 && General.DateSubtract(TimeFailedStartDelay) > 600)
                        {
                            State = IdleState.Terminate;
                            break;
                        }
                        Logger.Instance.Write(Parent, "Demonbuddy:{0}: Delayed start failed! ({1} seconds overtime)", Parent.Demonbuddy.Proc.Id, General.DateSubtract(StartDelay));
                        TimeFailedStartDelay = DateTime.Now;
                        FailedStartDelay++;
                        return "Restart";
                    }
                    break;
                case IdleState.CheckIdle:
                    _lastIdleAction = DateTime.Now; // Update Last Idle action time
                    return IdleAction;
                case IdleState.Busy:
                    if (Stats.IsRunning && !Stats.IsPaused && Stats.IsInGame)
                    {
                        Reset();
                    }
                    else if (General.DateSubtract(_lastIdleAction) > 10)
                    {
                        if (Failed >= 3)
                            State = IdleState.Terminate;

                        Failed++;
                        Reset();
                    }
                    break;
                case IdleState.UserStop:
                    if (Stats.IsRunning)
                        State = IdleState.CheckIdle;
                    break;
                case IdleState.UserPause:
                        if(!Stats.IsPaused)
                            State = IdleState.CheckIdle;
                    break;
                case IdleState.NewProfile:
                    State = IdleState.CheckIdle;
                    return "LoadProfile " + Parent.ProfileSchedule.GetProfile;
                case IdleState.Terminate:
                    Parent.Restart();
                    break;
            }
            return "Roger!";
        }

        public string IdleAction
        {
            get
            {
                if (Program.Pause) return "Roger!";

                Debug.WriteLine("STATS: LastRun:{0} LastGame:{1} LastPulse:{2} Run:{3} Pause:{4} InGame:{5}", General.DateSubtract(Stats.LastRun), General.DateSubtract(Stats.LastGame), General.DateSubtract(Stats.LastPulse), Stats.IsRunning, Stats.IsPaused, Stats.IsInGame);
                if (!Stats.IsRunning && General.DateSubtract(Stats.LastRun) > 30)
                {
                    if (!FixAttemptCounter()) return "Roger!";
                    Logger.Instance.Write(Parent, "Demonbuddy:{0}: is stopped to long for a unknown reason (30 seconds)", Parent.Demonbuddy.Proc.Id);
                    return "Restart";
                }
                if (Stats.IsPaused && General.DateSubtract(Stats.LastRun) > 30)
                {
                    if (!FixAttemptCounter()) return "Roger!";
                    Logger.Instance.Write(Parent, "Demonbuddy:{0}: is paused to long (30 seconds)", Parent.Demonbuddy.Proc.Id);
                    State = IdleState.Terminate;
                    return "Roger!";
                }
                if (!Stats.IsPaused && General.DateSubtract(Stats.LastPulse) > 60)
                {
                    if (!FixAttemptCounter()) return "Roger!";
                    Logger.Instance.Write(Parent, "Demonbuddy:{0}: is not pulsing while it should to (60 seconds)", Parent.Demonbuddy.Proc.Id);
                    return "FixPulse";
                }
                if (!Stats.IsInGame && General.DateSubtract(Stats.LastGame) > 30)
                {
                    if (!FixAttemptCounter()) return "Roger!";
                    Logger.Instance.Write(Parent, "Demonbuddy:{0}: is not in a game to long for unkown reason (30 seconds)", Parent.Demonbuddy.Proc.Id);
                    return "Restart";
                }

                return "Roger!";
            }
        }

        private DateTime _fixAttemptTime;
        public bool FixAttemptCounter()
        {
            if (General.DateSubtract(_fixAttemptTime) > 300)
                FixAttempts = 0;

            FixAttempts++;
            _fixAttemptTime = DateTime.Now;
            if (FixAttempts > 3)
            {
                Parent.Stop();
                return false;
            }
            return true;
        }
        public void Reset(bool all = false, bool freshstart = false)
        {
            State = IdleState.CheckIdle;
            Stats.Reset();
            
            if (all)
            {
                IsInitialized = false;
                State = IdleState.Initialize;
                Failed = 0;
                FailedStartDelay = 0;
            }
            if (freshstart)
            {
                FixAttempts = 0;
            }
        }
    }
}
