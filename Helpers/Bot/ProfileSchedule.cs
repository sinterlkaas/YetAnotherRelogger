using System;
using System.Linq;
using System.Xml.Serialization;
using System.ComponentModel;


namespace YetAnotherRelogger.Helpers.Bot
{
    public class ProfileScheduleClass
    {
        public ProfileScheduleClass()
        {
            Current = new Profile();
            Profiles = new BindingList<Profile>();
        }

        public bool UseThirdPartyPlugin { get; set; }
        public int MaxRandomRuns { get; set; }
        public int MaxRandomTime { get; set; }
        public BindingList<Profile> Profiles { get; set; }
        

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
                var rnd = new Random();
                Current = Profiles.FirstOrDefault(x => x.IsDone == false);
                if (Current == null)
                {
                    Logger.Instance.Write("All profiles are done resetting cycle");
                    foreach (var p in Profiles)
                        p.IsDone = false;

                    Current = Profiles[0];
                }

                Count = 0;
                StartTime = DateTime.Now;
                _addRuns = rnd.Next(0, MaxRandomRuns);
                _addTime = rnd.Next(0, MaxRandomTime);

                Logger.Instance.Write("Current profile: \"{0}\" Runs:{1} Time:{2} mintues ({3})", Current.Name, MaxRuns, MaxTime, Current.Location);
                return Current.Location;
            }
        }

        [XmlIgnore]
        public bool IsDone
        {
            get
            {
                if ((Current.Runs > 0 && Count > Current.Runs + _addRuns) || (Current.Minutes > 0 && DateTime.Now.Subtract(StartTime).TotalMinutes > Current.Minutes + _addTime))
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
        }
        public string Name { get; set; }
        public string Location { get; set; }
        public int Runs { get; set; }
        public int Minutes { get; set; }
        [XmlIgnore] public bool IsDone { get; set; }
    }
}
