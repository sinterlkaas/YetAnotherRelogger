using System;
using System.Threading;

using YetAnotherRelogger.Helpers;
using YetAnotherRelogger.Helpers.Tools;

namespace YetAnotherRelogger
{
    public sealed class ForegroundChecker
    {
        #region singleton
        static readonly ForegroundChecker instance = new ForegroundChecker();

        static ForegroundChecker()
        {
        }

        ForegroundChecker()
        {
        }

        public static ForegroundChecker Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion

        private Thread _fcThread;

        public void Start()
        {
            if (_fcThread != null)
                _fcThread.Abort();

            _fcThread = new Thread(new ThreadStart(ForegroundCheckerWorker)) {IsBackground = true};
            _fcThread.Start();
        }
        public void Stop()
        {
            _fcThread.Abort();
        }

        private IntPtr _lastDiablo;
        private IntPtr _lastDemonbuddy;
        private void ForegroundCheckerWorker()
        {
            try
            {
                while (true)
                {
                    var bots = BotSettings.Instance.Bots;
                    var hwnd = WinAPI.GetForegroundWindow();

                    if (_lastDemonbuddy != hwnd && _lastDiablo != hwnd)
                    {
                        _lastDemonbuddy = _lastDiablo = IntPtr.Zero;
                        foreach (var bot in bots)
                        {
                            var time = DateTime.Now;
                            if (!bot.IsStarted || !bot.IsRunning || !bot.Diablo.IsRunning || !bot.Demonbuddy.IsRunning)
                                continue;
                            if (bot.Diablo.Proc.MainWindowHandle != hwnd) 
                                continue;

                            _lastDiablo = bot.Diablo.MainWindowHandle;
                            _lastDemonbuddy = bot.Demonbuddy.MainWindowHandle;
                            Logger.Instance.WriteGlobal("<{0}> Diablo:{1}: has focus. Bring attached Demonbuddy to front", bot.Name, bot.Diablo.Proc.Id);

                            // Bring demonbuddy to front
                            WinAPI.ShowWindow(_lastDemonbuddy, WinAPI.WindowShowStyle.ShowNormal);
                            WinAPI.SetForegroundWindow(_lastDemonbuddy);
                            var timeout = DateTime.Now;
                            while (WinAPI.GetForegroundWindow() != _lastDemonbuddy)
                            {
                                if (General.DateSubtract(timeout, false) > 500)
                                {
                                    WinAPI.ShowWindow(_lastDemonbuddy, WinAPI.WindowShowStyle.ForceMinimized);
                                    Thread.Sleep(300);
                                    WinAPI.ShowWindow(_lastDemonbuddy, WinAPI.WindowShowStyle.ShowNormal);
                                    Thread.Sleep(300);
                                    WinAPI.SetForegroundWindow(_lastDemonbuddy);
                                    if (WinAPI.GetForegroundWindow() != _lastDemonbuddy)
                                        Logger.Instance.WriteGlobal("<{0}> Failed to bring Demonbuddy to front", bot.Name);
                                    break;
                                }
                                Thread.Sleep(100);
                            }

                            // Switch back to diablo
                            WinAPI.ShowWindow(_lastDiablo, WinAPI.WindowShowStyle.ShowNormal);
                            WinAPI.SetForegroundWindow(_lastDiablo);
                            while (WinAPI.GetForegroundWindow() != _lastDiablo)
                            {
                                if (General.DateSubtract(timeout, false) > 500)
                                {
                                    WinAPI.ShowWindow(_lastDiablo, WinAPI.WindowShowStyle.ForceMinimized);
                                    Thread.Sleep(300);
                                    WinAPI.ShowWindow(_lastDiablo, WinAPI.WindowShowStyle.ShowNormal);
                                    Thread.Sleep(300);
                                    WinAPI.SetForegroundWindow(_lastDiablo);
                                    break;
                                }
                                Thread.Sleep(100);
                            }

                            // calculate sleeptime
                            var sleep = (int)(Program.Sleeptime - DateTime.Now.Subtract(time).TotalMilliseconds);
                            if (sleep > 0) Thread.Sleep(sleep);
                        }
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                DebugHelper.Exception(ex);
                Thread.Sleep(5000);
                ForegroundCheckerWorker();
            }
        }
    }
}
