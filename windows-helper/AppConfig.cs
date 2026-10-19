using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace PubgZoneHelper;

public sealed class AppConfig
{
    public bool PreferUrl { get; set; } = true;
    public string AcceleratorUrl { get; set; } = "dolphin://";
    public string AcceleratorPath { get; set; } = "";
    public string SteamPath { get; set; } = "";
    public string PubgPath { get; set; } = "";
    public string PubgAppId { get; set; } = "578080";
}

public static class ConfigStore
{
    public static string Dir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PUBGZoneHelper");

    public static string FilePath => Path.Combine(Dir, "config.ini");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var config = new AppConfig();
                foreach (var raw in File.ReadAllLines(FilePath))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    var split = line.IndexOf('=');
                    if (split <= 0) continue;
                    var key = line.Substring(0, split).Trim();
                    var value = line.Substring(split + 1).Trim();
                    switch (key)
                    {
                        case "PreferUrl":
                            bool.TryParse(value, out var prefer);
                            config.PreferUrl = prefer;
                            break;
                        case "AcceleratorUrl":
                            config.AcceleratorUrl = value;
                            break;
                        case "AcceleratorPath":
                            config.AcceleratorPath = value;
                            break;
                        case "SteamPath":
                            config.SteamPath = value;
                            break;
                        case "PubgPath":
                            config.PubgPath = value;
                            break;
                        case "PubgAppId":
                            config.PubgAppId = value;
                            break;
                    }
                }
                AutoDetect(config);
                return config;
            }
        }
        catch
        {
            // Fall through to a fresh config.
        }

        var fresh = new AppConfig();
        AutoDetect(fresh);
        Save(fresh);
        return fresh;
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllLines(FilePath, new[]
        {
            "# PUBGZoneHelper config",
            "PreferUrl=" + config.PreferUrl.ToString(CultureInfo.InvariantCulture),
            "AcceleratorUrl=" + config.AcceleratorUrl,
            "AcceleratorPath=" + config.AcceleratorPath,
            "SteamPath=" + config.SteamPath,
            "PubgPath=" + config.PubgPath,
            "PubgAppId=" + config.PubgAppId
        });
    }

    public static void AutoDetect(AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.SteamPath))
            config.SteamPath = DetectSteam();
        if (string.IsNullOrWhiteSpace(config.AcceleratorPath))
            config.AcceleratorPath = DetectDolphin();
        if (string.IsNullOrWhiteSpace(config.PubgPath))
            config.PubgPath = DetectPubg(config.SteamPath);
        if (string.IsNullOrWhiteSpace(config.AcceleratorUrl))
            config.AcceleratorUrl = "dolphin://";
    }

    public static string DetectSteam()
    {
        try
        {
            var path = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            if (!string.IsNullOrWhiteSpace(path))
            {
                var exe = Path.Combine(path!.Replace('/', '\\'), "steam.exe");
                if (File.Exists(exe)) return exe;
            }

            path = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam",
                "InstallPath",
                null) as string;
            if (!string.IsNullOrWhiteSpace(path))
            {
                var exe = Path.Combine(path, "steam.exe");
                if (File.Exists(exe)) return exe;
            }
        }
        catch
        {
            // Registry is unavailable on non-Windows development machines.
        }

        foreach (var candidate in new[]
        {
            @"C:\Program Files (x86)\Steam\steam.exe",
            @"C:\Program Files\Steam\steam.exe"
        })
        {
            if (File.Exists(candidate)) return candidate;
        }
        return "";
    }

    public static string DetectDolphin()
    {
        try
        {
            var command = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\dolphin\shell\open\command",
                "",
                null) as string;
            if (!string.IsNullOrWhiteSpace(command))
            {
                var path = ExtractQuotedPath(command!);
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) return path;
            }
        }
        catch
        {
            // Registry is unavailable on non-Windows development machines.
        }

        foreach (var candidate in new[]
        {
            @"C:\Program Files (x86)\Dolphin\DolphinQ.exe",
            @"C:\Program Files\Dolphin\DolphinQ.exe"
        })
        {
            if (File.Exists(candidate)) return candidate;
        }
        return "";
    }

    public static string DetectPubg(string steamPath)
    {
        foreach (var library in SteamLibraries(steamPath))
        {
            foreach (var folder in new[] { "PUBG", "PUBG Battlegrounds" })
            {
                var candidate = Path.Combine(
                    library, "steamapps", "common", folder,
                    "TslGame", "Binaries", "Win64", "TslGame.exe");
                if (File.Exists(candidate)) return candidate;
            }
        }

        foreach (var candidate in new[]
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\PUBG\TslGame\Binaries\Win64\TslGame.exe",
            @"D:\SteamLibrary\steamapps\common\PUBG\TslGame\Binaries\Win64\TslGame.exe",
            @"E:\SteamLibrary\steamapps\common\PUBG\TslGame\Binaries\Win64\TslGame.exe"
        })
        {
            if (File.Exists(candidate)) return candidate;
        }
        return "";
    }

    private static IEnumerable<string> SteamLibraries(string steamPath)
    {
        if (string.IsNullOrWhiteSpace(steamPath)) yield break;
        var root = Path.GetDirectoryName(steamPath);
        if (string.IsNullOrWhiteSpace(root)) yield break;
        yield return root;

        foreach (var vdf in new[]
        {
            Path.Combine(root, "steamapps", "libraryfolders.vdf"),
            Path.Combine(root, "config", "libraryfolders.vdf")
        })
        {
            if (!File.Exists(vdf)) continue;
            foreach (var line in File.ReadAllLines(vdf))
            {
                var match = Regex.Match(line, "\"path\"\\s+\"([^\"]+)\"");
                if (match.Success)
                    yield return match.Groups[1].Value.Replace("\\\\", "\\");
            }
        }
    }

    private static string ExtractQuotedPath(string command)
    {
        command = command.Trim();
        if (command.StartsWith("\""))
        {
            var end = command.IndexOf('"', 1);
            if (end > 1) return command.Substring(1, end - 1);
        }
        var space = command.IndexOf(' ');
        return space > 0 ? command.Substring(0, space) : command;
    }
}
