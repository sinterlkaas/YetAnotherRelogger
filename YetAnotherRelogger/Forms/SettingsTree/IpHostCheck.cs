using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using YetAnotherRelogger.Properties;

namespace YetAnotherRelogger.Forms.SettingsTree
{
    public partial class IpHostCheck : UserControl
    {
        public IpHostCheck()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.ConnectionCheckIpHostList = textBox1.Text;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.ConnectionCheckIpHost = checkBox4.Checked;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.ConnectionCheckCloseBots = checkBox5.Checked;
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.ConnectionCheckIpCheck = checkBox7.Checked;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.ConnectionCheckHostCheck = checkBox1.Checked;
        }
    }
}
