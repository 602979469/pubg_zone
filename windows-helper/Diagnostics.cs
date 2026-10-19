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
            $"PubgPath: {config.PubgPath} (exists={File.Exists(config.PubgPath)})",
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

    public static string WriteRotation()
    {
        var data = RotationClient.LoadAsync().GetAwaiter().GetResult();
        var lines = new List<string>
        {
            $"Loaded: {data.Loaded}",
            $"Patch: {data.Patch}",
            $"Week: {data.WeekLabel}"
        };
        foreach (var region in data.Regions)
        {
            lines.Add($"{region.Short} {region.Name}: {string.Join(", ", region.Maps)}");
        }

        var path = Path.Combine(Path.GetTempPath(), "PUBGZoneHelper-rotation.txt");
        File.WriteAllLines(path, lines, Encoding.UTF8);
        return path;
    }
}
