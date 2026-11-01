using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace HomeworkBoard
{
    internal static class AppIcon
    {
        private static Icon _cached;

        public static Icon Get()
        {
            if (_cached != null) return _cached;
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("HomeworkBoard.app.ico"))
                {
                    if (stream != null) _cached = new Icon(stream);
                }
            }
            catch
            {
            }
            if (_cached == null) _cached = SystemIcons.Application;
            return _cached;
        }
    }

    internal static class Dpi
    {
        private static float _scale = 0f;

        public static float Scale
        {
            get
            {
                if (_scale <= 0f)
                {
                    try
                    {
                        using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
                            _scale = graphics.DpiX / 96f;
                    }
                    catch
                    {
                        _scale = 1f;
                    }
                    if (_scale < 1f) _scale = 1f;
                }
                return _scale;
            }
        }

        public static int S(float value)
        {
            return (int)Math.Round(value * Scale);
        }
    }
}
