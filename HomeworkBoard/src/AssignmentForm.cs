using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HomeworkBoard
{
    internal class AssignmentForm : ThemedDialog
    {
        private readonly AppData _data;
        private readonly Assignment _editing;
        private readonly TextBox _titleBox;
        private readonly ComboBox _subjectBox;
        private readonly ComboBox _platformBox;
        private readonly DateTimePicker _datePicker;
        private readonly ComboBox _hourBox;
        private readonly Label _errorLabel;

        public AssignmentForm(AppData data, Assignment editing)
            : base(editing == null ? "添加作业" : "编辑作业", 410, 474)
        {
            _data = data;
            _editing = editing;

            AddLabel("作业名称", 72);
            _titleBox = new TextBox();
            _titleBox.Name = "TitleBox";
            Theme.StyleField(_titleBox);
            _titleBox.AutoSize = false;
            _titleBox.SetBounds(Dpi.S(24), Dpi.S(94), Dpi.S(362), Dpi.S(35));
            _titleBox.MaxLength = 80;
            Controls.Add(_titleBox);

            AddLabel("科目", 145);
            _subjectBox = new ComboBox();
            _subjectBox.Name = "SubjectBox";
            Theme.StyleCombo(_subjectBox);
            _subjectBox.DropDownStyle = ComboBoxStyle.DropDown;
            _subjectBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            _subjectBox.AutoCompleteSource = AutoCompleteSource.ListItems;
            _subjectBox.SetBounds(Dpi.S(24), Dpi.S(167), Dpi.S(362), Dpi.S(34));
            _subjectBox.MaxLength = 24;
            _subjectBox.Items.AddRange(BuildSubjectList().ToArray());
            Controls.Add(_subjectBox);

            AddLabel("平台", 218);
            _platformBox = new ComboBox();
            _platformBox.Name = "PlatformBox";
            Theme.StyleCombo(_platformBox);
            _platformBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _platformBox.SetBounds(Dpi.S(24), Dpi.S(240), Dpi.S(312), Dpi.S(34));
            Controls.Add(_platformBox);
            FillPlatforms(null);

            RoundedButton addPlatform = new RoundedButton(true);
            addPlatform.Name = "AddPlatformButton";
            addPlatform.Text = "+";
            addPlatform.Font = Theme.MakeFont(12f, FontStyle.Bold);
            addPlatform.SetBounds(Dpi.S(348), Dpi.S(239), Dpi.S(38), Dpi.S(36));
            addPlatform.Click += delegate { AddPlatform(); };
            Controls.Add(addPlatform);

            AddLabel("截止时间（精确到小时）", 290);
            _datePicker = new DateTimePicker();
            _datePicker.Name = "DueDate";
            _datePicker.Format = DateTimePickerFormat.Custom;
            _datePicker.CustomFormat = "yyyy 年 M 月 d 日";
            _datePicker.Font = Theme.MakeFont(9.8f, FontStyle.Regular);
            _datePicker.SetBounds(Dpi.S(24), Dpi.S(312), Dpi.S(256), Dpi.S(34));
            Controls.Add(_datePicker);

            _hourBox = new ComboBox();
            _hourBox.Name = "DueHour";
            Theme.StyleCombo(_hourBox);
            _hourBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _hourBox.SetBounds(Dpi.S(292), Dpi.S(312), Dpi.S(94), Dpi.S(34));
            for (int h = 0; h < 24; h++)
                _hourBox.Items.Add(h.ToString("00") + ":00");
            Controls.Add(_hourBox);

            _errorLabel = new Label();
            _errorLabel.AutoSize = false;
            _errorLabel.BackColor = Color.Transparent;
            _errorLabel.ForeColor = Theme.Danger;
            _errorLabel.Font = Theme.MakeFont(8.2f, FontStyle.Regular);
            _errorLabel.SetBounds(Dpi.S(24), Dpi.S(356), Dpi.S(362), Dpi.S(36));
            Controls.Add(_errorLabel);

            RoundedButton cancel = new RoundedButton(false);
            cancel.Name = "CancelButton";
            cancel.Text = "取消";
            cancel.SetBounds(Dpi.S(196), Dpi.S(406), Dpi.S(82), Dpi.S(40));
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);

            RoundedButton save = new RoundedButton(true);
            save.Name = "SaveButton";
            save.Text = editing == null ? "添加作业" : "保存修改";
            save.SetBounds(Dpi.S(286), Dpi.S(406), Dpi.S(100), Dpi.S(40));
            save.Click += SaveClick;
            Controls.Add(save);

            AcceptButton = save;
            CancelButton = cancel;
            ActiveControl = _titleBox;

            if (editing == null)
            {
                DateTime now = DateTime.Now;
                int defaultHour = now.Hour + 2;
                if (defaultHour > 23)
                {
                    _datePicker.Value = now.Date.AddDays(1);
                    _hourBox.SelectedIndex = 9;
                }
                else
                {
                    _datePicker.Value = now.Date;
                    _hourBox.SelectedIndex = defaultHour;
                }
            }
            else
            {
                _titleBox.Text = editing.Title;
                _subjectBox.Text = editing.Subject;
                _datePicker.Value = editing.Due.Date;
                _hourBox.SelectedIndex = editing.Due.Hour;
                for (int i = 0; i < _platformBox.Items.Count; i++)
                {
                    if (string.Equals(_platformBox.Items[i].ToString(), editing.Platform,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        _platformBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void AddLabel(string text, int y)
        {
            Label label = Theme.MakeLabel(text, true);
            label.Font = Theme.MakeFont(8.2f, FontStyle.Regular);
            label.SetBounds(Dpi.S(24), Dpi.S(y), Dpi.S(362), Dpi.S(20));
            Controls.Add(label);
        }

        private List<string> BuildSubjectList()
        {
            List<string> subjects = new List<string>();
            for (int i = 0; i < _data.Assignments.Count; i++)
            {
                string subject = (_data.Assignments[i].Subject ?? string.Empty).Trim();
                if (subject.Length == 0) continue;
                bool exists = false;
                for (int j = 0; j < subjects.Count; j++)
                {
                    if (string.Equals(subjects[j], subject, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists) subjects.Add(subject);
            }
            subjects.Sort(StringComparer.CurrentCulture);
            return subjects;
        }

        private void FillPlatforms(string select)
        {
            _platformBox.BeginUpdate();
            _platformBox.Items.Clear();
            for (int i = 0; i < _data.Platforms.Count; i++)
                _platformBox.Items.Add(_data.Platforms[i]);
            _platformBox.EndUpdate();

            if (!string.IsNullOrEmpty(select))
            {
                for (int i = 0; i < _platformBox.Items.Count; i++)
                {
                    if (string.Equals(_platformBox.Items[i].ToString(), select,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        _platformBox.SelectedIndex = i;
                        return;
                    }
                }
            }
            if (_platformBox.Items.Count > 0 && _platformBox.SelectedIndex < 0)
                _platformBox.SelectedIndex = 0;
        }

        private void AddPlatform()
        {
            string name = PromptDialog.Show(this, "添加平台", "平台名称（例如：学校教务系统）", string.Empty);
            if (name == null) return;
            name = name.Trim();
            if (name.Length == 0) return;

            for (int i = 0; i < _data.Platforms.Count; i++)
            {
                if (string.Equals(_data.Platforms[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    FillPlatforms(_data.Platforms[i]);
                    _errorLabel.Text = string.Empty;
                    return;
                }
            }

            _data.Platforms.Add(name);
            try
            {
                DataStore.Save(_data);
            }
            catch (Exception ex)
            {
                _data.Platforms.Remove(name);
                MessageBox.Show(this, "保存平台失败：" + ex.Message, "作业看板",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            FillPlatforms(name);
            _errorLabel.Text = string.Empty;
            _platformBox.Focus();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _titleBox.Focus();
            _titleBox.SelectAll();
        }

        private void SaveClick(object sender, EventArgs e)
        {
            string title = _titleBox.Text.Trim();
            string subject = _subjectBox.Text.Trim();
            if (title.Length == 0)
            {
                ShowError("请填写作业名称。", _titleBox);
                return;
            }
            if (subject.Length == 0)
            {
                ShowError("请填写科目。", _subjectBox);
                return;
            }
            if (_platformBox.SelectedIndex < 0)
            {
                ShowError("请选择平台，或点击右侧 + 添加一个平台。", _platformBox);
                return;
            }
            if (_hourBox.SelectedIndex < 0)
            {
                ShowError("请选择截止小时。", _hourBox);
                return;
            }

            DateTime due = _datePicker.Value.Date.AddHours(_hourBox.SelectedIndex);
            bool unchangedPastDue = _editing != null && due == _editing.Due;
            if (due <= DateTime.Now && !unchangedPastDue)
            {
                ShowError("截止时间早于当前时间，请选择之后的时间。", _datePicker);
                return;
            }

            string platform = _platformBox.SelectedItem.ToString();
            if (_editing == null)
            {
                _data.Assignments.Add(new Assignment(title, subject, platform, due));
            }
            else
            {
                _editing.Title = title;
                _editing.Subject = subject;
                _editing.Platform = platform;
                _editing.Due = due;
            }

            try
            {
                DataStore.Save(_data);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "保存作业失败：" + ex.Message, "作业看板",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ShowError(string message, Control focus)
        {
            _errorLabel.Text = message;
            if (focus != null) focus.Focus();
        }
    }
}
