using System;
using System.Runtime.InteropServices;

namespace HomeworkBoard
{
    internal static class NativeMethods
    {
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MINIMIZE = 0xF020;
        public const int WM_EXITSIZEMOVE = 0x0232;
        public const int WM_SHOWHOMEWORKBOARD = 0x8000 + 41;
        public const int CS_DROPSHADOW = 0x00020000;
        public const int GCL_STYLE = -26;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWCP_ROUND = 2;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
        public static extern IntPtr GetClassLongPtr(IntPtr hWnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetClassLongPtr")]
        public static extern IntPtr SetClassLongPtr(IntPtr hWnd, int index, IntPtr newValue);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void EnableRoundedCorners(IntPtr handle)
        {
            try
            {
                int preference = DWMWCP_ROUND;
                DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            }
            catch
            {
                // Windows 10 and older simply keep square corners.
            }
        }

        public static void EnableDropShadow(IntPtr handle)
        {
            try
            {
                IntPtr style = GetClassLongPtr(handle, GCL_STYLE);
                long value = style.ToInt64() | CS_DROPSHADOW;
                SetClassLongPtr(handle, GCL_STYLE, new IntPtr(value));
            }
            catch
            {
            }
        }

    }
}
