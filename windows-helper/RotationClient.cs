using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace PubgZoneHelper;

public sealed class RegionInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Short { get; set; } = "";
    public string Accent { get; set; } = "#f2a33a";
    public List<string> Maps { get; set; } = new();
}

public sealed class RotationData
{
    public string Patch { get; set; } = "";
    public string WeekLabel { get; set; } = "";
    public List<RegionInfo> Regions { get; set; } = new();
    public bool Loaded { get; set; }

    public static RotationData Empty => new();
}

public static class RotationClient
{
    private const string ApiUrl = "https://pubg.jakt.online/api/rotation";

    private static readonly Dictionary<string, string> MapNames = new()
    {
        ["Erangel"] = "艾伦格",
        ["Miramar"] = "米拉玛",
        ["Taego"] = "泰戈",
        ["Sanhok"] = "萨诺",
        ["Vikendi"] = "维寒迪",
        ["Deston"] = "德斯顿",
        ["Rondo"] = "荣都",
        ["Karakin"] = "卡拉金",
        ["Paramo"] = "帕拉莫"
    };

    public static string MapName(string name) =>
        MapNames.TryGetValue(name, out var cn) ? cn : name;

    public static async Task<RotationData> LoadAsync()
    {
        string? json = null;
        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using var client = new WebClient { Encoding = Encoding.UTF8 };
            json = await client.DownloadStringTaskAsync(ApiUrl);
            SaveCache(json);
        }
        catch
        {
            json = LoadCache();
        }

        if (string.IsNullOrWhiteSpace(json)) return RotationData.Empty;
        try
        {
            return Parse(json!);
        }
        catch
        {
            return RotationData.Empty;
        }
    }

    private static RotationData Parse(string json)
    {
        var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        var root = serializer.Deserialize<Dictionary<string, object>>(json);
        if (root == null) return RotationData.Empty;

        var pc = AsDict(root, "pc");
        var weeks = pc == null ? new ArrayList() : AsList(pc, "weeks");
        var weekIndex = CurrentWeekIndex(weeks);
        var week = weekIndex >= 0 && weekIndex < weeks.Count ? weeks[weekIndex] as Dictionary<string, object> : null;

        var data = new RotationData
        {
            Patch = AsString(root, "patch"),
            WeekLabel = week == null
                ? ""
                : $"{FormatDate(AsString(week, "start"))} – {FormatDate(AsString(week, "end"))}",
            Loaded = true
        };

        foreach (var item in AsList(root, "regions"))
        {
            if (item is not Dictionary<string, object> region) continue;
            var regionWeeks = AsList(region, "weeks");
            var maps = new List<string>();
            if (weekIndex >= 0 && weekIndex < regionWeeks.Count && regionWeeks[weekIndex] is ArrayList list)
            {
                foreach (var map in list) maps.Add(AsString(map));
            }

            data.Regions.Add(new RegionInfo
            {
                Id = AsString(region, "id"),
                Name = AsString(region, "name"),
                Short = AsString(region, "short"),
                Accent = AsString(region, "accent"),
                Maps = maps
            });
        }
        return data;
    }

    private static int CurrentWeekIndex(ArrayList weeks)
    {
        var now = DateTimeOffset.Now;
        for (var i = 0; i < weeks.Count; i++)
        {
            if (weeks[i] is not Dictionary<string, object> week) continue;
            if (DateTimeOffset.TryParse(AsString(week, "start"), out var start)
                && DateTimeOffset.TryParse(AsString(week, "end"), out var end)
                && now >= start && now < end)
                return i;
        }
        if (weeks.Count > 0
            && weeks[0] is Dictionary<string, object> first
            && DateTimeOffset.TryParse(AsString(first, "start"), out var firstStart)
            && now < firstStart)
            return 0;
        return weeks.Count > 0 ? weeks.Count - 1 : -1;
    }

    private static string FormatDate(string value)
    {
        return DateTimeOffset.TryParse(value, out var date)
            ? date.ToLocalTime().ToString("MM/dd HH:mm", CultureInfo.InvariantCulture)
            : "";
    }

    private static Dictionary<string, object>? AsDict(Dictionary<string, object> source, string key)
        => source.TryGetValue(key, out var value) ? value as Dictionary<string, object> : null;

    private static ArrayList AsList(Dictionary<string, object> source, string key)
        => source.TryGetValue(key, out var value) && value is ArrayList list ? list : new ArrayList();

    private static string AsString(Dictionary<string, object> source, string key)
        => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? "" : "";

    private static string AsString(object? value)
        => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";

    private static string CachePath => Path.Combine(ConfigStore.Dir, "rotation.json");

    private static void SaveCache(string json)
    {
        try
        {
            Directory.CreateDirectory(ConfigStore.Dir);
            File.WriteAllText(CachePath, json, Encoding.UTF8);
        }
        catch
        {
            // Cache is best-effort.
        }
    }

    private static string? LoadCache()
    {
        try
        {
            return File.Exists(CachePath) ? File.ReadAllText(CachePath, Encoding.UTF8) : null;
        }
        catch
        {
            return null;
        }
    }
}
