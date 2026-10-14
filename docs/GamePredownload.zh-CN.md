# 游戏预下载

已定位安装目录且存在游戏程序时，Nebula 会查询该游戏、该区服的官方预下载资源。只有未来版本及有效下载资源同时存在，启动按钮旁才出现独立的「预下载」按钮。进入游戏页、重新定位游戏、窗口状态触发版本检查，以及停留页面每 5 分钟都会刷新状态。

## 支持范围

| 游戏 / 区服 | 检测来源 | 下载方式 |
| --- | --- | --- |
| 崩坏 3、原神、崩坏：星穹铁道、绝区零的已接入区服 | HoYoPlay `pre_download` 包或分块分支 | 沿用压缩包、Sophon / LDIFF、语音包与现有更新流程 |
| 明日方舟国服、B 服 | 鹰角 `get_latest` 的 `pre_patch` | 下载 `patches` 分卷或单个预下载包，并校验 MD5 |
| 明日方舟：终末地国服、全球服 | 对应 Hypergryph / GRYPHLINK 接口的 `pre_patch` | 同上 |
| 鸣潮国服、全球服 | 各自 `index.json` 的 `predownload`，随后读取其 `resources` | 按 `resourcesBasePath` 与清单中的路径、长度、MD5 下载 |
| 明日方舟国际服 | Yostar 使用独立发行协议，本次没有适配其预下载协议 | 不显示预下载按钮，不回退到国服接口 |
| 鹰角目录其他产品、未接入的库洛游戏 | 不在当前本地功能白名单 | 不开启预下载 |

鹰角查询会先只读解析安装目录 `config.ini` 中的版本，支持启动器的 AES 文件格式及明文配置。无法读取安装版本时不猜测增量补丁的基础版本。国服使用渠道 `1/1`，明日方舟 B 服使用 `2/2`，终末地全球服使用 GRYPHLINK 域名、独立 appcode 和渠道 `6/6`。

鸣潮的 `predownloadSwitch` 开启并不代表已经发布预下载资源；仍须存在比 `default.version` 更新的 `predownload.version`、资源清单和有效文件。开关关闭、资源为空、接口失败、版本已正式上线时都不显示新任务入口。当前任务暂停或出错时保留入口以便继续。

## 操作与边界

点击按钮查看下载大小与可用空间，再开始下载。任务复用现有队列、进度、限速、暂停和继续操作；重新启动软件后，可以再次点击预下载复用磁盘上的缓存。下载支持 HTTP Range，不支持 Range 的服务器会从头重写临时文件。文件长度与 MD5 都通过后才保存为完成文件，全部文件完成后写入完成记录。

米哈游继续使用原有缓存和补丁应用机制。鹰角、库洛资源保存在：

```text
<游戏安装目录>/Nebula_Predownload/<game_biz>/<目标版本>/files/
```

鹰角、库洛当前只完成预下载和校验，**不解包、不应用补丁、不修改已安装版本，也不支持官方启动器自动复用该目录**。更新游戏仍需官方启动器；下载确认窗口会明确说明这一点。鸣潮目前使用预下载分支提供的完整资源清单，尚未依据本地版本选择差分包，因此可能需要较大磁盘空间。旧版本缓存不会自动删除，可在不下载时手动清理对应版本目录。

切换游戏或页面卸载会取消尚未完成的检测，旧响应不会更新新页面的按钮。已完成状态同时检查资源指纹、文件长度及修改时间；重新下载时仍会逐文件校验 MD5。路径越界、重复文件、缓存目录内链接、无效 URL 或校验值会被拒绝。

## 实现位置

- `src/Nebula.Core/Predownload/`：官方清单解析、HoYoPlay 可用性判断、隔离缓存与可恢复文件下载。
- `src/Nebula.Core/Hypergryph/HypergryphGameUpdateLinks.cs`：鹰角区服映射。
- `src/Nebula/Features/GameLauncher/GameLauncherPage.xaml.cs`：按钮显示、刷新与任务状态。
- `src/Nebula/Features/GameInstall/PreDownloadDialog.xaml.cs`：下载确认、大小和磁盘空间检查。
- `src/Nebula.RPC/GameInstall/GameInstallService.cs`：预下载任务队列；鹰角/库洛走独立分支，避免执行 HoYoPlay 的解包与清理逻辑。

## 验证与协议依据

2026-10-14 只读请求：明日方舟国服/B 服未返回预下载包；鸣潮国服/全球服开关为 `1`，但没有 `predownload` 分支。终末地不传安装版本时也不返回预下载，传入 `1.5.3` 后，国服/全球服均返回目标 `1.6.3`，各 9 个分卷，下载量分别为 9,364,056,947 和 9,259,037,082 字节。使用实际 `GamePredownloadClient` 解析成功，并对两个区服首个分卷执行 HEAD，请求成功且长度与清单一致；没有下载完整游戏包。

离线回归覆盖 24 项场景，包括各区服映射、未来版本/空资源判断、鹰角配置读取、鸣潮资源 URL 与 gzip 响应、错误响应与取消、路径约束、完成状态失效、续传、服务器忽略 Range、完整临时文件复用、MD5 失败以及暂停恢复。运行：

```powershell
dotnet run --project tests/Nebula.Predownload.Tests -c Release
dotnet run --project tests/Nebula.Hypergryph.Tests -c Release
```

协议核对参考以下实现及官方接口的实际响应，未引入第三方代码依赖：

- [Hypergryph API 数据结构](https://github.com/misaka10843/Hi3Helper.Plugin.Hypergryph/blob/main/Hi3Helper.Hypergryph.Core/Management/Api/HgApiStructs.cs)：`pre_patch`、`patches` 与分卷字段。
- [Hypergryph 配置文件格式](https://github.com/misaka10843/Hi3Helper.Plugin.Hypergryph/blob/main/Hi3Helper.Hypergryph.Core/Utils/HgCrypto.cs)：启动器配置的固定 AES 参数。
- [终末地全球服渠道配置](https://github.com/misaka10843/Hi3Helper.Plugin.Hypergryph/blob/main/Hi3Helper.Plugin.Endfield/Management/PresetConfig/EndfieldGlobalPreSetConfig.cs)与[明日方舟 B 服配置](https://github.com/misaka10843/Hi3Helper.Plugin.Hypergryph/blob/main/Hi3Helper.Plugin.Arknights/Management/PresetConfig/ArknightsBiliPresetConfig.cs)。
- [鸣潮资源清单与预下载分支](https://gist.github.com/DiceTsuki/770b229126354e5167e45ae0d5de3d0c)：资源 URL 字段约定；国服现行清单同时实测验证。

实际预下载窗口期间的完整下载、UAC 交互和官方启动器复用尚未做实机验收；自动复用与补丁应用需要后续独立接入。
