using System.Diagnostics;
using System.Linq;

namespace Utilities
{
    public static class ProcessSupport
    {
        public static bool IsProcessRunning(string nameWithoutExtension)
        {
            if (Process.GetProcessesByName(nameWithoutExtension).Length > 0)
                return true;
            else return false;
        }

        public static void StartProcess(string processFilePath)
        {
            Process.Start(processFilePath);
        }

        public static void KillProcesses(string[] namesWithoutExtension)
        {
            foreach (string processName in namesWithoutExtension)
                KillProcess(processName);
        }

        public static void KillProcess(string nameWithoutExtension)
        {
            if (string.IsNullOrEmpty(nameWithoutExtension))
                return;

            foreach (Process prs in Process.GetProcesses().Where(x => x.ProcessName.ToLowerInvariant() == nameWithoutExtension.ToLowerInvariant()))
                prs.Kill();
        }

        public static void KillProcess(int pid)
        {
            if (pid == 0)
                return;

            foreach (Process prs in Process.GetProcesses().Where(x => x.Id == pid))
                prs.Kill();
        }
    }
}
