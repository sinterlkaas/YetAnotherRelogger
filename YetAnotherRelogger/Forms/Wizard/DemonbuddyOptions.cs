using System;
using System.Drawing;
using System.Windows.Forms;

namespace YetAnotherRelogger.Forms.Wizard
{
    public partial class DemonbuddyOptions : UserControl
    {
        public DemonbuddyOptions(WizardMain parent)
        {
            WM = parent;
            InitializeComponent();
        }
        private WizardMain WM;

        private void DemonbuddyOptions_Load(object sender, EventArgs e)
        {
            checkBox1.Checked = true;
            this.VisibleChanged += new EventHandler(DemonbuddyOptions_VisibleChanged);
            this.comboBox2.SelectedIndex = 2;
        }

        void DemonbuddyOptions_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
                WM.NextStep("Demonbuddy Settings");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Demonbuddy.exe|*.exe";
            ofd.FileName = "Demonbuddy.exe";
            ofd.Title = "Browse to Demonbuddy.exe";
            if (ofd.ShowDialog() == DialogResult.OK)
                textBox4.Text = ofd.FileName;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            label13.Enabled =
                label14.Enabled =
                label15.Enabled =
                label16.Enabled =
                textBox5.Enabled = textBox6.Enabled = textBox10.Enabled = textBox11.Enabled = checkBox4.Checked;
        }

    }
}
