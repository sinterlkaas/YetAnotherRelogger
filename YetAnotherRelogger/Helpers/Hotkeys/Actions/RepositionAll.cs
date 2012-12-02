using System;

namespace YetAnotherRelogger.Helpers.Hotkeys.Actions
{
    public class RepositionAll : IHotkeyAction
    {
        public string Name { get { return "RepositionAll";  } }
        public string Author { get { return "sinterlkaas"; } }
        public string Description { get { return "Reposition All Windows"; } }
        public Version Version { get { return new Version(1,0,0); } }

        public void OnPressed(Hotkey hotkey)
        {
            Logger.Instance.WriteGlobal("Hotkey pressed: {0}+{1} : {2}", hotkey.Modifier.ToString().Replace(", ", "+"), hotkey.Key ,Name );
            AutoPosition.PositionWindows();
        }

        public bool Equals(IHotkeyAction other)
        {
            return (other.Name == Name) && (other.Version == Version);
        }
    }
}