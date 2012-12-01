using System;

namespace YetAnotherRelogger.Helpers.Hotkeys
{
    public interface IHotkeyAction : IEquatable<IHotkeyAction>
    {
        string Name { get; }
        string Author { get; }
        string Description { get; }
        Version Version { get; }
        void OnPressed();
    }
}
