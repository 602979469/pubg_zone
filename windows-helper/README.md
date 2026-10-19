# PUBG 换区助手（Windows）

一个可配置的 Windows 小工具，配合 `pubg.jakt.online` 使用。

## 功能

- 首次启动 3 步引导：PUBG `TslGame.exe`、加速器 `DolphinQ.exe`、Steam（自动检测）。
- 主界面参考海豚加速器面板：左侧大区、中间本周地图和两个大按钮、右侧换区教程与工具。
- 右上角齿轮随时重新选择路径。
- 大区地图数据读取 `pubg.jakt.online/api/rotation`，展示本周地图池。
- 一键关闭 Steam、Steam WebHelper、游戏覆盖层、PUBG / BattlEye 相关进程。
- 打开加速器：支持 `dolphin://` 协议链接，或直接启动配置好的 EXE。
- 首次运行自动检测 Steam 和 Dolphin 路径，也可以手动重新选择。
- 配置保存在 `%APPDATA%\PUBGZoneHelper\config.ini`，下次自动读取。
- 「开启游戏」= 启动 Steam + PUBG（优先 Steam URL，失败回退到 `TslGame.exe`）。
- 主界面和换区教程里都有「一键换区」。

## 编译

在 macOS 上交叉编译 .NET Framework 4.8（Windows 10/11 自带运行时）：

```bash
dotnet build windows-helper/PUBGZoneHelper.csproj -c Release
```

产物在 `windows-helper/bin/Release/net48/PUBGZoneHelper.exe`，约 50KB。

## 配置

首次运行会自动检测：

- Steam：`HKCU\Software\Valve\Steam\SteamPath`
- Dolphin：`HKLM\SOFTWARE\Classes\dolphin\shell\open\command`
- PUBG：从 Steam 库目录查找 `steamapps\common\PUBG\TslGame\Binaries\Win64\TslGame.exe`

默认加速器链接是 `dolphin://`，会打开海豚面板。若想直接跳到 PUBG，可改成 `dolphin://start_acc_game/<gameId>`（游戏 ID 以海豚客户端为准）。

如果协议打开失败，程序会自动尝试用配置的 `DolphinQ.exe` 启动。

## 权限

程序请求管理员权限（UAC），因为结束 `steamservice.exe` 和 BattlEye 相关进程通常需要管理员权限。
