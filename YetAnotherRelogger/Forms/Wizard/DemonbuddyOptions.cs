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
            VisibleChanged += new EventHandler(DemonbuddyOptions_VisibleChanged);
            comboBox2.SelectedIndex = 2;
            comboBox1.SelectedIndex = 0;
        }

        void DemonbuddyOptions_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
                WM.NextStep("Demonbuddy Settings");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
                          {
                              Filter = "Demonbuddy.exe|*.exe",
                              FileName = "Demonbuddy.exe",
                              Title = "Browse to Demonbuddy.exe"
                          };
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

        private void button4_Click_1(object sender, EventArgs e)
        {
            WM.AffinityDemonbuddy.ShowDialog(this);
        }

        public bool ValidateInput()
        {
           return (WM.ValidateTextbox(textBox1) & 
                    WM.ValidateTextbox(textBox2) &
                    WM.ValidateTextbox(textBox3) &
                    WM.ValidateTextbox(textBox4) 
                );
        }
    }
}
