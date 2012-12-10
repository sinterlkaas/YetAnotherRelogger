using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using YetAnotherRelogger.Helpers.Tools;


namespace YetAnotherRelogger.Helpers.Bot
{
    public class BotClass : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public BotClass()
        {
            Name = string.Empty;
            Description = String.Empty;
            AntiIdle = new AntiIdleClass();
        }
        public string Name { get; set; }
        public string Description { get; set; }
        
        public bool IsEnabled { get; set; }

        [XmlIgnore] private DemonbuddyClass _demonbuddy;
        public DemonbuddyClass Demonbuddy
        {
            get { return _demonbuddy; } 
            set {
                var db = value;
                db.Parent = this;
                _demonbuddy = db;
            } 
        }
        [XmlIgnore] private DiabloClass _diablo;
        public DiabloClass Diablo
        {
            get { return _diablo; } 
            set {
                var d = value;
                d.Parent = this;
                _diablo = d;
            } 
        }

        [XmlIgnore] private AntiIdleClass _antiIdle;
        [XmlIgnore] public AntiIdleClass AntiIdle
        {
            get { return _antiIdle; }
            set
            {
                var ai = value;
                ai.Parent = this;
                _antiIdle = ai;
            }
        }

        public WeekSchedule Week { get; set; }


        public ProfileScheduleClass ProfileSchedule { get; set; }

        [XmlIgnore] public bool IsStarted { get; set; }
        [XmlIgnore] public bool IsRunning { get; set; }

        // Standby to try again at a later moment
        [XmlIgnore] private bool _isStandby;
        [XmlIgnore] public bool IsStandby
        {
            get
            {
                // Increase retry count by 15 mins with a max of 1 hour
                if (_isStandby && General.DateSubtract(_standbyTime) > 900 * (AntiIdle.InitAttempts > 4 ? 4 : AntiIdle.InitAttempts))
                {
                    _isStandby = false;
                    _diablo.Start();
                    _demonbuddy.Start();
                }
                return _isStandby;
            }
            private set
            {
                _standbyTime = DateTime.Now;
                _isStandby = value;
            }
        }
        [XmlIgnore] private DateTime _standbyTime;

        [XmlIgnore] private string _status;
        [XmlIgnore] public string Status { get { return _status; } set { SetField(ref _status, value, "Status"); } }

        [XmlIgnore] public DateTime StartTime { get; set; }
        [XmlIgnore] private string _runningtime;
        [XmlIgnore] public string RunningTime { get { return _runningtime; } set { SetField(ref _runningtime, value, "RunningTime"); } }

        #region Advanced Options Variables
        // Windows User
        public bool UseWindowsUser { get; set; }
        public bool CreateWindowsUser { get; set; }
        public string WindowsUserName { get; set; }
        public string WindowsUserPassword { get; set; }

        // Diablo Clone
        public bool UseDiabloClone { get; set; }
        public string DiabloCloneLocation { get; set; }

        // D3Prefs
        public string D3PrefsLocation { get; set; }
        #endregion

        [XmlIgnore] private string _demonbuddyPid;
        [XmlIgnore] public string DemonbuddyPid { get { return _demonbuddyPid; } set { SetField(ref _demonbuddyPid, value, "DemonbuddyPid"); } }
        
        public void Start(bool force = false)
        {
            AntiIdle.Reset(freshstart: true);
            IsStarted = true;
            IsStandby = false;
            Week.ForceStart = force;
            Status = (force ? "Forced start" : "Started");
            if (force)
                Logger.Instance.Write(this, "Forced to start! ");
        }

        public void Stop()
        {
            Logger.Instance.Write(this, "Stopping");
            Status = "Stopped";
            IsStarted = false;
            IsRunning = false;
            IsStandby = false;
            _diablo.Stop();
            _demonbuddy.Stop();
        }

        public void Standby()
        {
            Logger.Instance.Write(this, "Standby!");
            Status = "Standby";
            IsStandby = true;
            _diablo.Stop();
            _demonbuddy.Stop();
        }

        public void Restart()
        {
            Logger.Instance.Write(this, "Restarting...");
            Status = "Restarting";
            _diablo.Stop();
            _demonbuddy.Stop();
        }
    }
}
