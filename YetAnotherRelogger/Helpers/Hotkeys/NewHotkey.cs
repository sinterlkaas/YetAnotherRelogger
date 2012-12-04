using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using YetAnotherRelogger.Forms.SettingsTree;
using YetAnotherRelogger.Helpers.Hotkeys.Actions;
using YetAnotherRelogger.Properties;

namespace YetAnotherRelogger.Helpers.Hotkeys
{
    public partial class NewHotkey : Form
    {
        public NewHotkey()
        {
            InitializeComponent();
            HotkeyNew = new Hotkey ();
            
        }

        public NewHotkey(Hotkey hotkey)
        {
            InitializeComponent();
            Text = "Edit Hotkey";
            textBox1.Text = hotkey.Name;
            textBox2.Text = string.Format("{0}+{1}", hotkey.Modifier.ToString().Replace(", ", "+"), hotkey.Key);
            HotkeyNew = hotkey;
        }

        public Hotkey HotkeyNew;
        private CatchHotkey _catchHotkey;
        public override sealed string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        private void NewHotkey_Load(object sender, EventArgs e)
        { // Load
            Closed += new EventHandler(NewHotkey_Closed);
            UpdateDataGridview();
        }

        public void UpdateDataGridview()
        {
            dataGridView1.DataSource = HotkeyNew.Actions;
            dataGridView1.Columns["Order"].Visible = false;
            dataGridView1.Columns["UniqueId"].Visible = false;

            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.MultiSelect = false;
            foreach (DataGridViewColumn column in dataGridView1.Columns)
                column.ReadOnly = true;
        }

        private void button1_Click(object sender, EventArgs e)
        { // Catch new hotkey
            _catchHotkey = new CatchHotkey(this);
            _catchHotkey.ShowDialog(this);
        }

        private void button3_Click(object sender, EventArgs e)
        { // Save
            // Check if Hotkey is valid to be saved
            if (textBox1.Text.Trim().Equals(""))
            {
                MessageBox.Show(this, "You did not enter a name for your Hotkey", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            HotkeyNew.Name = textBox1.Text.Trim();
            var hk = Settings.Default.HotKeys.FirstOrDefault(x => x.HookId == HotkeyNew.HookId);
            if (hk == null)
            {
                GlobalHotkeys.Instance.Add(HotkeyNew);
            }
            else
            {
                GlobalHotkeys.Instance.Remove(hk.HookId);
                GlobalHotkeys.Instance.Add(HotkeyNew);
            }
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        { // Cancel
            Close();
        }

        private void NewHotkey_Closed(object sender, EventArgs e)
        {
            var HKs = Program.Mainform.UcSetting as HotKeys;
            HKs.UpdateGridview();
        }

        private void button4_Click(object sender, EventArgs e)
        { // Edit
            var action = new SelectAction(HotkeyNew);
            action.ShowDialog(this);
        }

        private void button5_Click(object sender, EventArgs e)
        { // Open config window
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.Index < 0)
                return;
            var selected = dataGridView1.CurrentRow;
            var action = ActionContainer.GetAction((string)selected.Cells["Name"].Value, (Version)selected.Cells["Version"].Value);
            if (action != null)
            {
                if (action.ConfigWindow != null)
                    action.ConfigWindow.ShowDialog(this);
            }
        }
    }
}
