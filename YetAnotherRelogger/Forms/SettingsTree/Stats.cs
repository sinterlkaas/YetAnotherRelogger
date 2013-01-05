using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace YetAnotherRelogger.Forms.SettingsTree
{
    public partial class Stats : UserControl
    {
        public Stats()
        {
            InitializeComponent();
        }

        private void StatsEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (!StatsEnabled.Checked)
                StatsUpdater.Instance.Stop();
            else
                StatsUpdater.Instance.Start();
        }
    }
}
