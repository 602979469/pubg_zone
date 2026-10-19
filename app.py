#!/usr/bin/env python3
"""Local web app for PUBG regional map rotations.

Run:  python app.py
Open: http://127.0.0.1:8765

No third-party packages are required.  The rotation data is refreshed from
pubgstatistics.com in a background thread and cached in data/rotation.json.
"""

from __future__ import annotations

import argparse
import json
import os
import threading
import time
import webbrowser
from http import HTTPStatus
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlparse

import scraper

ROOT = Path(__file__).resolve().parent
REFRESH_SECONDS = 6 * 60 * 60


class RotationState:
    def __init__(self) -> None:
        self._lock = threading.Lock()
        self._data = scraper.load_cache() or {}
        self._error: str | None = None
        self._refreshing = False

    def snapshot(self) -> dict:
        with self._lock:
            data = dict(self._data)
            data["refreshing"] = self._refreshing
            data["refresh_error"] = self._error
            data["cache_age_seconds"] = (
                scraper.cache_age_seconds(self._data) if self._data else None
            )
            return data

    def needs_refresh(self) -> bool:
        with self._lock:
            if not self._data:
                return True
            age = scraper.cache_age_seconds(self._data)
            return age is None or age > REFRESH_SECONDS

    def refresh(self, blocking: bool = True) -> dict:
        with self._lock:
            if self._refreshing:
                already_running = True
            else:
                already_running = False
                self._refreshing = True

        if already_running:
            if blocking:
                for _ in range(300):
                    time.sleep(0.1)
                    with self._lock:
                        if not self._refreshing:
                            break
            return self.snapshot()

        try:
            fresh = scraper.fetch_rotation()
            scraper.save_cache(fresh)
            with self._lock:
                self._data = fresh
                self._error = None
        except Exception as error:  # noqa: BLE001 - surfaced in the UI
            with self._lock:
                self._error = str(error)
        finally:
            with self._lock:
                self._refreshing = False
        return self.snapshot()

    def refresh_in_background(self) -> None:
        threading.Thread(target=self.refresh, kwargs={"blocking": False}, daemon=True).start()


STATE = RotationState()


class Handler(SimpleHTTPRequestHandler):
    server_version = "PUBGRotation/1.0"

    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(ROOT), **kwargs)

    def log_message(self, fmt: str, *args) -> None:
        print(f"[{self.log_date_time_string()}] {fmt % args}")

    def _send_json(self, payload: dict, status: int = HTTPStatus.OK) -> None:
        body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self) -> None:  # noqa: N802 - stdlib naming
        parsed = urlparse(self.path)
        if parsed.path == "/api/rotation":
            query = parse_qs(parsed.query)
            if query.get("refresh", ["0"])[0] in ("1", "true"):
                self._send_json(STATE.refresh(blocking=True))
            else:
                self._send_json(STATE.snapshot())
            return
        if parsed.path == "/health":
            self._send_json({"ok": True})
            return
        if parsed.path == "/":
            self.path = "/index.html"
        super().do_GET()

    def do_POST(self) -> None:  # noqa: N802 - stdlib naming
        if urlparse(self.path).path == "/api/refresh":
            self._send_json(STATE.refresh(blocking=True))
            return
        self.send_error(HTTPStatus.NOT_FOUND)


def main() -> None:
    parser = argparse.ArgumentParser(description="PUBG map rotation local app")
    parser.add_argument("--port", type=int, default=int(os.environ.get("PORT", "8765")))
    parser.add_argument("--no-browser", action="store_true")
    args = parser.parse_args()

    if STATE.needs_refresh():
        STATE.refresh_in_background()

    httpd = ThreadingHTTPServer(("127.0.0.1", args.port), Handler)
    url = f"http://127.0.0.1:{args.port}"
    print(f"PUBG rotation app: {url}")
    print("Press Ctrl+C to stop.")
    if not args.no_browser:
        threading.Timer(0.6, lambda: webbrowser.open(url)).start()
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        print("\nStopped.")
    finally:
        httpd.server_close()


if __name__ == "__main__":
    main()
