#!/usr/bin/env python3
"""PUBG weekly map rotation scraper.

Primary source: https://pubgstatistics.com/maprotation
The page is a Next.js app whose server payload contains structured rotation
data for every region.  This module extracts that payload with the standard
library only, so it runs on a clean Windows Python install.

The official announcement is used for a human-readable source link:
https://pubg.com/en/news  (articles titled "Map Service Report - Update x.y")
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import urllib.error
import urllib.request
from datetime import datetime, time, timedelta, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent
CACHE_PATH = ROOT / "data" / "rotation.json"

STATS_URL = "https://pubgstatistics.com/maprotation"
NEWS_URL = "https://pubg.com/en/news"
USER_AGENT = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
    "AppleWebKit/537.36 (KHTML, like Gecko) "
    "Chrome/130.0 Safari/537.36"
)

MAP_ORDER = [
    "Erangel",
    "Miramar",
    "Taego",
    "Sanhok",
    "Vikendi",
    "Deston",
    "Rondo",
    "Karakin",
    "Paramo",
]

REGION_META = {
    "AS": {"name": "亚洲", "short": "AS", "accent": "#f2a33a", "order": 1},
    "SEA": {"name": "东南亚", "short": "SEA", "accent": "#3fbf8f", "order": 2},
    "NA": {"name": "北美", "short": "NA", "accent": "#5b9cf5", "order": 3},
    "EU": {"name": "欧洲", "short": "EU", "accent": "#4dd0e1", "order": 4},
    "SA": {"name": "南美", "short": "SA", "accent": "#c084fc", "order": 5},
    "RU": {"name": "俄罗斯", "short": "RU", "accent": "#f28b82", "order": 6},
    "Console": {"name": "主机全区域", "short": "主机", "accent": "#aab4c0", "order": 8},
}


def http_get(url: str, timeout: int = 20) -> str:
    request = urllib.request.Request(
        url,
        headers={
            "User-Agent": USER_AGENT,
            "Accept-Language": "en-US,en;q=0.9",
            "Accept": "text/html,application/xhtml+xml",
        },
    )
    with urllib.request.urlopen(request, timeout=timeout) as response:
        return response.read().decode("utf-8", "replace")


def parse_datetime(value: str) -> datetime:
    """Parse ISO dates, including Next.js '$D...' date markers."""
    value = value.replace("$D", "")
    if value.endswith("Z"):
        value = value[:-1] + "+00:00"
    return datetime.fromisoformat(value).astimezone(timezone.utc)


def iso(value: datetime) -> str:
    return value.astimezone(timezone.utc).isoformat().replace("+00:00", "Z")


def extract_next_payload(html: str) -> str:
    """Join the strings passed to self.__next_f.push([1, "..."]) calls."""
    parts = re.findall(
        r'self\.__next_f\.push\(\[1,("(?:[^"\\]|\\.)*")\]\)', html
    )
    if not parts:
        raise ValueError("Next.js payload not found in map rotation page")
    return "".join(json.loads(part) for part in parts)


def find_object_end(text: str, start: int) -> int:
    """Return the index just past a balanced JSON object, respecting strings."""
    depth = 0
    in_string = False
    escaped = False
    for index in range(start, len(text)):
        char = text[index]
        if in_string:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
            continue
        if char == '"':
            in_string = True
        elif char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return index + 1
    return -1


def parse_patches(html: str) -> list[dict]:
    text = extract_next_payload(html)
    patches: dict[str, dict] = {}
    for match in re.finditer(r'\{"patch":"', text):
        end = find_object_end(text, match.start())
        if end < 0:
            continue
        try:
            candidate = json.loads(text[match.start() : end])
        except json.JSONDecodeError:
            continue
        if isinstance(candidate.get("data"), list):
            patches[str(candidate.get("patch") or len(patches))] = candidate
    if not patches:
        raise ValueError("No rotation patch objects found")
    return list(patches.values())


def pick_patch(patches: list[dict], now: datetime | None = None) -> dict:
    now = now or datetime.now(timezone.utc)
    usable = []
    for patch in patches:
        try:
            start = parse_datetime(patch["start"])
            end = parse_datetime(patch["end"])
        except (KeyError, ValueError):
            continue
        if patch.get("enabled", True):
            usable.append((start, end, patch))
    if not usable:
        raise ValueError("No enabled rotation patch found")

    active = [item for item in usable if item[0] <= now < item[1]]
    if active:
        return max(active, key=lambda item: item[0])[2]

    past = [item for item in usable if item[0] <= now]
    if past:
        return max(past, key=lambda item: item[0])[2]
    return max(usable, key=lambda item: item[0])[2]


def ordered_maps(raw_maps: dict) -> list[str]:
    names = []
    for key, value in raw_maps.items():
        name = value.get("name") if isinstance(value, dict) else None
        names.append(name or key)
    rank = {name: index for index, name in enumerate(MAP_ORDER)}
    return sorted(names, key=lambda name: (rank.get(name, 99), name))


def build_weeks(patch: dict, count: int, console: bool = False) -> list[dict]:
    start_key = "consoleStart" if console else "start"
    end_key = "consoleEnd" if console else "end"
    time_key = "consoleMapChangeTime" if console else "mapChangeTime"
    start = parse_datetime(patch[start_key])
    end = parse_datetime(patch[end_key])
    clock = patch.get(time_key, "07:00" if console else "02:00")
    hour, minute = (int(part) for part in clock.split(":"))
    base = datetime.combine(start.date(), time(hour, minute), tzinfo=timezone.utc)
    weekday = int(
        patch.get("consoleMapChangeWeekday" if console else "mapChangeWeekday", 4 if console else 3)
    )
    # The payload stores ISO weekdays (Monday=1); Python uses Monday=0.
    while base.weekday() != weekday - 1:
        base += timedelta(days=1)

    weeks = []
    for index in range(count):
        week_start = base + timedelta(days=7 * index)
        # The patch metadata's end can be a few hours before the next
        # rotation; a week always runs exactly seven days.
        week_end = week_start + timedelta(days=7)
        weeks.append(
            {
                "index": index + 1,
                "start": iso(week_start),
                "end": iso(week_end),
            }
        )
    return weeks


def build_rotation(patch: dict) -> dict:
    pc_count = 1
    for region in patch.get("data", []):
        if region.get("regionId") not in ("Ranked", "Console"):
            pc_count = max(pc_count, len(region.get("weeks") or []))

    console_count = 1
    for region in patch.get("data", []):
        if region.get("regionId") == "Console":
            console_count = max(console_count, len(region.get("weeks") or []))

    pc_regions = []
    console_regions = []
    ranked = None

    for region in patch.get("data", []):
        region_id = str(region.get("regionId") or "")
        if region_id == "KAKAO":
            # Kakao requires a Korean account; it is not a usable PC region
            # for this tool.
            continue
        weeks = [
            ordered_maps(week.get("maps") or {})
            for week in (region.get("weeks") or [])
        ]
        meta = REGION_META.get(
            region_id,
            {"name": region_id, "short": region_id.upper(), "accent": "#8b949e", "order": 99},
        )
        item = {
            "id": region_id,
            "name": meta["name"],
            "short": meta["short"],
            "accent": meta["accent"],
            "order": meta["order"],
            "kind": region.get("type", "selection"),
            "weeks": weeks,
        }

        if region_id == "Ranked":
            ranked = {"maps": weeks[0] if weeks else []}
            continue
        if region_id == "Console":
            console_regions.append(item)
        if region.get("forPc") and region_id != "Console":
            pc_regions.append(item)

    return {
        "patch": str(patch.get("patch") or ""),
        "pc": {
            "start": iso(parse_datetime(patch["start"])),
            "end": iso(parse_datetime(patch["end"])),
            "weekday": patch.get("mapChangeWeekday"),
            "time_utc": patch.get("mapChangeTime"),
            "weeks": build_weeks(patch, pc_count),
        },
        "console": {
            "start": iso(parse_datetime(patch["consoleStart"])),
            "end": iso(parse_datetime(patch["consoleEnd"])),
            "weekday": patch.get("consoleMapChangeWeekday"),
            "time_utc": patch.get("consoleMapChangeTime"),
            "weeks": build_weeks(patch, console_count, console=True),
        },
        "regions": pc_regions,
        "console_regions": console_regions,
        "ranked": ranked,
        "maps": MAP_ORDER,
    }


def find_official_article() -> tuple[str | None, str | None]:
    """Find the newest 'Map Service Report' article link."""
    try:
        html = http_get(NEWS_URL, timeout=15)
    except (urllib.error.URLError, TimeoutError, OSError):
        return None, None

    pattern = re.compile(
        r'postId:(\d+)(?:(?!postId:).){0,1600}?title:"(Map Service Report[^"]*)"',
        re.S,
    )
    match = pattern.search(html)
    if not match:
        return None, None
    return f"https://pubg.com/en/news/{match.group(1)}", match.group(2)


def fetch_rotation() -> dict:
    html = http_get(STATS_URL)
    patch = pick_patch(parse_patches(html))
    data = build_rotation(patch)
    official_url, official_title = find_official_article()
    data.update(
        {
            "source": "pubgstatistics.com",
            "source_url": STATS_URL,
            "official_url": official_url,
            "official_title": official_title,
            "fetched_at": iso(datetime.now(timezone.utc)),
            "stale": current_week(data) is None,
        }
    )
    return data


def save_cache(data: dict, path: Path = CACHE_PATH) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(".tmp")
    temporary.write_text(
        json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    temporary.replace(path)


def load_cache(path: Path = CACHE_PATH) -> dict | None:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return None


def cache_age_seconds(data: dict) -> float | None:
    try:
        fetched = parse_datetime(data["fetched_at"])
    except (KeyError, ValueError):
        return None
    return (datetime.now(timezone.utc) - fetched).total_seconds()


def current_week(data: dict, platform: str = "pc") -> int | None:
    weeks = (data.get(platform) or {}).get("weeks") or []
    now = datetime.now(timezone.utc)
    for week in weeks:
        try:
            if parse_datetime(week["start"]) <= now < parse_datetime(week["end"]):
                return int(week["index"])
        except (KeyError, ValueError):
            continue
    return None


def main() -> int:
    parser = argparse.ArgumentParser(description="Fetch PUBG map rotation")
    parser.add_argument(
        "--print",
        action="store_true",
        help="print the parsed JSON instead of writing the cache",
    )
    args = parser.parse_args()

    try:
        data = fetch_rotation()
    except Exception as error:  # noqa: BLE001 - CLI should show the real error
        print(f"fetch failed: {error}", file=sys.stderr)
        return 1

    if args.print:
        print(json.dumps(data, ensure_ascii=False, indent=2))
    else:
        save_cache(data)
        week = current_week(data)
        print(
            f"patch {data['patch']} saved to {CACHE_PATH} "
            f"(PC current week: {week or 'between rotations'})"
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
