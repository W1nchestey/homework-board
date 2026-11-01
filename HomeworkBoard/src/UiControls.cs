using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HomeworkBoard
{
    internal class RoundedButton : Button
    {
        private bool _hover;
        private bool _pressed;
        private readonly bool _accent;

        public RoundedButton(bool accent)
        {
            _accent = accent;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = Theme.MakeFont(9.2f, FontStyle.Bold);
            TabStop = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _pressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush clear = new SolidBrush(Parent != null ? Parent.BackColor : Theme.Canvas))
                g.FillRectangle(clear, ClientRectangle);
            Color back;
            Color ink;
            if (_accent)
            {
                back = _pressed ? Color.FromArgb(178, 66, 34) : (_hover ? Theme.AccentHover : Theme.Accent);
                ink = Color.White;
            }
            else
            {
                back = _pressed ? Color.FromArgb(222, 216, 204) : (_hover ? Color.FromArgb(233, 228, 218) : Color.FromArgb(240, 236, 228));
                ink = Theme.Ink;
            }
            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (GraphicsPath path = Theme.RoundRect(rect, Dpi.S(10)))
            using (SolidBrush brush = new SolidBrush(back))
                g.FillPath(brush, path);

            if (Focused && ShowFocusCues)
            {
                RectangleF inner = new RectangleF(Dpi.S(3), Dpi.S(3), Width - Dpi.S(6), Height - Dpi.S(6));
                using (GraphicsPath path = Theme.RoundRect(inner, Dpi.S(8)))
                using (Pen pen = new Pen(Color.FromArgb(90, ink), 1f))
                    g.DrawPath(pen, path);
            }

            Theme.DrawText(g, Text, Font, ink, new RectangleF(0, 0, Width, Height),
                StringAlignment.Center, StringAlignment.Center, false);
        }
    }

    internal class ThemedDialog : Form
    {
        private readonly string _dialogTitle;
        private readonly Font _headingFont;
        private readonly Font _eyebrowFont;
        private readonly Font _bodyFont;
        private Rectangle _closeRect;
        private bool _closeHover;
        private bool _dragging;
        private Point _dragStart;
        private Point _dialogStart;

        protected ThemedDialog(string title, int width, int height)
        {
            _dialogTitle = title;
            Text = title;
            _headingFont = Theme.MakeFont(12.5f, FontStyle.Bold);
            _eyebrowFont = Theme.MakeFont(6.8f, FontStyle.Bold);
            _bodyFont = Theme.MakeFont(9f, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Theme.Canvas;
            Font = _bodyFont;
            ClientSize = new Size(Dpi.S(width), Dpi.S(height));
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.EnableRoundedCorners(Handle);
            NativeMethods.EnableDropShadow(Handle);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // The whole surface is painted in OnPaint.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (SolidBrush baseBrush = new SolidBrush(Theme.Canvas))
                g.FillRectangle(baseBrush, ClientRectangle);

            RectangleF all = new RectangleF(0.5f, 0.5f, ClientSize.Width - 1f, ClientSize.Height - 1f);
            using (GraphicsPath path = Theme.RoundRect(all, Dpi.S(16)))
            using (SolidBrush brush = new SolidBrush(Theme.Canvas))
                g.FillPath(brush, path);
            using (GraphicsPath path = Theme.RoundRect(all, Dpi.S(16)))
            using (Pen pen = new Pen(Theme.Border, 1f))
                g.DrawPath(pen, path);

            Theme.DrawText(g, _dialogTitle, _headingFont, Theme.Ink,
                new RectangleF(Dpi.S(24), Dpi.S(10), ClientSize.Width - Dpi.S(92), Dpi.S(42)),
                StringAlignment.Near, StringAlignment.Center, false);
            Theme.DrawText(g, "HOMEWORK BOARD", _eyebrowFont,
                Color.FromArgb(183, 173, 155),
                new RectangleF(Dpi.S(26), Dpi.S(44), ClientSize.Width - Dpi.S(100), Dpi.S(16)),
                StringAlignment.Near, StringAlignment.Center, false);

            _closeRect = new Rectangle(ClientSize.Width - Dpi.S(48), Dpi.S(13), Dpi.S(30), Dpi.S(30));
            if (_closeHover)
            {
                Theme.DrawRoundedFill(g, _closeRect, Dpi.S(15), Color.FromArgb(235, 229, 218));
            }
            using (Pen pen = new Pen(_closeHover ? Theme.Ink : Theme.Muted, Dpi.S(1.5f)))
            {
                int cx = _closeRect.Left + _closeRect.Width / 2;
                int cy = _closeRect.Top + _closeRect.Height / 2;
                int r = Dpi.S(4);
                g.DrawLine(pen, cx - r, cy - r, cx + r, cy + r);
                g.DrawLine(pen, cx + r, cy - r, cx - r, cy + r);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool overClose = _closeRect.Contains(e.Location);
            if (overClose != _closeHover)
            {
                _closeHover = overClose;
                Cursor = overClose ? Cursors.Hand : Cursors.Default;
                Invalidate(_closeRect);
            }
            if (_dragging)
            {
                Point screen = PointToScreen(e.Location);
                Location = new Point(_dialogStart.X + screen.X - _dragStart.X,
                    _dialogStart.Y + screen.Y - _dragStart.Y);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_closeRect.Contains(e.Location))
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                    return;
                }
                if (e.Y < Dpi.S(64))
                {
                    _dragging = true;
                    _dragStart = PointToScreen(e.Location);
                    _dialogStart = Location;
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _dragging = false;
            base.OnMouseUp(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_closeHover)
            {
                _closeHover = false;
                Cursor = Cursors.Default;
                Invalidate(_closeRect);
            }
            base.OnMouseLeave(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _headingFont.Dispose();
            _eyebrowFont.Dispose();
            _bodyFont.Dispose();
            base.OnFormClosed(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    internal static class PromptDialog
    {
        public static string Show(IWin32Window owner, string title, string label, string initial)
        {
            using (PromptForm form = new PromptForm(title, label, initial))
            {
                if (form.ShowDialog(owner) == DialogResult.OK)
                    return form.Value;
            }
            return null;
        }

        private class PromptForm : ThemedDialog
        {
            private readonly TextBox _textBox;
            private readonly Label _error;

            public string Value
            {
                get { return _textBox.Text.Trim(); }
            }

            public PromptForm(string title, string label, string initial)
                : base(title, 360, 214)
            {
                Label caption = Theme.MakeLabel(label, true);
                caption.SetBounds(Dpi.S(24), Dpi.S(70), Dpi.S(312), Dpi.S(22));
                Controls.Add(caption);

                _textBox = new TextBox();
                _textBox.Name = "PromptValue";
                Theme.StyleField(_textBox);
                _textBox.SetBounds(Dpi.S(24), Dpi.S(96), Dpi.S(312), Dpi.S(34));
                _textBox.Text = initial ?? string.Empty;
                _textBox.MaxLength = 24;
                Controls.Add(_textBox);

                _error = new Label();
                _error.AutoSize = false;
                _error.BackColor = Color.Transparent;
                _error.ForeColor = Theme.Danger;
                _error.Font = Theme.MakeFont(8f, FontStyle.Regular);
                _error.SetBounds(Dpi.S(24), Dpi.S(134), Dpi.S(310), Dpi.S(20));
                Controls.Add(_error);

                RoundedButton cancel = new RoundedButton(false);
                cancel.Text = "取消";
                cancel.SetBounds(Dpi.S(210), Dpi.S(160), Dpi.S(58), Dpi.S(36));
                cancel.DialogResult = DialogResult.Cancel;
                Controls.Add(cancel);

                RoundedButton ok = new RoundedButton(true);
                ok.Name = "PromptOk";
                ok.Text = "确定";
                ok.SetBounds(Dpi.S(276), Dpi.S(160), Dpi.S(60), Dpi.S(36));
                ok.Click += OkClick;
                Controls.Add(ok);

                AcceptButton = ok;
                CancelButton = cancel;
                ActiveControl = _textBox;
            }

            protected override void OnShown(EventArgs e)
            {
                base.OnShown(e);
                _textBox.Focus();
                _textBox.SelectAll();
            }

            private void OkClick(object sender, EventArgs e)
            {
                if (Value.Length == 0)
                {
                    _error.Text = "请输入名称。";
                    _textBox.Focus();
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
