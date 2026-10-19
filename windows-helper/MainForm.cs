using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PubgZoneHelper;

public sealed class MainForm : Form
{
    private readonly TextBox _log = new();
    private readonly ComboBox _mode = new();
    private readonly TextBox _url = new();
    private readonly TextBox _acceleratorPath = new();
    private readonly TextBox _steamPath = new();
    private readonly TextBox _pubgId = new();
    private readonly Button _closeButton = new();
    private readonly Button _acceleratorButton = new();
    private readonly Button _steamButton = new();
    private readonly Button _pubgButton = new();
    private readonly Button _flowButton = new();
    private readonly Button _steamPubgButton = new();
    private AppConfig _config = new();

    public MainForm()
    {
        Text = "PUBG 换区助手";
        ClientSize = new Size(760, 620);
        MinimumSize = new Size(780, 660);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(18, 22, 27);
        ForeColor = Color.FromArgb(235, 238, 242);
        Font = new Font("Microsoft YaHei UI", 9F);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildUi();
        LoadConfig();
    }

    private void BuildUi()
    {
        var title = new Label
        {
            Text = "PUBG 换区助手",
            Font = new Font("Microsoft YaHei UI", 19F, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 209, 138),
            AutoSize = true,
            Location = new Point(22, 16)
        };
        var subtitle = new Label
        {
            Text = "关闭 Steam / PUBG → 打开加速器 → 选好区服 → 重新启动",
            ForeColor = Color.FromArgb(150, 160, 172),
            AutoSize = true,
            Location = new Point(24, 54)
        };
        Controls.Add(title);
        Controls.Add(subtitle);

        var actions = new FlowLayoutPanel
        {
            Location = new Point(20, 86),
            Size = new Size(330, 280),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        ConfigureButton(_flowButton, "一键换区流程", Color.FromArgb(242, 163, 58), Color.FromArgb(28, 22, 14));
        ConfigureButton(_closeButton, "一键关闭 Steam / PUBG", Color.FromArgb(190, 74, 74), Color.White);
        ConfigureButton(_acceleratorButton, "打开加速器", Color.FromArgb(52, 64, 78), Color.White);
        ConfigureButton(_steamButton, "启动 Steam", Color.FromArgb(52, 64, 78), Color.White);
        ConfigureButton(_pubgButton, "启动 PUBG", Color.FromArgb(52, 64, 78), Color.White);
        ConfigureButton(_steamPubgButton, "启动 Steam + PUBG", Color.FromArgb(52, 64, 78), Color.White);

        actions.Controls.AddRange(new Control[]
        {
            _flowButton, _closeButton, _acceleratorButton, _steamButton, _pubgButton, _steamPubgButton
        });
        Controls.Add(actions);

        var settings = new GroupBox
        {
            Text = "加速器与路径设置",
            Location = new Point(370, 82),
            Size = new Size(370, 300),
            ForeColor = Color.FromArgb(200, 210, 220)
        };
        Controls.Add(settings);

        AddLabel(settings, "打开方式", 14, 28);
        _mode.Items.AddRange(new object[] { "协议链接（推荐）", "直接启动 EXE" });
        _mode.DropDownStyle = ComboBoxStyle.DropDownList;
        _mode.Location = new Point(92, 24);
        _mode.Width = 250;
        StyleInput(_mode);
        settings.Controls.Add(_mode);

        AddLabel(settings, "协议链接", 14, 64);
        _url.Location = new Point(92, 60);
        _url.Width = 250;
        StyleInput(_url);
        settings.Controls.Add(_url);

        AddLabel(settings, "加速器 EXE", 14, 100);
        _acceleratorPath.Location = new Point(92, 96);
        _acceleratorPath.Width = 176;
        StyleInput(_acceleratorPath);
        settings.Controls.Add(_acceleratorPath);
        var chooseAccel = new Button { Text = "选择", Location = new Point(274, 95), Size = new Size(68, 26) };
        StyleSmallButton(chooseAccel);
        chooseAccel.Click += (_, _) => ChooseFile(_acceleratorPath, "选择加速器 EXE", "DolphinQ.exe|DolphinQ.exe|EXE|*.exe");
        settings.Controls.Add(chooseAccel);

        AddLabel(settings, "Steam EXE", 14, 136);
        _steamPath.Location = new Point(92, 132);
        _steamPath.Width = 176;
        StyleInput(_steamPath);
        settings.Controls.Add(_steamPath);
        var chooseSteam = new Button { Text = "选择", Location = new Point(274, 131), Size = new Size(68, 26) };
        StyleSmallButton(chooseSteam);
        chooseSteam.Click += (_, _) => ChooseFile(_steamPath, "选择 steam.exe", "steam.exe|steam.exe|EXE|*.exe");
        settings.Controls.Add(chooseSteam);

        AddLabel(settings, "PUBG AppID", 14, 172);
        _pubgId.Location = new Point(92, 168);
        _pubgId.Width = 100;
        StyleInput(_pubgId);
        settings.Controls.Add(_pubgId);

        var save = new Button { Text = "保存配置", Location = new Point(92, 212), Size = new Size(100, 30) };
        StyleSmallButton(save);
        save.Click += (_, _) => SaveConfig(true);
        settings.Controls.Add(save);

        var detect = new Button { Text = "重新检测", Location = new Point(202, 212), Size = new Size(100, 30) };
        StyleSmallButton(detect);
        detect.Click += (_, _) =>
        {
            _config = new AppConfig { PreferUrl = true };
            ConfigStore.AutoDetect(_config);
            BindConfig();
            SaveConfig(true);
            Log("已重新检测路径");
        };
        settings.Controls.Add(detect);

        var hint = new Label
        {
            Text = "默认 dolphin:// 打开面板；也可填 dolphin://start_acc_game/5 直达游戏。\n协议打开失败会自动尝试 EXE。",
            ForeColor = Color.FromArgb(130, 142, 155),
            Location = new Point(14, 252),
            Size = new Size(340, 40)
        };
        settings.Controls.Add(hint);

        _log.Location = new Point(20, 398);
        _log.Size = new Size(720, 200);
        _log.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.BackColor = Color.FromArgb(12, 16, 20);
        _log.ForeColor = Color.FromArgb(190, 205, 215);
        _log.Font = new Font("Consolas", 9F);
        _log.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(_log);

        _mode.SelectedIndexChanged += (_, _) => UpdateMode();
        _flowButton.Click += async (_, _) => await RunFlow();
        _closeButton.Click += async (_, _) => await RunClose();
        _acceleratorButton.Click += (_, _) => RunAccelerator();
        _steamButton.Click += (_, _) => RunSteam();
        _pubgButton.Click += (_, _) => RunPubg();
        _steamPubgButton.Click += async (_, _) => await RunSteamPubg();
    }

    private static void AddLabel(Control parent, string text, int x, int y)
    {
        parent.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = Color.FromArgb(165, 176, 188)
        });
    }

    private static void ConfigureButton(Button button, string text, Color background, Color foreground)
    {
        button.Text = text;
        button.Size = new Size(310, 40);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.FromArgb(55, 66, 78);
        button.BackColor = background;
        button.ForeColor = foreground;
        button.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(0, 0, 0, 8);
    }

    private static void StyleSmallButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.FromArgb(70, 82, 96);
        button.BackColor = Color.FromArgb(40, 49, 60);
        button.ForeColor = Color.FromArgb(225, 232, 240);
        button.Cursor = Cursors.Hand;
    }

    private static void StyleInput(Control control)
    {
        control.BackColor = Color.FromArgb(24, 30, 37);
        control.ForeColor = Color.FromArgb(230, 236, 242);
    }

    private void LoadConfig()
    {
        _config = ConfigStore.Load();
        BindConfig();
        Log($"配置文件: {ConfigStore.FilePath}");
        Log($"管理员权限: {ProcessTools.IsAdministrator()}");
        Log($"Steam: {(_config.SteamPath.Length > 0 ? _config.SteamPath : "未找到")}");
        Log($"加速器: {(_config.AcceleratorPath.Length > 0 ? _config.AcceleratorPath : "未找到")}");
    }

    private void BindConfig()
    {
        _mode.SelectedIndex = _config.PreferUrl ? 0 : 1;
        _url.Text = _config.AcceleratorUrl;
        _acceleratorPath.Text = _config.AcceleratorPath;
        _steamPath.Text = _config.SteamPath;
        _pubgId.Text = _config.PubgAppId;
        UpdateMode();
    }

    private void UpdateMode()
    {
        var urlMode = _mode.SelectedIndex == 0;
        _url.Enabled = urlMode;
        _acceleratorPath.Enabled = !urlMode;
    }

    private void SaveConfig(bool log)
    {
        _config.PreferUrl = _mode.SelectedIndex == 0;
        _config.AcceleratorUrl = _url.Text.Trim();
        _config.AcceleratorPath = _acceleratorPath.Text.Trim();
        _config.SteamPath = _steamPath.Text.Trim();
        _config.PubgAppId = _pubgId.Text.Trim();
        ConfigStore.Save(_config);
        if (log) Log("配置已保存");
    }

    private static void ChooseFile(TextBox target, string title, string filter)
    {
        using var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true
        };
        if (dialog.ShowDialog() == DialogResult.OK)
            target.Text = dialog.FileName;
    }

    private async Task RunClose()
    {
        SaveConfig(false);
        SetButtons(false);
        Log("开始关闭 Steam / PUBG 相关进程…");
        try
        {
            await Task.Run(() => ProcessTools.CloseSteamAndPubg(Log));
        }
        catch (Exception ex)
        {
            Log($"关闭失败: {ex.Message}");
        }
        finally
        {
            SetButtons(true);
        }
    }

    private async Task RunFlow()
    {
        await RunClose();
        RunAccelerator();
        MessageBox.Show(
            "Steam / PUBG 已关闭，加速器已打开。\n请在加速器里选好区服，再点「启动 Steam」或「启动 PUBG」。",
            "PUBG 换区助手",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void RunAccelerator()
    {
        SaveConfig(false);
        Launcher.OpenAccelerator(_config, Log);
    }

    private void RunSteam()
    {
        SaveConfig(false);
        Launcher.StartSteam(_config, Log);
    }

    private void RunPubg()
    {
        SaveConfig(false);
        Launcher.StartPubg(_config, Log);
    }

    private async Task RunSteamPubg()
    {
        SaveConfig(false);
        if (!Launcher.StartSteam(_config, Log)) return;
        Log("等待 8 秒后启动 PUBG…");
        await Task.Delay(8000);
        Launcher.StartPubg(_config, Log);
    }

    private void SetButtons(bool enabled)
    {
        foreach (var button in new[]
        {
            _flowButton, _closeButton, _acceleratorButton, _steamButton, _pubgButton, _steamPubgButton
        })
        {
            button.Enabled = enabled;
        }
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => Log(message)));
            return;
        }
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
}
