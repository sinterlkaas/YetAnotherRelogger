using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms.DataVisualization.Charting;
using YetAnotherRelogger.Helpers;
using YetAnotherRelogger.Helpers.Stats;
using YetAnotherRelogger.Helpers.Tools;

namespace YetAnotherRelogger
{
    public sealed class StatsUpdater
    {
        #region singleton
        static readonly StatsUpdater instance = new StatsUpdater();

        static StatsUpdater()
        {
        }

        StatsUpdater()
        {
        }

        public static StatsUpdater Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion
        private Thread _statsUpdater;
        public void Start()
        {
            _statsUpdater = new Thread(new ThreadStart(StatsUpdaterWorker)) { IsBackground = true };
            _statsUpdater.Start();
        }

        public void Stop()
        {
            _statsUpdater.Abort();
        }

        public void StatsUpdaterWorker()
        {
            var usages = new CpuRamUsage();
            var cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var totalRam = PerformanceInfo.GetTotalMemory();
            
            prepareMainGraphCpu();
            prepareMainGraphMemory();
            while (true)
            {
                // Update Cpu/Ram Usage
                usages.Update();

                double diabloCpuUsage = 0;
                long diabloRamUsage = 0;
                int diabloCount = 0;
                double demonbuddyCpuUsage = 0;
                long demonbuddyRamUsage = 0;
                int demonbuddyCount = 0;
                
                foreach (var bot in BotSettings.Instance.Bots)
                {
                    // Update bot uptime
                    bot.RunningTime = bot.IsRunning ? DateTime.Now.Subtract(bot.StartTime).ToString(@"hh\hmm\mss\s") : "";


                    // Calculate total Cpu/Ram usage for Diablo
                    if (bot.Diablo.IsRunning)
                    {
                        try
                        {
                            var usage = usages.GetUsageById(bot.Diablo.Proc.Id);
                            diabloCpuUsage += usage.Cpu;
                            diabloRamUsage += usage.Memory;
                            diabloCount++;
                        }
                        catch
                        {}
                    }
                    // Calculate total Cpu/Ram usage for Demonbuddy
                    if (bot.Demonbuddy.IsRunning)
                    {
                        try
                        {
                            var usage = usages.GetUsageById(bot.Demonbuddy.Proc.Id);
                            demonbuddyCpuUsage += usage.Cpu;
                            demonbuddyRamUsage += usage.Memory;
                            demonbuddyCount++;
                        }
                        catch
                        {}
                    }

                    
                }
                // Update Stats label on mainform
                updateMainformStatsLabel(
                    string.Format("Cpu Usage : Diablo[{0}] {1}%, Demonbuddy[{2}] {3}%, Total {4}%" + Environment.NewLine + 
                                  "Ram Usage : Diablo[{0}] {5}Gb, Demonbuddy[{2}] {6}Mb, Total {7}Gb",
                                  diabloCount, Math.Round(diabloCpuUsage,1), demonbuddyCount, Math.Round(demonbuddyCpuUsage,1), Math.Round(diabloCpuUsage+demonbuddyCpuUsage,1),
                                  Math.Round(diabloRamUsage / Math.Pow(2 , 30), 2), Math.Round(demonbuddyRamUsage / Math.Pow(2 , 20),2), Math.Round((diabloRamUsage / Math.Pow(2,30)) + (demonbuddyRamUsage / Math.Pow(2,30)), 2)));
                // Add point to Graph
                updateMainGraphCpu("All Usage",diabloCpuUsage + demonbuddyCpuUsage);
                updateMainGraphCpu("Diablo", diabloCpuUsage);
                updateMainGraphCpu("Demonbuddy", demonbuddyCpuUsage);
                updateMainGraphCpu("Total System", cpuUsage.NextValue());

                updateMainGraphMemory("All Usage", (double)(diabloRamUsage + demonbuddyRamUsage) / totalRam * 100);
                updateMainGraphMemory("Diablo", (double)diabloRamUsage / totalRam * 100);
                updateMainGraphMemory("Demonbuddy", (double)demonbuddyRamUsage / totalRam * 100);
                updateMainGraphMemory("Total System", (double)PerformanceInfo.GetPhysicalUsedMemory() / totalRam * 100);
               
                Thread.Sleep(500);
            }
        }

