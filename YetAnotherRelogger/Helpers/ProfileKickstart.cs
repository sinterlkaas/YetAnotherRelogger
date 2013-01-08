using System;
using System.IO;
using System.Text.RegularExpressions;
using YetAnotherRelogger.Helpers.Bot;
using YetAnotherRelogger.Properties;

namespace YetAnotherRelogger.Helpers
{
    public static class ProfileKickstart
    {
        // GameParams Regex Pattern
        private const string GameParamsRegex = @"(<GameParams .+/>)";
        // Kickstart profile layout
        private const string YarKickstart = @"<!-- This is a automaticly generated profile by YetAnotherRelogger
It is used to ensure a profile starts without causing DB to choken it -->
<Profile>
    <Name>YAR Profile Kickstart: {profile}</Name>
    {gameparams}
        <Order>
            <Kickstart Profile=""{profile_path}"" Delay=""{delay}""/>
        </Order>
    <KillMonsters>True</KillMonsters>
    <PickupLoot>True</PickupLoot>
</Profile>";
        public static string GenerateKickstart(Profile profile, bool tmpkickstart = false)
        {
            try
            {
                
                var path = Path.Combine(Path.GetDirectoryName(profile.Location), string.Format("YAR{0}_Kickstart.xml", tmpkickstart ? "_TMP" : ""));
                
                if (File.Exists(path))
                {
                    if (Settings.Default.KickstartAlwaysGenerateNew || tmpkickstart)
                    {
                        Logger.Instance.Write("Delete old Kickstart profile: {0}", path);
                        File.Delete(path);
                    }
                    else
                    {
                        Logger.Instance.Write("Using a already generated Kickstart profile: {0}", path);
                        return path;
                    }
                }
                    

                Logger.Instance.Write("Generate new Kickstart profile: {0}", path);
                var kickstartprofile = YarKickstart;
                // Replace stuff with current profile
                kickstartprofile = kickstartprofile.Replace("{profile}", profile.Name);
                kickstartprofile = kickstartprofile.Replace("{profile_path}", profile.Location);
                kickstartprofile = kickstartprofile.Replace("{delay}", Settings.Default.KickstartDelay.ToString());

                // Get current profile GameParams
                var gameparams= string.Empty;
                using (var reader = new StreamReader(profile.Location))
                {
                    string line;
                    // Read line for line and match with GameParamsRegex pattern to finde GameParams tag
                    while ((line =reader.ReadLine()) != null)
                    {
                        var m = new Regex(GameParamsRegex).Match(line);
                        if (m.Success)
                        {
                            gameparams = m.Groups[1].Value;
                            break;
                        }
                    }
                }

                // GameParams not found
                if (gameparams == string.Empty)
                {
                    Logger.Instance.Write("Failed to get GameParams in profile: ", profile.Location);
                    Logger.Instance.Write("Using the profile without Kickstart!");
                    return profile.Location;
                }

                // Insert current profile GameParams
                kickstartprofile = kickstartprofile.Replace("{gameparams}", gameparams);

                // Write Kickstart file to disk
                using (var writer = new StreamWriter(path))
                {
                    writer.WriteLine(kickstartprofile);
                }

                // Return path for kickstart file
                return path;
            }
            catch (Exception ex)
            {
                Logger.Instance.Write("Failed to generate Kickstart profile: {0}", ex.Message);
                Logger.Instance.Write("Using the profile without Kickstart!");
                DebugHelper.Exception(ex);
                return profile.Location;
            }
        }
    }
}
