using System;
using System.Linq;
using System.Windows.Forms;
using YetAnotherRelogger.Helpers.Hotkeys;
using YetAnotherRelogger.Properties;

namespace YetAnotherRelogger.Forms.SettingsTree
{
    public partial class HotKeys : UserControl
    {
        public HotKeys()
        {
            InitializeComponent();
        }

        private NewHotkey _newHotkey;

        private void HotKeys_Load(object sender, EventArgs e)
        {
            UpdateGridview();
        }

        private void button1_Click(object sender, EventArgs e)
        { // Create new hotkey
            _newHotkey = new NewHotkey();
            _newHotkey.ShowDialog(this);
        }

        private void button2_Click(object sender, EventArgs e)
        { // Edit hotkey
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.Index < 0) return;
            var hk = Settings.Default.HotKeys.FirstOrDefault(x => x.HookId == (int)dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells["HookId"].Value);
            if (hk == null) return;
            _newHotkey = new NewHotkey(hk);
            _newHotkey.ShowDialog(this);
        }

        private void button3_Click(object sender, EventArgs e)
        { // Delete hotkey
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.Index < 0) return;
            var hk = Settings.Default.HotKeys.FirstOrDefault(x => x.HookId == (int)dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells["HookId"].Value);
            if (hk == null) return;
            GlobalHotkeys.Instance.Remove(hk.HookId);
            UpdateGridview();
        }

        public void UpdateGridview()
        {
            dataGridView1.DataSource = Settings.Default.HotKeys;
            dataGridView1.Refresh();
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.MultiSelect = false;
            foreach (DataGridViewColumn column in dataGridView1.Columns)
                column.ReadOnly = true;
        }
    }
}
