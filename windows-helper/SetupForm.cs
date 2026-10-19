using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PubgZoneHelper;

public sealed class SetupForm : Form
{
    private readonly AppConfig _config;
    private readonly TextBox _pubg = new();
    private readonly TextBox _accelerator = new();
    private readonly TextBox _steam = new();
    private readonly Button _save = new();
    private readonly Button _saveSwitch = new();

    public bool StartSwitch { get; private set; }

    public SetupForm(AppConfig config, bool firstRun)
    {
        _config = config;
        Text = firstRun ? "首次使用：选择两个程序" : "设置路径";
        ClientSize = new Size(650, 330);
        MinimumSize = new Size(670, 370);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(18, 22, 27);
        ForeColor = Color.FromArgb(235, 238, 242);
        Font = new Font("Microsoft YaHei UI", 9.5F);
        AutoScaleMode = AutoScaleMode.Dpi;

        var title = new Label
        {
            Text = firstRun ? "第一次使用 · 3 步" : "程序路径",
            Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 209, 138),
            AutoSize = true,
            Location = new Point(24, 18)
        };
        Controls.Add(title);

        AddRow("1  PUBG 游戏", "TslGame.exe", _pubg, 58);
        AddRow("2  加速器", "DolphinQ.exe", _accelerator, 112);
        AddRow("3  Steam", "steam.exe", _steam, 166);

        var auto = new Button
        {
            Text = "自动检测",
            Location = new Point(24, 224),
            Size = new Size(110, 34)
        };
        StyleSmall(auto);
        auto.Click += (_, _) =>
        {
            var detected = new AppConfig();
            ConfigStore.AutoDetect(detected);
            if (!string.IsNullOrWhiteSpace(detected.PubgPath)) _pubg.Text = detected.PubgPath;
            if (!string.IsNullOrWhiteSpace(detected.AcceleratorPath)) _accelerator.Text = detected.AcceleratorPath;
            if (!string.IsNullOrWhiteSpace(detected.SteamPath)) _steam.Text = detected.SteamPath;
        };
        Controls.Add(auto);

        _save.Text = "保存";
        _save.Location = new Point(350, 224);
        _save.Size = new Size(100, 36);
        _save.FlatStyle = FlatStyle.Flat;
        _save.FlatAppearance.BorderColor = Color.FromArgb(242, 163, 58);
        _save.BackColor = Color.FromArgb(242, 163, 58);
        _save.ForeColor = Color.FromArgb(30, 22, 12);
        _save.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _save.Cursor = Cursors.Hand;
        _save.Click += (_, _) => SaveAndClose(false);
        Controls.Add(_save);

        _saveSwitch.Text = "保存并一键换区";
        _saveSwitch.Location = new Point(462, 224);
        _saveSwitch.Size = new Size(164, 36);
        _saveSwitch.FlatStyle = FlatStyle.Flat;
        _saveSwitch.FlatAppearance.BorderColor = Color.FromArgb(242, 163, 58);
        _saveSwitch.BackColor = Color.FromArgb(242, 163, 58);
        _saveSwitch.ForeColor = Color.FromArgb(30, 22, 12);
        _saveSwitch.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _saveSwitch.Cursor = Cursors.Hand;
        _saveSwitch.Click += (_, _) => SaveAndClose(true);
        Controls.Add(_saveSwitch);

        var cancel = new Button
        {
            Text = "取消",
            Location = new Point(144, 224),
            Size = new Size(78, 36)
        };
        StyleSmall(cancel);
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(cancel);

        _pubg.TextChanged += (_, _) => UpdateSave();
        _accelerator.TextChanged += (_, _) => UpdateSave();
        _steam.TextChanged += (_, _) => UpdateSave();
        UpdateSave();
    }

    private void AddRow(string label, string hint, TextBox box, int top)
    {
        Controls.Add(new Label
        {
            Text = label,
            Location = new Point(24, top + 3),
            Size = new Size(90, 24),
            ForeColor = Color.FromArgb(175, 186, 198)
        });
        box.Location = new Point(118, top);
        box.Size = new Size(390, 28);
        box.BackColor = Color.FromArgb(24, 30, 37);
        box.ForeColor = Color.FromArgb(230, 236, 242);
        Controls.Add(box);

        var choose = new Button
        {
            Text = "选择",
            Location = new Point(518, top - 1),
            Size = new Size(108, 30)
        };
        StyleSmall(choose);
        choose.Click += (_, _) => Choose(box, label, hint);
        Controls.Add(choose);
    }

    private void Choose(TextBox target, string label, string hint)
    {
        using var dialog = new OpenFileDialog
        {
            Title = $"选择 {label}",
            Filter = $"{hint}|{hint}|EXE|*.exe",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            target.Text = dialog.FileName;
    }

    private static void StyleSmall(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.FromArgb(66, 78, 92);
        button.BackColor = Color.FromArgb(38, 47, 58);
        button.ForeColor = Color.FromArgb(220, 228, 236);
        button.Cursor = Cursors.Hand;
    }

    private void UpdateSave()
    {
        _save.Enabled = File.Exists(_pubg.Text.Trim())
            && File.Exists(_accelerator.Text.Trim());
        _saveSwitch.Enabled = _save.Enabled;
    }

    private void SaveAndClose(bool startSwitch)
    {
        _config.PubgPath = _pubg.Text.Trim();
        _config.AcceleratorPath = _accelerator.Text.Trim();
        _config.SteamPath = _steam.Text.Trim();
        _config.PreferUrl = true;
        if (string.IsNullOrWhiteSpace(_config.AcceleratorUrl))
            _config.AcceleratorUrl = "dolphin://";
        ConfigStore.Save(_config);
        StartSwitch = startSwitch;
        DialogResult = DialogResult.OK;
        Close();
    }
}
