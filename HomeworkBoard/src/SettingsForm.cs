using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HomeworkBoard
{
    internal class SettingsForm : ThemedDialog
    {
        private readonly AppData _data;
        private readonly List<string> _platforms;
        private readonly CheckBox _autoStart;
        private readonly RadioButton _trayRadio;
        private readonly RadioButton _exitRadio;
        private readonly ListBox _platformList;
        private readonly LinkLabel _resetLink;

        public bool ResetWindowPosition { get; private set; }

        public SettingsForm(AppData data)
            : base("设置", 430, 552)
        {
            _data = data;
            _platforms = new List<string>(data.Platforms);

            AddSection("常规", 72);

            _autoStart = new CheckBox();
            _autoStart.Name = "AutoStartCheck";
            _autoStart.Text = "开机时自动启动";
            _autoStart.AutoSize = false;
            _autoStart.BackColor = Color.Transparent;
            _autoStart.ForeColor = Theme.Ink;
            _autoStart.Font = Theme.MakeFont(9.6f, FontStyle.Regular);
            _autoStart.SetBounds(Dpi.S(24), Dpi.S(94), Dpi.S(380), Dpi.S(30));
            _autoStart.Checked = StartupManager.IsEnabled();
            Controls.Add(_autoStart);

            Label autoHint = Theme.MakeLabel("登录 Windows 后自动显示作业看板", true);
            autoHint.Font = Theme.MakeFont(7.8f, FontStyle.Regular);
            autoHint.ForeColor = Theme.Faint;
            autoHint.SetBounds(Dpi.S(26), Dpi.S(124), Dpi.S(376), Dpi.S(18));
            Controls.Add(autoHint);

            Label minimizeLabel = Theme.MakeLabel("最小化或关闭时", false);
            minimizeLabel.Font = Theme.MakeFont(8.6f, FontStyle.Bold);
            minimizeLabel.SetBounds(Dpi.S(24), Dpi.S(154), Dpi.S(380), Dpi.S(20));
            Controls.Add(minimizeLabel);

            _trayRadio = new RadioButton();
            _trayRadio.Name = "TrayRadio";
            _trayRadio.Text = "隐藏到系统托盘，继续在后台运行";
            _trayRadio.AutoSize = false;
            _trayRadio.BackColor = Color.Transparent;
            _trayRadio.ForeColor = Theme.Ink;
            _trayRadio.Font = Theme.MakeFont(9.2f, FontStyle.Regular);
            _trayRadio.SetBounds(Dpi.S(26), Dpi.S(178), Dpi.S(378), Dpi.S(28));
            Controls.Add(_trayRadio);

            _exitRadio = new RadioButton();
            _exitRadio.Name = "ExitRadio";
            _exitRadio.Text = "直接退出程序";
            _exitRadio.AutoSize = false;
            _exitRadio.BackColor = Color.Transparent;
            _exitRadio.ForeColor = Theme.Ink;
            _exitRadio.Font = Theme.MakeFont(9.2f, FontStyle.Regular);
            _exitRadio.SetBounds(Dpi.S(26), Dpi.S(208), Dpi.S(378), Dpi.S(28));
            Controls.Add(_exitRadio);

            if (data.Settings.MinimizeAction == "exit") _exitRadio.Checked = true;
            else _trayRadio.Checked = true;

            Panel divider = new Panel();
            divider.BackColor = Theme.Border;
            divider.SetBounds(Dpi.S(24), Dpi.S(248), Dpi.S(382), 1);
            Controls.Add(divider);

            AddSection("作业平台", 262);
            Label platformHint = Theme.MakeLabel("这些选项会出现在添加和编辑作业的平台上", true);
            platformHint.Font = Theme.MakeFont(7.8f, FontStyle.Regular);
            platformHint.ForeColor = Theme.Faint;
            platformHint.SetBounds(Dpi.S(24), Dpi.S(288), Dpi.S(382), Dpi.S(18));
            Controls.Add(platformHint);

            _platformList = new ListBox();
            _platformList.Name = "PlatformList";
            _platformList.BorderStyle = BorderStyle.FixedSingle;
            _platformList.BackColor = Color.White;
            _platformList.ForeColor = Theme.Ink;
            _platformList.Font = Theme.MakeFont(9.4f, FontStyle.Regular);
            _platformList.IntegralHeight = false;
            _platformList.SetBounds(Dpi.S(24), Dpi.S(310), Dpi.S(294), Dpi.S(112));
            Controls.Add(_platformList);

            RoundedButton add = new RoundedButton(false);
            add.Name = "AddPlatformSettings";
            add.Text = "+ 添加";
            add.Font = Theme.MakeFont(8.6f, FontStyle.Bold);
            add.SetBounds(Dpi.S(330), Dpi.S(310), Dpi.S(76), Dpi.S(34));
            add.Click += delegate { AddPlatform(); };
            Controls.Add(add);

            RoundedButton remove = new RoundedButton(false);
            remove.Name = "RemovePlatformSettings";
            remove.Text = "删除";
            remove.Font = Theme.MakeFont(8.6f, FontStyle.Bold);
            remove.SetBounds(Dpi.S(330), Dpi.S(352), Dpi.S(76), Dpi.S(34));
            remove.Click += delegate { RemovePlatform(); };
            Controls.Add(remove);

            _resetLink = new LinkLabel();
            _resetLink.Name = "ResetPositionLink";
            _resetLink.Text = "重置看板到桌面默认位置";
            _resetLink.AutoSize = false;
            _resetLink.BackColor = Color.Transparent;
            _resetLink.LinkColor = Theme.Muted;
            _resetLink.ActiveLinkColor = Theme.Accent;
            _resetLink.VisitedLinkColor = Theme.Muted;
            _resetLink.LinkBehavior = LinkBehavior.HoverUnderline;
            _resetLink.Font = Theme.MakeFont(8.1f, FontStyle.Regular);
            _resetLink.SetBounds(Dpi.S(24), Dpi.S(438), Dpi.S(250), Dpi.S(24));
            _resetLink.LinkClicked += delegate
            {
                ResetWindowPosition = true;
                _resetLink.Text = "已设置：保存后将回到默认位置";
                _resetLink.LinkColor = Theme.Accent;
            };
            Controls.Add(_resetLink);

            RoundedButton cancel = new RoundedButton(false);
            cancel.Name = "SettingsCancel";
            cancel.Text = "取消";
            cancel.SetBounds(Dpi.S(214), Dpi.S(488), Dpi.S(78), Dpi.S(40));
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);

            RoundedButton save = new RoundedButton(true);
            save.Name = "SettingsSave";
            save.Text = "保存设置";
            save.SetBounds(Dpi.S(304), Dpi.S(488), Dpi.S(102), Dpi.S(40));
            save.Click += delegate { SaveSettings(); };
            Controls.Add(save);

            AcceptButton = save;
            CancelButton = cancel;
            FillList();
        }

        private void AddSection(string text, int y)
        {
            Label label = Theme.MakeLabel(text, false);
            label.Font = Theme.MakeFont(9.4f, FontStyle.Bold);
            label.SetBounds(Dpi.S(24), Dpi.S(y), Dpi.S(382), Dpi.S(22));
            Controls.Add(label);
        }

        private void FillList()
        {
            _platformList.BeginUpdate();
            _platformList.Items.Clear();
            for (int i = 0; i < _platforms.Count; i++)
                _platformList.Items.Add(_platforms[i]);
            _platformList.EndUpdate();
        }

        private void AddPlatform()
        {
            string name = PromptDialog.Show(this, "添加平台", "平台名称（例如：学校教务系统）", string.Empty);
            if (name == null) return;
            name = name.Trim();
            if (name.Length == 0) return;
            for (int i = 0; i < _platforms.Count; i++)
            {
                if (string.Equals(_platforms[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    _platformList.SelectedIndex = i;
                    return;
                }
            }
            _platforms.Add(name);
            FillList();
            _platformList.SelectedIndex = _platforms.Count - 1;
        }

        private void RemovePlatform()
        {
            int index = _platformList.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show(this, "请先在列表中选择一个平台。", "作业看板",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_platforms.Count <= 1)
            {
                MessageBox.Show(this, "至少需要保留一个平台。", "作业看板",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string name = _platforms[index];
            DialogResult result = MessageBox.Show(this,
                "确定删除平台“" + name + "”吗？\r\n已有作业仍会保留这个平台名称。",
                "删除平台", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes) return;
            _platforms.RemoveAt(index);
            FillList();
            if (_platformList.Items.Count > 0)
                _platformList.SelectedIndex = Math.Min(index, _platformList.Items.Count - 1);
        }

        private void SaveSettings()
        {
            if (_platforms.Count == 0)
            {
                MessageBox.Show(this, "至少需要保留一个平台。", "作业看板",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool startupEnabled = StartupManager.IsEnabled();
            if (_autoStart.Checked != startupEnabled)
            {
                try
                {
                    StartupManager.SetEnabled(_autoStart.Checked);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        "无法修改开机启动设置，但其他设置会继续保存。\r\n\r\n" + ex.Message,
                        "作业看板", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            _data.Platforms = new List<string>(_platforms);
            _data.Settings.MinimizeAction = _exitRadio.Checked ? "exit" : "tray";
            try
            {
                DataStore.Save(_data);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "保存设置失败：" + ex.Message, "作业看板",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
