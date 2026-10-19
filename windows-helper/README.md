# PUBG 换区助手（Windows）

一个可配置的 Windows 小工具，配合 `pubg.jakt.online` 使用。

## 功能

- 一键关闭 Steam、Steam WebHelper、游戏覆盖层、PUBG / BattlEye 相关进程。
- 打开加速器：支持 `dolphin://` 协议链接，或直接启动配置好的 EXE。
- 首次运行自动检测 Steam 和 Dolphin 路径，也可以手动重新选择。
- 配置保存在 `%APPDATA%\PUBGZoneHelper\config.ini`，下次自动读取。
- 启动 Steam、启动 PUBG（默认 AppID `578080`）。
- 「一键换区流程」= 关闭 Steam/PUBG → 打开加速器；选好区服后再启动。

## 编译

在 macOS 上交叉编译 win-x64 自包含单文件：

```bash
dotnet publish windows-helper/PUBGZoneHelper.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

产物在 `windows-helper/bin/Release/net8.0-windows/win-x64/publish/PUBGZoneHelper.exe`。

## 配置

首次运行会自动检测：

- Steam：`HKCU\Software\Valve\Steam\SteamPath`
- Dolphin：`HKLM\SOFTWARE\Classes\dolphin\shell\open\command`

默认加速器链接是 `dolphin://`，会打开海豚面板。若想直接跳到 PUBG，可改成 `dolphin://start_acc_game/<gameId>`（游戏 ID 以海豚客户端为准）。

如果协议打开失败，程序会自动尝试用配置的 `DolphinQ.exe` 启动。

## 权限

程序请求管理员权限（UAC），因为结束 `steamservice.exe` 和 BattlEye 相关进程通常需要管理员权限。
