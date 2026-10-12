# 游戏更新链接配置

游戏目录条目通过 `GameInfo.Update` 保存官方更新源。`manifest_url` 用于查询版本和资源清单，`launcher_url` 用于查询官方启动器配置，`package_url` 只保存接口返回并校验过的当前完整包地址。

| 厂商 | 游戏范围 | 配置内容 |
| --- | --- | --- |
| HoYoPlay | 米哈游已接入游戏及区服 | `getGamePackages`，按 `launcher_id`、语言和 `game_id` 动态生成 |
| Hypergryph | 鹰角官方产品目录 | `get_latest?appcode=...&channel=1&sub_channel=1&version=&launcher_appcode=...`；PC 产品另外保存当前 Windows 包地址 |
| Kuro | 鸣潮国服 | `G152 / 10003` 的游戏清单和启动器配置 |
| Kuro | 鸣潮全球服 | `G153 / 50004` 的游戏清单和启动器配置 |

米哈游和鹰角的地址由官方接口动态返回或按官方标识生成。鸣潮使用启动器公开的 appKey 配置：国服 `Y8xXrXk65DqFHEDgApn3cpK5lfczpFx5`，全球服 `obOHXFrFanqsaIEOmuKroCcbZkQRBC7c`。appKey 和包地址都可能轮换，更新器应优先重新读取清单，不应把版本包地址写死。

当前配置只完成更新源登记和读取入口，HoYoPlay 的现有安装/补丁流程仍是唯一完整接入的跨版本安装流程；鹰角与库洛的包下载、校验和安装流程仍需后续实现。
