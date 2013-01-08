using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using YetAnotherRelogger.Helpers;
using YetAnotherRelogger.Properties;
using AutoPos = YetAnotherRelogger.Helpers.AutoPosition;

namespace YetAnotherRelogger.Forms.SettingsTree
{
    public partial class AutoPosition : UserControl
    {
        public AutoPosition()
        {
            InitializeComponent();
            dataGridView1.CellValueChanged += new DataGridViewCellEventHandler(dataGridView1_CellValueChanged);
        }

        void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dataGridView1.Columns["Enabled"].Index && dataGridView1.SelectedCells.Count > 0)
            {
                try
                {
                    Settings.Default.AutoPosScreens.FirstOrDefault(x => x != null && x.Order == dataGridView1.SelectedCells[0].RowIndex).Enabled = (bool)dataGridView1.Rows[dataGridView1.SelectedCells[0].RowIndex].Cells["Enabled"].Value;
                }
                catch
                {
                }
            }
        }

        private void AutoPosition_Load(object sender, EventArgs e)
        {
            toggleStuff(false);
            updateGridView();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            toggleStuff(false);
            Settings.Default.AutoPosDemonbuddyCascade = radioButton2.Checked;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            toggleStuff(true);
            Settings.Default.AutoPosDemonbuddyCascade = !radioButton1.Checked;
        }


        private void toggleStuff(bool enable)
        {
            radioButton3.Enabled = enable;
            radioButton4.Enabled = enable;
            radioButton5.Enabled = enable;
            radioButton6.Enabled = enable;
            radioButton7.Enabled = enable;
            radioButton8.Enabled = enable;
            label1.Enabled = !enable;
            label8.Enabled = !enable;
            label9.Enabled = !enable;
            label10.Enabled = !enable;
            label11.Enabled = !enable;
            label12.Enabled = !enable;
            numericUpDown5.Enabled = !enable;
            numericUpDown6.Enabled = !enable;
            numericUpDown7.Enabled = !enable;
            numericUpDown8.Enabled = !enable;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Helpers.AutoPosition.PositionWindows();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Helpers.AutoPosition.UpdateScreens();
            updateGridView();
        }

        private void updateGridView()
        {
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = Settings.Default.AutoPosScreens;
            if (dataGridView1.DataSource == null) return;
            //dataGridView1.Columns["Order"].Visible = false;
            dataGridView1.Columns["Bounds"].Visible = false;
            dataGridView1.Columns["WorkingArea"].Visible = false;
            dataGridView1.Columns["DisplayDevice"].Visible = false;

            dataGridView1.Columns["Enabled"].Width = 48;
            dataGridView1.Columns["Order"].Width = 25;
        }

        private void button1_Click(object sender, EventArgs e)
        { // UP
            if (dataGridView1.SelectedCells.Count <= 0 && dataGridView1.SelectedCells[0].RowIndex <= 0) return;
            try
            {
                var index = dataGridView1.SelectedCells[0].RowIndex;
                var test = Settings.Default.AutoPosScreens.FirstOrDefault(x => x.Name == (string)dataGridView1.Rows[index].Cells["Name"].Value && x.Order > 0);
                if (test != null)
                {
                    test.Order--;
                    index--;
                    test = Settings.Default.AutoPosScreens.FirstOrDefault(x => index >= 0 && x.Name == (string) dataGridView1.Rows[index].Cells["Name"].Value && x.Order >= 0);
                    if (test != null) test.Order++;
                }
                // Sort and update list
                Settings.Default.AutoPosScreens.Sort((s1, s2) => s1.Order.CompareTo(s2.Order)); 
                updateGridView();
            }
            catch (Exception ex)
            {
                DebugHelper.Exception(ex);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        { // Down
            if (dataGridView1.SelectedCells.Count <= 0 && dataGridView1.SelectedCells[0].RowIndex <= 0) return;
            try
            {
                var index = dataGridView1.SelectedCells[0].RowIndex;
                var max = Settings.Default.AutoPosScreens.Count - 1;
                var test = Settings.Default.AutoPosScreens.FirstOrDefault(x => x.Name == (string)dataGridView1.Rows[index].Cells["Name"].Value && x.Order < Settings.Default.AutoPosScreens.Count - 1);
                if (test != null)
                {
                    test.Order++;
                    index++;
                    test = Settings.Default.AutoPosScreens.FirstOrDefault(x => index <= max && x.Name == (string)dataGridView1.Rows[index].Cells["Name"].Value && x.Order <= max);
                    if (test != null) test.Order--;
                }
                // Sort and update list
                Settings.Default.AutoPosScreens.Sort((s1, s2) => s1.Order.CompareTo(s2.Order));
                updateGridView();
            }
            catch (Exception ex)
            {
                DebugHelper.Exception(ex);
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.AutoPosDiabloNoFrame = checkBox3.Checked;
        }
    }
}
