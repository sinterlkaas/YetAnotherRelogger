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
                            if (!bot.IsStarted || !bot.IsRunning || !bot.Diablo.IsRunning || !bot.Demonbuddy.IsRunning)
                                continue;
                            if (bot.Diablo.Proc.MainWindowHandle != hwnd) 
                                continue;

                            _lastDiablo = bot.Diablo.Proc.MainWindowHandle;
                            _lastDemonbuddy = bot.Demonbuddy.Proc.MainWindowHandle;
                            Logger.Instance.WriteGlobal("<{0}> Diablo:{1}: has focus. Bring attached Demonbuddy to front", bot.Name, bot.Diablo.Proc.Id);
                            // Bring demonbuddy to front
                            WinAPI.ShowWindow(_lastDemonbuddy, WinAPI.WindowShowStyle.ShowNormal);
                            WinAPI.SetForegroundWindow(_lastDemonbuddy);
                            var timeout = DateTime.Now;
                            while (WinAPI.GetForegroundWindow() != _lastDemonbuddy)
                            {
                                // if
                                if (General.DateSubtract(timeout) > 3)
                                {
                                    Logger.Instance.WriteGlobal("<{0}> Failed to bring Demonbuddy to fron", bot.Name);
                                    break;
                                }
                                Thread.Sleep(100); // Dont hog all cpu resources
                            }
                            // Switch back to diablo
                            WinAPI.ShowWindow(_lastDiablo, WinAPI.WindowShowStyle.ShowNormal);
                            WinAPI.SetForegroundWindow(_lastDiablo);
                        }
                    }
                    Thread.Sleep(1000);
                }
            }
            catch
            {
                Thread.Sleep(5000);
                ForegroundCheckerWorker();
            }
        }
    }
}
