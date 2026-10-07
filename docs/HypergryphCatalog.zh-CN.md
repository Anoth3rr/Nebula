# 鹰角动态游戏目录

## 数据来源与覆盖范围

HoYoPlay 是米哈游 / HoYoverse 的官方 PC 启动器，提供游戏下载、更新和启动功能。Nebula 是独立的第三方项目，其中 `HoYoPlayClient` 负责访问官方接口，不代表 HoYoPlay 本身是开源项目。参见 [HoYoPlay 官方说明](https://www.hoyolab.com/article/34311662)。

鹰角也有统一启动器：中国大陆为「鹰角启动器」，海外为 GRYPHLINK，均有「全部游戏」入口。参见 [明日方舟 PC 安装指引](https://ak.hypergryph.com/news/0717) 和 [GRYPHLINK 官方说明](https://endfield.gryphline.com/en-us/news/9574)。

Nebula 接入以下已实测的官方数据源：

1. [森空岛产品配置](https://assets.skland.com/common-config/json/game-config.json)：从键、`appCode` 和 `name` 动态发现产品，没有限制为固定的几款游戏。
2. [森空岛社区配置](https://zonai.skland.com/web/v1/game)：按产品名称补充图标和背景。社区中的「纳斯特港」「开拓芯」不作为游戏导入。
3. 鹰角启动器 `GET https://launcher.hypergryph.com/api/game/get_latest`：使用目录返回的 `appcode`，并传入 `channel=1`、`sub_channel=1`、空 `version`、`launcher_appcode=abYeZZ16BPluCFyT`，查询中国官服 PC 发行信息。存在 HTTPS 的 `pkg.file_path`、Windows 路径及版本号才确认支持 PC。

以上接口不需要账号、token 或登录。当前接入的是中国官方产品配置；国际服与 B 服仍使用原有入口，并不据此自动生成或推测地区发行版本。产品目录也不保证覆盖鹰角未公布、未纳入此配置，或由其他平台单独发行的全部作品。

2026-10-07 实测结果：

| 官方目录产品 | 启动器 PC 资源 | 查询版本 |
| --- | --- | --- |
| 明日方舟 | 有 | 77.0.0 |
| 来自星尘 | 无 | — |
| 明日方舟：终末地 | 有 | 1.5.3 |
| 云·终末地 | 无 | — |
| 泡姆泡姆 | 有 | 1.2.1 |

只有启动器明确返回 HTTP 404、`RESOURCE_NOT_FOUND`、`game not exist` 才记为无 PC 资源；超时、普通 404、服务异常和格式错误均保留原来的判断，首次查询失败则记为未知。

## 应用行为

- 启动、切换语言及原有窗口激活刷新流程会并行刷新米哈游与鹰角目录。两者使用独立缓存，互不因对方接口失败而丢失条目。
- 名称、图标、游戏卡片、固定图标和当前选择均接入动态目录。只有中国目录的游戏在其他界面语言下也会显示。
- 继续使用 `arknights_cn`、`endfield_cn` 及原来的本地 ID，保留安装目录、固定项、时长与抽卡记录关联。其他产品使用稳定的 `hg-{slug}_cn`，不会路由到米哈游接口。
- 已确认提供 PC 版本的游戏支持定位本地游戏。新产品通过选择 `.exe` 保存安装目录和文件名，不猜测任意可执行文件。已有明日方舟和终末地保留原来的目录定位方式。
- 无 PC 版本或能力未知的动态条目提供「查看官网」。这次没有实现鹰角安装包下载、补丁安装、购买验证或自动登录；查询 PC 版本用于判定入口能力。
- 目录缓存存在 `CachedHypergryphGameInfo`；断网时可恢复。产品配置请求失败或返回空/错误结构时，不覆盖已有缓存；图片和单个游戏资源接口失败不阻止其他游戏刷新。

## 实现与验证

核心：`src/Nebula.Core/Hypergryph/HypergryphClient.cs`。缓存服务：`src/Nebula/Features/Hypergryph/HypergryphService.cs`。界面入口：`GameSelector` 和 `GameLauncherPage`。

离线回归覆盖官方目录解析、新产品发现、社区版区排除、旧 ID 兼容、图片失败、PC 查询失败、异常响应、缓存重启恢复、空结果保护及取消：

```powershell
dotnet run --project tests/Nebula.Hypergryph.Tests -c Release
```

可选真实网络核查（不下载游戏、不启动游戏）：

```powershell
dotnet run --project tests/Nebula.Hypergryph.Tests -c Release -- --live
```

测试中的产品与社区 fixture 取自 2026-10-07 官方匿名接口，并裁掉了测试不使用的社区内容字段。

本次验证结果：11 项离线回归通过；新客户端真实请求返回上表的 5 个产品；WinUI 主程序 Release/x64 构建成功，0 错误，109 个原有警告。使用本机临时 .NET 10 SDK，并通过 `CsWinRTWindowsMetadata` 指定已还原的 Windows 元数据。未实际启动游戏验证登录、反作弊或游戏内行为。
