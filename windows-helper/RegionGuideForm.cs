using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PubgZoneHelper;

public sealed class RegionGuideForm : Form
{
    public bool StartSwitch { get; private set; }

    private static readonly Dictionary<string, string[]> Nodes = new()
    {
        ["AS"] = new[] { "亚洲 - 香港", "亚洲 - 日本", "亚洲 - 韩国" },
        ["SEA"] = new[] { "东南亚 - 新加坡", "东南亚 - 马来西亚" },
        ["NA"] = new[] { "北美 - 美西 / 洛杉矶", "北美 - 美东" },
        ["EU"] = new[] { "欧洲 - 德国 / 法兰克福", "欧洲 - 荷兰" },
        ["SA"] = new[] { "南美 - 巴西 / 圣保罗" },
        ["RU"] = new[] { "俄罗斯 - 莫斯科" }
    };

    public RegionGuideForm(RegionInfo region, string weekLabel)
    {
        Text = $"{region.Name} · 换区教程";
        ClientSize = new Size(700, 560);
        MinimumSize = new Size(720, 600);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Microsoft YaHei UI", 9.5F);
        AutoScaleMode = AutoScaleMode.Dpi;

        var title = new Label
        {
            Text = $"{region.Name} 换区教程",
            Font = new Font("Microsoft YaHei UI", 19F, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 209, 138),
            AutoSize = true,
            Location = new Point(26, 20)
        };
        Controls.Add(title);

        var week = new Label
        {
            Text = string.IsNullOrWhiteSpace(weekLabel) ? "本周地图" : $"本周 · {weekLabel}",
            ForeColor = Theme.Muted,
            AutoSize = true,
            Location = new Point(29, 60)
        };
        Controls.Add(week);

        var maps = new FlowLayoutPanel
        {
            Location = new Point(26, 92),
            Size = new Size(648, 44),
            WrapContents = false,
            BackColor = Color.Transparent
        };
        foreach (var map in region.Maps)
        {
            maps.Controls.Add(new Label
            {
                Text = RotationClient.MapName(map),
                AutoSize = true,
                Padding = new Padding(10, 6, 10, 6),
                Margin = new Padding(0, 0, 8, 0),
                BackColor = Color.FromArgb(32, 42, 50),
                ForeColor = Color.FromArgb(210, 225, 235)
            });
        }
        Controls.Add(maps);

        var stepsTitle = new Label
        {
            Text = "换区步骤",
            Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
            ForeColor = Theme.Text,
            AutoSize = true,
            Location = new Point(26, 150)
        };
        Controls.Add(stepsTitle);

        var steps = new[]
        {
            "点下面的「一键换区」：程序会关闭 Steam / PUBG，并打开加速器。",
            $"在加速器里选目标节点：{string.Join(" / ", Nodes.TryGetValue(region.Id, out var nodes) ? nodes : new[] { "目标区服节点" })}。",
            "等加速器显示已加速后，回到本窗口点「一键换区」旁边的关闭，再点主页「开启游戏」。",
            "进大厅后看延迟；开一局确认地图池和本页一致。",
            "如果还是原区：换同区另一个节点，再走一次一键换区。",
            "改 Windows 时区不会换匹配大区；时区只影响时间显示。"
        };
        var box = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Panel,
            ForeColor = Color.FromArgb(205, 216, 226),
            Font = new Font("Microsoft YaHei UI", 10F),
            Location = new Point(26, 182),
            Size = new Size(648, 250),
            Text = string.Join(Environment.NewLine + Environment.NewLine, Prefix(steps))
        };
        Controls.Add(box);

        var switchButton = new RoundedButton
        {
            Text = "一键换区",
            Location = new Point(26, 456),
            Size = new Size(220, 58),
            BackColor = Theme.Accent,
            ForeColor = Color.FromArgb(30, 22, 12),
            HoverColor = Color.FromArgb(255, 183, 84),
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold)
        };
        switchButton.Click += (_, _) =>
        {
            StartSwitch = true;
            DialogResult = DialogResult.OK;
            Close();
        };
        Controls.Add(switchButton);

        var close = new RoundedButton
        {
            Text = "关闭",
            Location = new Point(262, 456),
            Size = new Size(110, 58),
            BackColor = Theme.Panel2,
            ForeColor = Theme.Text,
            HoverColor = Color.FromArgb(36, 45, 55),
            BorderColor = Theme.Line
        };
        close.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(close);

        var note = new Label
        {
            Text = "跨区延迟会明显增加；频繁切换网络出口可能触发账号验证。",
            ForeColor = Color.FromArgb(160, 135, 95),
            AutoSize = true,
            Location = new Point(390, 478)
        };
        Controls.Add(note);
    }

    private static IEnumerable<string> Prefix(IEnumerable<string> steps)
    {
        var index = 1;
        foreach (var step in steps)
            yield return $"{index++}. {step}";
    }
}
