using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace HomeworkBoard
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                Run();
            }
            catch (Exception ex)
            {
                LogFatal(ex);
                MessageBox.Show("作业看板启动失败：\r\n" + ex.Message + "\r\n\r\n日志：" + LogPath,
                    "作业看板", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void Run()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, @"Local\HomeworkBoard.SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    ShowExistingInstance();
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
                {
                    LogFatal(e.Exception);
                    MessageBox.Show("作业看板遇到错误：\r\n" + e.Exception.Message,
                        "作业看板", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
                {
                    LogFatal(e.ExceptionObject as Exception);
                };

                string warning;
                AppData data = DataStore.Load(out warning);
                Application.Run(new MainForm(data, warning));
            }
        }

        private static string LogPath
        {
            get { return Path.Combine(Path.GetTempPath(), "HomeworkBoard-startup.log"); }
        }

        private static void LogFatal(Exception ex)
        {
            if (ex == null) return;
            try
            {
                File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                    "  FATAL  " + ex + "\r\n");
            }
            catch
            {
            }
        }

        private static void ShowExistingInstance()
        {
            for (int i = 0; i < 20; i++)
            {
                IntPtr handle = NativeMethods.FindWindow(null, "作业看板");
                if (handle != IntPtr.Zero)
                {
                    NativeMethods.PostMessage(handle, NativeMethods.WM_SHOWHOMEWORKBOARD, IntPtr.Zero, IntPtr.Zero);
                    return;
                }
                Thread.Sleep(100);
            }
            MessageBox.Show("作业看板已经在运行，请在任务栏或系统托盘中找到它。",
                "作业看板", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
