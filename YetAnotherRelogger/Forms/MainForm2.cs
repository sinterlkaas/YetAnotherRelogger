using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using YetAnotherRelogger.Helpers;
using YetAnotherRelogger.Helpers.Hotkeys;
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

            // Check if current application path is the same as last saved path
            // this is used for Windows autostart in a sutation where user moved/renamed the relogger
            if (Settings.Default.StartWithWindows && !Settings.Default.Location.Equals(Application.ExecutablePath))
            {
                Logger.Instance.WriteGlobal("Application current path does not match last saved path. Updating registy key.");
                // Update to current location
                Settings.Default.Location = Application.ExecutablePath;
                // Update Regkey
                RegistryClass.WindowsAutoStartAdd();
            }

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

            // Minimize on start
            if (Settings.Default.MinimizeOnStart)
            {
                WindowState = FormWindowState.Minimized;
                if (Settings.Default.MinimizeToTray)
                {
                    HideMe();
                    ToggleIcon();
                    ShowNotification("Yet Another Relogger", "Minimize on start");
                }
            }

            // Load global hotkeys
            GlobalHotkeys.Instance.Load();
        }

        protected void MainForm2_Closing(object sender, CancelEventArgs e)
        {
            if (!bClose && Properties.Settings.Default.CloseToTray)
            {
                e.Cancel = true;
                HideMe();
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
            HideMe();
        }
        protected void Show_Click(Object sender, EventArgs e)
        {
            ShowMe();
            WinAPI.ShowWindow(this.Handle, WinAPI.WindowShowStyle.ShowNormal);
            ToggleIcon();
        }
        void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowMe();
            WinAPI.ShowWindow(this.Handle, WinAPI.WindowShowStyle.ShowNormal);
            ToggleIcon();
        }

        void ShowMe()
        {
            ShowInTaskbar = true;
            Visible = true;
            Show();
        }
        void HideMe()
        {
            ShowInTaskbar = false;
            Visible = false;
            Hide();
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
            dataGridView1.DataSource = BotSettings.Instance.Bots;
            dataGridView1.Refresh();
            dataGridView1.Columns["week"].Visible = false;
            dataGridView1.Columns["demonbuddy"].Visible = false;
            dataGridView1.Columns["diablo"].Visible = false;
            dataGridView1.Columns["isRunning"].Visible = false;
            dataGridView1.Columns["isStarted"].Visible = false;
            dataGridView1.Columns["profileSchedule"].Visible = false;
            dataGridView1.Columns["AntiIdle"].Visible = false;
            dataGridView1.Columns["StartTime"].Visible = false;
            dataGridView1.Columns["UseWindowsUser"].Visible = false;
            dataGridView1.Columns["CreateWindowsUser"].Visible = false;
            dataGridView1.Columns["WindowsUserName"].Visible = false;
            dataGridView1.Columns["WindowsUserPassword"].Visible = false;
            dataGridView1.Columns["D3PrefsLocation"].Visible = false;
            dataGridView1.Columns["IsStandby"].Visible = false;
            dataGridView1.Columns["UseDiabloClone"].Visible = false;
            dataGridView1.Columns["DiabloCloneLocation"].Visible = false;

            dataGridView1.Columns["isEnabled"].DisplayIndex = 1;
            dataGridView1.Columns["isEnabled"].HeaderText = "Enabled";
            dataGridView1.Columns["isEnabled"].Width = 50;

            dataGridView1.Columns["Name"].DisplayIndex = 2;
            dataGridView1.Columns["Name"].ReadOnly = true;

            dataGridView1.Columns["Description"].DisplayIndex = 3;
            dataGridView1.Columns["Description"].Width = 200;
            dataGridView1.Columns["Description"].ReadOnly = true;

            dataGridView1.Columns["Status"].ReadOnly = true;


            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.HeaderCell.Value = string.Format("{0:00}", (row.Index + 1));
            }

            dataGridView1.AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Start All
            foreach (var row in dataGridView1.Rows.Cast<DataGridViewRow>().Where(row => (bool) row.Cells["isEnabled"].Value))
            {
                BotSettings.Instance.Bots[row.Index].Start(force:checkBox1.Checked);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Open new bot wizard
            var wm = new Wizard.WizardMain {TopMost = true};
            wm.ShowDialog();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Edit bot
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.Index < 0)
                return;
            var wm = new Wizard.WizardMain(dataGridView1.CurrentRow.Index) {TopMost = true};

            wm.ShowDialog();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "Are you sure you want to close Yet Another Relogger?", "Close Yet Another Relogger?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                bClose = true;
                this.Close();
            }
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
            BotSettings.Instance.Bots[dataGridView1.CurrentRow.Index].Start();
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
            wm.ShowDialog();
        }
        private void forceStartToolStripMenuItem_Click(object sender, EventArgs e)
        { // Force Start single bot
            BotSettings.Instance.Bots[dataGridView1.CurrentRow.Index].Start(true);
        }
        #region Settings Tree

        public UserControl UcSetting = new UserControl(); // holds current settings user control
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
                case "ConnectionCheck":
                    tmp = new SettingsTree.ConnectionCheck();
                    break;
                case "IpHostCheck":
                    tmp = new SettingsTree.IpHostCheck();
                    break;
                case "AntiIdle":
                    tmp = new SettingsTree.AntiIdle();
                    break;
                case "ProfileKickstart":
                    tmp = new SettingsTree.ProfileKickstart();
                    break;
                case "HotKeys":
                    tmp = new SettingsTree.HotKeys();
                    break;
                case "Stats":
                    tmp = new SettingsTree.Stats();
                    break;
            }

            // Check if new user control should be displayed
            if (!tmp.Name.Equals(UcSetting.Name))
            {
                //var c = tabControl1.TabPages[1].Controls;
                var c = SettingsPanel.Controls;
                if (c.Contains(UcSetting)) c.Remove(UcSetting);
                
                UcSetting = tmp;
                //_ucSetting.Left = 180;
                c.Add(UcSetting);
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

        protected override void WndProc(ref Message message)
        {
            // Show first instance form
            if (message.Msg == SingleInstance.WM_SHOWFIRSTINSTANCE)
            {
                Show();
                WinAPI.ShowWindow(Handle, WinAPI.WindowShowStyle.ShowNormal);
                WinAPI.SetForegroundWindow(Handle);
            }
            base.WndProc(ref message);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9NF2Q47KYGNJL");
        } 
    }
}
