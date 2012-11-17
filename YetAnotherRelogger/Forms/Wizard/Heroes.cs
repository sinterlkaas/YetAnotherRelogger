using System;
using System.Windows.Forms;

namespace YetAnotherRelogger.Forms.Wizard
{
    public partial class Heroes : UserControl
    {
        public Heroes(WizardMain parent)
        {
            WM = parent;
            InitializeComponent();
            this.VisibleChanged += new EventHandler(Heroes_VisibleChanged);
        }
        private WizardMain WM;

        void Heroes_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
                WM.NextStep("Heroes");
        }

        private void Heroes_Load(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Launch the stuff
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
