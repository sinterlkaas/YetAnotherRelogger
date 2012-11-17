using System;
using System.Diagnostics;
using System.IO;
using YetAnotherRelogger.Properties;


namespace YetAnotherRelogger.Helpers
{
    public static class PluginVersionCheck
    {
        public static bool Check(string path)
        {
            try
            {
                Logger.Instance.Write("Checking plugin: {0}", path);
                if (File.Exists(path))
                {
                    var check = Resources.Plugin.Split('\n')[0].TrimEnd();
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var reader = new StreamReader(fs);
                        for (var i = 0; i < 3; i++)
                        {
                            var line = reader.ReadLine();
                            if (line != null && line.Equals(check))
                            {
                                Logger.Instance.Write("Plugin is installed and latest version");
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    Logger.Instance.Write("Plugin does not exist");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Write("Something went wrong: {0}", ex.ToString());
            }
            Logger.Instance.Write("Plugin is outdated!");
            return false;
        }

        public static void Install(string path)
        {
            try
            {
                Logger.Instance.Write("Installing latest plugin: {0}", path);
                if (File.Exists(path))
                    File.Delete(path);
                else if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                File.WriteAllText(path, Resources.Plugin);
            }
            catch (Exception ex)
            {
                Logger.Instance.Write("Something went wrong: {0}", ex.ToString());
            }
        }
    }
}
