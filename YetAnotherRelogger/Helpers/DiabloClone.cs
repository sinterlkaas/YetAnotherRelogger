using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using YetAnotherRelogger.Helpers.Bot;
using YetAnotherRelogger.Helpers.Tools;

namespace YetAnotherRelogger.Helpers
{
    public static class DiabloClone
    {
        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        // Dont link this list
        private static HashSet<NoLink> _noLinks = new HashSet<NoLink>
                             {
                                 new NoLink {Source=@"Data_D3\PC\MPQs\Cache\*",Directory = true},
                                 new NoLink {Source=@"InspectorReporter\ReportedBugs\*",Directory = true},
                                 new NoLink {Source=@".agent.db",Directory = false},
                                 new NoLink {Source=@"App-*.dmp",Directory = false},
                                 new NoLink {Source=@"*.lock",Directory = false},
                             };

        public static void Create(BotClass bot)
        {
            var imp = new Impersonator();
            try
            {
                
                if (bot.UseWindowsUser)
                    imp.Impersonate(bot.WindowsUserName, "localhost", bot.WindowsUserPassword);

                bot.Status = "Create Diablo Clone";
                var basepath = Path.GetDirectoryName(bot.Diablo.Location);
                var clonepath = Path.Combine(bot.DiabloCloneLocation, "Diablo III");

                // if diablo base path does not exist stop here!
                if (basepath != null && !Directory.Exists(basepath))
                {
                    bot.Stop();
                    throw new Exception("Diablo base directory does not exist!");
                }

                // Check if given language is installed on basepath
                var testpath = Path.Combine(basepath, @"Data_D3\PC\MPQs", General.GetLocale(bot.Diablo.Language));
                if (!Directory.Exists(testpath))
                {
                    bot.Stop();
                    throw new Exception(string.Format("ERROR: {0} language is not installed (path: {1})",
                                                      bot.Diablo.Language, testpath));
                }


                // if diablo clone does not exist create it
                if (!Directory.Exists(Path.Combine(clonepath, @"Data_D3\PC\MPQs\Cache")))
                {
                    Logger.Instance.Write(bot, "Creating new Diablo Clone");
                    Directory.CreateDirectory(Path.Combine(clonepath, @"Data_D3\PC\MPQs\Cache"));
                }

                // Create Search caches
                var baseFileCache = new FileListCache(basepath);
                var cloneFileCache = new FileListCache(clonepath);

                // Check if all links are made for our clone
                foreach (var p in baseFileCache.FileList)
                {
                    try
                    {
                        if (p.directory && !Directory.Exists(Path.Combine(clonepath.ToLower(), p.Path.ToLower())))
                        {
                            if (!_noLinks.Any(n => General.WildcardMatch(n.Source, p.Path)))
                            {
                                Logger.Instance.Write(bot, "NewLink: {0} -> {1}", Path.Combine(clonepath, p.Path),
                                                      Path.Combine(basepath, p.Path));
                                //if (!CreateSymbolicLink( Path.Combine(clonepath,p.Path),  Path.Combine(basepath,p.Path), 1))
                                //  throw new Exception("Failed to create link!");
                                Directory.CreateDirectory(Path.Combine(clonepath, p.Path));
                            }
                            continue;
                        }
                        if (!p.directory && !File.Exists(Path.Combine(clonepath.ToLower(), p.Path.ToLower())))
                        {
                            if (!_noLinks.Any(n => General.WildcardMatch(n.Source, p.Path)))
                            {
                                Logger.Instance.Write(bot, "NewLink: {0} -> {1}", Path.Combine(clonepath, p.Path),
                                                      Path.Combine(basepath, p.Path));
                                if (Path.GetExtension(Path.Combine(clonepath, p.Path)).ToLower().Equals(".exe"))
                                {
                                    if (
                                        !CreateHardLink(Path.Combine(clonepath, p.Path), Path.Combine(basepath, p.Path),
                                                        IntPtr.Zero))
                                        throw new Exception("Failed to create link!");
                                }
                                else
                                {
                                    if (
                                        !CreateSymbolicLink(Path.Combine(clonepath, p.Path),
                                                            Path.Combine(basepath, p.Path), 0))
                                        throw new Exception("Failed to create link!");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                // Remove links that have no target
                /*
                foreach (var p in cloneFileCache.FileList)
                {
                    try
                    {
                        if (p.directory && !Directory.Exists(Path.Combine(basepath, p.Path)))
                        {
                            if (!_noLinks.Any(n => General.WildcardMatch(n.Source.ToLower(), p.Path.ToLower())))
                                Console.WriteLine("Delete: {0}", p.Path);
                            continue;
                        }

                        if (!p.directory && !File.Exists(Path.Combine(basepath.ToLower(), p.Path.ToLower())))
                        {
                            if (!_noLinks.Any(n => General.WildcardMatch(n.Source, p.Path)))
                                Console.WriteLine("Delete: {0}", p.Path);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Write(bot, ex.ToString());
                    }
                }
                 */
                
            }
            catch (Exception ex)
            {
                bot.Stop();
                DebugHelper.Write(bot, "Failed to create clone!");
                DebugHelper.Exception(ex);
            }
            imp.Dispose();
        }
    }

    struct NoLink
    {
        public string Source;
        public bool Directory;
    }
}
