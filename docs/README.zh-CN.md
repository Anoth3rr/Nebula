[English](../README.md) | 简体中文 | [日本語](./README.ja-JP.md)


# Nebula

Nebula 是一个整合了多个厂商的游戏而开发的开源第三方启动器，支持米哈游 PC 端的所有游戏、鹰角网络 PC 端的所有游戏、鸣潮，目标是整合国内的部分二游。除了启动器的基本功能外，我还会根据个人需求增加一些拓展功能，比如：

-  记录游戏时间
-  切换游戏账号
-  浏览游戏截图
-  保存抽卡记录
-  切换游戏服务器

更多功能留给您自行探索。

## Fork 说明

本项目 fork from [Scighost/Starward](https://github.com/Scighost/Starward)，并参考了 [wuwatracker/wuwatracker](https://github.com/wuwatracker/wuwatracker) 和 [bhaoo/endfield-gacha](https://github.com/bhaoo/endfield-gacha)。

## 更新记录

### v0.0.0

- 新增明日方舟、明日方舟：终末地、鸣潮的支持入口，并在可用范围内加入国服与国际服标识。
- 新增鸣潮抽卡记录支持，物品元数据与查询逻辑参考了 [wuwatracker/wuwatracker](https://github.com/wuwatracker/wuwatracker)。
- 新增明日方舟：终末地抽卡记录支持，包括账号绑定、基于 token 的查询流程、角色池与武器池解析，以及导入和统计集成，参考了 [bhaoo/endfield-gacha](https://github.com/bhaoo/endfield-gacha)。
- 新增终末地物品元数据同步，支持从森空岛 Wiki 获取数据，并在本地 `EndfieldGachaInfo` 中存储、匹配图标、刷新已有抽卡记录的物品名称。
- 扩展 URL Protocol 文档，新增 `endfield_cn`、`endfield_global`、`wutheringwaves_cn`、`wutheringwaves_global` 游戏标识。
- 支持了原神、崩坏：星穹铁道、原神、明日方舟的官服与 B 服的互切。

## 安装

首先，您的设备需要满足以下要求：

- Windows 10 1809 (17763) 及以上的版本
- 已安装 [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2)
- 已安装 [WebP 映像扩展](https://apps.microsoft.com/detail/9pg2dk419drg)
- 为了更好的使用体验，请在系统设置中开启**透明效果**和**动画效果**
>[WebP 映像扩展](https://apps.microsoft.com/detail/9pg2dk419drg) 一般情况下系统自带，如果程序无法正常显示背景图片请自行检查是否安装。

然后在 [GitHub Release](https://github.com/Anoth3rr/Nebula/releases) 下载对应 CPU 架构的压缩包，解压后运行 `Nebula.exe` 并按提示操作。


## 开发

在本地编译应用，你需要安装 Visual Studio 2022 并选择以下负载：

-  .NET 桌面开发
-  使用 C++ 的桌面开发
-  通用 Windows 平台开发


## 鸣谢

本项目的开发参考并受益于以下项目，在此向这些项目的作者和社区表示感谢：

-  [Scighost/Starward](https://github.com/Scighost/Starward)：本项目的 fork 来源。
-  [CollapseLauncher/Collapse](https://github.com/CollapseLauncher/Collapse)：参考了部分启动器设计与实现思路。
-  [DGP-Studio/Snap.Hutao](https://github.com/DGP-Studio/Snap.Hutao)：开发过程中参考并获得了相关帮助。
-  [wuwatracker/wuwatracker](https://github.com/wuwatracker/wuwatracker)：鸣潮抽卡记录与物品元数据相关实现参考。
-  [bhaoo/endfield-gacha](https://github.com/bhaoo/endfield-gacha)：明日方舟：终末地抽卡记录相关实现参考。

以及本项目使用的[第三方库](./ThirdParty.md)。
