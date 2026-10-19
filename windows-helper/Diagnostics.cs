using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PubgZoneHelper;

public static class Diagnostics
{
    public static string Write()
    {
        var config = ConfigStore.Load();
        var lines = new List<string>
        {
            $"Time: {DateTime.Now:O}",
            $"OS: {Environment.OSVersion}",
            $"Admin: {ProcessTools.IsAdministrator()}",
            $"Config: {ConfigStore.FilePath}",
            $"SteamPath: {config.SteamPath} (exists={File.Exists(config.SteamPath)})",
            $"AcceleratorPath: {config.AcceleratorPath} (exists={File.Exists(config.AcceleratorPath)})",
            $"AcceleratorUrl: {config.AcceleratorUrl}",
            $"PreferUrl: {config.PreferUrl}",
            $"PubgAppId: {config.PubgAppId}"
        };
        foreach (var name in ProcessTools.TargetNames)
            lines.Add($"Process {name}: {ProcessTools.CountRunning(name)}");

        var path = Path.Combine(Path.GetTempPath(), "PUBGZoneHelper-diagnose.txt");
        File.WriteAllLines(path, lines, Encoding.UTF8);
        return path;
    }
}
