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
- 装备仓库按游戏保存武器/光锥、圣遗物/遗器、装备组或声骸；可搜索、筛选、排序、锁定和新增条目。
- 角色详情可从当前游戏仓库选择装备；换手时会自动解除原角色绑定，仓库条目会显示当前持有角色。
- 角色、武器和套装图标使用 BWiki 文件，示例图标已缓存到 `assets/wiki/`，离线时自动回退为首字母。
- 详情面板可直接调整等级与重复命座，并自动保存到本机。
- 配队方案支持查看成员完成度、新建、删除、导出和成员跳转。
- 顶部按钮支持 JSON 导入与导出，便于后续接入 Nebula 的本地数据服务。

## 统一数据模型

每个游戏都归一化为：

```text
games.<game>.characters[]
  id, name, rarity, duplicate, level, maxLevel
  weapon { name, rarity, refinement, level }
  talents[]
  gear { set, score, pieces[] }
  role, element, note

games.<game>.teams[]
  id, name, note, members[], tags[]

games.<game>.inventory.items[]
  id, kind, name, rarity, level, maxLevel, refinement
  score, pieces[], holderId, locked, iconCached, wikiFile, note
```

`Record.xlsx` 的“命座 / 星魂 / 共鸣链”等游戏专属列统一映射到 `duplicate`，在界面中由游戏元数据恢复对应显示名称；技能、装备和评分字段保留原表含义。

`kind` 会按游戏映射为 `weapon`、`relic`、`equipment` 或 `echo`。首次打开或导入旧 JSON 时，角色原有的装备会自动迁移到 `inventory.items[]` 并写入 `holderId`。Wiki 图标入口使用各游戏 BWiki 的 `Special:FilePath` 文件；缓存文件来自对应 Wiki 文件页，新增条目可在“Wiki 文件名”字段填写文件名。

图标来源：原神 [BWiki](https://wiki.biligame.com/ys/)、崩铁 [BWiki](https://wiki.biligame.com/sr/)、终末地 [BWiki](https://wiki.biligame.com/zmd/)、鸣潮 [BWiki](https://wiki.biligame.com/wutheringwaves/)。

## 与 Nebula 融合时的建议边界

后续接入时可以保留本目录的视图层，将 `sampleData` 替换为 Nebula 的读写适配器：

1. 由 `Nebula.Core.GameRecord` 提供账号与角色快照。
2. 用独立 JSON 存储用户维护的装备评分、备注和配队，不改写官方查询结果。
3. 通过 WebView2 或拆分后的组件嵌入 `GameRecordPage`，沿用现有游戏与账号选择上下文。
