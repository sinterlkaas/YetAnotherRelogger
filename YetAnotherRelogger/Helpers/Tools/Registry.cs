using Microsoft.Win32;

namespace YetAnotherRelogger.Helpers.Tools
{
    public static class RegistryClass
    {
        public static bool ChangeLocale(string Language)
        {
            try
            {
                var locale = Tools.General.GetLocale(Language);
                var key = Registry.CurrentUser.CreateSubKey(@"Software\Blizzard Entertainment\D3");
                if (key == null)
                {
                    Logger.Instance.Write("Failed to get registry key for changing locale!");
                    return false;
                }
                Logger.Instance.Write("Language set: {0} : {1}", Language, locale);
                key.SetValue("Locale", locale);
                key.Close();
                return true;
            }
            catch
            {
                Logger.Instance.Write("Failed to change locale!");
                return false;
            }
        }

        public static bool ChangeRegion(string Region)
        {
            
            try
            {
                var key = Registry.CurrentUser.CreateSubKey(@"Software\Blizzard Entertainment\Battle.net\D3\");
                if (key == null)
                {
                    Logger.Instance.Write("Failed to get registry key for changing region!");
                    return false;
                }

                var regionUrl = "";

                switch (Region)
                {
                    case "Europe":
                        regionUrl = "eu.actual.battle.net";
                        break;
                    case "America":
                        regionUrl = "us.actual.battle.net";
                        break;
                    case "Asia":
                        regionUrl = "kr.actual.battle.net";
                        break;
                    default:
                        Logger.Instance.Write("Unknown region ({0}) using Europe as our default region", Region);
                        regionUrl = "eu.actual.battle.net";
                        break;
                }
                Logger.Instance.Write("Region set: {0} : {1}", Region, regionUrl);
                key.SetValue("RegionUrl", regionUrl);
                key.Close();
                return true;
            }
            catch
            {
                Logger.Instance.Write("Changing Region Failed!");
                return false;
            }
        }
    }
}
