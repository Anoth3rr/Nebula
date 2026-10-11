# 库洛游戏目录与启动器协议核查（2026-10-11）

## 结论

库洛目前有一套可复用的逐游戏启动器和 CDN 更新协议，但截至本次核查，没有发现类似 HoYoPlay `getGames` 或森空岛产品配置那样、面向外部调用并返回“库洛旗下全部游戏”的公开目录接口。

库洛官方产品页展示《战双帕弥什》和《鸣潮》两款主要产品：[库洛产品信息](https://www.kurogames.com/games)。两个游戏仍然分别使用自己的官网：[鸣潮官网](https://wutheringwaves.kurogames.com/en/)、[战双帕弥什官网](https://pgr.kurogame.net/)。库洛企业站的当前前端配置也把两个入口按区域静态写成 `mc`（鸣潮）和 `zs`（战双），没有从目录接口加载游戏列表。

## 发现的数据接口

以下接口均按单个游戏和发行区域工作：

| 用途 | URL 结构 | 主要内容 |
| --- | --- | --- |
| 游戏更新清单 | `https://prod[-cn]-alicdn-gamestarter.kurogame.com/launcher/game/{GID}/{appId}_{token}/index.json` | CDN 列表、当前版本、`resource.json` 路径、补丁版本链和文件校验信息 |
| 启动器配置 | `https://prod[-cn]-alicdn-gamestarter.kurogame.com/launcher/launcher/{appId}_{token}/{GID}/index.json` | 启动器压缩包版本、更新地址、更新说明和背景素材 hash |
| 启动器背景 | `.../launcher/{appId}_{token}/{GID}/background/{backgroundHash}/{language}.json` | `backgroundFile`、`firstFrameImage`、`slogan` |
| 社区/社交入口 | `.../launcher/{GID}/{appId}_{token}/social/{language}.json` | 社交图标和跳转链接，不是全量游戏封面目录 |

我在 2026-10-11 对库洛官方 CDN 的鸣潮国服和全球服清单、启动器配置及背景元数据进行了只读请求。响应正常，但返回的都是当前游戏的版本、补丁和启动器素材，没有产品目录字段。

上面的 URL 结构、GID 和 token 关系由社区逆向项目整理，属于第三方资料，不是库洛官方 API 文档：[Vedaru/kuro](https://github.com/Vedaru/kuro)、[Wuthering Waves Launcher API Endpoints](https://gist.github.com/SunsetMkt/86faa10439caf9668dd6c8ef2e8ac1fd/0dd77d4dfb6e124c6e5da62fc736cfa3c9047dfb)。实际响应来自库洛官方 CDN，接入时仍应把地址和 token 视为可变配置。

## 已知游戏标识

| 游戏 | 区域 | GID / appId | token 情况 |
| --- | --- | --- | --- |
| 鸣潮 | 中国官服 | `G152 / 10003` | 社区资料显示为公开且相对稳定 |
| 鸣潮 | 全球服 | `G153 / 50004` | 社区资料显示为公开且相对稳定 |
| 战双帕弥什 | 中国服 | `G148 / 10012` | 需要从真实启动器运行时获取，可能轮换 |
| 战双帕弥什 | 全球服 | `G143 / 50015` | 需要从真实启动器运行时获取，可能轮换 |

因此，库洛协议可以支持“已知游戏的版本检查、补丁下载和启动器素材读取”，不能安全地用于自动发现未知游戏。战双的运行时 token 也使得直接内置一个长期有效的公共配置不可靠。

## 封面处理判断

启动器背景接口返回的是启动器背景视频、首帧和标语。它们可以作为单个游戏的宣传图或回退封面，但没有稳定的产品 Logo、卡片封面或全量游戏列表字段。若 Nebula 后续接入库洛，应为每个已知游戏维护小型静态配置，并将官方启动器素材作为可更新的封面来源；下载失败时回退到内置图片。

## Nebula 当前状态

- [`GameBiz.cs`](../src/Nebula.Core/GameBiz.cs) 当前只定义 `wutheringwaves_cn` 和 `wutheringwaves_global`，没有战双的游戏标识。
- [`GameRegistry.cs`](../src/Nebula.Core/GameRegistry.cs) 当前只登记鸣潮的两个 Windows 注册表路径。
- [`GameLauncherService.cs`](../src/Nebula/Features/GameLauncher/GameLauncherService.cs) 对鸣潮使用静态可执行文件候选名；本地版本读取仍只走 HoYoPlay 分支，库洛更新协议尚未接入。
- 本次没有新增库洛代码，也没有把社区逆向配置当成官方全量目录使用。

## 后续建议

若要接入库洛，建议分成三个独立部分：

1. 用静态、可更新的配置维护已知游戏、区域、GID 和 appId；
2. 为每个已配置游戏读取启动器版本、游戏版本和背景素材，并复用现有海报缓存；
3. 将战双 token 作为用户本地或启动器提取配置处理，不把它写死在程序中。

在找到稳定的官方目录接口之前，不建议实现一个声称能够自动发现全部库洛游戏的 `GetGamesAsync`。
