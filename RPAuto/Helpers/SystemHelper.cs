using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RPAuto.Helpers
{
    public class SystemHelper
    {
        public static IEnumerable<Process> GetApplicationProcesses()
        {
            return Process.GetProcesses()
                   .Where(p => p.MainWindowHandle != (IntPtr)0);
        }

        public static IEnumerable<string> GetApplicationProcessNames(ProcessReturnMode mode = ProcessReturnMode.Full)
        {
            var procs = GetApplicationProcesses();

            switch (mode)
            {
                case ProcessReturnMode.OnlyName:
                    return procs.Select(s => s.ProcessName).OrderBy(o => o);
                case ProcessReturnMode.OnlyDescription:
                    return procs.Select(s => s.MainWindowTitle).OrderBy(o => o);
                case ProcessReturnMode.Full:
                    return procs.Select(s => $"{s.ProcessName} ({s.MainWindowTitle})").OrderBy(o => o);
                default:
                    return null;
            }
        }

        public static Process FindProccessByDescription(string description)
        {
            var allprocs = GetApplicationProcesses();
            var proc = allprocs.FirstOrDefault(s => $"{s.ProcessName} ({s.MainWindowTitle})" == description);
            
            return proc != null ? proc : allprocs.FirstOrDefault(s => s.ProcessName.StartsWith(description.Split('(').First().Trim()));
        }

        public static void BringToFront(Process process)
        {
            BringToFront(process.MainWindowHandle);
        }

        public static void BringToFront(string mainWindowTitle)
        {
            BringToFront(FindWindow(null, mainWindowTitle));
        }

        private static void BringToFront(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return;

            if (IsIconic(handle))
                ShowWindow(handle, SW_RESTORE);

            SetForegroundWindow(handle);
        }

        #region Windows Directives
        const int SW_RESTORE = 9;

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(String lpClassName, String lpWindowName);

        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int nCmdShow);
        [DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);
        #endregion
    }

    public enum ProcessReturnMode
    {
        OnlyName,
        OnlyDescription,
        Full
    }
}
