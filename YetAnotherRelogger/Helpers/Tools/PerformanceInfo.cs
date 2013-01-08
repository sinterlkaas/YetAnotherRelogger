using System;
using System.Runtime.InteropServices;

namespace YetAnotherRelogger.Helpers.Tools
{
    public static class PerformanceInfo
    {
        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPerformanceInfo([Out] out PerformanceInformation performanceInformation, [In] int size);

        [StructLayout(LayoutKind.Sequential)]
        public struct PerformanceInformation
        {
            public int Size;
            public IntPtr CommitTotal;
            public IntPtr CommitLimit;
            public IntPtr CommitPeak;
            public IntPtr PhysicalTotal;
            public IntPtr PhysicalAvailable;
            public IntPtr SystemCache;
            public IntPtr KernelTotal;
            public IntPtr KernelPaged;
            public IntPtr KernelNonPaged;
            public IntPtr PageSize;
            public int HandlesCount;
            public int ProcessCount;
            public int ThreadCount;
        }

        public static Int64 GetPhysicalAvailableMemory()
        {
            try
            {
                var pi = new PerformanceInformation();
                if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
                {
                    return Convert.ToInt64((pi.PhysicalAvailable.ToInt64()*pi.PageSize.ToInt64()));
                }
            }
            catch (Exception ex)
            {
                DebugHelper.Exception(ex);
            }
            return -1;

        }
        public static Int64 GetPhysicalUsedMemory()
        {
            try
            {
                var pi = new PerformanceInformation();
                if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
                {
                    return Convert.ToInt64(GetTotalMemory() - (pi.PhysicalAvailable.ToInt64()*pi.PageSize.ToInt64()));
                }
            }
            catch (Exception ex)
            {
                DebugHelper.Exception(ex);
            }
            return -1;
        }

        public static Int64 GetTotalMemory()
        {
            try
            {
                var pi = new PerformanceInformation();
                if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
                {
                    return Convert.ToInt64((pi.PhysicalTotal.ToInt64()*pi.PageSize.ToInt64()));
                }
            }
            catch (Exception ex)
            {
                DebugHelper.Exception(ex);
            }
            return -1;
        }
    }
}
