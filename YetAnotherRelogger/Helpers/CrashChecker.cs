using System;
using System.Diagnostics;
using YetAnotherRelogger.Helpers.Tools;

namespace YetAnotherRelogger.Helpers
{
    public static class CrashChecker
    {
        public static bool IsResponding(Process proc)
        {
            if (proc == null) return false;
            return (testResponse(proc.MainWindowHandle));
        }
        public static bool IsResponding(IntPtr handle)
        {
            return (testResponse(handle));
        }
        private static bool testResponse(IntPtr handle)
        {
            UIntPtr dummy;
            var result = IntPtr.Zero;

            result = WinAPI.SendMessageTimeout(handle, 0, UIntPtr.Zero, IntPtr.Zero, WinAPI.SendMessageTimeoutFlags.SMTO_ABORTIFHUNG, 1000, out dummy);

            return (result != IntPtr.Zero);
        }
    }
}
