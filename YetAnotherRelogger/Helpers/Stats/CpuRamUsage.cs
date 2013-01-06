/* http://www.philosophicalgeek.com/2009/01/03/determine-cpu-usage-of-current-process-c-and-c/ */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace YetAnotherRelogger.Helpers.Stats
{
    public class CpuRamUsage
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetSystemTimes(out ComTypes.FILETIME lpIdleTime, out ComTypes.FILETIME lpKernelTime, out ComTypes.FILETIME lpUserTime);

        private ComTypes.FILETIME _lastSysKernel;
        private ComTypes.FILETIME _lastSysUser;
        private ComTypes.FILETIME _lastSysIdle;
        private HashSet<ProcUsage> _procUsageList;
        public double TotalCpuUsage { get; private set; }

        public CpuRamUsage()
        {
            TotalCpuUsage = 0;
            _lastSysKernel.dwHighDateTime = _lastSysKernel.dwLowDateTime = 0;
            _lastSysUser.dwHighDateTime = _lastSysUser.dwLowDateTime = 0;
            _lastSysIdle.dwHighDateTime = _lastSysIdle.dwLowDateTime = 0;
            _procUsageList = new HashSet<ProcUsage>();
        }

        public bool Update()
        {
            ComTypes.FILETIME sysIdle, sysKernel, sysUser;

            // Check if we can get current system cpu times
            if (!GetSystemTimes(out sysIdle, out sysKernel, out sysUser))
                return false;

            // Calculate tot system cpu time
            var sysKernelDiff = SubtractTimes(sysKernel, _lastSysKernel);
            var sysUserDiff = SubtractTimes(sysUser, _lastSysUser);
            var sysIdleDiff = SubtractTimes(sysIdle, _lastSysIdle);
            var sysTotal = sysKernelDiff + sysUserDiff;
            // Calculate total Cpu usage
            var totalUsage = sysTotal > 0 ? 100 - ((100d * sysIdleDiff) / sysTotal) : TotalCpuUsage;
            TotalCpuUsage = totalUsage < 0 ? 0 : totalUsage;

            var newList = new HashSet<ProcUsage>();
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    // Skip proc with id 0
                    if (proc.Id == 0) continue;

                    Int64 procTotal;
                    double usage;
                    var p = GetById(proc.Id);
                    if (p != null)
                    {
                        procTotal = proc.TotalProcessorTime.Ticks - p.LastProcTime.Ticks;
                        usage = sysTotal > 0 ?((100.0*procTotal)/sysTotal) : p.Usage.Cpu;
                    }
                    else
                    {
                        procTotal = 0;
                        usage = sysTotal > 0 ?((100.0*procTotal)/sysTotal) : 0;
                    }

                    // Add Process to list
                    newList.Add(new ProcUsage
                    {
                        Process = proc,
                        Usage = new Usage
                        {
                            Cpu = usage, // Calculate process CPU Usage
                            Memory = proc.PrivateMemorySize64
                        },
                        LastProcTime = proc.TotalProcessorTime
                    });
                }
                catch
                {
                    continue;
                }
                Thread.Sleep(1); // be nice for cpu
            }

            // Update last system times
            _lastSysKernel = sysKernel;
            _lastSysUser = sysUser;
            _lastSysIdle = sysIdle;

            // Update Process list
            _procUsageList = newList;
            return true;
        }

        public ProcUsage GetById(int id)
        {
            try
            {
                return _procUsageList.FirstOrDefault(x => x.Process.Id == id);
            }
            catch
            {
                return null;
            }

        }

        /// <summary>
        /// Get Process CPU Usage
        /// </summary>
        /// <param name="id">Process Id</param>
        /// <returns>Cpu usage</returns>
        public Usage GetUsageById(int id)
        {
            var p = GetById(id);
            return p != null ? p.Usage : null;
        }

        private static UInt64 SubtractTimes(ComTypes.FILETIME a, ComTypes.FILETIME b)
        {
            var aInt = ((UInt64)(a.dwHighDateTime << 32)) | (UInt64)a.dwLowDateTime;
            var bInt = ((UInt64)(b.dwHighDateTime << 32)) | (UInt64)b.dwLowDateTime;
            return aInt - bInt;
        }

        public class ProcUsage
        {
            public ProcUsage()
            {
                LastProcTime = TimeSpan.MinValue;
                Process = new Process();
            }
            public TimeSpan LastProcTime;
            public Process Process;
            public Usage Usage;
        }

        public class Usage
        {
            public double Cpu;
            public long Memory;
        }
    }
}
