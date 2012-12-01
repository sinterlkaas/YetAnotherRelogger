using System;
using System.Windows.Forms;

namespace YetAnotherRelogger.Helpers.Hotkeys.Actions
{
    public class RepositionCurrent : IHotkeyAction
    {
        public string Name { get { return "RepositionCurrent"; } }
        public string Author { get { return "sinterlkaas"; } }
        public string Description { get { return "Reposition Current Window"; } }
        public Version Version { get { return new Version(1,0,0); } }

        public void OnPressed()
        {
            // Hotkey pressed
            MessageBox.Show("Hello World!");
        }

        public bool Equals(IHotkeyAction other)
        {
            return (other.Name == Name) && (other.Version == Version);
        }
    }
}