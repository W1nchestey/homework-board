using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HomeworkBoard
{
    internal static class Theme
    {
        public static readonly Color Canvas = Color.FromArgb(247, 244, 237);
        public static readonly Color CanvasEmpty = Color.FromArgb(253, 252, 249);
        public static readonly Color Card = Color.FromArgb(255, 255, 252);
        public static readonly Color CardHover = Color.FromArgb(255, 252, 246);
        public static readonly Color Ink = Color.FromArgb(32, 34, 31);
        public static readonly Color Muted = Color.FromArgb(122, 126, 118);
        public static readonly Color Faint = Color.FromArgb(161, 163, 155);
        public static readonly Color Accent = Color.FromArgb(221, 92, 52);
        public static readonly Color AccentHover = Color.FromArgb(198, 75, 38);
        public static readonly Color AccentSoft = Color.FromArgb(250, 230, 220);
        public static readonly Color Border = Color.FromArgb(232, 226, 214);
        public static readonly Color BorderSoft = Color.FromArgb(240, 236, 227);
        public static readonly Color SubjectBack = Color.FromArgb(245, 239, 226);
        public static readonly Color SubjectInk = Color.FromArgb(125, 96, 48);
        public static readonly Color Danger = Color.FromArgb(190, 64, 50);
        public static readonly Color DangerSoft = Color.FromArgb(253, 235, 232);
        public static readonly Color Warm = Color.FromArgb(181, 119, 39);
        public static readonly Color White = Color.White;

        public const string FontFamily = "Microsoft YaHei UI";

        public static Font MakeFont(float points, FontStyle style)
        {
            return new Font(FontFamily, points, style, GraphicsUnit.Point);
        }

        public static GraphicsPath RoundRect(RectangleF bounds, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0.5f)
            {
                path.AddRectangle(bounds);
                return path;
            }
            float d = radius * 2f;
            if (d > bounds.Width) d = bounds.Width;
            if (d > bounds.Height) d = bounds.Height;
            path.AddArc(bounds.Left, bounds.Top, d, d, 180f, 90f);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270f, 90f);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0f, 90f);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        public static Color Mix(Color a, Color b, float amount)
        {
            if (amount < 0f) amount = 0f;
            if (amount > 1f) amount = 1f;
            return Color.FromArgb(
                (int)Math.Round(a.R + (b.R - a.R) * amount),
                (int)Math.Round(a.G + (b.G - a.G) * amount),
                (int)Math.Round(a.B + (b.B - a.B) * amount));
        }

        public static Color PlatformColor(string platform)
        {
            Color[] palette = new Color[]
            {
                Color.FromArgb(62, 112, 92),
                Color.FromArgb(54, 96, 128),
                Color.FromArgb(130, 92, 53),
                Color.FromArgb(112, 78, 115),
                Color.FromArgb(70, 105, 116),
                Color.FromArgb(125, 88, 76)
            };
            int hash = 0;
            if (platform != null)
            {
                for (int i = 0; i < platform.Length; i++)
                    hash = unchecked(hash * 31 + platform[i]);
            }
            if (hash < 0) hash = -hash;
            return palette[hash % palette.Length];
        }

        public static Color PlatformBack(string platform)
        {
            Color color = PlatformColor(platform);
            return Mix(color, Color.White, 0.84f);
        }

        public static void DrawText(Graphics g, string text, Font font, Color color,
            RectangleF rect, StringAlignment hAlign, StringAlignment vAlign, bool ellipsis)
        {
            using (SolidBrush brush = new SolidBrush(color))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = hAlign;
                format.LineAlignment = vAlign;
                format.FormatFlags = StringFormatFlags.NoWrap;
                format.HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None;
                if (ellipsis)
                    format.Trimming = StringTrimming.EllipsisCharacter;
                g.DrawString(text ?? string.Empty, font, brush, rect, format);
            }
        }

        public static void DrawText(Graphics g, string text, Font font, Color color,
            RectangleF rect, StringAlignment hAlign, StringAlignment vAlign)
        {
            DrawText(g, text, font, color, rect, hAlign, vAlign, true);
        }

        public static void DrawRoundedFill(Graphics g, RectangleF rect, float radius, Color color)
        {
            using (GraphicsPath path = RoundRect(rect, radius))
            using (SolidBrush brush = new SolidBrush(color))
                g.FillPath(brush, path);
        }

        public static void DrawRoundedOutline(Graphics g, RectangleF rect, float radius, Color color, float width)
        {
            using (GraphicsPath path = RoundRect(rect, radius))
            using (Pen pen = new Pen(color, width))
                g.DrawPath(pen, path);
        }

        public static void StyleButton(Button button, bool accent)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.UseVisualStyleBackColor = false;
            button.Font = MakeFont(9.5f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            if (accent)
            {
                button.BackColor = Accent;
                button.ForeColor = Color.White;
                button.FlatAppearance.MouseOverBackColor = AccentHover;
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(177, 66, 34);
            }
            else
            {
                button.BackColor = Color.FromArgb(239, 235, 227);
                button.ForeColor = Theme.Ink;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 227, 217);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(223, 217, 206);
            }
        }

        public static void StyleField(TextBox box)
        {
            box.BorderStyle = BorderStyle.FixedSingle;
            box.BackColor = Color.White;
            box.ForeColor = Ink;
            box.Font = MakeFont(10.5f, FontStyle.Regular);
        }

        public static void StyleCombo(ComboBox combo)
        {
            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = Color.White;
            combo.ForeColor = Ink;
            combo.Font = MakeFont(10f, FontStyle.Regular);
        }

        public static Label MakeLabel(string text, bool muted)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = false;
            label.BackColor = Color.Transparent;
            label.ForeColor = muted ? Muted : Ink;
            label.Font = MakeFont(muted ? 8.2f : 9f, FontStyle.Regular);
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }
    }
}
