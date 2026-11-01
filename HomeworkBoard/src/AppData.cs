using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace HomeworkBoard
{
    public class Assignment
    {
        public string Id;
        public string Title;
        public string Subject;
        public string Platform;
        public DateTime Due;

        public Assignment()
        {
        }

        public Assignment(string title, string subject, string platform, DateTime due)
        {
            Id = Guid.NewGuid().ToString("N");
            Title = title;
            Subject = subject;
            Platform = platform;
            Due = due;
        }
    }

    public class AppSettings
    {
        public string MinimizeAction = "tray"; // "tray" or "exit"
    }

    public class WindowPosition
    {
        public bool HasPosition;
        public int X;
        public int Y;
    }

    [XmlRoot("HomeworkBoard")]
    public class AppData
    {
        public List<Assignment> Assignments = new List<Assignment>();
        public List<string> Platforms = new List<string>();
        public AppSettings Settings = new AppSettings();
        public WindowPosition Window = new WindowPosition();

        public AppData()
        {
            Platforms.Add("学习通");
            Platforms.Add("雨课堂");
            Platforms.Add("智慧树");
            Platforms.Add("中国大学MOOC");
            Platforms.Add("钉钉");
            Platforms.Add("QQ群");
            Platforms.Add("邮箱");
        }

        public void Normalize()
        {
            if (Assignments == null) Assignments = new List<Assignment>();
            if (Platforms == null) Platforms = new List<string>();
            if (Settings == null) Settings = new AppSettings();
            if (Window == null) Window = new WindowPosition();

            List<string> clean = new List<string>();
            for (int i = 0; i < Platforms.Count; i++)
            {
                string name = (Platforms[i] ?? string.Empty).Trim();
                if (name.Length == 0) continue;
                bool duplicate = false;
                for (int j = 0; j < clean.Count; j++)
                {
                    if (string.Equals(clean[j], name, StringComparison.OrdinalIgnoreCase))
                    {
                        duplicate = true;
                        break;
                    }
                }
                if (!duplicate) clean.Add(name);
            }
            Platforms = clean;

            Assignments.RemoveAll(delegate(Assignment item) { return item == null; });
            for (int i = 0; i < Assignments.Count; i++)
            {
                Assignment item = Assignments[i];
                if (string.IsNullOrEmpty(item.Id)) item.Id = Guid.NewGuid().ToString("N");
                if (item.Title == null) item.Title = string.Empty;
                if (item.Subject == null) item.Subject = string.Empty;
                if (item.Platform == null) item.Platform = string.Empty;
            }

            if (Settings.MinimizeAction != "exit") Settings.MinimizeAction = "tray";
            Sort(Assignments);
        }

        public static void Sort(List<Assignment> items)
        {
            items.Sort(delegate(Assignment a, Assignment b)
            {
                int byDue = a.Due.CompareTo(b.Due);
                if (byDue != 0) return byDue;
                return string.Compare(a.Title, b.Title, StringComparison.CurrentCulture);
            });
        }
    }

    internal static class DataStore
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(AppData));

        public static string DataDirectory
        {
            get
            {
                string custom = Environment.GetEnvironmentVariable("HOMEWORKBOARD_DATA_DIR");
                if (!string.IsNullOrEmpty(custom)) return custom;
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appData, "HomeworkBoard");
            }
        }

        public static string DataFile
        {
            get { return Path.Combine(DataDirectory, "data.xml"); }
        }

        public static AppData Load(out string warning)
        {
            warning = null;
            AppData data = null;
            if (File.Exists(DataFile))
            {
                try
                {
                    using (FileStream stream = File.OpenRead(DataFile))
                    {
                        data = (AppData)Serializer.Deserialize(stream);
                    }
                }
                catch (Exception ex)
                {
                    warning = "读取数据失败，已使用空白看板。原文件已备份。\r\n" + ex.Message;
                    try
                    {
                        string backup = DataFile + ".broken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                        File.Copy(DataFile, backup, true);
                        File.Delete(DataFile);
                    }
                    catch
                    {
                    }
                }
            }
            if (data == null) data = new AppData();
            data.Normalize();
            return data;
        }

        public static void Save(AppData data)
        {
            if (data == null) return;
            data.Normalize();
            Directory.CreateDirectory(DataDirectory);
            string temp = DataFile + ".tmp";
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding(false);
            using (FileStream stream = File.Create(temp))
            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                Serializer.Serialize(writer, data);
            }

            try
            {
                if (File.Exists(DataFile))
                    File.Replace(temp, DataFile, DataFile + ".bak", true);
                else
                    File.Move(temp, DataFile);
            }
            catch
            {
                File.Copy(temp, DataFile, true);
                try { File.Delete(temp); }
                catch { }
            }
        }
    }
}
