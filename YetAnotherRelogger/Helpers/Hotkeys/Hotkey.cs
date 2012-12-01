using System;
using System.Collections.Generic;
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
            Actions = new List<Action>();
        }
        [XmlIgnore] public int HookId { get; set; }
        public string Name { get; set; }
        public ModifierKeys Modifier { get; set; }
        public Keys Key { get; set; }
        public List<Action> Actions { get; set; }
    }
    [Serializable]
    public class Action
    {
        public int Order { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Version Version { get; set; }
    }
}
