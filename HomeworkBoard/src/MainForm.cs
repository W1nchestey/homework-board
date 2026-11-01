using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace HomeworkBoard
{
    internal class MainForm : Form
    {
        private const int DesignWidth = 360;
        private const int DesignMinHeight = 182;
        private const int DesignMaxHeight = 476;

        private readonly AppData _data;
        private readonly Timer _refreshTimer;
        private readonly ToolTip _toolTip;
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _rowMenu;
        private readonly ContextMenuStrip _appMenu;
        private readonly ContextMenuStrip _trayMenu;

        private readonly Font _titleFont;
        private readonly Font _subtitleFont;
        private readonly Font _rowTitleFont;
        private readonly Font _dueFont;
        private readonly Font _statusFont;
        private readonly Font _badgeFont;
        private readonly Font _hintFont;
        private readonly Font _emptyTitleFont;
        private readonly Font _emptyHintFont;

        private Rectangle _addRect;
        private Rectangle _settingsRect;
        private Rectangle _minimizeRect;
        private Rectangle _scrollThumb;
        private Rectangle _scrollTrack;

        private int _rowStep;
        private int _rowHeight;
        private int _listTop;
        private int _footerHeight;
        private int _scroll;
        private int _maxScroll;
        private int _hoverRow = -1;
        private int _hoverPart;
        private int _menuRow = -1;
        private bool _scrollDragging;
        private int _scrollDragOffset;
        private bool _windowDragging;
        private Point _windowDragStart;
        private Point _windowStartLocation;
        private bool _mouseInside;
        private bool _exiting;
        private bool _loaded;

        public MainForm(AppData data, string loadWarning)
        {
            _data = data;
            _titleFont = Theme.MakeFont(11.5f, FontStyle.Bold);
            _subtitleFont = Theme.MakeFont(7.6f, FontStyle.Regular);
            _rowTitleFont = Theme.MakeFont(9.6f, FontStyle.Bold);
            _dueFont = Theme.MakeFont(8.8f, FontStyle.Bold);
            _statusFont = Theme.MakeFont(7.3f, FontStyle.Regular);
            _badgeFont = Theme.MakeFont(7.25f, FontStyle.Regular);
            _hintFont = Theme.MakeFont(7.1f, FontStyle.Regular);
            _emptyTitleFont = Theme.MakeFont(11f, FontStyle.Bold);
            _emptyHintFont = Theme.MakeFont(8.2f, FontStyle.Regular);

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            ShowInTaskbar = true;
            ShowIcon = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Icon = AppIcon.Get();
            Text = "作业看板";
            BackColor = Theme.Canvas;
            DoubleBuffered = true;
            KeyPreview = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            _rowStep = Dpi.S(80);
            _rowHeight = Dpi.S(70);
            _listTop = Dpi.S(78);
            _footerHeight = Dpi.S(28);

            _toolTip = new ToolTip();
            _toolTip.InitialDelay = 380;
            _toolTip.ReshowDelay = 150;
            _toolTip.ShowAlways = true;

            _rowMenu = BuildRowMenu();
            _appMenu = BuildAppMenu();
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = AppIcon.Get();
            _notifyIcon.Text = "作业看板";
            _notifyIcon.Visible = false;
            _trayMenu = BuildTrayMenu();
            _notifyIcon.ContextMenuStrip = _trayMenu;
            _notifyIcon.DoubleClick += delegate { RestoreFromTray(); };
            _notifyIcon.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) RestoreFromTray();
            };

            _refreshTimer = new Timer();
            _refreshTimer.Interval = 60000;
            _refreshTimer.Tick += delegate
            {
                if (Visible)
                {
                    Invalidate();
                    UpdateEmptyOpacity();
                }
            };
            _refreshTimer.Start();

            ApplySizing(false);
            PlaceInitialWindow();
            UpdateEmptyOpacity();
            Invalidate();

            if (!string.IsNullOrEmpty(loadWarning))
            {
                Shown += delegate
                {
                    MessageBox.Show(this, loadWarning, "作业看板", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                };
            }
        }

        private bool TrayMode
        {
            get { return _data.Settings.MinimizeAction != "exit"; }
        }

        private int CardWidth
        {
            get { return ClientSize.Width - Dpi.S(28); }
        }

        private int ListBottom
        {
            get { return ClientSize.Height - _footerHeight; }
        }

        private ContextMenuStrip BuildRowMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = Theme.MakeFont(9.2f, FontStyle.Regular);
            menu.BackColor = Color.White;
            menu.ShowImageMargin = false;
            menu.Padding = new Padding(Dpi.S(4));
            ToolStripMenuItem edit = new ToolStripMenuItem("编辑作业");
            edit.Padding = new Padding(Dpi.S(10), Dpi.S(4), Dpi.S(18), Dpi.S(4));
            edit.Click += delegate { EditRow(_menuRow); };
            ToolStripMenuItem delete = new ToolStripMenuItem("删除作业");
            delete.ForeColor = Theme.Danger;
            delete.Padding = new Padding(Dpi.S(10), Dpi.S(4), Dpi.S(18), Dpi.S(4));
            delete.Click += delegate { DeleteRow(_menuRow); };
            menu.Items.Add(edit);
            menu.Items.Add(delete);
            return menu;
        }

        private ContextMenuStrip BuildAppMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = Theme.MakeFont(9.2f, FontStyle.Regular);
            menu.BackColor = Color.White;
            menu.ShowImageMargin = false;
            menu.Padding = new Padding(Dpi.S(4));
            menu.Items.Add(MakeMenuItem("添加作业", delegate { AddAssignment(); }));
            menu.Items.Add(MakeMenuItem("设置", delegate { ShowSettings(); }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(MakeMenuItem("最小化", delegate { HandleMinimize(); }));
            menu.Items.Add(MakeMenuItem("退出", delegate { ExitApplication(); }));
            return menu;
        }

        private ContextMenuStrip BuildTrayMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = Theme.MakeFont(9.2f, FontStyle.Regular);
            menu.BackColor = Color.White;
            menu.ShowImageMargin = false;
            menu.Padding = new Padding(Dpi.S(4));
            ToolStripMenuItem show = MakeMenuItem("显示作业看板", delegate { RestoreFromTray(); });
            show.Font = Theme.MakeFont(9.3f, FontStyle.Bold);
            menu.Items.Add(show);
            menu.Items.Add(MakeMenuItem("添加作业", delegate
            {
                RestoreFromTray();
                AddAssignment();
            }));
            menu.Items.Add(MakeMenuItem("设置", delegate { ShowSettings(); }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(MakeMenuItem("退出", delegate { ExitApplication(); }));
            return menu;
        }

        private ToolStripMenuItem MakeMenuItem(string text, EventHandler handler)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Padding = new Padding(Dpi.S(10), Dpi.S(4), Dpi.S(18), Dpi.S(4));
            item.Click += handler;
            return item;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.EnableRoundedCorners(Handle);
            NativeMethods.EnableDropShadow(Handle);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _loaded = true;
            UpdateEmptyOpacity();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_exiting)
            {
                if (e.CloseReason == CloseReason.WindowsShutDown || e.CloseReason == CloseReason.TaskManagerClosing)
                {
                    _exiting = true;
                }
                else if (TrayMode)
                {
                    e.Cancel = true;
                    HideToTray();
                    return;
                }
                else
                {
                    _exiting = true;
                }
            }

            if (_exiting)
            {
                SaveWindowPosition();
                _notifyIcon.Visible = false;
                _refreshTimer.Stop();
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _rowMenu.Dispose();
            _appMenu.Dispose();
            _trayMenu.Dispose();
            _toolTip.Dispose();
            _refreshTimer.Dispose();
            _titleFont.Dispose();
            _subtitleFont.Dispose();
            _rowTitleFont.Dispose();
            _dueFont.Dispose();
            _statusFont.Dispose();
            _badgeFont.Dispose();
            _hintFont.Dispose();
            _emptyTitleFont.Dispose();
            _emptyHintFont.Dispose();
            base.OnFormClosed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_SYSCOMMAND &&
                (m.WParam.ToInt64() & 0xFFF0) == NativeMethods.SC_MINIMIZE)
            {
                HandleMinimize();
                return;
            }
            if (m.Msg == NativeMethods.WM_SHOWHOMEWORKBOARD)
            {
                RestoreFromTray();
                return;
            }
            if (m.Msg == NativeMethods.WM_EXITSIZEMOVE && _loaded)
            {
                SaveWindowPosition();
            }
            base.WndProc(ref m);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Entire widget surface is painted in OnPaint.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            bool empty = _data.Assignments.Count == 0;
            Color canvas = empty ? Theme.CanvasEmpty : Theme.Canvas;
            Color outline = empty ? Color.FromArgb(239, 235, 227) : Theme.Border;

            using (SolidBrush baseBrush = new SolidBrush(canvas))
                g.FillRectangle(baseBrush, ClientRectangle);

            RectangleF all = new RectangleF(0.5f, 0.5f, ClientSize.Width - 1f, ClientSize.Height - 1f);
            using (GraphicsPath path = Theme.RoundRect(all, Dpi.S(18)))
            using (SolidBrush brush = new SolidBrush(canvas))
                g.FillPath(brush, path);
            using (GraphicsPath path = Theme.RoundRect(all, Dpi.S(18)))
            using (Pen pen = new Pen(outline, 1f))
                g.DrawPath(pen, path);

            DrawHeader(g);
            if (empty)
                DrawEmptyState(g);
            else
                DrawAssignments(g);
        }

        private void DrawHeader(Graphics g)
        {
            int width = ClientSize.Width;
            int logoSize = Dpi.S(28);
            Rectangle logoRect = new Rectangle(Dpi.S(16), Dpi.S(17), logoSize, logoSize);
            Theme.DrawRoundedFill(g, logoRect, Dpi.S(8), Theme.Accent);
            using (Pen pen = new Pen(Color.White, Dpi.S(2f)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLines(pen, new PointF[]
                {
                    new PointF(logoRect.Left + Dpi.S(7), logoRect.Top + Dpi.S(14)),
                    new PointF(logoRect.Left + Dpi.S(12), logoRect.Top + Dpi.S(19)),
                    new PointF(logoRect.Left + Dpi.S(21), logoRect.Top + Dpi.S(9))
                });
            }

            int titleLeft = Dpi.S(53);
            Theme.DrawText(g, "作业看板", _titleFont, Theme.Ink,
                new RectangleF(titleLeft, Dpi.S(11), width - Dpi.S(171), Dpi.S(27)),
                StringAlignment.Near, StringAlignment.Center, false);

            string subtitle;
            if (_data.Assignments.Count == 0)
                subtitle = "把要做的事放在这里";
            else
                subtitle = _data.Assignments.Count + " 项作业 · 按截止时间排序";
            Theme.DrawText(g, subtitle, _subtitleFont, Theme.Muted,
                new RectangleF(titleLeft + Dpi.S(1), Dpi.S(38), width - Dpi.S(171), Dpi.S(18)),
                StringAlignment.Near, StringAlignment.Center, true);

            _addRect = new Rectangle(width - Dpi.S(58), Dpi.S(15), Dpi.S(38), Dpi.S(38));
            _settingsRect = new Rectangle(width - Dpi.S(92), Dpi.S(19), Dpi.S(28), Dpi.S(28));
            _minimizeRect = new Rectangle(width - Dpi.S(122), Dpi.S(19), Dpi.S(28), Dpi.S(28));

            if (_hoverPart == 2)
                Theme.DrawRoundedFill(g, _settingsRect, Dpi.S(14), Color.FromArgb(238, 233, 224));
            if (_hoverPart == 3)
                Theme.DrawRoundedFill(g, _minimizeRect, Dpi.S(14), Color.FromArgb(238, 233, 224));

            DrawSlidersIcon(g, _settingsRect, _hoverPart == 2 ? Theme.Ink : Theme.Muted);
            using (Pen pen = new Pen(_hoverPart == 3 ? Theme.Ink : Theme.Muted, Dpi.S(1.7f)))
            {
                int cy = _minimizeRect.Top + _minimizeRect.Height / 2 + Dpi.S(1);
                g.DrawLine(pen, _minimizeRect.Left + Dpi.S(8), cy,
                    _minimizeRect.Right - Dpi.S(8), cy);
            }

            Color addColor = _hoverPart == 1 ? Theme.AccentHover : Theme.Accent;
            if (_hoverPart == 1)
            {
                Rectangle glow = _addRect;
                glow.Inflate(Dpi.S(2), Dpi.S(2));
                Theme.DrawRoundedFill(g, glow, Dpi.S(21), Theme.AccentSoft);
            }
            using (GraphicsPath path = Theme.RoundRect(_addRect, Dpi.S(19)))
            using (SolidBrush brush = new SolidBrush(addColor))
                g.FillPath(brush, path);
            using (Pen pen = new Pen(Color.White, Dpi.S(2.1f)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                int cx = _addRect.Left + _addRect.Width / 2;
                int cy = _addRect.Top + _addRect.Height / 2;
                g.DrawLine(pen, cx - Dpi.S(7), cy, cx + Dpi.S(7), cy);
                g.DrawLine(pen, cx, cy - Dpi.S(7), cx, cy + Dpi.S(7));
            }
        }

        private void DrawSlidersIcon(Graphics g, Rectangle rect, Color color)
        {
            using (Pen pen = new Pen(color, Dpi.S(1.5f)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                int x1 = rect.Left + Dpi.S(8);
                int x2 = rect.Right - Dpi.S(8);
                int y1 = rect.Top + Dpi.S(10);
                int y2 = rect.Top + Dpi.S(15);
                int y3 = rect.Top + Dpi.S(20);
                g.DrawLine(pen, x1, y1, x2, y1);
                g.DrawLine(pen, x1, y2, x2, y2);
                g.DrawLine(pen, x1, y3, x2, y3);
            }
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillEllipse(brush, rect.Left + Dpi.S(11), rect.Top + Dpi.S(8), Dpi.S(4), Dpi.S(4));
                g.FillEllipse(brush, rect.Left + Dpi.S(16), rect.Top + Dpi.S(13), Dpi.S(4), Dpi.S(4));
                g.FillEllipse(brush, rect.Left + Dpi.S(10), rect.Top + Dpi.S(18), Dpi.S(4), Dpi.S(4));
            }
        }

        private void DrawEmptyState(Graphics g)
        {
            int width = ClientSize.Width;
            int centerY = Dpi.S(124);
            Rectangle icon = new Rectangle((width - Dpi.S(42)) / 2, centerY - Dpi.S(34), Dpi.S(42), Dpi.S(42));
            Theme.DrawRoundedFill(g, icon, Dpi.S(13), Color.FromArgb(248, 238, 230));
            using (Pen pen = new Pen(Color.FromArgb(208, 153, 126), Dpi.S(1.8f)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLines(pen, new PointF[]
                {
                    new PointF(icon.Left + Dpi.S(11), icon.Top + Dpi.S(21)),
                    new PointF(icon.Left + Dpi.S(18), icon.Top + Dpi.S(28)),
                    new PointF(icon.Left + Dpi.S(31), icon.Top + Dpi.S(14))
                });
            }
            Theme.DrawText(g, "暂无作业", _emptyTitleFont, Theme.Ink,
                new RectangleF(0, centerY + Dpi.S(10), width, Dpi.S(28)),
                StringAlignment.Center, StringAlignment.Center, false);
            Theme.DrawText(g, "点击右上角 + 添加第一项作业", _emptyHintFont, Theme.Muted,
                new RectangleF(0, centerY + Dpi.S(37), width, Dpi.S(22)),
                StringAlignment.Center, StringAlignment.Center, false);
        }

        private void DrawAssignments(Graphics g)
        {
            int listHeight = ListBottom - _listTop;
            if (listHeight <= 0) return;

            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(0, _listTop, ClientSize.Width, listHeight));
            DateTime now = DateTime.Now;

            for (int i = 0; i < _data.Assignments.Count; i++)
            {
                int top = _listTop + i * _rowStep - _scroll;
                if (top + _rowHeight < _listTop) continue;
                if (top > ListBottom) break;
                DrawCard(g, _data.Assignments[i], i, top, now);
            }

            g.Restore(state);
            DrawScrollBar(g, listHeight);

            RectangleF footer = new RectangleF(Dpi.S(18), ClientSize.Height - Dpi.S(23),
                ClientSize.Width - Dpi.S(36), Dpi.S(17));
            Theme.DrawText(g, "双击编辑 · 右键更多", _hintFont, Theme.Faint,
                footer, StringAlignment.Near, StringAlignment.Center, false);
            Theme.DrawText(g, "按截止时间", _hintFont, Theme.Faint,
                footer, StringAlignment.Far, StringAlignment.Center, false);
        }

        private void DrawCard(Graphics g, Assignment item, int index, int top, DateTime now)
        {
            Rectangle card = new Rectangle(Dpi.S(14), top, CardWidth, _rowHeight);
            bool hover = _hoverRow == index;
            bool overdue = item.Due < now;
            Color fill = hover ? Theme.CardHover : Theme.Card;
            if (overdue) fill = Theme.Mix(fill, Theme.DangerSoft, hover ? 0.52f : 0.35f);
            Theme.DrawRoundedFill(g, card, Dpi.S(12), fill);
            Theme.DrawRoundedOutline(g, card, Dpi.S(12),
                hover ? Color.FromArgb(224, 214, 199) : Theme.BorderSoft, 1f);

            int pad = Dpi.S(16);
            int deadlineWidth = Dpi.S(91);
            int titleWidth = card.Width - pad * 2 - deadlineWidth;
            Rectangle titleRect = new Rectangle(card.Left + pad, card.Top + Dpi.S(9),
                titleWidth, Dpi.S(22));
            Theme.DrawText(g, item.Title, _rowTitleFont, Theme.Ink, titleRect,
                StringAlignment.Near, StringAlignment.Center, true);

            Color platformColor = Theme.PlatformColor(item.Platform);
            Rectangle bar = new Rectangle(card.Left + Dpi.S(1), card.Top + Dpi.S(16),
                Dpi.S(3), card.Height - Dpi.S(32));
            Theme.DrawRoundedFill(g, bar, Dpi.S(1.5f), Color.FromArgb(190, platformColor));

            Rectangle badgeRow = new Rectangle(card.Left + pad, card.Top + Dpi.S(38),
                card.Width - pad * 2 - deadlineWidth, Dpi.S(22));
            DrawBadges(g, item, badgeRow);

            int right = card.Right - pad;
            Rectangle dueRect = new Rectangle(right - deadlineWidth, card.Top + Dpi.S(9),
                deadlineWidth, Dpi.S(21));
            Color dueColor = overdue ? Theme.Danger : Theme.Ink;
            Theme.DrawText(g, FormatDue(item.Due), _dueFont, dueColor,
                dueRect, StringAlignment.Far, StringAlignment.Center, false);

            Rectangle statusRect = new Rectangle(right - deadlineWidth, card.Top + Dpi.S(31),
                deadlineWidth, Dpi.S(19));
            Theme.DrawText(g, FormatRemaining(item.Due, now), _statusFont,
                StatusColor(item.Due, now), statusRect, StringAlignment.Far, StringAlignment.Center, false);
        }

        private void DrawBadges(Graphics g, Assignment item, Rectangle area)
        {
            string subject = string.IsNullOrEmpty(item.Subject) ? "未填科目" : item.Subject;
            string platform = string.IsNullOrEmpty(item.Platform) ? "未选平台" : item.Platform;

            int subjectMax = Dpi.S(142);
            int platformMax = Dpi.S(148);
            int gap = Dpi.S(6);
            int maxTotal = area.Width;
            if (maxTotal < Dpi.S(50)) return;

            Size subjectSize = TextRenderer.MeasureText(g, subject, _badgeFont,
                new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
            Size platformSize = TextRenderer.MeasureText(g, platform, _badgeFont,
                new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);

            int subjectWidth = Math.Min(subjectSize.Width + Dpi.S(20), subjectMax);
            int platformWidth = Math.Min(platformSize.Width + Dpi.S(25), platformMax);

            int excess = subjectWidth + gap + platformWidth - maxTotal;
            if (excess > 0)
                subjectWidth = Math.Max(Dpi.S(56), subjectWidth - excess);
            excess = subjectWidth + gap + platformWidth - maxTotal;
            if (excess > 0)
                platformWidth = Math.Max(Dpi.S(48), platformWidth - excess);

            Rectangle subjectRect = new Rectangle(area.Left, area.Top, subjectWidth, area.Height);
            Theme.DrawRoundedFill(g, subjectRect, Dpi.S(7), Theme.SubjectBack);
            Theme.DrawText(g, subject, _badgeFont, Theme.SubjectInk,
                new RectangleF(subjectRect.Left + Dpi.S(9), subjectRect.Top,
                    subjectRect.Width - Dpi.S(14), subjectRect.Height),
                StringAlignment.Near, StringAlignment.Center, true);

            Rectangle platformRect = new Rectangle(subjectRect.Right + gap, area.Top,
                platformWidth, area.Height);
            Theme.DrawRoundedFill(g, platformRect, Dpi.S(7), Theme.PlatformBack(item.Platform));
            using (SolidBrush dot = new SolidBrush(Theme.PlatformColor(item.Platform)))
                g.FillEllipse(dot, platformRect.Left + Dpi.S(7), platformRect.Top + platformRect.Height / 2 - Dpi.S(2),
                    Dpi.S(4), Dpi.S(4));
            Theme.DrawText(g, platform, _badgeFont, Theme.PlatformColor(item.Platform),
                new RectangleF(platformRect.Left + Dpi.S(14), platformRect.Top,
                    platformRect.Width - Dpi.S(19), platformRect.Height),
                StringAlignment.Near, StringAlignment.Center, true);
        }

        private void DrawScrollBar(Graphics g, int listHeight)
        {
            _maxScroll = Math.Max(0, _data.Assignments.Count * _rowStep - listHeight);
            if (_scroll > _maxScroll) _scroll = _maxScroll;
            if (_scroll < 0) _scroll = 0;

            _scrollTrack = new Rectangle(ClientSize.Width - Dpi.S(7), _listTop + Dpi.S(3),
                Dpi.S(3), listHeight - Dpi.S(6));
            if (_maxScroll <= 0)
            {
                _scrollThumb = Rectangle.Empty;
                return;
            }

            using (GraphicsPath path = Theme.RoundRect(_scrollTrack, Dpi.S(2)))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(236, 231, 221)))
                g.FillPath(brush, path);

            int content = _data.Assignments.Count * _rowStep;
            int thumbHeight = Math.Max(Dpi.S(26), (int)(_scrollTrack.Height * (float)listHeight / content));
            if (thumbHeight > _scrollTrack.Height) thumbHeight = _scrollTrack.Height;
            int travel = _scrollTrack.Height - thumbHeight;
            int thumbTop = _scrollTrack.Top + (int)Math.Round(travel * (_maxScroll == 0 ? 0f : (float)_scroll / _maxScroll));
            _scrollThumb = new Rectangle(_scrollTrack.Left - Dpi.S(2), thumbTop,
                _scrollTrack.Width + Dpi.S(4), thumbHeight);
            Theme.DrawRoundedFill(g, _scrollThumb, Dpi.S(3),
                _scrollDragging ? Color.FromArgb(152, 146, 134) : Color.FromArgb(182, 174, 160));
        }

        private Color StatusColor(DateTime due, DateTime now)
        {
            if (due < now) return Theme.Danger;
            TimeSpan left = due - now;
            if (left.TotalHours <= 24) return Theme.Warm;
            return Theme.Muted;
        }

        private string FormatDue(DateTime due)
        {
            DateTime today = DateTime.Today;
            string day;
            if (due.Date == today) day = "今天";
            else if (due.Date == today.AddDays(1)) day = "明天";
            else if (due.Date == today.AddDays(-1)) day = "昨天";
            else if (due.Year == today.Year) day = due.Month + "月" + due.Day + "日";
            else day = due.Year + "年" + due.Month + "月" + due.Day + "日";
            return day + " " + due.ToString("HH:mm");
        }

        private string FormatRemaining(DateTime due, DateTime now)
        {
            TimeSpan left = due - now;
            if (left.TotalSeconds < 0)
            {
                TimeSpan late = now - due;
                if (late.TotalDays >= 1)
                {
                    int lateDays = (int)late.TotalDays;
                    int lateHours = late.Hours;
                    if (lateDays < 7 && lateHours > 0)
                        return "已逾期 " + lateDays + " 天 " + lateHours + " 小时";
                    return "已逾期 " + lateDays + " 天";
                }
                if (late.TotalHours >= 1) return "已逾期 " + (int)late.TotalHours + " 小时";
                return "已逾期";
            }
            if (left.TotalMinutes < 60)
            {
                if (left.TotalMinutes < 1) return "即将截止";
                return "剩 " + Math.Max(1, (int)left.TotalMinutes) + " 分钟";
            }
            if (left.TotalHours < 24)
                return "剩 " + (int)left.TotalHours + " 小时";
            int days = (int)left.TotalDays;
            int hours = left.Hours;
            if (days < 7 && hours > 0)
                return "剩 " + days + " 天 " + hours + " 小时";
            return "剩 " + days + " 天";
        }

        private void ApplySizing(bool adjustLocation)
        {
            int count = _data.Assignments.Count;
            int desired;
            if (count == 0)
                desired = Dpi.S(DesignMinHeight);
            else
                desired = _listTop + count * _rowStep + _footerHeight;
            int maxHeight = Dpi.S(DesignMaxHeight);
            Screen screen = CurrentScreen();
            int screenCap = screen.WorkingArea.Height - Dpi.S(40);
            if (maxHeight > screenCap) maxHeight = screenCap;
            if (desired > maxHeight) desired = maxHeight;
            if (desired < Dpi.S(DesignMinHeight)) desired = Dpi.S(DesignMinHeight);

            ClientSize = new Size(Dpi.S(DesignWidth), desired);
            if (adjustLocation && _loaded) KeepOnScreen();
            UpdateEmptyOpacity();
            Invalidate();
        }

        private Screen CurrentScreen()
        {
            if (IsHandleCreated) return Screen.FromControl(this);
            return Screen.PrimaryScreen;
        }

        private void KeepOnScreen()
        {
            Screen screen = CurrentScreen();
            Rectangle work = screen.WorkingArea;
            int x = Left;
            int y = Top;
            if (Right > work.Right) x = work.Right - Width;
            if (Bottom > work.Bottom) y = work.Bottom - Height;
            if (x < work.Left) x = work.Left;
            if (y < work.Top) y = work.Top;
            if (x != Left || y != Top) Location = new Point(x, y);
        }

        private void PlaceInitialWindow()
        {
            Screen screen = Screen.PrimaryScreen;
            Rectangle work = screen.WorkingArea;
            int x;
            int y;
            if (_data.Window.HasPosition)
            {
                x = _data.Window.X;
                y = _data.Window.Y;
                Rectangle virtualArea = SystemInformation.VirtualScreen;
                if (x > virtualArea.Right - Dpi.S(80) || y > virtualArea.Bottom - Dpi.S(60) ||
                    x + Width < virtualArea.Left + Dpi.S(80) || y + Height < virtualArea.Top + Dpi.S(40))
                {
                    x = work.Right - Width - Dpi.S(24);
                    y = work.Top + (work.Height - Height) / 2;
                }
            }
            else
            {
                x = work.Right - Width - Dpi.S(24);
                y = work.Top + (work.Height - Height) / 2;
            }
            Location = new Point(x, y);
            KeepOnScreen();
        }

        private void SaveWindowPosition()
        {
            _data.Window.HasPosition = true;
            _data.Window.X = Left;
            _data.Window.Y = Top;
            try
            {
                DataStore.Save(_data);
            }
            catch
            {
            }
        }

        private void UpdateEmptyOpacity()
        {
            bool empty = _data.Assignments.Count == 0;
            if (!empty)
            {
                if (Opacity < 1f) Opacity = 1f;
                BackColor = Theme.Canvas;
                return;
            }
            BackColor = Theme.CanvasEmpty;
            float target = _mouseInside ? 0.94f : 0.20f;
            if (Math.Abs(Opacity - target) > 0.001f) Opacity = target;
        }

        private void RefreshAfterDataChange()
        {
            _data.Normalize();
            _scroll = 0;
            ApplySizing(true);
            Invalidate();
            UpdateEmptyOpacity();
        }

        private int HitRow(Point point)
        {
            if (point.Y < _listTop || point.Y >= ListBottom) return -1;
            if (point.X < Dpi.S(14) || point.X > ClientSize.Width - Dpi.S(14)) return -1;
            int index = (point.Y - _listTop + _scroll) / _rowStep;
            if (index < 0 || index >= _data.Assignments.Count) return -1;
            int top = _listTop + index * _rowStep - _scroll;
            if (point.Y < top || point.Y > top + _rowHeight) return -1;
            return index;
        }

        private int HitHeaderButton(Point point)
        {
            if (_addRect.Contains(point)) return 1;
            if (_settingsRect.Contains(point)) return 2;
            if (_minimizeRect.Contains(point)) return 3;
            return 0;
        }

        private void UpdateHover(Point client)
        {
            int part = HitHeaderButton(client);
            int row = -1;
            if (part == 0 && !_scrollThumb.IsEmpty)
            {
                if (_scrollThumb.Contains(client))
                {
                    Cursor = Cursors.Hand;
                    SetHover(part, row);
                    return;
                }
            }
            if (part == 0) row = HitRow(client);

            Cursor = part != 0 || row >= 0 ? Cursors.Hand : Cursors.Default;
            SetHover(part, row);
        }

        private void SetHover(int part, int row)
        {
            if (_hoverPart == part && _hoverRow == row) return;
            _hoverPart = part;
            _hoverRow = row;
            _toolTip.Hide(this);
            if (part == 1) ShowTip("添加作业", _addRect);
            else if (part == 2) ShowTip("设置", _settingsRect);
            else if (part == 3) ShowTip("最小化", _minimizeRect);
            Invalidate();
        }

        private void ShowTip(string text, Rectangle nextTo)
        {
            _toolTip.Show(text, this, nextTo.Left, nextTo.Bottom + Dpi.S(3), 2500);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _mouseInside = true;
            UpdateEmptyOpacity();
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _mouseInside = false;
            _toolTip.Hide(this);
            SetHover(0, -1);
            Cursor = Cursors.Default;
            UpdateEmptyOpacity();
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_windowDragging)
            {
                Point screen = Cursor.Position;
                Location = new Point(
                    _windowStartLocation.X + screen.X - _windowDragStart.X,
                    _windowStartLocation.Y + screen.Y - _windowDragStart.Y);
                return;
            }
            if (_scrollDragging && _maxScroll > 0)
            {
                int trackTravel = _scrollTrack.Height - _scrollThumb.Height;
                if (trackTravel > 0)
                {
                    int y = e.Y - _scrollDragOffset;
                    int relative = y - _scrollTrack.Top;
                    float ratio = (float)relative / trackTravel;
                    if (ratio < 0f) ratio = 0f;
                    if (ratio > 1f) ratio = 1f;
                    _scroll = (int)Math.Round(ratio * _maxScroll);
                    Invalidate();
                }
                return;
            }
            UpdateHover(e.Location);
            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!_scrollThumb.IsEmpty && _scrollThumb.Contains(e.Location) && _maxScroll > 0)
                {
                    _scrollDragging = true;
                    _scrollDragOffset = e.Y - _scrollThumb.Top;
                    Invalidate();
                    return;
                }
                int part = HitHeaderButton(e.Location);
                if (part == 1)
                {
                    _toolTip.Hide(this);
                    AddAssignment();
                    return;
                }
                if (part == 2)
                {
                    _toolTip.Hide(this);
                    ShowSettings();
                    return;
                }
                if (part == 3)
                {
                    _toolTip.Hide(this);
                    HandleMinimize();
                    return;
                }
                if (e.Y < Dpi.S(74))
                {
                    _windowDragging = true;
                    _windowDragStart = Cursor.Position;
                    _windowStartLocation = Location;
                    Capture = true;
                    return;
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_windowDragging)
            {
                _windowDragging = false;
                Capture = false;
                KeepOnScreen();
                SaveWindowPosition();
                _mouseInside = Bounds.Contains(Cursor.Position);
                UpdateEmptyOpacity();
                Invalidate();
                return;
            }
            if (_scrollDragging)
            {
                _scrollDragging = false;
                Invalidate();
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            if (_windowDragging && !Capture)
            {
                _windowDragging = false;
                KeepOnScreen();
                SaveWindowPosition();
                _mouseInside = Bounds.Contains(Cursor.Position);
                UpdateEmptyOpacity();
                Invalidate();
            }
            base.OnMouseCaptureChanged(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            int row = HitRow(e.Location);
            if (row >= 0 && e.Button == MouseButtons.Left)
            {
                EditRow(row);
                return;
            }
            base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (_maxScroll > 0)
            {
                _scroll -= Math.Sign(e.Delta) * Dpi.S(50);
                if (_scroll < 0) _scroll = 0;
                if (_scroll > _maxScroll) _scroll = _maxScroll;
                Invalidate();
            }
            base.OnMouseWheel(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int row = HitRow(e.Location);
                if (row >= 0)
                {
                    _menuRow = row;
                    _rowMenu.Show(this, e.Location);
                }
                else
                {
                    _appMenu.Show(this, e.Location);
                }
            }
            base.OnMouseClick(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.N))
            {
                AddAssignment();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Oemcomma))
            {
                ShowSettings();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void AddAssignment()
        {
            HideTip();
            using (AssignmentForm form = new AssignmentForm(_data, null))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    RefreshAfterDataChange();
            }
            _mouseInside = Bounds.Contains(Cursor.Position);
            UpdateEmptyOpacity();
            Invalidate();
        }

        private void EditRow(int index)
        {
            if (index < 0 || index >= _data.Assignments.Count) return;
            HideTip();
            Assignment item = _data.Assignments[index];
            using (AssignmentForm form = new AssignmentForm(_data, item))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    RefreshAfterDataChange();
            }
            Invalidate();
        }

        private void DeleteRow(int index)
        {
            if (index < 0 || index >= _data.Assignments.Count) return;
            Assignment item = _data.Assignments[index];
            DialogResult result = MessageBox.Show(this,
                "确定删除“" + item.Title + "”吗？\r\n删除后无法恢复。",
                "删除作业", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes) return;
            _data.Assignments.RemoveAt(index);
            try { DataStore.Save(_data); }
            catch (Exception ex)
            {
                MessageBox.Show(this, "保存失败：" + ex.Message, "作业看板",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            RefreshAfterDataChange();
        }

        private void ShowSettings()
        {
            HideTip();
            using (SettingsForm form = new SettingsForm(_data))
            {
                DialogResult result;
                if (Visible)
                {
                    result = form.ShowDialog(this);
                }
                else
                {
                    form.StartPosition = FormStartPosition.CenterScreen;
                    result = form.ShowDialog();
                }
                if (result == DialogResult.OK)
                {
                    if (form.ResetWindowPosition)
                    {
                        _data.Window.HasPosition = false;
                        PlaceInitialWindow();
                        SaveWindowPosition();
                    }
                    RefreshAfterDataChange();
                    Invalidate();
                }
            }
        }

        private void HideTip()
        {
            _toolTip.Hide(this);
        }

        private void HandleMinimize()
        {
            HideTip();
            SaveWindowPosition();
            if (TrayMode)
                HideToTray();
            else
                ExitApplication();
        }

        private void HideToTray()
        {
            if (_exiting) return;
            Hide();
            _notifyIcon.Visible = true;
            _refreshTimer.Stop();
        }

        private void RestoreFromTray()
        {
            if (_exiting) return;
            _notifyIcon.Visible = false;
            Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
            BringToFront();
            _refreshTimer.Start();
            _mouseInside = Bounds.Contains(Cursor.Position);
            UpdateEmptyOpacity();
            Invalidate();
        }

        private void ExitApplication()
        {
            _exiting = true;
            _notifyIcon.Visible = false;
            SaveWindowPosition();
            Close();
            Application.Exit();
        }
    }
}
