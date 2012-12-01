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
/*
        // Position and Sizing
        RepositionAll = 0,
        Reposition,  // Reposition/Resize current diablo 
        Maximize,    // Reposition/Resize current diablo to preset 
        FullScreen,  // Make current diablo fullscreen
        // Bot Control
        PauseResumeAll,
        PauseResumeCurrent,
        // Misc
        PlaySound
*/