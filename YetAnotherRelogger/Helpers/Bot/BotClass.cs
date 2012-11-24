using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;


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

        [XmlIgnore] private string _status;
        [XmlIgnore] public string Status { get { return _status; } set { SetField(ref _status, value, "Status"); } }

        [XmlIgnore] public DateTime StartTime { get; set; }
        [XmlIgnore] private string _runningtime;
        [XmlIgnore] public string RunningTime { get { return _runningtime; } set { SetField(ref _runningtime, value, "RunningTime"); } }

        public void Stop()
        {
            Logger.Instance.Write(this, "Stopping");
            Status = "Stopped";
            IsStarted = false;
            IsRunning = false;
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
