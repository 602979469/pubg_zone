# PUBG 本周地图轮换

一个本地运行的周末小工具：汇总 PUBG PC / 主机各大区本周的轮换地图，选中大区后给出加速器换区步骤。

它不读取游戏内存、不注入游戏进程，也不修改游戏文件。

## 快速启动（Windows）

1. 安装 Python 3.9 或更高版本，安装时勾选 **Add Python to PATH**。
2. 双击 `start.bat`。
3. 浏览器会自动打开 `http://127.0.0.1:8765`。

命令行方式：

```bat
python app.py
```

Mac / Linux：

```bash
cd pubg-rotation
python3 app.py
```

端口被占用时：

```bash
python app.py --port 8766
```

## 功能

- PC / 主机平台切换。
- 只展示本周地图池，不显示其他周的数据。
- 按地图筛选，例如只看本周有“维寒迪”的大区。
- 大区详情：本周地图池、推荐加速器节点、换区清单，可一键复制。
- 地图池按大区展示，包含 AS、SEA、NA、SA、EU、RU 和竞技模式。

## Windows 换区助手

直接运行 [windows-helper/dist/PUBGZoneHelper.exe](windows-helper/dist/PUBGZoneHelper.exe)（需要管理员权限，UAC 会弹窗）。

- 首次启动分 3 步选择 `TslGame.exe`、`DolphinQ.exe`，Steam 自动检测
- 主界面按海豚面板风格做成三栏：左侧大区、中间地图与两个大按钮、右侧教程和工具
- 右上角齿轮可随时重新选择路径
- 大区地图数据直接读取 `pubg.jakt.online/api/rotation`
- 一键关闭 Steam、Steam WebHelper、游戏覆盖层、PUBG / BattlEye 相关进程
- 打开加速器：默认 `dolphin://` 展开面板，也可以改成 `dolphin://start_acc_game/<gameId>`
- 协议打开失败时自动回退到配置好的 EXE
- 配置保存在 `%APPDATA%\PUBGZoneHelper\config.ini`
- 主界面和换区教程里都有「一键换区」；选好区服后再点「开启游戏」

它只做进程管理和程序启动，不注入游戏、不修改游戏文件。

## 数据来源

- 结构化数据：`https://pubgstatistics.com/maprotation`
- 官方公告：PUBG News 里的 “Map Service Report - Update x.y”

后端启动时会读取 `data/rotation.json` 缓存，并在缓存超过 6 小时时后台刷新。也可以手动刷新：

```bash
python scraper.py
```

页面上点“刷新数据”会立即重新抓取。

截至 2026-10-19，抓到的是 **Update 43.2**，本周为 10/14 10:00 – 10/21 10:00（北京时间）。

后端仍会抓取三期数据用于判断“当前是哪一周”，但页面只展示本周。

地图池以游戏内为准。如果 PUBG 临时调整，页面可能滞后。

## 关于“换大区”

**改 Windows 时区不会切换匹配大区。** PUBG 根据当前网络出口自动分配服务器；时区只影响系统时钟和页面显示。

实际换区步骤：

1. 完全退出 PUBG 和 Steam，包括任务栏托盘。
2. 打开加速器，搜索“绝地求生 / PUBG”，选择目标区服节点：
   - AS：亚洲（香港 / 日本 / 韩国）
   - SEA：东南亚（新加坡 / 马来西亚）
   - NA：北美（美西 / 洛杉矶 / 美东）
   - EU：欧洲（德国 / 法兰克福 / 荷兰）
   - SA：南美（巴西 / 圣保罗）
   - RU：俄罗斯（莫斯科）
3. 先开加速，再启动 Steam 和 PUBG。
4. 进大厅看延迟，开一局确认地图池。
5. 如果仍是原区，换同区另一个节点，或重启加速器和游戏。

不同加速器的节点名称不一样，认准“目标区服”即可。跨区延迟会明显增加，频繁切换网络出口可能触发账号验证；请遵守 PUBG 服务条款，风险自负。

## 文件

| 文件 | 作用 |
| --- | --- |
| `app.py` | 本地 HTTP 服务，提供页面和 `/api/rotation` |
| `scraper.py` | 抓取并解析轮换数据 |
| `index.html` | 页面（原生 HTML/CSS/JS，无外部依赖） |
| `data/rotation.json` | 离线缓存 |
| `start.bat` / `start.sh` | 一键启动 |

## 公网部署

公网地址：`https://pubg.jakt.online`

公网机是轻量服务器，不使用 Docker / Kubernetes：

- `systemd` 直接运行 Python 标准库服务，监听 `127.0.0.1:8765`
- Caddy 反代 `pubg.jakt.online`，自动申请证书
- 服务限制 `MemoryMax=128M`、`CPUQuota=25%`
- 不装 cron，不做定时轮询；更新时手动 `git pull` 并重启，节省流量

首次安装（root）：

```bash
useradd --system --no-create-home --shell /usr/sbin/nologin pubg 2>/dev/null || true
git clone https://github.com/602979469/pubg_zone.git /opt/pubg-zone
chown -R pubg:pubg /opt/pubg-zone

install -m 644 /opt/pubg-zone/deploy/pubg-zone.service /etc/systemd/system/
systemctl daemon-reload
systemctl enable --now pubg-zone

# 将 deploy/Caddyfile.pubg 的内容追加到 /etc/caddy/Caddyfile
caddy validate --config /etc/caddy/Caddyfile
systemctl reload caddy
```

每次更新代码后，在公网机执行：

```bash
/opt/pubg-zone/update.sh
```

它等价于 `git pull --ff-only && systemctl restart pubg-zone && curl /health`。
脚本不需要参数；如果你在公网机本地改过代码，先 commit/push，再运行它。

查看状态：

```bash
systemctl status pubg-zone
journalctl -u pubg-zone -n 50 --no-pager
```

## 排查

- **页面打不开**：确认 `app.py` 还在运行；默认只监听 `127.0.0.1`。
- **数据没更新**：点页面“刷新数据”，或运行 `python scraper.py` 看报错。
- **抓取失败**：第三方页面结构可能变化；先用缓存数据，再检查 `scraper.py` 里的 `STATS_URL`。
- **杀毒软件拦截**：本工具只启动本地 Python HTTP 服务，不修改游戏；如被拦截，把项目目录加入信任。
