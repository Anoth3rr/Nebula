# 鹰角动态游戏目录

## 数据来源与覆盖范围

HoYoPlay 是米哈游 / HoYoverse 的官方 PC 启动器，提供游戏下载、更新和启动功能。Nebula 是独立的第三方项目，其中 `HoYoPlayClient` 负责访问官方接口，不代表 HoYoPlay 本身是开源项目。参见 [HoYoPlay 官方说明](https://www.hoyolab.com/article/34311662)。

鹰角也有统一启动器：中国大陆为「鹰角启动器」，海外为 GRYPHLINK，均有「全部游戏」入口。参见 [明日方舟 PC 安装指引](https://ak.hypergryph.com/news/0717) 和 [GRYPHLINK 官方说明](https://endfield.gryphline.com/en-us/news/9574)。

Nebula 接入以下已实测的官方数据源：

1. [森空岛产品配置](https://assets.skland.com/common-config/json/game-config.json)：从键、`appCode` 和 `name` 动态发现产品，目录发现与本地功能白名单分开。
2. 鹰角启动器 `POST https://launcher.hypergryph.com/api/proxy/web/batch_proxy`：按产品请求 `get_main_bg_image` 和 `get_game_image`。各请求参数包含 `appcode`、`language=zh-cn`、`channel=1`、`sub_channel=1`、`platform=Windows`、`source=launcher`。后两项缺失时，实测可能只返回 `kind` 而没有图片内容。
3. [森空岛社区配置](https://zonai.skland.com/web/v1/game)：按产品名称补充图标，以及启动器未提供的背景。社区中的「纳斯特港」「开拓芯」不作为游戏导入。
4. 鹰角启动器 `GET https://launcher.hypergryph.com/api/game/get_latest`：使用目录返回的 `appcode`，并传入 `channel=1`、`sub_channel=1`、空 `version`、`launcher_appcode=abYeZZ16BPluCFyT`，查询中国官服 PC 发行信息。存在 HTTPS 的 `pkg.file_path`、Windows 路径及版本号才确认提供 PC 资源；此结果不会自动扩大白名单。

以上接口不需要账号、token 或登录。当前接入的是中国官方产品配置；国际服与 B 服仍使用原有入口，并不据此自动生成或推测地区发行版本。产品目录也不保证覆盖鹰角未公布、未纳入此配置，或由其他平台单独发行的全部作品。

2026-10-11 实测结果：

| 官方目录产品 | 启动器 PC 资源 | 查询版本 | 本地启动白名单 | 图片来源 |
| --- | --- | --- | --- | --- |
| 明日方舟 | 有 | 78.0.0 | 是 | 启动器列表封面与主背景 |
| 来自星尘 | 无 | — | 否 | 森空岛背景 |
| 明日方舟：终末地 | 有 | 1.5.3 | 是 | 启动器主背景（兼作列表封面） |
| 云·终末地 | 无 | — | 否 | 内置占位图 |
| 泡姆泡姆 | 有 | 1.2.1 | 否 | 启动器主背景（兼作列表封面） |

只有启动器明确返回 HTTP 404、`RESOURCE_NOT_FOUND`、`game not exist` 才记为无 PC 资源；超时、普通 404、服务异常和格式错误均保留原来的判断，首次查询失败则记为未知。

## 应用行为

- 启动、切换语言及原有窗口激活刷新流程会并行刷新米哈游与鹰角目录。两者使用独立缓存，互不因对方接口失败而丢失条目。
- 名称、图标、游戏卡片、固定图标和当前选择均接入动态目录。只有中国目录的游戏在其他界面语言下也会显示。
- 继续使用 `arknights_cn`、`endfield_cn` 及原来的本地 ID，保留安装目录、固定项、时长与抽卡记录关联。其他产品使用稳定的 `hg-{slug}_cn`，不会路由到米哈游接口。
- `GameBiz.IsHypergryphGame()` 的白名单仅包含明日方舟和明日方舟：终末地，沿用已有官服、B 服或国际服适配入口。`IsHypergryphCatalogGame()` 识别整个鹰角目录，仅用于元数据和图片路由。
- 白名单外的产品仍可展示、选择和固定，与米哈游未适配条目共用 `GameCatalogAction`：有 HTTPS 官网/预约链接时打开链接，否则显示 Coming Soon。即使已发现 PC 安装包，也不开放定位、安装、本地启动或第三方启动工具；旧版保存的任意 `.exe` 配置不再参与启动。
- 白名单内保留原来的安装目录定位和启动方式。这次没有实现鹰角安装包下载、补丁安装、购买验证或自动登录。
- 目录缓存存在 `CachedHypergryphGameInfo`；断网时可恢复。产品配置请求失败或返回空/错误结构时，不覆盖已有缓存；图片和单个游戏资源接口失败不阻止其他游戏刷新。

## 封面与背景

鹰角和米哈游都通过 `GameInfo.Display` 驱动同一个游戏选择器：`Thumbnail` 用于列表卡片，`Background` 用于启动页，`Icon` 用于固定图标。鹰角启动器没有提供专用 Logo 时保留文字标题。

- 列表封面优先使用 `get_game_image_rsp.cover`，为空则使用其中的 `bg`，还没有则使用 `get_main_bg_image_rsp.main_bg_image.url`。
- 启动页优先使用 `main_bg_image.url`。本次只使用静态图片，不自动播放接口中的视频。
- 启动器图片来源单独随目录缓存，刷新失败或返回空字段时保留；首次获取不到时回退到森空岛图片，再到内置封面/占位图。社区回退图不会覆盖已成功缓存的启动器图片。
- 米哈游与鹰角共用 `GamePosterCache` 下载到 `CacheFolder/bg` 并记录版本海报；同一 URL 合并下载，完整写入后才登记缓存。
- 用户自定义背景优先于在线默认图；鹰角内置背景作为回退，不再强制覆盖在线图。刷新当前游戏的背景 URL 后通知界面更新，并使用现有背景切换器。
- 国际服/B 服保留既有入口。没有该区服目录图片时复用同一游戏的中国官方图片，不据此推断地区发行信息。

## 实现与验证

核心：`src/Nebula.Core/Hypergryph/HypergryphClient.cs`。缓存服务：`src/Nebula/Features/Hypergryph/HypergryphService.cs`。共用海报缓存：`src/Nebula/Features/Background/GamePosterCache.cs`。界面入口：`GameSelector` 和 `GameLauncherPage`。

离线回归覆盖目录解析、新产品发现、两款游戏白名单、未适配条目的共用行为、旧 ID 兼容、官方封面优先级、缺图回退、旧缓存升级、离线恢复、海报缓存及取消：

```powershell
dotnet run --project tests/Nebula.Hypergryph.Tests -c Release
```

可选真实网络核查（不下载游戏、不启动游戏）：

```powershell
dotnet run --project tests/Nebula.Hypergryph.Tests -c Release -- --live
```

测试中的产品与社区 fixture 取自 2026-10-07 官方匿名接口，启动器图片 fixture 取自 2026-10-11；均来自无需登录的公开接口。

2026-10-11 验证：18 项目录/图片离线回归与 16 项启动参数检查通过，真实接口返回上表的 5 个产品及图片。WinUI 主程序 Release/x64 构建通过，0 错误，109 个既有警告；构建时通过 `CsWinRTWindowsMetadata` 指向本机已还原的 Windows 元数据。未实际启动游戏验证登录、反作弊或游戏内行为。
