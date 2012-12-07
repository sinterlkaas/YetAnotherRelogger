using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using YetAnotherRelogger.Properties;

namespace YetAnotherRelogger.Forms.Wizard
{
    public partial class DiabloOptions : UserControl
    {
        public DiabloOptions(WizardMain parent)
        {
            WM = parent;
            InitializeComponent();
        }
        private WizardMain WM;

        private void DiabloOptions_Load(object sender, EventArgs e)
        {
            this.VisibleChanged += new EventHandler(DiabloOptions_VisibleChanged);
            textBox2.KeyPress += new KeyPressEventHandler(NumericCheck);
            textBox9.KeyPress += new KeyPressEventHandler(NumericCheck);
            textBox10.KeyPress += new KeyPressEventHandler(NumericCheck);
            textBox11.KeyPress += new KeyPressEventHandler(NumericCheck);
            textBox12.KeyPress += new KeyPressEventHandler(NumericCheck);
            comboBox3.SelectedIndex = 2;
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 1;
        }

        void NumericCheck(object sender, KeyPressEventArgs e)
        {
            e.Handled = Helpers.Tools.General.NumericOnly(e.KeyChar);
        }

        void DiabloOptions_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
               WM.NextStep("Diablo Settings");
        }

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                label5.Enabled = label6.Enabled = label7.Enabled = label8.Enabled = label9.Enabled = true;
                textBox4.Enabled = textBox5.Enabled = textBox7.Enabled = textBox6.Enabled = textBox8.Enabled = true;
                button1.Enabled = button3.Enabled = true;
            }
            else
            {
                label5.Enabled = label6.Enabled = label7.Enabled = label8.Enabled = label9.Enabled = false;
                textBox4.Enabled = textBox5.Enabled = textBox7.Enabled = textBox6.Enabled = textBox8.Enabled = false;
                button1.Enabled = button3.Enabled = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
                          {
                              Filter = "Diablo III.exe|*.exe",
                              FileName = "Diablo III.exe",
                              Title = "Browse to Diablo III.exe"
                          };
            if (ofd.ShowDialog() == DialogResult.OK)
                textBox1.Text = ofd.FileName;
            
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            label13.Enabled =
                label14.Enabled =
                label15.Enabled =
                label16.Enabled =
                textBox2.Enabled = textBox9.Enabled = textBox10.Enabled = textBox11.Enabled = checkBox3.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            label11.Enabled = label17.Enabled = textBox12.Enabled = textBox13.Enabled = checkBox2.Checked;
            if (checkBox2.Checked && (string.IsNullOrEmpty(Settings.Default.ISBoxerPath) || !File.Exists(Settings.Default.ISBoxerPath)))
            {
                // Locate Inner space
                var ofd = new OpenFileDialog
                {
                    Filter = "Inner Space.exe|*.exe",
                    FileName = "Inner Space.exe",
                    Title = "Browse to Inner Space.exe"
                };
                if (ofd.ShowDialog() == DialogResult.OK)
                    Settings.Default.ISBoxerPath = ofd.FileName;
                ofd.Dispose();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            WM.AffinityDiablo.ShowDialog(this);
        }

        public bool ValidateInput()
        {
            return (WM.ValidateTextbox(textBox3) &
                    WM.ValidateTextbox(textBox1) &
                    WM.ValidateMaskedTextbox(maskedTextBox1) &
                    (!checkBox2.Checked || (WM.ValidateTextbox(textBox12) & WM.ValidateTextbox(textBox13)))
                );
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
