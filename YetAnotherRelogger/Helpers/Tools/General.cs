using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using YetAnotherRelogger.Properties;

namespace YetAnotherRelogger.Helpers.Tools
{
    public static class General
    {
        /// <summary>
        /// Get PriorityClass as shown in GUI
        /// </summary>
        /// <param name="number">GUI priority number</param>
        /// <returns>ProcessPriorityClass that matches the given number</returns>
        public static ProcessPriorityClass GetPriorityClass(int number)
        {
            switch (number)
            {
                case 0:
                    return ProcessPriorityClass.High;
                case 1:
                    return ProcessPriorityClass.AboveNormal;
                case 2:
                    return ProcessPriorityClass.Normal;
                case 3:
                    return ProcessPriorityClass.BelowNormal;
                case 4:
                    return ProcessPriorityClass.Idle;
                default:
                    return ProcessPriorityClass.Normal;
            }
        }

        public static bool NumericOnly(char c)
        {
            return !(char.IsNumber(c) || c == '\b');
        }

        public static void AgentKiller()
        {
            foreach (var p in Process.GetProcessesByName("Agent"))
            {
                try
                {
                    if (!p.MainModule.FileVersionInfo.ProductName.Equals("Battle.net Update Agent")) continue;

                    Logger.Instance.Write("Killing Agent.exe:{0}", p.Id);
                    p.Kill();
                }
                catch
                {
                    continue;
                }
            }
        }

        public static string GetLocale(string input)
        {
            string output;
            switch (input)
            {
                case "English (US)":
                    output = "enUS";
                    break;
                case "English (Brittain)":
                    output = "enGB";
                    break;
                case "German":
                    output = "deDE";
                    break;
                case "Russian":
                    output = "ruRU";
                    break;
                case "Spannish (Spain)":
                    output = "esES";
                    break;
                case "Spannish (Mexico)":
                    output = "esMX";
                    break;
                case "Korean":
                    output = "koKR";
                    break;
                case "Chinese":
                    output = "zhCN";
                    break;
                case "Traditional Chinese":
                    output = "zhTW";
                    break;
                case "Italian":
                    output = "itIT";
                    break;
                case "Portuguese (Portugal)":
                    output = "ptPT";
                    break;
                case "Portuguese (Brazil)":
                    output = "ptBR";
                    break;
                case "French":
                    output = "frFR";
                    break;
                case "Polish":
                    output = "plPL";
                    break;
                default:
                    output = "enGB";
                    break;
            }

            return output;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="date">DateTime to subtract from current datetime</param>
        /// <param name="seconds"></param>
        /// <returns>Seconds / Miliseconds from current date time</returns>
        public static double DateSubtract(DateTime date, bool seconds = true)
        {
            return (seconds ? DateTime.Now.Subtract(date).TotalSeconds : DateTime.Now.Subtract(date).TotalMilliseconds);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ticks">Ticks to subtract from current datetime</param>
        /// <param name="seconds"></param>
        /// <returns>Seconds / Miliseconds from current date time</returns>
        public static double DateSubtract(long ticks, bool seconds = true)
        {
            var date = new DateTime(ticks);
            return (seconds ? DateTime.Now.Subtract(date).TotalSeconds : DateTime.Now.Subtract(date).TotalMilliseconds);
        }


        public static bool WildcardMatch(String pattern, String input)
        {// http://www.codeproject.com/Tips/57304/Use-wildcard-characters-and-to-compare-strings
            if (String.CompareOrdinal(pattern, input) == 0)
            {
                return true;
            }

            if (String.IsNullOrEmpty(input))
            {
                return String.IsNullOrEmpty(pattern.Trim(new Char[1] { '*' }));
            }
            
            if (pattern.Length == 0)
            {
                return false;
            }
            
            if (pattern[0] == '?')
            {
                return WildcardMatch(pattern.Substring(1), input.Substring(1));
            }
            
            if (pattern[pattern.Length - 1] == '?')
            {
                return WildcardMatch(pattern.Substring(0, pattern.Length - 1), input.Substring(0, input.Length - 1));
            }
            if (pattern[0] == '*')
            {
                return WildcardMatch(pattern.Substring(1), input) || WildcardMatch(pattern, input.Substring(1));
            }
            if (pattern[pattern.Length - 1] == '*')
            {
                return WildcardMatch(pattern.Substring(0, pattern.Length - 1), input) || WildcardMatch(pattern, input.Substring(0, input.Length - 1));
            }
            return pattern[0] == input[0] && WildcardMatch(pattern.Substring(1), input.Substring(1));
        }
    }
}
