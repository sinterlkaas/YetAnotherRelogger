using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace YetAnotherRelogger.Forms.Wizard
{
    public partial class Advanced : UserControl
    {
        public Advanced(WizardMain parent)
        {
            InitializeComponent();
            WM = parent;
        }

        private WizardMain WM;
        private void Advanced_Load(object sender, EventArgs e)
        {
            VisibleChanged += new EventHandler(Advanced_VisibleChanged);
        }

        void Advanced_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
                WM.NextStep("Advanced Settings");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "D3Prefs.txt|*.txt",
                FileName = "D3Prefs.txt",
                Title = "Browse to D3Prefs.txt"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
                textBox3.Text = ofd.FileName;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog {Description = "Select Diablo III clone location"};
            if (fbd.ShowDialog() == DialogResult.OK)
                textBox2.Text = fbd.SelectedPath;
        }
    }
}
