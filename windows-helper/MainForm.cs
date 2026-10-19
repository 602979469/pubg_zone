using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PubgZoneHelper;

public sealed class MainForm : Form
{
    private readonly Label _status = new();
    private readonly Label _patch = new();
    private readonly Label _heroTitle = new();
    private readonly Label _heroWeek = new();
    private readonly FlowLayoutPanel _regionFlow = new();
    private readonly FlowLayoutPanel _heroMaps = new();
    private readonly RoundedPanel _hero = new();
    private readonly RoundedButton _guideButton = new();
    private readonly RoundedButton _refreshMaps = new();
    private readonly RoundedButton _switchButton = new();
    private readonly RoundedButton _playButton = new();
    private readonly List<string> _log = new();

    private AppConfig _config = new();
    private RotationData _rotation = RotationData.Empty;
    private RegionInfo? _selected;
    private bool _loadingMaps;

    public MainForm()
    {
        Text = "PUBG 换区助手";
        ClientSize = new Size(1000, 700);
        MinimumSize = new Size(1020, 740);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Microsoft YaHei UI", 9.5F);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildUi();
        LoadConfig();
        Shown += async (_, _) =>
        {
            _ = LoadRotationAsync();
            if (EnsureConfigured(true, out var startSwitch) && startSwitch)
                await SwitchRegion();
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using var brush = new LinearGradientBrush(
            ClientRectangle,
            Color.FromArgb(11, 15, 19),
            Color.FromArgb(20, 27, 33),
            LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(brush, ClientRectangle);
        base.OnPaint(e);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);
        root.Controls.Add(BuildHeader(), 0, 0);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(22, 18, 22, 18)
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 206));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 252));
        root.Controls.Add(body, 0, 1);

        body.Controls.Add(BuildSidebar(), 0, 0);
        body.Controls.Add(BuildCenter(), 1, 0);
        body.Controls.Add(BuildTools(), 2, 0);
    }

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        header.Paint += (_, e) =>
        {
            using var brush = new LinearGradientBrush(
                header.ClientRectangle,
                Color.FromArgb(26, 34, 42),
                Color.FromArgb(15, 20, 25),
                LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(brush, header.ClientRectangle);
            using var line = new Pen(Theme.Line);
            e.Graphics.DrawLine(line, 0, header.Height - 1, header.Width, header.Height - 1);
        };

        header.Controls.Add(new Label
        {
            Text = "绝地求生",
            Font = new Font("Microsoft YaHei UI", 22F, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 209, 138),
            AutoSize = true,
            Location = new Point(28, 14)
        });
        header.Controls.Add(new Label
        {
            Text = "PUBG 本周地图 · 换区助手",
            ForeColor = Theme.Muted,
            AutoSize = true,
            Location = new Point(31, 57)
        });

        _patch.Text = "UPDATE —";
        _patch.ForeColor = Theme.Accent;
        _patch.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _patch.AutoSize = true;
        _patch.Location = new Point(820, 20);
        _patch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        header.Controls.Add(_patch);

        header.Controls.Add(new Label
        {
            Text = "本周地图",
            ForeColor = Theme.Muted,
            AutoSize = true,
            Location = new Point(820, 48),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        });

        var gear = new RoundedButton
        {
            Text = "⚙",
            Location = new Point(930, 22),
            Size = new Size(42, 42),
            CornerRadius = 21,
            BackColor = Color.FromArgb(32, 41, 51),
            ForeColor = Color.FromArgb(225, 234, 243),
            HoverColor = Color.FromArgb(44, 55, 67),
            BorderColor = Theme.Line,
            Font = new Font("Segoe UI Symbol", 15F)
        };
        gear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        gear.Click += (_, _) => ShowSetup();
        header.Controls.Add(gear);
        return header;
    }

    private Control BuildSidebar()
    {
        var panel = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 14, 0),
            FillColor = Theme.Panel,
            BorderColor = Theme.Line,
            Padding = new Padding(14, 16, 14, 14)
        };
        panel.Controls.Add(new Label
        {
            Text = "大区选择",
            Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
            ForeColor = Theme.Text,
            AutoSize = true,
            Location = new Point(16, 16)
        });
        panel.Controls.Add(new Label
        {
            Text = "本周地图池",
            ForeColor = Theme.Muted,
            AutoSize = true,
            Location = new Point(18, 44)
        });

        _regionFlow.Location = new Point(10, 76);
        _regionFlow.Size = new Size(180, 500);
        _regionFlow.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _regionFlow.FlowDirection = FlowDirection.TopDown;
        _regionFlow.WrapContents = false;
        _regionFlow.AutoScroll = true;
        _regionFlow.BackColor = Color.Transparent;
        panel.Controls.Add(_regionFlow);
        return panel;
    }

    private Control BuildCenter()
    {
        _hero.Dock = DockStyle.Fill;
        _hero.Margin = new Padding(0, 0, 14, 0);
        _hero.FillColor = Theme.Panel;
        _hero.BorderColor = Theme.Line;
        _hero.Padding = new Padding(24, 20, 24, 18);

        _hero.Controls.Add(new Label
        {
            Text = "当前大区",
            ForeColor = Theme.Muted,
            AutoSize = true,
            Location = new Point(24, 20)
        });

        _heroTitle.Text = "正在加载…";
        _heroTitle.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Bold);
        _heroTitle.ForeColor = Theme.Text;
        _heroTitle.AutoSize = true;
        _heroTitle.Location = new Point(24, 46);
        _hero.Controls.Add(_heroTitle);

        _heroWeek.Text = "正在加载地图…";
        _heroWeek.ForeColor = Theme.Muted;
        _heroWeek.AutoSize = true;
        _heroWeek.Location = new Point(27, 92);
        _hero.Controls.Add(_heroWeek);

        _hero.Controls.Add(new Label
        {
            Text = "本周地图",
            ForeColor = Theme.Muted,
            AutoSize = true,
            Location = new Point(24, 132)
        });

        _heroMaps.Location = new Point(24, 158);
        _heroMaps.Size = new Size(430, 112);
        _heroMaps.WrapContents = true;
        _heroMaps.BackColor = Color.Transparent;
        _hero.Controls.Add(_heroMaps);

        _status.Text = "准备就绪";
        _status.ForeColor = Theme.Muted;
        _status.Location = new Point(26, 380);
        _status.Size = new Size(430, 24);
        _hero.Controls.Add(_status);

        var actions = new TableLayoutPanel
        {
            Location = new Point(24, 420),
            Size = new Size(430, 74),
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _switchButton.Text = "一键换区\r\n关闭 Steam/PUBG，打开加速器";
        _switchButton.Dock = DockStyle.Fill;
        _switchButton.Margin = new Padding(0, 0, 7, 0);
        _switchButton.BackColor = Theme.Accent;
        _switchButton.ForeColor = Color.FromArgb(30, 22, 12);
        _switchButton.HoverColor = Color.FromArgb(255, 183, 84);
        _switchButton.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        _switchButton.Click += async (_, _) => await SwitchRegion();

        _playButton.Text = "开启游戏\r\n启动 Steam + PUBG";
        _playButton.Dock = DockStyle.Fill;
        _playButton.Margin = new Padding(7, 0, 0, 0);
        _playButton.BackColor = Theme.Green;
        _playButton.ForeColor = Color.White;
        _playButton.HoverColor = Color.FromArgb(78, 180, 126);
        _playButton.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        _playButton.Click += async (_, _) => await PlayGame();

        actions.Controls.Add(_switchButton, 0, 0);
        actions.Controls.Add(_playButton, 1, 0);
        _hero.Controls.Add(actions);
        return _hero;
    }

    private Control BuildTools()
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 38));

        var guide = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 12),
            FillColor = Theme.Panel,
            BorderColor = Theme.Line,
            Padding = new Padding(16)
        };
        guide.Controls.Add(new Label
        {
            Text = "换区教程",
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
            ForeColor = Theme.Text,
            AutoSize = true,
            Location = new Point(16, 14)
        });
        var tips = new Label
        {
            Text = "1. 一键关闭 Steam/PUBG\n2. 在加速器选目标区服\n3. 回来开启游戏",
            ForeColor = Theme.Muted,
            AutoSize = true,
            Location = new Point(17, 48)
        };
        guide.Controls.Add(tips);

        _guideButton.Text = "查看换区教程";
        _guideButton.Location = new Point(16, 110);
        _guideButton.Size = new Size(218, 42);
        _guideButton.BackColor = Theme.Accent;
        _guideButton.ForeColor = Color.FromArgb(30, 22, 12);
        _guideButton.HoverColor = Color.FromArgb(255, 183, 84);
        _guideButton.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _guideButton.Click += (_, _) => ShowGuide();
        guide.Controls.Add(_guideButton);
        host.Controls.Add(guide, 0, 0);

        var tools = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            FillColor = Theme.Panel,
            BorderColor = Theme.Line,
            Padding = new Padding(16)
        };
        tools.Controls.Add(new Label
        {
            Text = "工具",
            Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
            ForeColor = Theme.Text,
            AutoSize = true,
            Location = new Point(16, 14)
        });

        _refreshMaps.Text = "刷新地图";
        _refreshMaps.Location = new Point(16, 44);
        _refreshMaps.Size = new Size(100, 34);
        _refreshMaps.BackColor = Theme.Panel2;
        _refreshMaps.ForeColor = Theme.Text;
        _refreshMaps.HoverColor = Color.FromArgb(36, 45, 55);
        _refreshMaps.BorderColor = Theme.Line;
        _refreshMaps.Click += async (_, _) => await LoadRotationAsync();
        tools.Controls.Add(_refreshMaps);

        var settings = new RoundedButton
        {
            Text = "设置路径",
            Location = new Point(124, 44),
            Size = new Size(100, 34),
            BackColor = Theme.Panel2,
            ForeColor = Theme.Text,
            HoverColor = Color.FromArgb(36, 45, 55),
            BorderColor = Theme.Line
        };
        settings.Click += (_, _) => ShowSetup();
        tools.Controls.Add(settings);

        var logs = new RoundedButton
        {
            Text = "查看日志",
            Location = new Point(16, 88),
            Size = new Size(100, 34),
            BackColor = Theme.Panel2,
            ForeColor = Theme.Text,
            HoverColor = Color.FromArgb(36, 45, 55),
            BorderColor = Theme.Line
        };
        logs.Click += (_, _) => ShowLog();
        tools.Controls.Add(logs);
        host.Controls.Add(tools, 0, 1);
        return host;
    }

    private void LoadConfig()
    {
        _config = ConfigStore.Load();
        Log($"配置文件: {ConfigStore.FilePath}");
    }

    private async Task LoadRotationAsync()
    {
        if (_loadingMaps) return;
        _loadingMaps = true;
        _refreshMaps.Enabled = false;
        _heroWeek.Text = "正在加载地图…";
        _heroMaps.Controls.Clear();
        try
        {
            _rotation = await RotationClient.LoadAsync();
            if (_rotation.Loaded && _rotation.Regions.Count > 0)
            {
                _selected = _rotation.Regions.Find(r => r.Id == "AS") ?? _rotation.Regions[0];
                _patch.Text = $"UPDATE {_rotation.Patch}";
                _heroWeek.Text = $"本周 · {_rotation.WeekLabel}";
                RenderRegions();
                RenderHero();
                Log($"地图已加载: {_rotation.Patch}, {_rotation.Regions.Count} 个大区");
            }
            else
            {
                _heroTitle.Text = "地图加载失败";
                _heroWeek.Text = "不影响一键换区和开启游戏，稍后可点「刷新地图」";
                _patch.Text = "UPDATE —";
                _status.Text = "地图数据不可用";
            }
        }
        catch (Exception ex)
        {
            _heroWeek.Text = "地图加载失败，不影响换区和开游戏";
            Log($"地图加载失败: {ex.Message}");
        }
        finally
        {
            _loadingMaps = false;
            _refreshMaps.Enabled = true;
        }
    }

    private void RenderRegions()
    {
        _regionFlow.Controls.Clear();
        foreach (var region in _rotation.Regions)
        {
            var selected = _selected?.Id == region.Id;
            var chip = new RoundedButton
            {
                Text = $"{region.Name}  {region.Short}",
                Size = new Size(168, 42),
                Margin = new Padding(0, 0, 0, 7),
                CornerRadius = 10,
                BackColor = selected ? ParseColor(region.Accent) : Theme.Panel2,
                ForeColor = selected ? Color.FromArgb(25, 25, 25) : Theme.Text,
                HoverColor = selected ? ParseColor(region.Accent) : Color.FromArgb(36, 45, 55),
                BorderColor = ParseColor(region.Accent),
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold)
            };
            var captured = region;
            chip.Click += (_, _) =>
            {
                _selected = captured;
                RenderRegions();
                RenderHero();
            };
            _regionFlow.Controls.Add(chip);
        }
    }

    private void RenderHero()
    {
        if (_selected == null) return;
        _heroTitle.Text = _selected.Name;
        _heroMaps.Controls.Clear();
        foreach (var map in _selected.Maps)
        {
            _heroMaps.Controls.Add(new Label
            {
                Text = RotationClient.MapName(map),
                AutoSize = true,
                Padding = new Padding(12, 8, 12, 8),
                Margin = new Padding(0, 0, 8, 8),
                BackColor = Color.FromArgb(31, 40, 49),
                ForeColor = Color.FromArgb(215, 228, 238),
                Font = new Font("Microsoft YaHei UI", 10F)
            });
        }
    }

    private async void ShowGuide()
    {
        if (_selected == null)
        {
            MessageBox.Show(this, "地图还没有加载完成。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var guide = new RegionGuideForm(_selected, _rotation.WeekLabel);
        if (guide.ShowDialog(this) == DialogResult.OK && guide.StartSwitch)
            await SwitchRegion();
    }

    private bool EnsureConfigured(bool firstRun, out bool startSwitch)
    {
        startSwitch = false;
        if (!string.IsNullOrWhiteSpace(_config.PubgPath)
            && File.Exists(_config.PubgPath)
            && !string.IsNullOrWhiteSpace(_config.AcceleratorPath)
            && File.Exists(_config.AcceleratorPath))
            return true;

        using var setup = new SetupForm(_config, firstRun);
        if (setup.ShowDialog(this) == DialogResult.OK)
        {
            _config = ConfigStore.Load();
            startSwitch = setup.StartSwitch;
            Log("路径配置已更新");
            return true;
        }

        SetStatus("先点右上角齿轮，选择 PUBG 和加速器");
        return false;
    }

    private async void ShowSetup()
    {
        using var setup = new SetupForm(_config, false);
        if (setup.ShowDialog(this) != DialogResult.OK) return;
        _config = ConfigStore.Load();
        SetStatus("路径已保存");
        Log("路径配置已更新");
        if (setup.StartSwitch) await SwitchRegion();
    }

    private async Task SwitchRegion()
    {
        if (!EnsureConfigured(false, out _)) return;
        SetBusy(true, "正在关闭 Steam / PUBG…");
        try
        {
            await Task.Run(() => ProcessTools.CloseSteamAndPubg(Log));
            SetStatus("正在打开加速器…");
            if (!Launcher.OpenAccelerator(_config, Log))
            {
                SetStatus("加速器打开失败");
                MessageBox.Show(
                    this,
                    "没有打开加速器。请点右上角齿轮重新选择 DolphinQ.exe，或把协议链接改成 dolphin://。",
                    "PUBG 换区助手",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            SetStatus("已打开加速器。选好区服后，点「开启游戏」");
        }
        catch (Exception ex)
        {
            SetStatus("换区失败");
            Log($"换区失败: {ex.Message}");
            MessageBox.Show(this, ex.Message, "换区失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private async Task PlayGame()
    {
        if (!EnsureConfigured(false, out _)) return;
        SetBusy(true, "正在启动 Steam 和 PUBG…");
        try
        {
            var ok = await Task.Run(() => Launcher.StartGame(_config, Log));
            if (ok)
            {
                SetStatus("已发送启动命令，等待游戏进入");
            }
            else
            {
                SetStatus("启动游戏失败");
                MessageBox.Show(
                    this,
                    "启动失败。请确认 Steam 已安装，并重新选择 TslGame.exe。",
                    "PUBG 换区助手",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            SetStatus("启动游戏失败");
            Log($"启动失败: {ex.Message}");
            MessageBox.Show(this, ex.Message, "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private void SetBusy(bool busy, string? status)
    {
        _switchButton.Enabled = !busy;
        _playButton.Enabled = !busy;
        if (status is not null) SetStatus(status);
    }

    private void SetStatus(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetStatus(text)));
            return;
        }
        _status.Text = text;
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => Log(message)));
            return;
        }
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        _log.Add(line);
        if (_log.Count > 500) _log.RemoveAt(0);
    }

    private void ShowLog()
    {
        using var dialog = new Form
        {
            Text = "日志",
            ClientSize = new Size(640, 420),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = Theme.Bg,
            ForeColor = Theme.Text
        };
        dialog.Controls.Add(new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(12, 16, 20),
            ForeColor = Color.FromArgb(190, 205, 215),
            Font = new Font("Consolas", 9F),
            Text = string.Join(Environment.NewLine, _log)
        });
        dialog.ShowDialog(this);
    }

    private static Color ParseColor(string value)
    {
        try
        {
            return ColorTranslator.FromHtml(value);
        }
        catch
        {
            return Theme.Accent;
        }
    }
}
