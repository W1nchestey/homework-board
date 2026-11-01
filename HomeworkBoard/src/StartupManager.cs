using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace HomeworkBoard
{
    internal static class StartupManager
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "HomeworkBoard";

        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    if (key == null) return false;
                    string value = key.GetValue(ValueName, null) as string;
                    if (string.IsNullOrEmpty(value)) return false;
                    return string.Equals(Normalize(value), Normalize(Application.ExecutablePath),
                        StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (key == null) throw new InvalidOperationException("无法访问 Windows 启动项。");
                if (enabled)
                    key.SetValue(ValueName, "\"" + Application.ExecutablePath + "\"", RegistryValueKind.String);
                else
                    key.DeleteValue(ValueName, false);
            }
        }

        private static string Normalize(string value)
        {
            if (value == null) return string.Empty;
            string result = value.Trim();
            if (result.Length >= 2 && result[0] == '"')
            {
                int end = result.IndexOf('"', 1);
                if (end > 1) result = result.Substring(1, end - 1);
            }
            return result.Trim();
        }
    }
}
