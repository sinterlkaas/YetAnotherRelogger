using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace YetAnotherRelogger.Forms.Wizard
{
    public partial class SetAffinity : Form
    {
        public List<CheckBox> cpus = new List<CheckBox>();
        public SetAffinity()
        {
            InitializeComponent();
            
            for (var i = 0; i < Environment.ProcessorCount; ++i)
            {
                CheckBox cpuBox = new CheckBox();
                this.panel1.Controls.Add(cpuBox);
                cpuBox.AutoSize = true;
                cpuBox.Location = new System.Drawing.Point(4, 4 + i * 23);
                cpuBox.Name = string.Format("checkBoxCpu{0}", i);
                cpuBox.Size = new System.Drawing.Size(80, 17);
                cpuBox.TabIndex = 0;
                cpuBox.Text = string.Format("cpu {0}", i);
                cpuBox.UseVisualStyleBackColor = true;
                cpuBox.Checked = true;

                cpus.Add(cpuBox);
            }
        }

        private void SetAffinity_Load(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (var box in cpus)
                box.Checked = true;
        }

        // Disable Close button
        private const int CP_NOCLOSE_BUTTON = 0x200;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        } 
    }
}
