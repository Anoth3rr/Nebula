# Nebula 练度工作台（独立原型）

这是一个不依赖 Nebula 主程序的静态前端原型，用来先验证练度统计与配队工作流。数据默认来自 `Record.xlsx` 中的原神、崩铁、终末地和鸣潮示例，并在浏览器 `localStorage` 中保存修改。

## 运行

在仓库根目录启动任意静态文件服务器，例如：

```powershell
python -m http.server 4173
```

然后打开 `http://127.0.0.1:4173/progression-dashboard/`。也可以直接双击 `index.html`，但导入/导出和本地存储在部分浏览器的 `file:` 页面上会受限制。

## 当前能力

- 按游戏切换角色档案，支持原神、崩铁、终末地、鸣潮。
- 角色列表按名称/装备/套装搜索，按星级和练度状态筛选，按完成度、评分或名称排序。
- 角色详情展示等级、命座/星魂/潜能/共鸣链、技能、装备、套装评分和培养备注。
- 原神角色可选择圣遗物套装、时之沙/空之杯/理之冠主词条；崩铁角色可选择四件套、二件套/位面以及躯干/脚部/位面球/连结绳主词条。生之花/死之羽、头部/手部的固定主词条会直接展示，副词条在角色目标中手动填写。
- 装备仓库按游戏保存武器/光锥、圣遗物/遗器、装备组或声骸；可搜索、筛选、排序、锁定和新增条目。
- 仓库新增圣遗物/遗器时可选择套装、部位和该部位主词条，副词条手填；条目会自动命名为“套装名·部位”，角色详情中的“养成目标”与仓库中的“实际装备”分开保存。
- 仓库按部位保存圣遗物/遗器，每件条目使用对应的 BWiki 部位图标；角色详情可分别为每个部位选择仓库条目。
- 角色详情可从当前游戏仓库选择装备；换手时会自动解除原角色绑定，仓库条目会显示当前持有角色。
- 角色、武器和套装图标使用 BWiki 文件，示例图标已缓存到 `assets/wiki/`，离线时自动回退为首字母。
- 详情面板可直接调整等级与重复命座，并自动保存到本机；“编辑角色”可修改名称、星级、元素/属性、定位、等级上限、技能等级、头像底色和角色备注。
- 配队方案支持查看成员完成度、新建、编辑、删除、导出和成员跳转；“编辑配队”可修改名称、备注、标签和成员。
- 顶部按钮支持 JSON 导入与导出，便于后续接入 Nebula 的本地数据服务。

## 统一数据模型

每个游戏都归一化为：

```text
games.<game>.characters[]
  id, name, rarity, duplicate, level, maxLevel
  weapon { name, rarity, refinement, level }
  talents[]
  buildTarget {
    sets[], mainStats { <slot>: <stat> }, substats, set
  }
  gear { set, sets[], score, pieces[], mainStats{}, substats }  # 当前实际装备快照
  role, element, note

games.<game>.teams[]
  id, name, note, members[], tags[]

games.<game>.inventory.items[]
  id, kind, name, rarity, level, maxLevel, refinement
  score, pieces[], slot, aggregateId?, sets[], mainStats{}, substats, customName
  holderId, locked, iconCached, wikiFile, note
```

`Record.xlsx` 的“命座 / 星魂 / 共鸣链”等游戏专属列统一映射到 `duplicate`，在界面中由游戏元数据恢复对应显示名称；技能、评分和部位评分字段保留原表含义。原神旧表中的 `断章·攻火暴` 这类字符串会迁移到角色 `buildTarget`（套装 + 可变部位主词条），不会再作为圣遗物备注；“补充”列会迁移到目标副词条。只有识别到套装的旧装备才会生成仓库占位条目并保留评分，纯主词条目标不会伪造实际装备。

`kind` 会按游戏映射为 `weapon`、`relic`、`equipment` 或 `echo`。首次打开或导入旧 JSON 时，角色原有的武器和可识别套装会按游戏部位拆分为多个 `inventory.items[]` 条目并写入 `holderId`；目标字段不会被仓库条目的主词条覆盖。Wiki 图标入口使用各游戏 BWiki 的 `Special:FilePath` 文件，优先读取 `assets/wiki/` 缓存，缓存缺失时尝试从 Wiki 加载；新增条目也可在“Wiki 文件名”字段填写文件名。原神使用“生之花/死之羽/时之沙/空之杯/理之冠”，崩铁使用“头部/手部/躯干/脚部/位面球/连结绳”；崩铁各部位的文件名遵循 Wiki 中的实际物品名。

图标来源：原神 [BWiki](https://wiki.biligame.com/ys/)、崩铁 [BWiki](https://wiki.biligame.com/sr/)、终末地 [BWiki](https://wiki.biligame.com/zmd/)、鸣潮 [BWiki](https://wiki.biligame.com/wutheringwaves/)。

## 与 Nebula 融合时的建议边界

后续接入时可以保留本目录的视图层，将 `sampleData` 替换为 Nebula 的读写适配器：

1. 由 `Nebula.Core.GameRecord` 提供账号与角色快照。
2. 用独立 JSON 存储用户维护的装备评分、备注和配队，不改写官方查询结果。
3. 通过 WebView2 或拆分后的组件嵌入 `GameRecordPage`，沿用现有游戏与账号选择上下文。
