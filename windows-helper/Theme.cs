using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PubgZoneHelper;

public static class Theme
{
    public static readonly Color Bg = Color.FromArgb(10, 13, 16);
    public static readonly Color Panel = Color.FromArgb(18, 23, 28);
    public static readonly Color Panel2 = Color.FromArgb(24, 30, 37);
    public static readonly Color Line = Color.FromArgb(42, 52, 62);
    public static readonly Color Text = Color.FromArgb(235, 239, 243);
    public static readonly Color Muted = Color.FromArgb(140, 152, 164);
    public static readonly Color Accent = Color.FromArgb(242, 163, 58);
    public static readonly Color Green = Color.FromArgb(64, 160, 110);

    public static GraphicsPath Rounded(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        if (d <= 0) { path.AddRectangle(rect); return path; }
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public sealed class RoundedPanel : Panel
{
    public int CornerRadius { get; set; } = 16;
    public Color BorderColor { get; set; } = Theme.Line;
    public Color FillColor { get; set; } = Theme.Panel;

    public RoundedPanel()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.Rounded(rect, CornerRadius);
        using var fill = new SolidBrush(FillColor);
        e.Graphics.FillPath(fill, path);
        using var pen = new Pen(BorderColor);
        e.Graphics.DrawPath(pen, path);
        base.OnPaint(e);
    }
}

public sealed class RoundedButton : Button
{
    private bool _hover;
    public int CornerRadius { get; set; } = 14;
    public Color HoverColor { get; set; } = Color.Empty;
    public Color BorderColor { get; set; } = Color.Empty;

    public RoundedButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        BackColor = Theme.Panel2;
        ForeColor = Theme.Text;
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
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        var fillColor = _hover && HoverColor != Color.Empty
            ? HoverColor
            : (Enabled ? BackColor : Color.FromArgb(32, 38, 45));
        using var path = Theme.Rounded(rect, CornerRadius);
        using var fill = new SolidBrush(fillColor);
        e.Graphics.FillPath(fill, path);

        if (BorderColor != Color.Empty)
        {
            using var pen = new Pen(BorderColor);
            e.Graphics.DrawPath(pen, path);
        }

        var flags = TextFormatFlags.HorizontalCenter
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.WordBreak
            | TextFormatFlags.NoPadding;
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            Rectangle.Inflate(rect, -10, -4),
            Enabled ? ForeColor : Theme.Muted,
            flags);
    }
}
