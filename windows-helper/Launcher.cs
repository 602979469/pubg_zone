using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace PubgZoneHelper;

public static class Launcher
{
    public static bool OpenUrl(string url, Action<string> log)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            log("协议链接为空");
            return false;
        }
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            log($"已打开: {url}");
            return true;
        }
        catch (Exception ex)
        {
            log($"打开链接失败: {ex.Message}");
            return false;
        }
    }

    public static bool StartExe(string path, Action<string> log)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            log("EXE 路径不存在");
            return false;
        }
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            log($"已启动: {path}");
            return true;
        }
        catch (Exception ex)
        {
            log($"启动 EXE 失败: {ex.Message}");
            return false;
        }
    }

    public static bool OpenAccelerator(AppConfig config, Action<string> log)
    {
        if (config.PreferUrl && OpenUrl(config.AcceleratorUrl, log))
            return true;
        if (StartExe(config.AcceleratorPath, log))
            return true;
        log("没有可用的加速器入口，请选择 DolphinQ.exe 或填写 dolphin:// 链接");
        return false;
    }

    public static bool StartSteam(AppConfig config, Action<string> log)
    {
        if (StartExe(config.SteamPath, log)) return true;
        log("未找到 Steam，请在设置里选择 steam.exe");
        return false;
    }

    public static bool StartPubg(AppConfig config, Action<string> log)
    {
        var appId = string.IsNullOrWhiteSpace(config.PubgAppId) ? "578080" : config.PubgAppId.Trim();
        return OpenUrl($"steam://rungameid/{appId}", log);
    }

    public static bool StartGame(AppConfig config, Action<string> log)
    {
        if (Process.GetProcessesByName("steam").Length == 0)
        {
            if (!StartSteam(config, log)) return false;
            log("等待 Steam 启动…");
            for (var i = 0; i < 20; i++)
            {
                Thread.Sleep(500);
                if (Process.GetProcessesByName("steam").Length > 0) break;
            }
            Thread.Sleep(2000);
        }

        var appId = string.IsNullOrWhiteSpace(config.PubgAppId) ? "578080" : config.PubgAppId.Trim();
        if (OpenUrl($"steam://rungameid/{appId}", log)) return true;

        if (!string.IsNullOrWhiteSpace(config.PubgPath) && File.Exists(config.PubgPath))
            return StartExe(config.PubgPath, log);

        log("启动 PUBG 失败，请检查 Steam 或重新选择 TslGame.exe");
        return false;
    }
}
