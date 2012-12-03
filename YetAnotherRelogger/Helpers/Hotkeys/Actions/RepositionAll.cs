using System;
using System.Windows.Forms;

namespace YetAnotherRelogger.Helpers.Hotkeys.Actions
{
    public class RepositionAll : IHotkeyAction
    {
        public string Name { get { return "RepositionAll";  } }
        public string Author { get { return "sinterlkaas"; } }
        public string Description { get { return "Reposition All Windows"; } }
        public Version Version { get { return new Version(1,0,0); } }
        public Form ConfigWindow { get { return null; } }

        private Hotkey _hotkey;
        public void OnInitialize(Hotkey hotkey)
        {
            _hotkey = hotkey;
        }
        public void OnDispose()
        {

        }
        public void OnPressed()
        {
            Logger.Instance.WriteGlobal("Hotkey pressed: {0}+{1} : {2}", _hotkey.Modifier.ToString().Replace(", ", "+"), _hotkey.Key ,Name );
            AutoPosition.PositionWindows();
        }
        

        public bool Equals(IHotkeyAction other)
        {
            return (other.Name == Name) && (other.Version == Version);
        }
    }
}