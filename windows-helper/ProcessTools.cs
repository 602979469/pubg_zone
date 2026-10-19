using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading;

namespace PubgZoneHelper;

public static class ProcessTools
{
    public static readonly string[] TargetNames =
    {
        "steam",
        "steamwebhelper",
        "gameoverlayui",
        "steamerrorreporter",
        "steamservice",
        "TslGame",
        "TslGame_BE"
    };

    public static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static int CloseSteamAndPubg(Action<string> log)
    {
        var closed = 0;
        foreach (var name in TargetNames)
        {
            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName(name);
            }
            catch (Exception ex)
            {
                log($"无法枚举 {name}: {ex.Message}");
                continue;
            }

            foreach (var process in processes)
            {
                try
                {
                    log($"关闭 {process.ProcessName} (PID {process.Id})");
                    if (process.MainWindowHandle != IntPtr.Zero)
                        process.CloseMainWindow();

                    if (!process.WaitForExit(2500))
                    {
                        process.Kill();
                        process.WaitForExit(2500);
                    }
                    closed++;
                }
                catch (Exception ex)
                {
                    log($"关闭 {process.ProcessName} 失败: {ex.Message}");
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        Thread.Sleep(800);
        var remaining = new List<string>();
        foreach (var name in TargetNames)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName(name))
                {
                    remaining.Add(process.ProcessName);
                    process.Dispose();
                }
            }
            catch
            {
                // Ignore access-denied probes.
            }
        }

        if (remaining.Count > 0)
            log($"仍在运行: {string.Join(", ", remaining.Distinct())}");
        else
            log("Steam / PUBG 相关进程已全部退出");
        return closed;
    }

    public static int CountRunning(string name)
    {
        try
        {
            var processes = Process.GetProcessesByName(name);
            foreach (var process in processes) process.Dispose();
            return processes.Length;
        }
        catch
        {
            return -1;
        }
    }
}
