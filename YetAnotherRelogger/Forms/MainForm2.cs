using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using YetAnotherRelogger.Helpers;
using YetAnotherRelogger.Helpers.Tools;
using YetAnotherRelogger.Properties;

namespace YetAnotherRelogger.Forms
{
    public partial class MainForm2 : Form
    {
        public MainForm2()
        {
            InitializeComponent();
            treeView1.NodeMouseClick += new TreeNodeMouseClickEventHandler(treeView1_NodeMouseClick);
        }

        private ContextMenu m_menu;
        private bool bClose;

        private void MainForm2_Load(object sender, EventArgs e)
        {
            this.Text = string.Format("Yet Another Relogger [{0}] BETA", Program.VERSION);

            Logger.Instance.WriteGlobal("Yet Another Relogger Version {0}", Program.VERSION);
            // Check if we are run as admin
            if (!Program.IsRunAsAdmin)
                Logger.Instance.WriteGlobal("WE DON'T HAVE ADMIN RIGHTS!!");

            this.Resize += new EventHandler(MainForm2_Resize);

            // Set stuff for list of bots
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.MultiSelect = false;
            dataGridView1.MouseUp += new MouseEventHandler(dataGridView1_MouseUp);
            dataGridView1.CellValueChanged += new DataGridViewCellEventHandler(dataGridView1_CellValueChanged);
            UpdateGridView();

            // OnClose
            Closing += new CancelEventHandler(MainForm2_Closing);

            // TrayIcon
            ToggleIcon();
            TrayIcon.Icon = this.Icon;
            TrayIcon.DoubleClick += new EventHandler(TrayIcon_DoubleClick);
            m_menu = new ContextMenu();
            m_menu.MenuItems.Add(0, new MenuItem("Show", new EventHandler(Show_Click)));
            m_menu.MenuItems.Add(1, new MenuItem("Hide", new EventHandler(Hide_Click)));
            m_menu.MenuItems.Add(2, new MenuItem("Exit", new EventHandler(Exit_Click)));
            TrayIcon.ContextMenu = m_menu;
        }

        protected void MainForm2_Closing(object sender, CancelEventArgs e)
        {
            if (!bClose && Properties.Settings.Default.CloseToTray)
            {
                e.Cancel = true;
                this.Hide();
                ToggleIcon();
                ShowNotification("Yet Another Relogger", "Is still running");
                
            }
        }

        #region Tray Icon

        public void ShowNotification(string title, string msg, ToolTipIcon icon = ToolTipIcon.None)
        {
            if (!Properties.Settings.Default.ShowNotification || !TrayIcon.Visible) return;
            TrayIcon.ShowBalloonTip(500, title, msg, icon);
        }
        public void ToggleIcon()
        {
            TrayIcon.Visible = (Properties.Settings.Default.AlwaysShowTray ||
                                (!this.Visible || this.WindowState == FormWindowState.Minimized));
        }
        protected void Exit_Click(Object sender, EventArgs e)
        {
            bClose = true;
            this.Close();
        }
        protected void Hide_Click(Object sender, EventArgs e)
        {
            ToggleIcon();
            ShowNotification("Yet Another Relogger", "Is still running");
            this.Hide();
        }
        protected void Show_Click(Object sender, EventArgs e)
        {
            this.Show();
            WinAPI.ShowWindow(this.Handle, WinAPI.WindowShowStyle.ShowNormal);
            ToggleIcon();
        }
        void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            WinAPI.ShowWindow(this.Handle, WinAPI.WindowShowStyle.ShowNormal);
            ToggleIcon();
        }
        #endregion

