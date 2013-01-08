using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.ComponentModel;
using YetAnotherRelogger.Helpers.Enums;
using YetAnotherRelogger.Helpers.Tools;
using YetAnotherRelogger.Properties;


namespace YetAnotherRelogger.Helpers.Bot
{
    public class ProfileScheduleClass
    {
        public ProfileScheduleClass()
        {
            Current = new Profile() { IsDone = true };
            Profiles = new BindingList<Profile>();
        }

        public bool UseThirdPartyPlugin { get; set; }
        public int MaxRandomRuns { get; set; }
        public int MaxRandomTime { get; set; }
        public BindingList<Profile> Profiles { get; set; }
        public bool Random { get; set; }
        

        [XmlIgnore] public Profile Current;
        [XmlIgnore] public int Count;
        [XmlIgnore] public int MaxRuns { get { return Current.Runs + _addRuns; } }
        [XmlIgnore] public int MaxTime { get { return Current.Minutes + _addTime; } }
        [XmlIgnore] public DateTime StartTime;
        [XmlIgnore] private int _addRuns;
        [XmlIgnore] private int _addTime;

        [XmlIgnore]
        public string GetProfile
        {
            get
            {
                // Stay on same profile when not ready yet
                if (!Current.IsDone) return Current.Location;

                var rnd = new MersenneTwister();

                var listcount = Profiles.Count(x => !x.IsDone);
                // Check if we need to reset list
                if (listcount == 0)
                {
                    Logger.Instance.Write("All profiles are done resetting cycle");
                    foreach (var p in Profiles) p.IsDone = false; // reset each profile in list
                    listcount = Profiles.Count();
                }
                Count = 0; // Reset run counter
                StartTime = DateTime.Now; // Reset Start time
                var filtered = from x in Profiles.Where(x => !x.IsDone).Select((item, index) => new { item, index }) where x.index % 2 == rnd.Next(0, listcount - 1) select x.item;
                var enumerable = filtered as List<Profile> ?? filtered.ToList();
                Current = Random && enumerable.FirstOrDefault() != null ? enumerable.FirstOrDefault() : Profiles.FirstOrDefault(x => !x.IsDone);
                _addRuns = rnd.Next(0, MaxRandomRuns);
                _addTime = rnd.Next(0, MaxRandomTime);

                Logger.Instance.Write("Current profile: \"{0}\" Runs:{1} Time:{2} mintues ({3})", Current.Name, MaxRuns, MaxTime, Current.Location);

                return Settings.Default.UseKickstart ? ProfileKickstart.GenerateKickstart(Current) : Current.Location;
            }
        }

        [XmlIgnore]
        public bool IsDone
        {
            get
            {
                var maxminutes = (Current.Minutes > 59 ? 59 : Current.Minutes) + _addTime;
                maxminutes = (maxminutes > 59 ? 59 : maxminutes);

                if ((Current.Runs > 0 && Count >= Current.Runs + _addRuns) || (Current.Minutes > 0 && DateTime.Now.Subtract(StartTime).TotalMinutes > maxminutes))
                {
                    Current.IsDone = true;
                    return true;
                }
                return false;
            }
        }
    }
    public class Profile
    {
        public Profile()
        {
            MonsterPowerLevel = MonsterPower.Disabled;
        }
        public string Name { get; set; }
        public string Location { get; set; }
        public int Runs { get; set; }
        public int Minutes { get; set; }
        public MonsterPower MonsterPowerLevel { get; set; }
        [XmlIgnore] public bool IsDone { get; set; }
    }
}
