using System;
using System.Windows.Forms;
using YetAnotherRelogger.Helpers.Tools;
using YetAnotherRelogger.Properties;

namespace YetAnotherRelogger.Forms.SettingsTree
{
    public partial class General : UserControl
    {
        public General()
        {
            InitializeComponent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AlwaysShowTray = checkBox1.Checked;
            Program.Mainform.ToggleIcon();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.MinimizeToTray = checkBox2.Checked;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.CloseToTray = checkBox4.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ShowNotification = checkBox3.Checked;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.UseD3Starter = checkBox5.Checked;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.D3StarterPath = textBox1.Text;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // Locate Apoc D3Starter
            var ofd = new OpenFileDialog
            {
                Filter = "D3Starter.exe|*.exe",
                FileName = "D3Starter.exe",
                Title = "Browse to D3Starter.exe"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = ofd.FileName;
                Properties.Settings.Default.D3StarterPath = ofd.FileName;
            }
            ofd.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Locate Inner space
            var ofd = new OpenFileDialog
            {
                Filter = "Inner Space.exe|*.exe",
                FileName = "Inner Space.exe",
                Title = "Browse to Inner Space.exe"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = ofd.FileName;
                Properties.Settings.Default.ISBoxerPath = ofd.FileName;
            }
            ofd.Dispose();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ISBoxerPath = textBox2.Text;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            Settings.Default.DemonbuddyStartDelay = numericUpDown1.Value;
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox6.Checked)
                ForegroundChecker.Instance.Start();
            else
                ForegroundChecker.Instance.Stop();
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            // Add/Remove registry key at windows startup
            if (checkBox7.Checked)
                RegistryClass.WindowsAutoStartAdd();
            else
                RegistryClass.WindowsAutoStartDel();
        }
    }
}
