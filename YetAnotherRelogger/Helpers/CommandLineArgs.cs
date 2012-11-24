using System;

namespace YetAnotherRelogger.Helpers
{
    public static class CommandLineArgs
    {
        public static bool WindowsAutoStart { get; set; }
        public static bool AutoStart { get; set; }

        public static void Get()
        {
            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "-winstart":
                        WindowsAutoStart = true;
                        break;
                    case "-autostart":
                        AutoStart = true;
                        break;
                    default:
                        // Unknown argument passed
                        // Do nothing
                        break;
                }
            }
        }
    }
}
