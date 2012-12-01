using System;
using System.Windows.Forms;
using YetAnotherRelogger.Helpers.Tools;

namespace YetAnotherRelogger.Helpers.Hotkeys
{
    public partial class CatchHotkey : Form
    {
        public CatchHotkey(NewHotkey parent)
        {
            InitializeComponent();
            _parent = parent;
            _hotkey = parent.HotkeyNew;
        }

        private NewHotkey _parent;
        private Hotkey _hotkey;

        private void CatchHotkey_Load(object sender, EventArgs e)
        {
            KeyPreview = true;
            KeyDown += new KeyEventHandler(CatchHotkey_KeyDown);
            KeyUp += new KeyEventHandler(CatchHotkey_KeyUp);
            Closed += new EventHandler(CatchHotkey_Closed);
        }

        private void CatchHotkey_KeyDown(object sender, KeyEventArgs e)
        {
            label3.Text = string.Format("{0}+{1}", GlobalHotkeys.KeysToModifierKeys(e.Modifiers).ToString().Replace(", ", "+"), e.KeyCode);
            _hotkey.Modifier = GlobalHotkeys.KeysToModifierKeys(e.Modifiers);
            _hotkey.Key = e.KeyCode;
            e.SuppressKeyPress = true;
            e.Handled = false;
        }
        private void CatchHotkey_KeyUp(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            e.Handled = false;
        }

        private void button2_Click(object sender, EventArgs e)
        { // Close
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        { // Ready / Save
            _parent.HotkeyNew = _hotkey;
            Close();
        }

        void CatchHotkey_Closed(object sender, EventArgs e)
        {
            _parent.textBox2.Text = string.Format("{0}+{1}", _hotkey.Modifier.ToString().Replace(", ", "+"), _hotkey.Key);
        }
    }
}