        #region CPU Graph
        private void prepareMainGraphCpu()
        {
            if (Program.Mainform != null && Program.Mainform.CpuUsage != null)
            {
                try
                {
                    Program.Mainform.Invoke(new Action(() =>
                    {
                        // Clear mainform stats
                        var mainCpuGraph = Program.Mainform.CpuUsage;
                        mainCpuGraph.Series.Clear();
                        mainCpuGraph.Palette = ChartColorPalette.Pastel;
                        //mainCpuGraph.Titles.Add("CPU Usage");
                        // Add Series
                        mainCpuGraph.Series.Add("All Usage");
                        mainCpuGraph.Series["All Usage"].ChartType = SeriesChartType.FastLine;
                        mainCpuGraph.Series["All Usage"].Points.Add(0);
                        mainCpuGraph.Series["All Usage"].YAxisType = AxisType.Primary;
                        mainCpuGraph.Series["All Usage"].YValueType = ChartValueType.Double;
                        mainCpuGraph.Series["All Usage"].IsXValueIndexed = false;
                        mainCpuGraph.Series["All Usage"].Color = Color.DarkSlateBlue;

                        mainCpuGraph.Series.Add("Demonbuddy");
                        mainCpuGraph.Series["Demonbuddy"].ChartType = SeriesChartType.FastLine;
                        mainCpuGraph.Series["Demonbuddy"].Points.Add(0);
                        mainCpuGraph.Series["Demonbuddy"].YAxisType = AxisType.Primary;
                        mainCpuGraph.Series["Demonbuddy"].YValueType = ChartValueType.Double;
                        mainCpuGraph.Series["Demonbuddy"].IsXValueIndexed = false;
                        mainCpuGraph.Series["Demonbuddy"].Color = Color.Red;

                        mainCpuGraph.Series.Add("Diablo");
                        mainCpuGraph.Series["Diablo"].ChartType = SeriesChartType.FastLine;
                        mainCpuGraph.Series["Diablo"].Points.Add(0);
                        mainCpuGraph.Series["Diablo"].YAxisType = AxisType.Primary;
                        mainCpuGraph.Series["Diablo"].YValueType = ChartValueType.Double;
                        mainCpuGraph.Series["Diablo"].IsXValueIndexed = false;
                        mainCpuGraph.Series["Diablo"].Color = Color.Green;

                        mainCpuGraph.Series.Add("Total System");
                        mainCpuGraph.Series["Total System"].ChartType = SeriesChartType.FastLine;
                        mainCpuGraph.Series["Total System"].Points.Add(0);
                        mainCpuGraph.Series["Total System"].YAxisType = AxisType.Primary;
                        mainCpuGraph.Series["Total System"].YValueType = ChartValueType.Double;
                        mainCpuGraph.Series["Total System"].IsXValueIndexed = false;
                        mainCpuGraph.Series["Total System"].Color = Color.SpringGreen;

                        mainCpuGraph.ResetAutoValues();

                        mainCpuGraph.ChartAreas[0].AxisY.Maximum = 100; //Max Y 
                        mainCpuGraph.ChartAreas[0].AxisY.Minimum = 0;
                        mainCpuGraph.ChartAreas[0].AxisX.Enabled = AxisEnabled.False;
                        mainCpuGraph.ChartAreas[0].AxisY.Title = "CPU usage %";
                        mainCpuGraph.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
                    }));
                }
                catch
                {
                }
            }
        }
        private void updateMainGraphCpu(string serie, double value)
        {
            if (Program.Mainform != null && Program.Mainform.CpuUsage != null)
            {
                try
                {
                    Program.Mainform.Invoke(new Action(() =>
                    {
                        var mainCpuGraph = Program.Mainform.CpuUsage;
                        mainCpuGraph.Series[serie].Points.AddY(value);
                        if (mainCpuGraph.Series[serie].Points.Count > 120)
                            mainCpuGraph.Series[serie].Points.RemoveAt(0);
                    }));
                }
                catch
                {
                }
            }
        }
        #endregion
        #region Memory Graph
        private void prepareMainGraphMemory()
        {
            if (Program.Mainform != null && Program.Mainform.MemoryUsage != null)
            {
                try
                {
                    Program.Mainform.Invoke(new Action(() =>
                    {

                        // Clear mainform stats
                        var mainMemoryGraph = Program.Mainform.MemoryUsage;
                        mainMemoryGraph.Series.Clear();
                        mainMemoryGraph.Palette = ChartColorPalette.Pastel;
                        // Add Series
                        mainMemoryGraph.Series.Add("All Usage");
                        mainMemoryGraph.Series["All Usage"].ChartType = SeriesChartType.FastLine;
                        mainMemoryGraph.Series["All Usage"].Points.Add(0);
                        mainMemoryGraph.Series["All Usage"].YAxisType = AxisType.Primary;
                        mainMemoryGraph.Series["All Usage"].YValueType = ChartValueType.Double;
                        mainMemoryGraph.Series["All Usage"].IsXValueIndexed = false;
                        mainMemoryGraph.Series["All Usage"].Color = Color.DarkSlateBlue;

                        mainMemoryGraph.Series.Add("Demonbuddy");
                        mainMemoryGraph.Series["Demonbuddy"].ChartType = SeriesChartType.FastLine;
                        mainMemoryGraph.Series["Demonbuddy"].Points.Add(0);
                        mainMemoryGraph.Series["Demonbuddy"].YAxisType = AxisType.Primary;
                        mainMemoryGraph.Series["Demonbuddy"].YValueType = ChartValueType.Double;
                        mainMemoryGraph.Series["Demonbuddy"].IsXValueIndexed = false;
                        mainMemoryGraph.Series["Demonbuddy"].Color = Color.Red;

                        mainMemoryGraph.Series.Add("Diablo");
                        mainMemoryGraph.Series["Diablo"].ChartType = SeriesChartType.FastLine;
                        mainMemoryGraph.Series["Diablo"].Points.Add(0);
                        mainMemoryGraph.Series["Diablo"].YAxisType = AxisType.Primary;
                        mainMemoryGraph.Series["Diablo"].YValueType = ChartValueType.Double;
                        mainMemoryGraph.Series["Diablo"].IsXValueIndexed = false;
                        mainMemoryGraph.Series["Diablo"].Color = Color.Green;

                        mainMemoryGraph.Series.Add("Total System");
                        mainMemoryGraph.Series["Total System"].ChartType = SeriesChartType.FastLine;
                        mainMemoryGraph.Series["Total System"].Points.Add(0);
                        mainMemoryGraph.Series["Total System"].YAxisType = AxisType.Primary;
                        mainMemoryGraph.Series["Total System"].YValueType = ChartValueType.Double;
                        mainMemoryGraph.Series["Total System"].IsXValueIndexed = false;
                        mainMemoryGraph.Series["Total System"].Color = Color.SpringGreen;

                        mainMemoryGraph.ResetAutoValues();

                        mainMemoryGraph.ChartAreas[0].AxisY.Maximum = 100; //Max Y 
                        mainMemoryGraph.ChartAreas[0].AxisY.Minimum = 0;
                        mainMemoryGraph.ChartAreas[0].AxisX.Enabled = AxisEnabled.False;
                        mainMemoryGraph.ChartAreas[0].AxisY.Title = "Memory usage %";
                        mainMemoryGraph.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
                    }));
                }
                catch
                {
                }
            }
        }
        private void updateMainGraphMemory(string serie, double value)
        {
            if (Program.Mainform != null && Program.Mainform.MemoryUsage != null)
            {
                try
                {
                    Program.Mainform.Invoke(new Action(() =>
                    {
                        var mainMemoryGraph = Program.Mainform.MemoryUsage;
                        mainMemoryGraph.Series[serie].Points.AddY(value);
                        if (mainMemoryGraph.Series[serie].Points.Count > 120)
                            mainMemoryGraph.Series[serie].Points.RemoveAt(0);
                    }));
                }
                catch
                {
                }
            }
        }
        #endregion
        private void updateMainformStatsLabel(string text)
        {
            if (Program.Mainform != null && Program.Mainform.labelStats != null)
            {
                try
                {
                    Program.Mainform.Invoke(new Action(() => Program.Mainform.labelStats.Text = text));
                }
                catch
                {
                    // Failed! do nothing
                }
            }
        }
    }
}
