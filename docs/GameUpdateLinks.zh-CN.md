# 游戏更新链接配置

游戏目录条目通过 `GameInfo.Update` 保存官方更新源。`manifest_url` 用于查询版本和资源清单，`launcher_url` 用于查询官方启动器配置，`package_url` 保存接口返回的资源地址；鹰角的 `pkg.file_path` 是文件资源目录，不能当作一个 ZIP 安装包直接下载。

| 厂商 | 游戏范围 | 配置内容 |
| --- | --- | --- |
| HoYoPlay | 米哈游已接入游戏及区服 | `getGamePackages`，按 `launcher_id`、语言和 `game_id` 动态生成 |
| Hypergryph | 鹰角官方产品目录 | `get_latest?appcode=...&channel=1&sub_channel=1&version=&launcher_appcode=...`；PC 产品另外保存当前 Windows 包地址 |
| Hypergryph | 明日方舟 B 服 | 独立渠道 `channel=2&sub_channel=2` |
| GRYPHLINK | 终末地全球服 | `launcher.gryphline.com`、全球服 appcode 和渠道 `6/6` |
| Kuro | 鸣潮国服 | `G152 / 10003` 的游戏清单和启动器配置 |
| Kuro | 鸣潮全球服 | `G153 / 50004` 的游戏清单和启动器配置 |

米哈游和鹰角的地址由官方接口动态返回或按官方标识生成。鸣潮使用启动器公开的 appKey 配置：国服 `Y8xXrXk65DqFHEDgApn3cpK5lfczpFx5`，全球服 `obOHXFrFanqsaIEOmuKroCcbZkQRBC7c`。appKey 和包地址都可能轮换，更新器应优先重新读取清单，不应把版本包地址写死。

明日方舟国际服使用独立 Yostar 协议，尚未适配更新源，不能使用国服地址兜底。

HoYoPlay 的现有安装/补丁流程仍是唯一完整接入的跨版本安装流程。鹰角与库洛已增加预下载资源检测、缓存下载和校验，安装与补丁应用尚未接入；详见[游戏预下载](GamePredownload.zh-CN.md)。
