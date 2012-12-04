using System;
using System.Diagnostics;
using YetAnotherRelogger.Helpers.Tools;
using YetAnotherRelogger.Properties;

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
        public int FailedInitCount;
        public DateTime InitTime;

        public int LastCoinage;
        public DateTime LastCoinageIncrease;
        public DateTime LastCoinageBugReported;
        public DateTime LastCoinageReset; // So we give it a minute to get in shape

        private DateTime _lastIdleAction;

        public AntiIdleClass()
        {
            FixAttempts = 0;
            Stats = new BotStats();
            ResetCoinage();
        }

        // When program is paused, we don't want this to run on and on
        public void ResetCoinage()
        {
            LastCoinageIncrease = DateTime.Now;
            LastCoinageBugReported = DateTime.Now;
            LastCoinage = 0;
            LastCoinageReset = DateTime.MinValue;
        }

        public void UpdateCoinage(int NewCoinage)
        {
            if(NewCoinage < 0)
            {
                Debug.WriteLine("We could not read Coinage assuming problem");
                return;
            }
            if (NewCoinage == 0)
            {
                Debug.WriteLine("We got 0 gold, which is often a glitch, assuming problem");
                return;
            }

            Debug.WriteLine("We received Coinage info! Old: {0}; New {1}, we are {2}",
                LastCoinage, NewCoinage, (LastCoinage != NewCoinage)?"good":"lazy");

            if (NewCoinage < LastCoinage)
            {
                // We either repaired, or went shopping, all is well
                LastCoinageIncrease = DateTime.Now;
            }
            else if (NewCoinage > LastCoinage)
            {
                // We got more monies, all is well
                LastCoinageIncrease = DateTime.Now;
                LastCoinageBugReported = DateTime.Now;
            }
            // Otherwise we are stuck on the same gold, and that's not profitable.
            // Yes, the if above could be: NewCoinage != LastCoinage, but I wanted
            // to explain why we have those two
            LastCoinage = NewCoinage;
        }

        public string Reply()
        {
            switch (State)
            {
                case IdleState.Initialize:
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
                    ResetCoinage();
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

                // Prints a warning about gold error
                if (Settings.Default.GoldInfoLogging && General.DateSubtract(LastCoinageIncrease) > 30)
                {
                    if (General.DateSubtract(LastCoinageBugReported) > 30)
                    {
                        if (Properties.Settings.Default.UseGoldTimer)
                            Logger.Instance.Write(Parent, "Demonbuddy:{0}: has not gained any gold in {1} seconds, limit {2}",
                                Parent.Demonbuddy.Proc.Id, (int)General.DateSubtract(LastCoinageIncrease),
                                (int)Properties.Settings.Default.GoldTimer);
                        else
                            Logger.Instance.Write(Parent, "Demonbuddy:{0}: has not gained any gold in {1} seconds, limit NONE",
                                Parent.Demonbuddy.Proc.Id, (int)General.DateSubtract(LastCoinageIncrease));
                        LastCoinageBugReported = DateTime.Now;
                    }
                }

                // If we are w/o gold change for 2 minutes, send reset, but at max every 45s
                if (Properties.Settings.Default.UseGoldTimer &&
                    General.DateSubtract(LastCoinageIncrease) > (double)Properties.Settings.Default.GoldTimer)
                {
                    if(General.DateSubtract(LastCoinageReset) < 45) // we still give it a chance
                        return "Roger!";
					// When we give up, it sends false, we send Roger and kill DB
                    if (!FixAttemptCounter()) return "Roger!";
					Logger.Instance.Write(Parent, "Demonbuddy:{0}: has not gained any gold in {1} seconds, trying reset", Parent.Demonbuddy.Proc.Id,
                        (int)General.DateSubtract(LastCoinageIncrease));
                    LastCoinageReset = DateTime.Now;
                    return "Restart";
                }

                return "Roger!";
            }
        }

        private DateTime _fixAttemptTime;
        public bool FixAttemptCounter()
        {
            if (General.DateSubtract(_fixAttemptTime) > 420)
                FixAttempts = 0;

            FixAttempts++;
            _fixAttemptTime = DateTime.Now;
            if (FixAttempts > 3)
            {
                //Parent.Stop();
                Parent.Restart();
                return false;
            }
            return true;
        }
        public void Reset(bool all = false, bool freshstart = false)
        {
            State = IdleState.CheckIdle;
            Stats.Reset();

            ResetCoinage();
            
            if (all)
            {
                IsInitialized = false;
                InitTime = DateTime.Now;
                State = IdleState.Initialize;
                Failed = 0;
                FailedStartDelay = 0;
            }
            if (freshstart)
            {
                FailedInitCount = 0;
                FixAttempts = 0;
            }
        }
    }
}
