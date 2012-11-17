/* http://www.pinvoke.net/default.aspx/user32/EnumWindows.html */
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace YetAnotherRelogger.Helpers.Tools
{
    public static class FindWindow
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, ref SearchData data);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, ref SearchData data);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private class SearchData
        {
            public string SearchString;
            public IntPtr Handle;
            public int ParentId;
        }

        private static bool EnumProcClass(IntPtr hWnd, ref SearchData data)
        {
            uint procId;
            GetWindowThreadProcessId(hWnd, out procId);
            if (data.ParentId != (int) procId) return true;

            var sb = new StringBuilder(1024);
            GetClassName(hWnd, sb, sb.Capacity);
            if (sb.ToString().StartsWith(data.SearchString))
            {
                data.Handle = hWnd;
                return false; // Found our window; Halt enumeration
            }
            return true; // Continue .. enumeration
        }

        private static bool EnumProcContainsClass(IntPtr hWnd, ref SearchData data)
        {
            uint procId;
            GetWindowThreadProcessId(hWnd, out procId);
            if (data.ParentId != (int) procId) return true;

            var sb = new StringBuilder(1024);
            GetClassName(hWnd, sb, sb.Capacity);
            if (sb.ToString().Contains(data.SearchString))
            {
                data.Handle = hWnd;
                return false; // Found our window; Halt enumeration
            }
            return true; // Continue .. enumeration
        }

        private static bool EnumProcCaption(IntPtr hWnd, ref SearchData data)
        {
            uint procId;
            GetWindowThreadProcessId(hWnd, out procId);
            if (data.ParentId != (int) procId) return true;

            var sb = new StringBuilder(1024);
            GetWindowText(hWnd, sb, sb.Capacity);
            if (sb.ToString().StartsWith(data.SearchString))
            {
                data.Handle = hWnd;
                return false; // Found the wnd, halt enumeration
            }
            return true;
        }

        private static bool EnumProcContainsCaption(IntPtr hWnd, ref SearchData data)
        {
            uint procId;
            GetWindowThreadProcessId(hWnd, out procId);
            if (data.ParentId != (int) procId) return true;

            var sb = new StringBuilder(1024);
            GetWindowText(hWnd, sb, sb.Capacity);
            if (sb.ToString().Contains(data.SearchString))
            {
                data.Handle = hWnd;
                return false; // Found the wnd, halt enumeration
            }
            return true;
        }
        private static bool EnumProcCaptionEquals(IntPtr hWnd, ref SearchData data)
        {
            uint procId;
            GetWindowThreadProcessId(hWnd, out procId);
            if (data.ParentId != (int)procId) return true;

            var sb = new StringBuilder(1024);
            GetWindowText(hWnd, sb, sb.Capacity);
            if (sb.ToString().Equals(data.SearchString))
            {
                data.Handle = hWnd;
                return false; // Found the wnd, halt enumeration
            }
            return true;
        }
        public static IntPtr FindWindowClass(string search, int pid)
        {
            var sd = new SearchData {SearchString = search, ParentId = pid};
            EnumWindows(new EnumWindowsProc(EnumProcClass), ref sd);
            return sd.Handle;
        }

        public static IntPtr WindowContainsClass(string search, int pid)
        {
            var sd = new SearchData {SearchString = search, ParentId = pid};
            EnumWindows(new EnumWindowsProc(EnumProcContainsClass), ref sd);
            return sd.Handle;
        }

        public static IntPtr FindWindowCaption(string search, int pid)
        {
            var sd = new SearchData {SearchString = search, ParentId = pid};
            EnumWindows(new EnumWindowsProc(EnumProcCaption), ref sd);
            return sd.Handle;
        }

        public static IntPtr EqualsWindowCaption(string search, int pid)
        {
            var sd = new SearchData { SearchString = search, ParentId = pid };
            EnumWindows(new EnumWindowsProc(EnumProcCaptionEquals), ref sd);
            return sd.Handle;
        }

        public static IntPtr FindWindowContainsCaption(string search, int pid)
        {
            var sd = new SearchData {SearchString = search, ParentId = pid};
            EnumWindows(new EnumWindowsProc(EnumProcContainsCaption), ref sd);
            return sd.Handle;
        }
    }
}