        void MainForm2_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState && Properties.Settings.Default.MinimizeToTray)
            {
                ToggleIcon();
                ShowNotification("Yet Another Relogger", "Is still running");
                this.Hide();
            }
        }

        void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dataGridView1.Columns["isEnabled"].Index)
            {
                try
                {
                    BotSettings.Instance.Bots[e.RowIndex].IsEnabled = (bool)dataGridView1[e.ColumnIndex, e.RowIndex].Value;
                    BotSettings.Instance.Save();
                }
                catch
                {

                }
            }
        }

        void dataGridView1_MouseUp(object sender, MouseEventArgs e)
        {
            var hitTestInfo = dataGridView1.HitTest(e.X, e.Y);
            if (hitTestInfo.Type == DataGridViewHitTestType.Cell)
            {
                if (e.Button == MouseButtons.Right)
                {
                    contextMenuStrip1.Show(dataGridView1, new Point(e.X, e.Y));
                    selectRow(hitTestInfo.RowIndex);
                }
                else if (e.Button == MouseButtons.Left)
                {
                    selectRow(hitTestInfo.RowIndex);
                }
            }
        }

        void selectRow(int index)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
                row.Selected = false;
            dataGridView1.Rows[index].Selected = true;
            dataGridView1.CurrentCell = dataGridView1.Rows[index].Cells[0];
        }

        public void UpdateGridView()
        {
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = BotSettings.Instance.Bots;
            dataGridView1.Columns["week"].Visible = false;
            dataGridView1.Columns["demonbuddy"].Visible = false;
            dataGridView1.Columns["diablo"].Visible = false;
            dataGridView1.Columns["isRunning"].Visible = false;
            dataGridView1.Columns["isStarted"].Visible = false;
            dataGridView1.Columns["profileSchedule"].Visible = false;
            dataGridView1.Columns["AntiIdle"].Visible = false;


            dataGridView1.Columns["isEnabled"].DisplayIndex = 1;
            dataGridView1.Columns["isEnabled"].HeaderText = "Enabled";
            dataGridView1.Columns["isEnabled"].Width = 50;

            dataGridView1.Columns["Name"].DisplayIndex = 2;
            dataGridView1.Columns["Name"].ReadOnly = true;

            dataGridView1.Columns["Description"].DisplayIndex = 3;
            dataGridView1.Columns["Description"].Width = 300;
            dataGridView1.Columns["Description"].ReadOnly = true;

            dataGridView1.Columns["Status"].ReadOnly = true;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.HeaderCell.Value = string.Format("{0:00}", (row.Index + 1));
            }

            dataGridView1.AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders);
        }

        private void button1_Click(object sender, EventArgs e)
        { // Start All
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (!((bool) row.Cells["isEnabled"].Value)) continue;

                BotSettings.Instance.Bots[row.Index].AntiIdle.Reset(freshstart: true);
                BotSettings.Instance.Bots[row.Index].Week.ForceStart = checkBox1.Checked;
                BotSettings.Instance.Bots[row.Index].IsStarted = true;
                if (checkBox1.Checked) Logger.Instance.Write(BotSettings.Instance.Bots[row.Index], "Forced to start! ");
                BotSettings.Instance.Bots[row.Index].Status = (checkBox1.Checked ? "Forced start": "Started");
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Open new bot wizard
            var wm = new Wizard.WizardMain {TopMost = true};
            wm.Show();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Edit bot
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.Index < 0)
                return;
            var wm = new Wizard.WizardMain(dataGridView1.CurrentRow.Index) {TopMost = true};

            wm.Show();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            bClose = true;
            this.Close();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (richTextBox1.Lines.Length > 300)
                richTextBox1.Clear();
            // scroll down
            richTextBox1.ScrollToCaret();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Relogger.Instance.Stop();
            // Stop All
            foreach (var bot in BotSettings.Instance.Bots)
            {
                bot.Stop();
            }
            Relogger.Instance.Start();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {// Start
            BotSettings.Instance.Bots[dataGridView1.CurrentRow.Index].AntiIdle.Reset(freshstart: true);
            BotSettings.Instance.Bots[dataGridView1.CurrentRow.Index].IsStarted = true;
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {// Stop
            if (BotSettings.Instance.Bots[dataGridView1.CurrentRow.Index].IsStarted)
                BotSettings.Instance.Bots[dataGridView1.CurrentRow.Index].Stop();
        }

        private void statsToolStripMenuItem_Click(object sender, EventArgs e)
        {// Bot Stats
        }
        
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {// Delete Bot
            if (MessageBox.Show("Are you sure you want to delete this bot?", "Delete bot", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                BotSettings.Instance.Bots.RemoveAt(dataGridView1.CurrentRow.Index);
                BotSettings.Instance.Save();
                UpdateGridView();
            }
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {// Edit bot
            var wm = new Wizard.WizardMain(dataGridView1.CurrentRow.Index) {TopMost = true};
            wm.Show();
        }
        private void forceStartToolStripMenuItem_Click(object sender, EventArgs e)
        { // Force Start single bot
            BotSettings.Instance.Bots.RemoveAt(dataGridView1.CurrentRow.Index);
            BotSettings.Instance.Bots[dataGridView1.CurrentRow.Index].Week.ForceStart = true;
            BotSettings.Instance.Bots[dataGridView1.CurrentRow.Index].IsStarted = true;
        }
        #region Settings Tree
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private UserControl _ucSetting = new UserControl(); // holds current settings user control
        void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            var tmp = new UserControl();
            switch (e.Node.Name)
            {
                case "General": // General
                    tmp = new SettingsTree.General();
                    break;
                case "AutoPos": // Auto postion
                    tmp = new SettingsTree.AutoPosition();
                    break;
                case "PingCheck":
                case "ConnectionCheck": // Auto postion
                    tmp = new SettingsTree.ConnectionCheck();
                    break;
                case "IpHostCheck": // Auto postion
                    tmp = new SettingsTree.IpHostCheck();
                    break;
            }

            // Check if new user control should be displayed
            if (!tmp.Name.Equals(_ucSetting.Name))
            {
                //var c = tabControl1.TabPages[1].Controls;
                var c = SettingsPanel.Controls;
                if (c.Contains(_ucSetting)) c.Remove(_ucSetting);
                
                _ucSetting = tmp;
                //_ucSetting.Left = 180;
                c.Add(_ucSetting);
            }
        }
        #endregion

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        

        private void button6_Click(object sender, EventArgs e)
        {
            if (Program.Pause)
            {
                Program.Pause = false;
                button6.Text = "Pause";
            }
            else
            {
                Program.Pause = true;
                button6.Text = "Unpause";
            }
        }
    }
}
