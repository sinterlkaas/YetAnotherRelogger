using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml.Serialization;
using YetAnotherRelogger.Helpers.Tools;

namespace YetAnotherRelogger.Helpers.Hotkeys
{
    [Serializable]
    public class Hotkey
    {
        public Hotkey()
        {
            Modifier = new ModifierKeys();
            Key = new Keys();
            Actions = new BindingList<Action>();
        }
        [XmlIgnore] public int HookId { get; set; }
        public string Name { get; set; }
        public ModifierKeys Modifier { get; set; }
        public Keys Key { get; set; }
        public BindingList<Action> Actions { get; set; }
    }
    [Serializable]
    public class Action : INotifyPropertyChanged
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

        [XmlIgnore] private int _order;
        public int Order { get { return _order; } set { SetField(ref _order, value, "Order"); } }
        [XmlIgnore] private string _name;
        public string Name { get { return _name; } set { SetField(ref _name, value, "Name"); } }
        public string Description { get; set; }
        public Version Version { get; set; }
    }
}
