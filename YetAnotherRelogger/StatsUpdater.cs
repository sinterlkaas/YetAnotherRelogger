using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms.DataVisualization.Charting;
using YetAnotherRelogger.Helpers;
using YetAnotherRelogger.Helpers.Bot;
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
            prepareMainGraphConnections();
            prepareMainGraphGold();
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
                double goldPerHour = 0;
                foreach (var bot in BotSettings.Instance.Bots)
                {
                    var chartStats = bot.ChartStats;
                    // Update bot uptime
                    bot.RunningTime = bot.IsRunning ? DateTime.Now.Subtract(bot.StartTime).ToString(@"hh\hmm\mss\s") : "";

                    // Update bot specific Chart stats
                    CreateChartStats(bot, Program.Mainform.GoldStats, ChartValueType.Double);

                    if (bot.IsRunning)
                    {
                        #region Calculate System Usage
                        if (bot.Diablo.IsRunning)
                        {
                            // Calculate total Cpu/Ram usage for Diablo
                            try
                            {
                                var usage = usages.GetUsageById(bot.Diablo.Proc.Id);
                                diabloCpuUsage += usage.Cpu;
                                diabloRamUsage += usage.Memory;
                                diabloCount++;
                            }
                            catch
                            {
                            }
                        }

                        if (bot.Demonbuddy.IsRunning)
                        {
                            // Calculate total Cpu/Ram usage for Demonbuddy
                            try
                            {
                                var usage = usages.GetUsageById(bot.Demonbuddy.Proc.Id);
                                demonbuddyCpuUsage += usage.Cpu;
                                demonbuddyRamUsage += usage.Memory;
                                demonbuddyCount++;
                            }
                            catch
                            {
                            }

                        }
                        #endregion
                        #region Calculate Gold
                        // Calculate Gold
                        var coinage = bot.AntiIdle.Stats.Coinage;
                        if (coinage > 0)
                        {
                            if (chartStats.GoldPerHour.StartCoinage <= 0)
                            {
                                chartStats.GoldPerHour.StartCoinage = coinage;
                                chartStats.GoldPerHour.StartTime = DateTime.Now;
                            }
                            else if (coinage > chartStats.GoldPerHour.LastCoinage)
                            {
                                chartStats.GoldPerHour.LastUpdate = DateTime.Now;
                                chartStats.GoldPerHour.LastGain = (coinage - chartStats.GoldPerHour.LastCoinage);

                                if (chartStats.GoldPerHour.LastGain < 0)
                                {
                                    chartStats.GoldPerHour.StartCoinage = 0;
                                    continue;
                                }

                                var hours = DateTime.Now.Subtract(chartStats.GoldPerHour.StartTime).TotalSeconds/3600;
                                var botGph = Math.Round((coinage - chartStats.GoldPerHour.StartCoinage)/hours, 0);
                                
                                goldPerHour += botGph;
                                
                                var serie = Program.Mainform.GoldStats.Series.FirstOrDefault(x => x.Name == bot.Name);
                                if (serie != null)
                                {
                                    updateMainformGraph(Program.Mainform.GoldStats, serie.Name, botGph, limit:7200,autoscale:true);
                                }
                            }
                        }
                        #endregion
                    }
                    else
                    {

                    }
                }
                // Update Stats label on mainform
                /*
                 updateMainformStatsLabel(
                    string.Format("Cpu Usage : Diablo[{0}] {1}%, Demonbuddy[{2}] {3}%, Total {4}%" + Environment.NewLine + 
                                  "Ram Usage : Diablo[{0}] {5}Gb, Demonbuddy[{2}] {6}Mb, Total {7}Gb" + Environment.NewLine +
                                  "Connections: {8}",
                                  diabloCount, Math.Round(diabloCpuUsage,1), demonbuddyCount, Math.Round(demonbuddyCpuUsage,1), Math.Round(diabloCpuUsage+demonbuddyCpuUsage,1),
                                  Math.Round(diabloRamUsage / Math.Pow(2 , 30), 2), Math.Round(demonbuddyRamUsage / Math.Pow(2 , 20),2), Math.Round((diabloRamUsage / Math.Pow(2,30)) + (demonbuddyRamUsage / Math.Pow(2,30)), 2),
                                  Communicator.StatConnections));
                 */

                // add to Cpu graph
                
                var graph = Program.Mainform.CpuUsage;
                var allusage = diabloCpuUsage + demonbuddyCpuUsage;
                updateMainformGraph(graph, "All Usage", allusage, legend: string.Format("All usage: {0,11}%", allusage.ToString("000.0")));
                updateMainformGraph(graph, "Diablo", diabloCpuUsage, legend: string.Format("Diablo: {0,16}%", diabloCpuUsage.ToString("000.0")));
                updateMainformGraph(graph, "Demonbuddy", demonbuddyCpuUsage, legend: string.Format("Demonbuddy: {0,4}%", demonbuddyCpuUsage.ToString("000.0")));
                var cpu = cpuUsage.NextValue();
                updateMainformGraph(graph, "Total System", cpu, legend: string.Format("Total System: {0,2}%", cpu.ToString("000.0")));
                
                // add to Memory graph
                graph = Program.Mainform.MemoryUsage;
                allusage = (double) (diabloRamUsage + demonbuddyRamUsage)/totalRam*100;
                var diablousage = (double) diabloRamUsage/totalRam*100;
                var demonbuddyusage = (double) demonbuddyRamUsage/totalRam*100;
                updateMainformGraph(graph, "All Usage", allusage, legend: string.Format("All usage: {0,11}%", ((double)(diabloRamUsage + demonbuddyRamUsage) / totalRam * 100).ToString("000.0")));
                updateMainformGraph(graph, "Diablo", diablousage, legend: string.Format("Diablo: {0,16}%", diablousage.ToString("000.0")));
                updateMainformGraph(graph, "Demonbuddy", demonbuddyusage, legend: string.Format("Demonbuddy: {0,4}%", demonbuddyusage.ToString("000.0")));
                var mem = (double)PerformanceInfo.GetPhysicalUsedMemory()/totalRam*100;
                updateMainformGraph(graph, "Total System", mem, legend: string.Format("Total System: {0,2}%", mem.ToString("000.0")));

                // add to Connection graph
                updateMainformGraph(Program.Mainform.CommConnections,"Connections", Communicator.StatConnections, legend:string.Format("Connections {0}", Communicator.StatConnections), autoscale:true);
                Communicator.StatConnections = 0;
                
                // add to Gold Graph
                updateMainformGraph(Program.Mainform.GoldStats, "Gph", goldPerHour, legend: string.Format("Gph {0}", goldPerHour),autoscale:true,limit:7200);
                
                Thread.Sleep(500);
            }
        }

        #region Chart Stats Per Bot Creation
        private Color[] ChartColors = new Color[]
                                          {
                                              Color.DarkOrange,
                                              Color.DarkOrchid,
                                              Color.DarkRed,
                                              Color.DarkSeaGreen,
                                              Color.DeepPink,
                                              Color.DeepSkyBlue,
                                              Color.Goldenrod,
                                              Color.LimeGreen,
                                              Color.Magenta,
                                              Color.Red,
                                              Color.MidnightBlue,
                                              Color.Teal
                                          };
        private void CreateChartStats(BotClass bot, Chart graph, ChartValueType valueType = ChartValueType.Auto)
        {
            if (Program.Mainform != null && graph != null)
            {
                try
                {
                    Program.Mainform.Invoke(new Action(() =>
                    {
                        if (bot.IsRunning)
                        {
                            var serie = graph.Series.FirstOrDefault(x => x.Name == bot.Name);
                            if (serie == null)
                            {
                                // Add Series
                                graph.Series.Add(bot.Name);
                                graph.Series[bot.Name].ChartType = SeriesChartType.FastLine;
                                graph.Series[bot.Name].Points.Add(0);
                                graph.Series[bot.Name].YAxisType = AxisType.Primary;
                                graph.Series[bot.Name].YValueType = valueType;
                                graph.Series[bot.Name].IsXValueIndexed = false;
                                
                                foreach (var color in ChartColors)
                                {
                                    if (graph.Series.Any(x => x.Color != color))
                                    {
                                        graph.Series[bot.Name].Color = color;
                                        break;
                                    }
                                    graph.Series[bot.Name].Color = Color.Black;
                                }
                                graph.Series[bot.Name].Name = bot.Name;
                            }
                        }
                        else
                        {
                            var serie = graph.Series.FirstOrDefault(x => x.Name == bot.Name);
                            if (serie != null)
                                graph.Series.Remove(serie);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }
        #endregion
        #region Gold Stats
        private void prepareMainGraphGold()
        {
            if (Program.Mainform != null && Program.Mainform.GoldStats != null)
            {
                try
                {
                    Program.Mainform.Invoke(new Action(() =>
                    {
                        // Clear mainform stats
                        var graph = Program.Mainform.GoldStats;
                        graph.Series.Clear();
                        graph.Palette = ChartColorPalette.Pastel;
                        graph.Titles.Add("Gold Statistics");
                        // Add Series
                        graph.Series.Add("Gph");
                        graph.Series["Gph"].ChartType = SeriesChartType.FastLine;
                        graph.Series["Gph"].Points.Add(0);
                        graph.Series["Gph"].YAxisType = AxisType.Primary;
                        graph.Series["Gph"].YValueType = ChartValueType.Double;
                        graph.Series["Gph"].IsXValueIndexed = false;
                        graph.Series["Gph"].Color = Color.DarkSlateBlue;

                        graph.ResetAutoValues();
                        graph.ChartAreas[0].AxisY.Minimum = 0;
                        graph.ChartAreas[0].AxisX.Enabled = AxisEnabled.False;
                        graph.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
                    }));
                }
                catch
                {
                }
            }
        }
        #endregion
        
        #region Communicator connections
        private void prepareMainGraphConnections()
        {
            if (Program.Mainform != null && Program.Mainform.CommConnections != null)
            {
                try
                {
                    Program.Mainform.Invoke(new Action(() =>
                    {
                        // Clear mainform stats
                        var graph = Program.Mainform.CommConnections;
                        graph.Series.Clear();
                        graph.Palette = ChartColorPalette.Pastel;
                        graph.Titles.Add("Communicator Open Connections");
                        // Add Series
                        graph.Series.Add("Connections");
                        graph.Series["Connections"].ChartType = SeriesChartType.FastLine;
                        graph.Series["Connections"].Points.Add(0);
                        graph.Series["Connections"].YAxisType = AxisType.Primary;
                        graph.Series["Connections"].YValueType = ChartValueType.Int32;
                        graph.Series["Connections"].IsXValueIndexed = false;
                        graph.Series["Connections"].Color = Color.DarkSlateBlue;

                        graph.ResetAutoValues();
                        graph.ChartAreas[0].AxisY.Maximum = 255; //Max Y 
                        graph.ChartAreas[0].AxisY.Minimum = 0;
                        graph.ChartAreas[0].AxisX.Enabled = AxisEnabled.False;
                        graph.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
                    }));
                }
                catch
                {
                }
            }
        }
        #endregion
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
                        var graph = Program.Mainform.CpuUsage;
                        graph.Series.Clear();
                        graph.Palette = ChartColorPalette.Pastel;
                        graph.Titles.Add("Processor Usage");
                        // Add Series
                        graph.Series.Add("All Usage");
                        graph.Series["All Usage"].ChartType = SeriesChartType.FastLine;
                        graph.Series["All Usage"].Points.Add(0);
                        graph.Series["All Usage"].YAxisType = AxisType.Primary;
                        graph.Series["All Usage"].YValueType = ChartValueType.Double;
                        graph.Series["All Usage"].IsXValueIndexed = false;
                        graph.Series["All Usage"].Color = Color.DarkSlateBlue;

                        graph.Series.Add("Demonbuddy");
                        graph.Series["Demonbuddy"].ChartType = SeriesChartType.FastLine;
                        graph.Series["Demonbuddy"].Points.Add(0);
                        graph.Series["Demonbuddy"].YAxisType = AxisType.Primary;
                        graph.Series["Demonbuddy"].YValueType = ChartValueType.Double;
                        graph.Series["Demonbuddy"].IsXValueIndexed = false;
                        graph.Series["Demonbuddy"].Color = Color.Red;

                        graph.Series.Add("Diablo");
                        graph.Series["Diablo"].ChartType = SeriesChartType.FastLine;
                        graph.Series["Diablo"].Points.Add(0);
                        graph.Series["Diablo"].YAxisType = AxisType.Primary;
                        graph.Series["Diablo"].YValueType = ChartValueType.Double;
                        graph.Series["Diablo"].IsXValueIndexed = false;
                        graph.Series["Diablo"].Color = Color.Green;

                        graph.Series.Add("Total System");
                        graph.Series["Total System"].ChartType = SeriesChartType.FastLine;
                        graph.Series["Total System"].Points.Add(0);
                        graph.Series["Total System"].YAxisType = AxisType.Primary;
                        graph.Series["Total System"].YValueType = ChartValueType.Double;
                        graph.Series["Total System"].IsXValueIndexed = false;
                        graph.Series["Total System"].Color = Color.SpringGreen;

                        graph.ResetAutoValues();

                        graph.ChartAreas[0].AxisY.Maximum = 100; //Max Y 
                        graph.ChartAreas[0].AxisY.Minimum = 0;
                        graph.ChartAreas[0].AxisX.Enabled = AxisEnabled.False;
                        graph.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
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
                        var graph = Program.Mainform.MemoryUsage;
                        graph.Series.Clear();
                        graph.Palette = ChartColorPalette.Pastel;
                        graph.Titles.Add("Memory Usage");
                        // Add Series
                        graph.Series.Add("All Usage");
                        graph.Series["All Usage"].ChartType = SeriesChartType.FastLine;
                        graph.Series["All Usage"].Points.Add(0);
                        graph.Series["All Usage"].YAxisType = AxisType.Primary;
                        graph.Series["All Usage"].YValueType = ChartValueType.Double;
                        graph.Series["All Usage"].IsXValueIndexed = false;
                        graph.Series["All Usage"].Color = Color.DarkSlateBlue;

                        graph.Series.Add("Demonbuddy");
                        graph.Series["Demonbuddy"].ChartType = SeriesChartType.FastLine;
                        graph.Series["Demonbuddy"].Points.Add(0);
                        graph.Series["Demonbuddy"].YAxisType = AxisType.Primary;
                        graph.Series["Demonbuddy"].YValueType = ChartValueType.Double;
                        graph.Series["Demonbuddy"].IsXValueIndexed = false;
                        graph.Series["Demonbuddy"].Color = Color.Red;

                        graph.Series.Add("Diablo");
                        graph.Series["Diablo"].ChartType = SeriesChartType.FastLine;
                        graph.Series["Diablo"].Points.Add(0);
                        graph.Series["Diablo"].YAxisType = AxisType.Primary;
                        graph.Series["Diablo"].YValueType = ChartValueType.Double;
                        graph.Series["Diablo"].IsXValueIndexed = false;
                        graph.Series["Diablo"].Color = Color.Green;

                        graph.Series.Add("Total System");
                        graph.Series["Total System"].ChartType = SeriesChartType.FastLine;
                        graph.Series["Total System"].Points.Add(0);
                        graph.Series["Total System"].YAxisType = AxisType.Primary;
                        graph.Series["Total System"].YValueType = ChartValueType.Double;
                        graph.Series["Total System"].IsXValueIndexed = false;
                        graph.Series["Total System"].Color = Color.SpringGreen;

                        graph.ResetAutoValues();

                        graph.ChartAreas[0].AxisY.Maximum = 100; //Max Y 
                        graph.ChartAreas[0].AxisY.Minimum = 0;
                        graph.ChartAreas[0].AxisX.Enabled = AxisEnabled.False;
                        graph.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
                    }));
                }
                catch
                {
                }
            }
        }
        #endregion

        private void updateMainformGraph(Chart graph, string serie, double value, int limit=120, string legend = null,bool autoscale = false)
        {
            if (Program.Mainform != null && graph != null)
            {
                try
                {
                    Program.Mainform.Invoke(new Action(() =>
                    {
                        try
                        {
                            if (legend != null) graph.Series[serie].LegendText = legend;

                            while(graph.Series[serie].Points.Count < limit)
                                graph.Series[serie].Points.Add(0);
                            graph.Series[serie].Points.Add(value);
                            if (graph.Series[serie].Points.Count > limit)
                                graph.Series[serie].Points.RemoveAt(0);
                            if (autoscale)
                            {
                                graph.ChartAreas[0].AxisY.Minimum = Double.NaN;
                                graph.ChartAreas[0].AxisY.Maximum = Double.NaN;
                                graph.ChartAreas[0].RecalculateAxesScale();
                            }
                        }
                        catch (Exception ex)
                        {// Suppress exceptions
                            Debug.WriteLine(ex);
                        }
                    }));
                }
                catch
                {// Suppress exceptions
                }
            }
        }
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
