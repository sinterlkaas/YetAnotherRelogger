using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace YetAnotherRelogger.Helpers.Hotkeys
{
    public partial class SelectAction : Form
    {
        public SelectAction(Hotkey hotkey)
        {
            InitializeComponent();
            _hotkey = hotkey;
            _actions = hotkey.Actions;
        }

        private Hotkey _hotkey;
        private List<Action> _actions;

        private void SelectAction_Load(object sender, EventArgs e)
        {
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = ActionContainer.Actions;

            dataGridView1.Columns["Order"].Visible = false;
            foreach (var column in dataGridView1.Columns)
                ((DataGridViewColumn)column).ReadOnly = true;
        }
        private void button2_Click(object sender, EventArgs e)
        { // Cancel
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        { // Add Selected
           Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        
    }
}
