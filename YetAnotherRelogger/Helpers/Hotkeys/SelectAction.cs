using System;
using System.ComponentModel;
using System.Linq;
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
            _actionContainer = new ActionContainer();
        }

        private Hotkey _hotkey;
        private BindingList<Action> _actions;
        private ActionContainer _actionContainer;

        private BindingList<Action> AvailableActions
        {
            get
            {
                var newlist = (from action in _actionContainer.Actions let skip = _actions.Any(test => action.Name.Equals(test.Name) && action.Version.Equals(test.Version)) where !skip select action).ToList();
                return new BindingList<Action>(newlist);
            }
        }

        private void SelectAction_Load(object sender, EventArgs e)
        {
            UpdateGridview();
        }

        private void button2_Click(object sender, EventArgs e)
        { // Close
            _hotkey.Actions = _actions;
            Close();
        }

        private void button4_Click(object sender, EventArgs e)
        { // Add to list
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.Index < 0)
                return;
            var selected = dataGridView1.CurrentRow;
            var action = _actionContainer.Actions.FirstOrDefault(x => x.Name.Equals(selected.Cells["Name"].Value) && x.Version.Equals(selected.Cells["Version"].Value));
            if (action != null)
            {
                action.Order = _actions.Count;
                _actions.Add(action);
                UpdateGridview();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        { // Remove from list
            if (dataGridView2.CurrentRow == null || dataGridView2.CurrentRow.Index < 0)
                return;
            var selected = dataGridView2.CurrentRow;
            var action = _actions.FirstOrDefault(x => x.Name.Equals(selected.Cells["Name"].Value) && x.Version.Equals(selected.Cells["Version"].Value));
            if (action != null)
            {
                _actions.Remove(action);
                UpdateGridview();
            }
        }

        private void button8_Click(object sender, EventArgs e)
        { // Move up
            if (dataGridView2.SelectedCells.Count <= 0 || dataGridView2.SelectedCells[0].RowIndex <= 0) return;
            try
            {
                var index = dataGridView2.SelectedCells[0].RowIndex;
                var test = _actions.FirstOrDefault(x => x.Name == (string)dataGridView2.Rows[index].Cells["Name"].Value && x.Order > 0);
                if (test != null)
                {
                    test.Order--;
                    index--;
                    test = _actions.FirstOrDefault(x => index >= 0 && x.Name == (string)dataGridView2.Rows[index].Cells["Name"].Value && x.Order >= 0);
                    if (test != null) test.Order++;
                }
                // Sort and update list
                var newlist = _actions.ToList();
                newlist.Sort((s1, s2) => s1.Order.CompareTo(s2.Order));
                _actions = new BindingList<Action>(newlist);
                UpdateGridview();
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteGlobal(ex.ToString());
            }
        }

        private void button7_Click(object sender, EventArgs e)
        { // Move down
            if (dataGridView2.SelectedCells.Count <= 0 || dataGridView2.SelectedCells[0].RowIndex <= 0) return;
            try
            {
                var index = dataGridView2.SelectedCells[0].RowIndex;
                var max = _actions.Count - 1;
                var test = _actions.FirstOrDefault(x => x.Name == (string)dataGridView2.Rows[index].Cells["Name"].Value && x.Order < _actions.Count - 1);
                if (test != null)
                {
                    test.Order++;
                    index++;
                    test = _actions.FirstOrDefault(x => index <= max && x.Name == (string)dataGridView2.Rows[index].Cells["Name"].Value && x.Order <= max);
                    if (test != null) test.Order--;
                }
                // Sort and update list
                var newlist = _actions.ToList();
                newlist.Sort((s1, s2) => s1.Order.CompareTo(s2.Order));
                _actions = new BindingList<Action>(newlist);
                UpdateGridview();
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteGlobal(ex.ToString());
            }
        }
       
        public void UpdateGridview()
        {
            dataGridView2.DataSource = _actions;
            dataGridView1.DataSource = AvailableActions;
            dataGridView1.Refresh();
            dataGridView2.Refresh();
            dataGridView1.Columns["Order"].Visible = false;
            dataGridView1.Columns["UniqueId"].Visible = false;
            dataGridView2.Columns["Order"].Visible = false;
            dataGridView2.Columns["UniqueId"].Visible = false;
            
            
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.MultiSelect = false;
            foreach (DataGridViewColumn column in dataGridView1.Columns)
                column.ReadOnly = true;
            dataGridView2.AllowUserToAddRows = false;
            dataGridView2.MultiSelect = false;
            foreach (DataGridViewColumn column in dataGridView2.Columns)
                column.ReadOnly = true;
        }
    }
}
