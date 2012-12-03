using System;
using System.Windows.Forms;

namespace YetAnotherRelogger.Helpers.Hotkeys
{
    public interface IHotkeyAction : IEquatable<IHotkeyAction>
    {
        string Name { get; }
        string Author { get; }
        string Description { get; }
        Version Version { get; }
        Form ConfigWindow { get; }
        void OnInitialize(Hotkey hotkey);
        void OnDispose();
        void OnPressed();
    }
}
