/* global Blob, URL */

const STORAGE_KEY = "nebula-progression-workbench-v1";
const DATA_VERSION = 4;
const MAX_TALENT = 13;

const gameMeta = {
  genshin: {
    name: "原神",
    short: "Genshin",
    cover: "assets/genshin.jpg",
    accent: "#d8ad6b",
    duplicateLabel: "命座",
    equipmentLabel: "武器",
    gearLabel: "圣遗物",
    scoreLabel: "圣遗物评分",
    levelMax: 90,
    scoreTarget: 245,
    talentLabels: ["普攻", "战技", "爆发"],
  },
  starrail: {
    name: "崩铁",
    short: "Star Rail",
    cover: "assets/starrail.jpg",
    accent: "#a9a2ed",
    duplicateLabel: "星魂",
    equipmentLabel: "光锥",
    gearLabel: "遗器",
    scoreLabel: "遗器评分",
    levelMax: 80,
    scoreTarget: 310,
    talentLabels: ["普攻", "战技", "终结技"],
  },
  endfield: {
    name: "终末地",
    short: "Endfield",
    cover: "assets/endfield.jpg",
    accent: "#e8896f",
    duplicateLabel: "潜能",
    equipmentLabel: "武器",
    gearLabel: "装备组",
    scoreLabel: "技能完成度",
    levelMax: 90,
    scoreTarget: 100,
    talentLabels: ["普攻", "战技", "连携", "终结"],
  },
  wuthering: {
    name: "鸣潮",
    short: "Wuthering",
    cover: "assets/wutheringwaves.jpg",
    accent: "#71c8b8",
    duplicateLabel: "共鸣链",
    equipmentLabel: "声骸",
    gearLabel: "声骸套装",
    scoreLabel: "声骸评分",
    levelMax: 90,
    scoreTarget: 205,
    talentLabels: ["主属性", "副属性"],
  },
};

const wikiBases = {
  genshin: "https://wiki.biligame.com/ys/Special:FilePath/",
  starrail: "https://wiki.biligame.com/sr/Special:FilePath/",
  endfield: "https://wiki.biligame.com/zmd/Special:FilePath/",
  wuthering: "https://wiki.biligame.com/wutheringwaves/Special:FilePath/",
};

const wikiHomes = {
  genshin: "https://wiki.biligame.com/ys/",
  starrail: "https://wiki.biligame.com/sr/",
  endfield: "https://wiki.biligame.com/zmd/",
  wuthering: "https://wiki.biligame.com/wutheringwaves/",
};

const inventoryMeta = {
  genshin: [
    { key: "weapon", label: "武器" },
    { key: "relic", label: "圣遗物" },
  ],
  starrail: [
    { key: "weapon", label: "光锥" },
    { key: "relic", label: "遗器" },
  ],
  endfield: [
    { key: "weapon", label: "武器" },
    { key: "equipment", label: "装备组" },
  ],
  wuthering: [
    { key: "echo", label: "声骸" },
  ],
};

const wikiRelicFiles = {
  genshin: {
    "穹境": "穹境示现之夜生之花.png",
    "纺月": "纺月的夜歌生之花.png",
    "晨星": "晨星与月的晓歌生之花.png",
    "深林": "深林的记忆生之花.png",
    "剧团": "黄金剧团生之花.png",
    "勇者": "烬城勇者绘卷生之花.png",
    "风套": "翠绿之影生之花.png",
    "终曲": "深廊终曲生之花.png",
    "绝缘": "绝缘之旗印生之花.png",
    "断章": "谐律异想断章生之花.png",
  },
  starrail: {
    "诗人": "哀歌覆国的诗人.png",
    "拾骨地": "谧宁拾骨地.png",
    "铁骑": "荡除蠹灾的铁骑.png",
    "劫火": "灼尽炼狱的新骸.png",
    "女武神": "烈阳惊雷的女武神.png",
    "巨树": "渊思寂虑的巨树.png",
    "船长": "恶海逐波的船长.png",
    "格拉默": "苍穹战线格拉默.png",
  },
};

const wikiUnavailable = new Set([
  "endfield/苍冥星梦图标.png",
  "endfield/警用工具组图标.png",
  "endfield/潮涌图标.png",
  "starrail/灼尽炼狱的新骸.png",
  "starrail/苍穹战线格拉默.png",
]);

const statOption = (value, label, short) => ({ value, label, short });

const relicStats = {
  hpFlat: statOption("hp_flat", "生命值", "生"),
  atkFlat: statOption("atk_flat", "攻击力", "攻"),
  hpPct: statOption("hp_pct", "生命值%", "生"),
  atkPct: statOption("atk_pct", "攻击力%", "攻"),
  defPct: statOption("def_pct", "防御力%", "防"),
  em: statOption("em", "元素精通", "精"),
  er: statOption("er", "元素充能效率%", "充"),
  critRate: statOption("crit_rate", "暴击率%", "暴"),
  critDmg: statOption("crit_dmg", "暴击伤害%", "爆"),
  healing: statOption("healing", "治疗加成%", "治"),
  effectHit: statOption("effect_hit", "效果命中%", "效命"),
  speed: statOption("speed", "速度", "速"),
  breakEffect: statOption("break_effect", "击破特攻%", "击破"),
  physicalDmg: statOption("physical_dmg", "物理伤害加成%", "物"),
  pyroDmg: statOption("pyro_dmg", "火元素伤害加成%", "火"),
  cryoDmg: statOption("cryo_dmg", "冰元素伤害加成%", "冰"),
  hydroDmg: statOption("hydro_dmg", "水元素伤害加成%", "水"),
  electroDmg: statOption("electro_dmg", "雷元素伤害加成%", "雷"),
  anemoDmg: statOption("anemo_dmg", "风元素伤害加成%", "风"),
  geoDmg: statOption("geo_dmg", "岩元素伤害加成%", "岩"),
  dendroDmg: statOption("dendro_dmg", "草元素伤害加成%", "草"),
  fireDmg: statOption("fire_dmg", "火属性伤害提高%", "火"),
  iceDmg: statOption("ice_dmg", "冰属性伤害提高%", "冰"),
  lightningDmg: statOption("lightning_dmg", "雷属性伤害提高%", "雷"),
  windDmg: statOption("wind_dmg", "风属性伤害提高%", "风"),
  quantumDmg: statOption("quantum_dmg", "量子属性伤害提高%", "量子"),
  imaginaryDmg: statOption("imaginary_dmg", "虚数属性伤害提高%", "虚数"),
  energyRegen: statOption("energy_regen", "能量恢复效率%", "充"),
};

const genshinMainStatOptions = [
  relicStats.hpPct,
  relicStats.atkPct,
  relicStats.defPct,
  relicStats.em,
  relicStats.er,
];

const genshinGobletOptions = [
  relicStats.hpPct,
  relicStats.atkPct,
  relicStats.defPct,
  relicStats.em,
  relicStats.pyroDmg,
  relicStats.cryoDmg,
  relicStats.hydroDmg,
  relicStats.electroDmg,
  relicStats.anemoDmg,
  relicStats.geoDmg,
  relicStats.dendroDmg,
  relicStats.physicalDmg,
];

const genshinCircletOptions = [
  relicStats.hpPct,
  relicStats.atkPct,
  relicStats.defPct,
  relicStats.em,
  relicStats.critRate,
  relicStats.critDmg,
  relicStats.healing,
];

const starrailBodyOptions = [
  relicStats.hpPct,
  relicStats.atkPct,
  relicStats.defPct,
  relicStats.critRate,
  relicStats.critDmg,
  relicStats.healing,
  relicStats.effectHit,
];

const starrailSphereOptions = [
  relicStats.hpPct,
  relicStats.atkPct,
  relicStats.defPct,
  relicStats.physicalDmg,
  relicStats.fireDmg,
  relicStats.iceDmg,
  relicStats.lightningDmg,
  relicStats.windDmg,
  relicStats.quantumDmg,
  relicStats.imaginaryDmg,
];

const starrailRopeOptions = [
  relicStats.hpPct,
  relicStats.atkPct,
  relicStats.defPct,
  relicStats.breakEffect,
  relicStats.energyRegen,
];

const buildTargetMeta = {
  genshin: {
    label: "圣遗物",
    setFields: [{ key: "set", label: "套装" }],
    setOptions: [
      { value: "穹境", label: "穹境示现之夜", aliases: ["穹境", "穹境示现之夜"], icon: "穹境示现之夜生之花.png" },
      { value: "纺月", label: "纺月的夜歌", aliases: ["纺月", "纺月的夜歌"], icon: "纺月的夜歌生之花.png" },
      { value: "晨星", label: "晨星与月的晓歌", aliases: ["晨星", "晨星与月的晓歌"], icon: "晨星与月的晓歌生之花.png" },
      { value: "深林", label: "深林的记忆", aliases: ["深林", "深林的记忆"], icon: "深林的记忆生之花.png" },
      { value: "剧团", label: "黄金剧团", aliases: ["剧团", "黄金剧团"], icon: "黄金剧团生之花.png" },
      { value: "勇者", label: "烬城勇者绘卷", aliases: ["勇者", "烬城勇者绘卷"], icon: "烬城勇者绘卷生之花.png" },
      { value: "风套", label: "翠绿之影", aliases: ["风套", "翠绿之影"], icon: "翠绿之影生之花.png" },
      { value: "终曲", label: "深廊终曲", aliases: ["终曲", "深廊终曲"], icon: "深廊终曲生之花.png" },
      { value: "绝缘", label: "绝缘之旗印", aliases: ["绝缘", "绝缘之旗印"], icon: "绝缘之旗印生之花.png" },
      { value: "断章", label: "谐律异想断章", aliases: ["断章", "谐律异想断章"], icon: "谐律异想断章生之花.png" },
      { value: "黑曜", label: "黑曜秘典", aliases: ["黑曜", "黑曜秘典"] },
      { value: "逐影", label: "逐影猎人", aliases: ["逐影", "逐影猎人"] },
      { value: "追忆", label: "追忆之注连", aliases: ["追忆", "追忆之注连"] },
      { value: "魔女", label: "炽烈的炎之魔女", aliases: ["魔女", "炽烈的炎之魔女"] },
      { value: "海染", label: "海染砗磲", aliases: ["海染", "海染砗磲"] },
      { value: "千岩", label: "千岩牢固", aliases: ["千岩", "千岩牢固"] },
      { value: "宗室", label: "昔日宗室之仪", aliases: ["宗室", "昔日宗室之仪"] },
      { value: "饰金", label: "饰金之梦", aliases: ["饰金", "饰金之梦"] },
      { value: "如雷", label: "如雷的盛怒", aliases: ["如雷", "如雷的盛怒"] },
      { value: "水仙", label: "水仙之梦", aliases: ["水仙", "水仙之梦"] },
      { value: "华馆", label: "华馆梦醒形骸记", aliases: ["华馆", "华馆梦醒形骸记"] },
      { value: "来歆", label: "来歆余响", aliases: ["来歆", "来歆余响"] },
      { value: "沙楼", label: "沙上楼阁史话", aliases: ["沙楼", "沙上楼阁史话"] },
    ],
    slots: [
      { key: "flower", label: "生之花", fixed: relicStats.hpFlat },
      { key: "plume", label: "死之羽", fixed: relicStats.atkFlat },
      { key: "sands", label: "时之沙", options: genshinMainStatOptions },
      { key: "goblet", label: "空之杯", options: genshinGobletOptions },
      { key: "circlet", label: "理之冠", options: genshinCircletOptions },
    ],
  },
  starrail: {
    label: "遗器",
    setFields: [{ key: "set4", label: "四件套" }, { key: "set2", label: "二件套 / 位面" }],
    setOptions: [
      { value: "诗人", label: "哀歌覆国的诗人", aliases: ["诗人", "哀歌覆国的诗人"], icon: "哀歌覆国的诗人.png" },
      { value: "拾骨地", label: "谧宁拾骨地", aliases: ["拾骨地", "谧宁拾骨地"], icon: "谧宁拾骨地.png" },
      { value: "救世主", label: "救世主", aliases: ["救世主"] },
      { value: "翁瓦克", label: "翁瓦克", aliases: ["翁瓦克", "翁法罗斯"] },
      { value: "女武神", label: "烈阳惊雷的女武神", aliases: ["女武神", "烈阳惊雷的女武神"], icon: "烈阳惊雷的女武神.png" },
      { value: "巨树", label: "渊思寂虑的巨树", aliases: ["巨树", "渊思寂虑的巨树"], icon: "渊思寂虑的巨树.png" },
      { value: "船长", label: "恶海逐波的船长", aliases: ["船长", "恶海逐波的船长"], icon: "恶海逐波的船长.png" },
      { value: "格拉默", label: "苍穹战线格拉默", aliases: ["格拉默", "苍穹", "乐园", "苍穹战线格拉默"], icon: "苍穹战线格拉默.png" },
      { value: "铁骑", label: "荡除蠹灾的铁骑", aliases: ["铁骑", "荡除蠹灾的铁骑"], icon: "荡除蠹灾的铁骑.png" },
      { value: "劫火", label: "灼尽炼狱的始焉", aliases: ["劫火", "灼尽炼狱的新骸"] },
      { value: "钟表匠", label: "机心戏梦的钟表匠", aliases: ["钟表匠", "机心戏梦的钟表匠"] },
      { value: "司铎", label: "重循苦旅的司铎", aliases: ["司铎", "重循苦旅的司铎"] },
      { value: "露莎卡", label: "沉欢醉饮的海滨", aliases: ["露莎卡", "沉欢醉饮的海滨"] },
      { value: "枪手", label: "野穗伴行的快枪手", aliases: ["枪手", "野穗伴行的快枪手"] },
      { value: "天才", label: "繁星璀璨的天才", aliases: ["天才", "繁星璀璨的天才"] },
      { value: "信使", label: "骇域漫游的信使", aliases: ["信使", "骇域漫游的信使"] },
      { value: "过客", label: "云无留迹的过客", aliases: ["过客", "云无留迹的过客"] },
      { value: "匹诺康尼", label: "梦想之地匹诺康尼", aliases: ["匹诺康尼", "梦想之地匹诺康尼"] },
      { value: "茨冈尼亚", label: "无主荒星茨冈尼亚", aliases: ["茨冈尼亚", "无主荒星茨冈尼亚"] },
      { value: "出云", label: "出云显世与高天神国", aliases: ["出云", "出云显世与高天神国"] },
      { value: "停转", label: "停转的萨尔索图", aliases: ["停转", "停转的萨尔索图"] },
    ],
    slots: [
      { key: "head", label: "头部", fixed: relicStats.hpFlat },
      { key: "hands", label: "手部", fixed: relicStats.atkFlat },
      { key: "body", label: "躯干", options: starrailBodyOptions },
      { key: "feet", label: "脚部", options: [relicStats.hpPct, relicStats.atkPct, relicStats.defPct, relicStats.speed] },
      { key: "sphere", label: "位面球", options: starrailSphereOptions },
      { key: "rope", label: "连结绳", options: starrailRopeOptions },
    ],
  },
};

// BWiki uses item names rather than slot names for Star Rail relic pieces.
// Keep the mapping local so the inventory can still resolve a precise image offline/without API calls.
const relicSlotIconFiles = {
  starrail: {
    诗人: { head: "诗人的莳萝花冠.png", hands: "诗人的贵金手镯.png", body: "诗人的缀星裙摆.png", feet: "诗人的钉银之履.png" },
    拾骨地: { sphere: "哀地里亚的殁名祭碑.png", rope: "哀地里亚的入溟骨链.png" },
    救世主: { head: "救世主的登程风帽.png", hands: "救世主的执剑护手.png", body: "救世主的传承战袍.png", feet: "救世主的拓荒长靴.png" },
    翁瓦克: { sphere: "翁瓦克的诞生之岛.png", rope: "翁瓦克的环岛海岸.png" },
    女武神: { head: "女武神的翱翔翼盔.png", hands: "女武神的骑枪手铠.png", body: "女武神的启程披风.png", feet: "女武神的勋礼马刺.png" },
    巨树: { sphere: "神悟树庭的沉思巨桹.png", rope: "神悟树庭的联识叶路.png" },
    船长: { head: "船长的领航笠帽.png", hands: "船长的捕光星盘.png", body: "船长的驭风斗篷.png", feet: "船长的踏浪游靴.png" },
    格拉默: { sphere: "格拉默的铁骑兵团.png", rope: "格拉默的寂静坟碑.png" },
    铁骑: { head: "铁骑的索敌战盔.png", hands: "铁骑的摧坚铁腕.png", body: "铁骑的银影装甲.png", feet: "铁骑的行空护胫.png" },
    劫火: { sphere: "铸炼宫的莲华灯芯.png", rope: "铸炼宫的焰轮天绸.png" },
    钟表匠: { head: "钟表匠的极目透镜.png", hands: "钟表匠的交运腕表.png", body: "钟表匠的空幻礼服.png", feet: "钟表匠的隐梦革履.png" },
    司铎: { head: "司铎的律音耳坠.png", hands: "司铎的授邀手套.png", body: "司铎的圣职礼服.png", feet: "司铎的苦旅短靴.png" },
    露莎卡: { sphere: "酣歌海垠的礁屿灯塔.png", rope: "酣歌海垠的歌咏步道.png" },
    枪手: { head: "快枪手的野穗毡帽.png", hands: "快枪手的粗革手套.png", body: "快枪手的猎风披肩.png", feet: "快枪手的铆钉马靴.png" },
    天才: { head: "天才的超距遥感.png", hands: "天才的频变捕手.png", body: "天才的元域深潜.png", feet: "天才的引力漫步.png" },
    信使: { head: "信使的全息目镜.png", hands: "信使的百变义手.png", body: "信使的密信挎包.png", feet: "信使的酷跑板鞋.png" },
    过客: { head: "过客的逢春木簪.png", hands: "过客的游龙臂鞲.png", body: "过客的残绣风衣.png", feet: "过客的冥途游履.png" },
    匹诺康尼: { sphere: "匹诺康尼的堂皇酒店.png", rope: "匹诺康尼的逐梦轨道.png" },
    茨冈尼亚: { sphere: "茨冈尼亚的母神卧榻.png", rope: "茨冈尼亚的轮回纽结.png" },
    出云: { sphere: "出云的祸津众神.png", rope: "出云的终始一刀.png" },
    停转: { sphere: "萨尔索图的移动城市.png", rope: "萨尔索图的晨昏界线.png" },
  },
};

const genericRelicSlotIconFiles = {
  genshin: {
    flower: "黄金剧团生之花.png",
    plume: "黄金剧团死之羽.png",
    sands: "黄金剧团时之沙.png",
    goblet: "黄金剧团空之杯.png",
    circlet: "黄金剧团理之冠.png",
  },
  starrail: {
    head: "诗人的莳萝花冠.png",
    hands: "诗人的贵金手镯.png",
    body: "诗人的缀星裙摆.png",
    feet: "诗人的钉银之履.png",
    sphere: "哀地里亚的殁名祭碑.png",
    rope: "哀地里亚的入溟骨链.png",
  },
};

const cachedRelicIconFiles = new Set([
  ...Object.values(wikiRelicFiles).flatMap((files) => Object.values(files)),
  ...Object.values(genericRelicSlotIconFiles).flatMap((files) => Object.values(files)),
]);

const relicSlotAliases = {
  flower: ["flower", "生之花", "花"],
  plume: ["plume", "死之羽", "羽"],
  sands: ["sands", "时之沙", "沙", "沙漏"],
  goblet: ["goblet", "空之杯", "杯"],
  circlet: ["circlet", "理之冠", "冠", "头"],
  head: ["head", "头部", "头"],
  hands: ["hands", "手部", "手"],
  body: ["body", "躯干", "身体", "身"],
  feet: ["feet", "脚部", "脚"],
  sphere: ["sphere", "位面球", "球"],
  rope: ["rope", "连结绳", "绳"],
};

const makeRole = (id, name, rarity, duplicate, level, weapon, weaponRarity, weaponRefinement, weaponLevel, talents, gearSet, score, pieces, note, role, element, color, extra = {}) => ({
  id,
  name,
  rarity,
  duplicate,
  level,
  maxLevel: extra.maxLevel || undefined,
  weapon: weapon ? { name: weapon, rarity: weaponRarity || 4, refinement: weaponRefinement || 1, level: weaponLevel || level } : null,
  talents: talents || [],
  gear: { set: gearSet || "未配置", score: score ?? null, pieces: pieces || [] },
  note: note || "",
  role: role || "未分类",
  element: element || "",
  color: color || "#4a5265",
  ...extra,
});

const sampleData = {
  version: 1,
  activeGame: "genshin",
  games: {
    genshin: {
      characters: [
        makeRole("gs-nefer", "奈芙尔", 5, 0, 90, "霜辰", 4, 1, 90, [1, 10, 9], "穹境 · 精精暴", 216.6, [38.1, 39.5, 44.8, 49, 45.1], "暴击45+，爆伤140+，元素精通800+", "主C", "草", "#a7bd82"),
        makeRole("gs-lauma", "菈乌玛", 5, 0, 90, "天光的纺琴", 4, 5, 90, [1, 9, 9], "纺月 · 精精精", 168.8, [22.9, 32.8, 36.1, 45.4, 31.5], "攻击1000+，精通800+，充能180+", "辅助", "冰", "#94b9d0"),
        makeRole("gs-columbina", "哥伦比娅", 5, 0, 90, "西风秘典", 4, 5, 70, [7, 8, 8], "晨星 · 充生暴", 174.8, [42.1, 40.5, 42.4, 25.5, 24.2], "生命29500+，充能210+，暴击60+，爆伤220+", "辅助", "冰", "#aa9fd4"),
        makeRole("gs-nahida", "纳西妲", 5, 1, 90, "流浪的晚星", 4, 1, 90, [6, 10, 10], "深林 · 精精精", null, [], "生命15000+，精通800+，攻击1100+，充能150+", "副C", "草", "#99c98c"),
        makeRole("gs-furina", "芙宁娜", 5, 6, 90, "静水流涌之辉", 5, 1, 90, [10, 13, 13], "剧团 · 生水暴", 246, [50.3, 49.6, 47.9, 46.8, 51.3], "", "辅助", "水", "#78a9d0"),
        makeRole("gs-xilonen", "希诺宁", 5, 0, 90, "岩峰巡歌", 5, 1, 90, [1, 10, 8], "勇者 · 充防防", null, [], "充能170+，防御2450+", "辅助", "岩", "#d0aa73"),
        makeRole("gs-kazuha", "枫原万叶", 5, 0, 90, "西福斯的月光", 4, 2, 90, [2, 8, 8], "风套 · 精精精", null, [], "精通800+，充能120+", "辅助", "风", "#81b8a2"),
        makeRole("gs-skirk", "丝柯克", 5, 0, 90, "雾切之回光", 5, 1, 90, [4, 10, 10], "终曲 · 攻冰暴", 222, [52.6, 37.6, 47.4, 50.4, 34], "攻击2000+，暴击60+，暴伤200+", "主C", "冰", "#8ea7db"),
        makeRole("gs-aikofei", "爱可菲", 5, 0, 90, "护摩之杖", 5, 1, 90, [6, 9, 10], "剧团 · 攻冰暴", 243.1, [42.6, 44.1, 48.3, 52.5, 55.5], "攻击2000+，充能135+，暴击65+，暴伤200+", "副C", "冰", "#d98c9b"),
        makeRole("gs-citlali", "茜特菈莉", 5, 0, 90, "祭礼残章", 4, 1, 90, [1, 8, 8], "勇者 · 充精精", 145.7, [5.8, 32.8, 36, 36.7, 34.4], "精通800+，充能175+", "辅助", "冰", "#86b9d7"),
        makeRole("gs-shenhe", "申鹤", 5, 1, 90, "息灾", 5, 1, 70, [2, 9, 7], "攻攻攻 · 攻击", 110.8, [5.4, 11.7, 33.2, 29.4, 31.1], "攻击3000+", "辅助", "冰", "#adc5de"),
        makeRole("gs-yinepve", "伊涅芙", 5, 0, 90, "支离轮光", 5, 1, 70, [1, 10, 8], "纺月 · 精攻暴", 205, [40.7, 36.6, 55.1, 29.3, 43.4], "精通380+，攻击2000+，充能140+", "副C", "雷", "#b89ad4"),
        makeRole("gs-neuvillette", "那维莱特", 5, 1, 90, "万世流涌大典", 5, 1, 90, [10, 10, 10], "勇者 · 生水暴", 250.7, [38, 45.7, 57, 54.2, 55.8], "生命32000+", "主C", "水", "#6fa6d3"),
        makeRole("gs-yelan", "夜兰", 5, 1, 90, "若水", 5, 1, 90, [6, 10, 10], "绝缘 · 充水爆", null, [], "生命23000+，精通80+，充能220+", "副C", "水", "#4f81c1"),
        makeRole("gs-mavuika", "玛薇卡", 5, 0, 90, "焚曜千阳", 5, 1, 90, [7, 10, 10], "黑曜 · 攻火暴", 201.3, [38.2, 41.7, 36.7, 39, 45.7], "精通80+，攻击2250+，暴击40+，暴伤200+", "主C", "火", "#d47b60"),
        makeRole("gs-arlecchino", "阿蕾奇诺", 5, 1, 90, "赤月之形", 5, 1, 90, [10, 10, 8], "断章 · 攻火暴", 215.6, [43.9, 46.8, 28.3, 46.4, 50.3], "攻击2000+，暴击60+，暴伤180+", "主C", "火", "#c77b76"),
        makeRole("gs-fischl", "菲谢尔", 4, 6, 90, "天空之翼", 5, 1, 1, [], "未配置", null, [], "", "副C", "雷", "#ac9fda"),
        makeRole("gs-xingqiu", "行秋", 4, 6, 90, "祭礼剑", 4, 5, 90, [], "未配置", null, [], "", "副C", "水", "#6599d1"),
        makeRole("gs-bennett", "班尼特", 4, 6, 90, "风鹰剑", 5, 2, 90, [], "未配置", null, [], "", "辅助", "火", "#d48963"),
        makeRole("gs-xiangling", "香菱", 4, 6, 90, "「渔获」", 4, 2, 90, [], "未配置", null, [], "", "副C", "火", "#d36f5f"),
        makeRole("gs-iansan", "伊安珊", 4, 0, 86, "西风长枪", 4, 5, 20, [], "未配置", null, [], "", "辅助", "雷", "#b28fc2"),
        makeRole("gs-aino", "爱诺", 4, 1, 80, "玛海菈的水色", 4, 1, 1, [], "未配置", null, [], "", "辅助", "水", "#7eaac2"),
        makeRole("gs-chevreuse", "夏沃蕾", 4, 6, 80, "西风长枪", 4, 5, 90, [], "未配置", null, [], "", "辅助", "火", "#d98e62"),
        makeRole("gs-mona", "莫娜", 5, 1, 70, "讨龙英杰谭", 3, 5, 20, [], "未配置", null, [], "", "辅助", "水", "#6d91bd"),
        makeRole("gs-sucrose", "砂糖", 4, 6, 70, "讨龙英杰谭", 3, 1, 1, [], "未配置", null, [], "", "辅助", "风", "#81b69e"),
      ],
      teams: [
        { id: "gs-team-skirk", name: "丝柯克 · 冰水速切", note: "冰水双核，优先保证循环与爆发窗口。", members: ["gs-skirk", "gs-aikofei", "gs-furina", "gs-shenhe"], tags: ["冰水反应", "速切", "深境螺旋"] },
        { id: "gs-team-mavuika", name: "玛薇卡 · 纳塔爆发", note: "火伤主轴，希诺宁提供减抗与续航。", members: ["gs-mavuika", "gs-xilonen", "gs-kazuha", "gs-bennett"], tags: ["火伤", "减抗", "爆发"] },
        { id: "gs-team-nahida", name: "纳西妲 · 草系循环", note: "草系后台与水元素持续触发。", members: ["gs-nahida", "gs-yelan", "gs-furina", "gs-xilonen"], tags: ["草水", "后台", "持续伤害"] },
      ],
    },
    starrail: {
      characters: [
        makeRole("sr-butterfly", "遐蝶", 5, 0, 80, "让告别，更美一些", 5, 1, 80, [6, 10, 10, 10], "诗人 + 拾骨地", 258.2, [39, 41.4, 49.9, 41.3, 38.6, 48], "", "主C", "量子", "#9b88c4", { maxLevel: 80 }),
        makeRole("sr-evernight", "昔涟", 5, 2, 80, "爱如此刻永恒", 5, 1, 80, [6, 10, 10, 10], "救世主 + 翁法罗斯", 250.6, [51, 44.9, 35.8, 45.9, 41.2, 31.7], "", "辅助", "雷", "#d090a7", { maxLevel: 80 }),
        makeRole("sr-longnight", "长夜月", 5, 2, 80, "致长夜的星光", 5, 1, 80, [6, 10, 10, 10], "救世主 + 拾骨地", 256.3, [38.9, 46.9, 32.4, 44, 39.1, 54.9], "", "副C", "冰", "#8d9fc8", { maxLevel: 80 }),
        makeRole("sr-feather", "风堇", 5, 1, 80, "愿虹光永驻天空", 5, 1, 80, [6, 9, 10, 10], "女武神 + 巨树", 213.6, [28.9, 27.2, 48.4, 37.6, 40.4, 31.1], "", "辅助", "风", "#79ad9d", { maxLevel: 80 }),
        makeRole("sr-white", "白厄", 5, 0, 80, "记一位星神的陨落", 5, 5, 80, [6, 10, 10, 10], "船长 + 乐园", 254.6, [49.3, 51.2, 39.2, 46.5, 33.2, 35.2], "", "主C", "物理", "#d6a86d", { maxLevel: 80 }),
        makeRole("sr-firefly", "流萤", 5, 0, 80, "梦应归于何处", 5, 1, 80, [6, 10, 10, 10], "铁骑 + 劫火", 307.4, [51.6, 49.8, 52.5, 49.4, 56.4, 47.8], "", "主C", "火", "#d87469", { maxLevel: 80 }),
        makeRole("sr-fugue", "忘归人", 5, 0, 80, "孤独的疗愈", 5, 5, 80, [6, 9, 9, 10], "铁骑 + 劫火", null, [], "", "辅助", "火", "#c58175", { maxLevel: 80 }),
        makeRole("sr-lingsha", "灵砂", 5, 0, 80, "唯有香如故", 5, 1, 80, [6, 9, 9, 10], "铁骑 + 劫火", null, [], "", "生存", "火", "#d69f76", { maxLevel: 80 }),
        makeRole("sr-ruanmei", "阮·梅", 5, 2, 80, "记忆中的模样", 4, 5, 80, [6, 10, 10, 10], "钟表匠 + 翁瓦克", null, [], "", "辅助", "冰", "#7bb0ac", { maxLevel: 80 }),
        makeRole("sr-tibao", "缇宝", 4, 0, 80, "舞！舞！舞！", 4, 5, 80, [6, 10, 9, 10], "诗人 + 拾骨地", 229.1, [32.2, 38.4, 56.8, 34.5, 41.5, 25.7], "", "辅助", "量子", "#a999d2", { maxLevel: 80 }),
        makeRole("sr-dahlia", "大丽花", 4, 0, 80, "决心如汗珠般闪耀", 4, 5, 80, [6, 9, 9, 9], "铁骑 + 劫火", 198.2, [46.9, 51.8, 25.5, 24, 11.7, 38.4], "", "副C", "火", "#d27c6c", { maxLevel: 80 }),
        makeRole("sr-thelema", "刻律德菈", 4, 0, 80, "永远的迷境饭", 4, 5, 80, [6, 9, 9, 9], "司铎 + 露莎卡", null, [], "", "辅助", "风", "#8bb4a2", { maxLevel: 80 }),
      ],
      teams: [
        { id: "sr-team-firefly", name: "流萤 · 超击破", note: "高速循环，阮·梅与忘归人维持击破窗口。", members: ["sr-firefly", "sr-fugue", "sr-ruanmei", "sr-lingsha"], tags: ["击破", "火伤", "高频循环"] },
        { id: "sr-team-butterfly", name: "遐蝶 · 量子双核", note: "双量子核心，缇宝补足行动与增益。", members: ["sr-butterfly", "sr-longnight", "sr-evernight", "sr-tibao"], tags: ["量子", "召唤", "行动提前"] },
      ],
    },
    endfield: {
      characters: [
        makeRole("ef-leventine", "莱万汀", 6, 0, 90, "熔铸火焰", 5, 1, 90, [9, 12, 9, 12], "动火用 · 核心四件", 90, [], "火队主轴，终结技优先。", "主C", "火", "#dc7a5f"),
        makeRole("ef-wolfguard", "狼卫", 5, 0, 60, "同类相食", 4, 1, 60, [1, 1, 1, 1], "落潮轻甲", 49, [], "", "副C", "火", "#bf8b67"),
        makeRole("ef-aldela", "艾尔黛拉", 5, 0, 80, "苍冥星梦", 5, 1, 80, [1, 9, 1, 1], "生物辅助 · 三件", 63, [], "", "辅助", "火", "#8ab4a7"),
        makeRole("ef-antal", "安塔尔", 5, 0, 60, "爆破单元", 4, 1, 60, [1, 9, 1, 9], "落潮轻甲", 55, [], "", "副C", "火", "#d1916a"),
        makeRole("ef-chen", "陈千语", 6, 0, 90, "钢铁余音", 5, 1, 90, [10, 9, 9, 9], "点剑 · 核心四件", 90, [], "物理队主轴。", "主C", "物理", "#d1a56c"),
        makeRole("ef-admin", "管理员", 5, 0, 80, "宏愿", 5, 1, 80, [9, 9, 9, 9], "碾骨 · 重装组", 80, [], "", "辅助", "物理", "#8f9fae"),
        makeRole("ef-lifeng", "黎风", 5, 0, 80, "负山", 4, 1, 80, [9, 9, 9, 9], "长息 · 蓄电组", 80, [], "", "副C", "物理", "#9b9fca"),
        makeRole("ef-junwei", "骏卫", 5, 0, 60, "热熔切割器", 4, 1, 60, [9, 9, 9, 9], "碾骨 · 稳定组", 61, [], "", "副C", "物理", "#c68773"),
        makeRole("ef-yvon", "伊冯", 6, 0, 90, "警用工具组", 4, 1, 90, [8, 9, 8, 9], "警用 · 冰队组", 84, [], "冰队主轴。", "主C", "冰", "#78a8c0"),
        makeRole("ef-bieli", "别礼", 5, 0, 60, "潮涌", 4, 1, 60, [1, 6, 1, 6], "拓荒护甲", 48, [], "", "辅助", "冰", "#9eadd0"),
        makeRole("ef-sai", "赛希", 5, 0, 60, "潮涌", 4, 1, 60, [1, 6, 1, 6], "潮涌 · 辅助组", 47, [], "", "辅助", "冰", "#78a9be"),
        makeRole("ef-jelpetta", "洁尔佩塔", 5, 0, 60, "使命必达", 4, 1, 60, [1, 1, 1, 6], "潮涌 · 长息组", 39, [], "", "生存", "冰", "#8ab7c3"),
      ],
      teams: [
        { id: "ef-team-fire", name: "火队 · 熔铸循环", note: "莱万汀打出核心爆发，艾尔黛拉提供辅助。", members: ["ef-leventine", "ef-wolfguard", "ef-aldela", "ef-antal"], tags: ["火队", "灼热", "技能循环"] },
        { id: "ef-team-physical", name: "物理队 · 点剑", note: "陈千语为主轴，管理员负责稳住场面。", members: ["ef-chen", "ef-admin", "ef-lifeng", "ef-junwei"], tags: ["物理", "点剑", "持续输出"] },
        { id: "ef-team-ice", name: "冰队 · 潮涌", note: "冰属性控制与生存组合。", members: ["ef-yvon", "ef-bieli", "ef-sai", "ef-jelpetta"], tags: ["冰队", "控制", "生存"] },
      ],
    },
    wuthering: {
      characters: [
        makeRole("wu-qisaki", "千咲", 5, 0, 90, "命理 + 沉日", 5, 1, 90, [5, 4], "命理 · 沉日", 173.3, [37.5, 32.3, 31.8, 39.3, 32.4], "", "主C", "冰", "#8faad0"),
        makeRole("wu-younuo", "尤诺", 5, 0, 90, "荣斗 + 啸谷", 5, 1, 90, [5, 4], "荣斗 · 啸谷", 178.7, [30.9, 31.5, 42.1, 29.9, 44.3], "", "辅助", "风", "#86b89e"),
        makeRole("wu-augusta", "奥古斯塔", 5, 0, 90, "荣斗 + 彻空", 5, 1, 90, [5, 4], "荣斗 · 彻空", 177.7, [30.8, 29.4, 39.4, 44.4, 33.7], "", "主C", "雷", "#b89ad4"),
        makeRole("wu-floramel", "弗洛洛", 5, 0, 90, "失序 + 沉日", 5, 1, 90, [5, 4], "失序 · 沉日", 176.5, [35.5, 28.4, 33.5, 43.6, 35.5], "", "副C", "冰", "#9b9ed5"),
        makeRole("wu-cartethyia", "卡提希娅", 5, 1, 90, "荣光", 5, 1, 90, [5, 4], "荣光", 177.9, [39.1, 34.6, 32.6, 41.6, 30], "", "辅助", "风", "#80b6ac"),
        makeRole("wu-xiaokong", "夏空", 5, 0, 90, "流云", 5, 1, 90, [5, 4], "流云", 165.5, [37.2, 30.3, 38.2, 28.2, 31.6], "", "辅助", "气动", "#c3a26b"),
        makeRole("wu-cantarella", "坎特蕾拉", 5, 0, 90, "高天", 5, 1, 90, [5, 4], "高天", 149.3, [29.6, 32.2, 26.5, 23.2, 37.8], "", "副C", "湮灭", "#aa8eaf"),
        makeRole("wu-roccia", "洛可可", 5, 0, 90, "轻云", 5, 1, 90, [5, 4], "轻云", 199.1, [37.8, 38.7, 39.9, 40.4, 42.3], "", "辅助", "衍射", "#d48a86"),
        makeRole("wu-shorekeeper", "守岸人", 5, 0, 90, "隐世", 5, 1, 90, [5, 4], "隐世", 189.1, [34.7, 42.4, 21.3, 46, 44.7], "", "生存", "衍射", "#77a4bd"),
        makeRole("wu-jinhsi", "椿", 5, 0, 90, "沉日", 5, 1, 90, [5, 4], "沉日", 199.1, [41.4, 42.7, 40.1, 33.2, 41.7], "", "主C", "湮灭", "#d07c7e"),
      ],
      teams: [
        { id: "wu-team-augusta", name: "奥古斯塔 · 雷队", note: "雷属性主轴，守岸人负责生存与增益。", members: ["wu-augusta", "wu-younuo", "wu-roccia", "wu-shorekeeper"], tags: ["雷队", "增益", "生存"] },
        { id: "wu-team-skirk", name: "千咲 · 冰队", note: "冰属性持续输出，洛可可提供聚怪与辅助。", members: ["wu-qisaki", "wu-floramel", "wu-roccia", "wu-shorekeeper"], tags: ["冰队", "持续输出", "聚怪"] },
      ],
    },
  },
};

const state = {
  data: null,
  activeGame: "genshin",
  view: "overview",
  selectedRoleId: null,
  selectedTeamId: null,
  selectedInventoryId: null,
  query: "",
  inventoryQuery: "",
  inventoryKind: "all",
  inventorySort: "name",
  rarity: "all",
  status: "all",
  sort: "readiness",
  layout: "list",
  toastTimer: null,
  editingRoleId: null,
  editingTeamId: null,
  editingInventoryId: null,
};

const $ = (selector, root = document) => root.querySelector(selector);
const $$ = (selector, root = document) => Array.from(root.querySelectorAll(selector));

function clone(value) {
  return JSON.parse(JSON.stringify(value));
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function safeColor(value, fallback = "#4a5265") {
  return /^#[0-9a-f]{3,8}$/i.test(String(value || "")) ? value : fallback;
}

function activeGame() {
  return state.data.games[state.activeGame] || state.data.games.genshin;
}

function meta() {
  return gameMeta[state.activeGame] || gameMeta.genshin;
}

function roles() {
  return activeGame().characters || [];
}

function teams() {
  return activeGame().teams || [];
}

function findRole(id) {
  return roles().find((role) => role.id === id) || null;
}

function allRolesForGame(gameKey) {
  return state.data.games[gameKey]?.characters || [];
}

function average(values) {
  const valid = values.filter((value) => Number.isFinite(Number(value)));
  return valid.length ? valid.reduce((sum, value) => sum + Number(value), 0) / valid.length : 0;
}

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

function talentMaxForGame(gameKey = state.activeGame) {
  return gameKey === "endfield" ? 12 : gameKey === "wuthering" ? 5 : MAX_TALENT;
}

function roleReadiness(role) {
  const game = meta();
  const levelRatio = clamp((Number(role.level) || 0) / (Number(role.maxLevel) || game.levelMax), 0, 1);
  const talentMax = talentMaxForGame();
  const talentRatio = role.talents?.length ? clamp(average(role.talents) / talentMax, 0, 1) : levelRatio * 0.68;
  const scoreRatio = role.gear?.score == null ? levelRatio * 0.72 : clamp(Number(role.gear.score) / game.scoreTarget, 0.18, 1);
  const weaponRatio = role.weapon?.level ? clamp(Number(role.weapon.level) / (Number(role.weapon.maxLevel) || game.levelMax), 0, 1) : levelRatio * 0.65;
  const duplicateBonus = clamp((Number(role.duplicate) || 0) / 6, 0, 1) * 0.04;
  return Math.round(clamp((levelRatio * 0.35 + talentRatio * 0.3 + scoreRatio * 0.2 + weaponRatio * 0.15 + duplicateBonus) * 100, 0, 100));
}

function roleStatus(role) {
  const readiness = roleReadiness(role);
  if (readiness >= 88) return { key: "maxed", label: "已毕业", color: "var(--teal)" };
  if (readiness >= 62) return { key: "building", label: "培养中", color: "var(--accent-strong)" };
  return { key: "todo", label: "待补强", color: "var(--coral)" };
}

function readinessColor(readiness) {
  if (readiness >= 88) return "var(--teal)";
  if (readiness >= 62) return "var(--accent-strong)";
  return "var(--coral)";
}

function roleInitial(name) {
  return String(name || "?").trim().slice(0, 1);
}

function stars(rarity) {
  return "★".repeat(Math.max(0, Number(rarity) || 0));
}

function displayScore(role) {
  return role.gear?.score == null ? "—" : Number(role.gear.score).toFixed(1).replace(".0", "");
}

function formatLevel(role) {
  const max = role.maxLevel || meta().levelMax;
  return `${role.level ?? 0}/${max}`;
}

function getEquipmentName(role) {
  if (state.activeGame === "wuthering") return role.gear?.set || "未配置";
  return role.weapon?.name || "未配置";
}

function getGearSummary(role) {
  if (state.activeGame === "endfield") return role.gear?.set || "未配置";
  if (state.activeGame === "wuthering") return role.gear?.set || "未配置";
  return role.gear?.set || "未配置";
}

function relicConfig(gameKey = state.activeGame) {
  return buildTargetMeta[gameKey] || null;
}

function isRelicKind(gameKey, kind) {
  return kind === "relic" && Boolean(relicConfig(gameKey));
}

function relicSetOptions(gameKey = state.activeGame) {
  return relicConfig(gameKey)?.setOptions || [];
}

function relicSlots(gameKey = state.activeGame) {
  return relicConfig(gameKey)?.slots || [];
}

function variableRelicSlots(gameKey = state.activeGame) {
  return relicSlots(gameKey).filter((slot) => !slot.fixed);
}

function findRelicSetOption(gameKey, value) {
  const raw = String(value || "").trim();
  if (!raw) return null;
  const normalized = raw.replace(/\s+/g, "");
  return relicSetOptions(gameKey).find((option) => [option.value, option.label, ...(option.aliases || [])].some((candidate) => String(candidate).replace(/\s+/g, "") === normalized)) || null;
}

function normalizeRelicSetValue(gameKey, value) {
  const option = findRelicSetOption(gameKey, value);
  return option?.value || String(value || "").trim();
}

function relicSetLabel(gameKey, value) {
  const option = findRelicSetOption(gameKey, value);
  return option?.label || String(value || "").trim();
}

function statOptionsForSlot(slot) {
  return slot?.options || (slot?.fixed ? [slot.fixed] : []);
}

function findStatOption(slot, value) {
  const raw = String(value || "").trim();
  if (!raw) return null;
  return statOptionsForSlot(slot).find((option) => option.value === raw || option.label === raw || option.short === raw) || null;
}

function normalizeStatValue(slot, value) {
  return findStatOption(slot, value)?.value || "";
}

function statLabelForSlot(slot, value, fallback = "未选择") {
  return findStatOption(slot, value)?.label || fallback;
}

function statShortForSlot(slot, value) {
  return findStatOption(slot, value)?.short || "";
}

function findRelicSlot(gameKey, value) {
  const raw = String(value || "").trim();
  if (!raw) return null;
  const normalized = raw.replace(/\s+/g, "");
  return relicSlots(gameKey).find((slot) => {
    const aliases = relicSlotAliases[slot.key] || [];
    return [slot.key, slot.label, ...aliases].some((candidate) => String(candidate).replace(/\s+/g, "") === normalized);
  }) || null;
}

function normalizeRelicSlot(gameKey, value) {
  return findRelicSlot(gameKey, value)?.key || "";
}

function inferRelicSlotFromName(gameKey, value) {
  const raw = String(value || "").trim();
  if (!raw) return "";
  const suffix = raw.split(/[·•]/).pop()?.trim() || "";
  return normalizeRelicSlot(gameKey, suffix);
}

function relicSlotLabel(gameKey, value, fallback = "未指定部位") {
  return findRelicSlot(gameKey, value)?.label || fallback;
}

function relicSetIndexForSlot(gameKey, slotKey) {
  if (gameKey === "starrail" && ["sphere", "rope"].includes(slotKey)) return 1;
  return 0;
}

function relicSetValueForSlot(gameKey, sets, slotKey) {
  const values = Array.isArray(sets) ? sets : [];
  const preferred = values[relicSetIndexForSlot(gameKey, slotKey)];
  return preferred || values.find(Boolean) || "";
}

function relicItemDefaultName(gameKey, sets, slotKey) {
  const setValue = relicSetValueForSlot(gameKey, sets, slotKey);
  const setLabel = relicSetLabel(gameKey, setValue);
  const slotLabel = relicSlotLabel(gameKey, slotKey, "");
  if (!setLabel || !slotLabel) return "";
  return `${setLabel}·${slotLabel}`;
}

function relicPieceFileName(gameKey, setValue, slotKey) {
  const option = findRelicSetOption(gameKey, setValue);
  const slot = findRelicSlot(gameKey, slotKey);
  if (!option || !slot) return "";
  const mapped = relicSlotIconFiles[gameKey]?.[option.value]?.[slot.key];
  if (mapped) return mapped;
  if (gameKey === "genshin") return `${option.label}${slot.label}.png`;
  return "";
}

function relicPieceIconCandidates(gameKey, item) {
  const slotKey = normalizeRelicSlot(gameKey, item?.slot || item?.part || item?.position);
  const sets = Array.isArray(item?.sets) ? item.sets : String(item?.set || "").split(/\s*[+＋·]\s*/).filter(Boolean);
  const preferredSet = relicSetValueForSlot(gameKey, sets, slotKey);
  const setValues = unique([preferredSet, ...sets]);
  const exactFiles = setValues.map((value) => relicPieceFileName(gameKey, value, slotKey)).filter(Boolean);
  const genericFile = genericRelicSlotIconFiles[gameKey]?.[slotKey] || "";
  const fallbackSetFile = setValues.map((value) => {
    const option = findRelicSetOption(gameKey, value);
    return option?.icon || wikiRelicFiles[gameKey]?.[option?.value || value] || "";
  }).filter(Boolean);
  // Keep the selected set and slot together. A generic local icon must not
  // outrank the exact Wiki file for a newly added relic set.
  const localFor = (fileName) => cachedRelicIconFiles.has(fileName) ? wikiImageUrl(gameKey, fileName) : "";
  const localExact = exactFiles.map(localFor).filter(Boolean);
  const remoteExact = exactFiles.map((fileName) => wikiFileUrl(gameKey, fileName)).filter(Boolean);
  const localGeneric = localFor(genericFile);
  const remoteGeneric = wikiFileUrl(gameKey, genericFile);
  const localSet = fallbackSetFile.map(localFor).filter(Boolean);
  const remoteSet = fallbackSetFile.map((fileName) => wikiFileUrl(gameKey, fileName)).filter(Boolean);
  return unique([
    ...localExact,
    ...remoteExact,
    ...(localGeneric ? [localGeneric] : []),
    ...(remoteGeneric ? [remoteGeneric] : []),
    ...localSet,
    ...remoteSet,
  ]);
}

function relicSlotOptionsMarkup(gameKey, selected = "") {
  return `<option value="">未选择</option>${relicSlots(gameKey).map((slot) => `<option value="${escapeHtml(slot.key)}" ${slot.key === selected ? "selected" : ""}>${escapeHtml(slot.label)}</option>`).join("")}`;
}

function defaultBuildTarget(gameKey = state.activeGame) {
  const config = relicConfig(gameKey);
  return {
    sets: Array(config?.setFields?.length || 1).fill(""),
    mainStats: {},
    substats: "",
  };
}

function splitLegacyTarget(raw) {
  return String(raw || "").split(/[\/／]/)[0].trim();
}

function parseLegacyBuildTarget(gameKey, raw) {
  const config = relicConfig(gameKey);
  const target = defaultBuildTarget(gameKey);
  if (!config || !raw || String(raw).trim() === "未配置") return target;
  const source = splitLegacyTarget(raw);
  if (gameKey === "starrail") {
    const chunks = source.split(/\s*[+＋·]\s*/).map((value) => value.trim()).filter(Boolean);
    target.sets = chunks.slice(0, config.setFields.length).map((value) => normalizeRelicSetValue(gameKey, value));
    return target;
  }

  let statText = source.replace(/\s+/g, "");
  const candidates = relicSetOptions(gameKey)
    .flatMap((option) => (option.aliases || [option.value]).map((alias) => ({ option, alias: String(alias).replace(/\s+/g, "") })))
    .sort((a, b) => b.alias.length - a.alias.length);
  const match = candidates.find(({ alias }) => statText.startsWith(alias));
  if (match) {
    target.sets[0] = match.option.value;
    statText = statText.slice(match.alias.length);
  } else {
    const parts = source.split(/\s*[·+＋]\s*/);
    const setOption = parts.length > 1 ? findRelicSetOption(gameKey, parts[0]) : null;
    if (setOption) {
      target.sets[0] = setOption.value;
      parts.shift();
      statText = parts.join("");
    } else {
      statText = source;
    }
  }

  const shorthand = {
    攻: "atk_pct",
    生: "hp_pct",
    防: "def_pct",
    精: "em",
    充: "er",
    火: "pyro_dmg",
    冰: "cryo_dmg",
    水: "hydro_dmg",
    雷: "electro_dmg",
    风: "anemo_dmg",
    岩: "geo_dmg",
    草: "dendro_dmg",
    物: "physical_dmg",
    暴: "crit_rate",
    爆: "crit_dmg",
    治: "healing",
  };
  const parsedStats = [...statText.replace(/[·+＋\s]/g, "")].map((char) => shorthand[char]).filter(Boolean);
  variableRelicSlots(gameKey).forEach((slot, index) => {
    if (parsedStats[index]) target.mainStats[slot.key] = normalizeStatValue(slot, parsedStats[index]);
  });
  return target;
}

function parseLegacyRelicSets(gameKey, raw) {
  const config = relicConfig(gameKey);
  if (!config || !raw || String(raw).trim() === "未配置") return [];
  const source = splitLegacyTarget(raw);
  if (gameKey === "starrail") {
    return source.split(/\s*[+＋·]\s*/).map((value) => value.trim()).filter(Boolean).slice(0, config.setFields.length).map((value) => normalizeRelicSetValue(gameKey, value));
  }
  const compact = source.replace(/\s+/g, "");
  const candidates = relicSetOptions(gameKey)
    .flatMap((option) => (option.aliases || [option.value]).map((alias) => ({ option, alias: String(alias).replace(/\s+/g, "") })))
    .sort((a, b) => b.alias.length - a.alias.length);
  const match = candidates.find(({ alias }) => compact.startsWith(alias));
  if (match) return [match.option.value];
  const firstPart = source.split(/\s*[·+＋]\s*/)[0].trim();
  const option = findRelicSetOption(gameKey, firstPart);
  return option ? [option.value] : [];
}

function isLegacyStatOnlyRelic(gameKey, raw) {
  if (gameKey !== "genshin" || raw?.customName) return false;
  const source = typeof raw === "string" ? raw : raw?.set || raw?.name || "";
  if (!source || String(source).trim() === "未配置") return false;
  const parsed = parseLegacyBuildTarget(gameKey, source);
  return !parsed.sets.some(Boolean) && Object.keys(parsed.mainStats).length > 0;
}

function normalizeBuildTarget(gameKey, role) {
  const config = relicConfig(gameKey);
  if (!config || !role) return null;
  const hasTarget = role.buildTarget && typeof role.buildTarget === "object";
  const target = hasTarget ? defaultBuildTarget(gameKey) : parseLegacyBuildTarget(gameKey, role.gear?.set);
  const raw = hasTarget ? role.buildTarget : null;
  if (hasTarget) {
    const rawSets = Array.isArray(raw.sets)
      ? raw.sets
      : raw.set != null
        ? String(raw.set).split(/\s*[+＋·]\s*/).filter(Boolean)
        : [raw.set4, raw.set2].filter(Boolean);
    target.sets = config.setFields.map((field, index) => normalizeRelicSetValue(gameKey, rawSets[index] || ""));
    const rawStats = raw.mainStats || raw.main_stats || {};
    variableRelicSlots(gameKey).forEach((slot) => {
      const value = normalizeStatValue(slot, rawStats[slot.key]);
      if (value) target.mainStats[slot.key] = value;
    });
    target.substats = String(raw.substats ?? raw.subStats ?? "");
    if (isLegacyStatOnlyRelic(gameKey, raw) || isLegacyStatOnlyRelic(gameKey, role.gear)) {
      const repaired = parseLegacyBuildTarget(gameKey, role.gear?.set || raw.set);
      target.sets = repaired.sets;
      target.mainStats = { ...repaired.mainStats, ...target.mainStats };
      if (!target.substats) target.substats = String(role.note || "");
    }
  } else {
    target.substats = String(role.note || "");
    role.note = "";
  }
  target.set = target.sets.filter(Boolean).join(" + ");
  role.buildTarget = target;
  return target;
}

function roleBuildTarget(role, gameKey = state.activeGame) {
  return relicConfig(gameKey) ? (role?.buildTarget || normalizeBuildTarget(gameKey, role)) : null;
}

function buildTargetSummary(role, gameKey = state.activeGame) {
  const config = relicConfig(gameKey);
  const target = roleBuildTarget(role, gameKey);
  if (!config || !target) return "";
  const sets = target.sets.filter(Boolean).map((value) => relicSetLabel(gameKey, value)).filter(Boolean);
  const stats = variableRelicSlots(gameKey).map((slot) => statShortForSlot(slot, target.mainStats?.[slot.key])).filter(Boolean).join("");
  return [sets.join(" + "), stats].filter(Boolean).join(" · ");
}

function mainStatsSummary(gameKey, values, includeFixed = false) {
  return relicSlots(gameKey)
    .filter((slot) => includeFixed || !slot.fixed)
    .map((slot) => {
      const value = slot.fixed?.value || values?.[slot.key];
      if (!value) return "";
      return `${slot.label} ${statLabelForSlot(slot, value)}`;
    })
    .filter(Boolean)
    .join(" · ");
}

function unique(values) {
  return [...new Set(values.filter(Boolean))];
}

function wikiFileUrl(gameKey, fileName) {
  if (!fileName || !wikiBases[gameKey]) return "";
  return `${wikiBases[gameKey]}${encodeURIComponent(String(fileName).replace(/^文件:/, ""))}`;
}

function wikiAssetUrl(gameKey, fileName) {
  const raw = String(fileName || "").replace(/^文件:/, "");
  const safe = `${gameKey}--${raw}`.replace(/[\\/:*?"<>|]/g, "_");
  return `assets/wiki/${encodeURIComponent(safe)}`;
}

function wikiImageUrl(gameKey, fileName) {
  const raw = String(fileName || "").replace(/^文件:/, "");
  return wikiUnavailable.has(`${gameKey}/${raw}`) ? "" : wikiAssetUrl(gameKey, raw);
}

function isSampleRole(role) {
  return Object.values(sampleData.games || {}).some((game) => game.characters?.some((entry) => entry.id === role?.id && entry.name === role?.name));
}

function roleIconCandidates(gameKey, role) {
  const name = String(role?.name || "").trim();
  const dotVariant = name.replaceAll("·", "•");
  let names;
  if (gameKey === "genshin") names = [`无背景-角色-${name}.png`];
  else if (gameKey === "starrail") names = [`${name === "阮·梅" ? dotVariant : name}竖版头像.png`];
  else if (gameKey === "endfield") names = [`${name}头像.png`];
  else names = [`角色 ${name} 头像.png`];
  const urlForFile = isSampleRole(role) ? wikiImageUrl : wikiFileUrl;
  return unique(names.map((fileName) => urlForFile(gameKey, fileName)));
}

function inventoryIconCandidates(gameKey, item) {
  const name = String(item?.name || "").trim();
  const pieces = name.split(/\s*[·+＋]\s*/).filter(Boolean);
  const names = [];
  const remoteNames = [];
  const explicitUrls = item?.wikiFile ? [wikiFileUrl(gameKey, item.wikiFile)] : [];
  if (!item?.wikiFile && item?.iconCached !== false && item?.kind === "weapon") {
    if (gameKey === "endfield") names.push(`${name}图标.png`);
    else if (gameKey !== "wuthering" && !/[+＋]/.test(name)) names.push(`${name}.png`);
  }
  if (!item?.wikiFile && item?.iconCached !== false && item?.kind === "relic") {
    const candidates = relicPieceIconCandidates(gameKey, item);
    return unique([...explicitUrls, ...candidates]);
  }
  if (!item?.wikiFile && item?.iconCached !== false && item?.kind === "equipment" && gameKey === "endfield" && name === "熔铸火焰") names.push("熔铸火焰图标.png");
  if (!item?.wikiFile && item?.iconCached !== false && item?.kind === "echo" && gameKey === "wuthering" && name === "戍关长刃·定军") names.push("Mc wiki weapon 戍关长刃·定军.png");
  return unique([...explicitUrls, ...names.map((fileName) => wikiImageUrl(gameKey, fileName)), ...remoteNames.map((fileName) => wikiFileUrl(gameKey, fileName))]);
}

function iconMarkup(candidates, alt, fallback, className = "") {
  const urls = unique(candidates);
  const image = urls.length
    ? `<img src="${escapeHtml(urls[0])}" data-icon-fallbacks="${escapeHtml(urls.slice(1).join("|"))}" alt="${escapeHtml(alt)}" loading="lazy" />`
    : "";
  return `<span class="icon-frame ${className}">${image}<span class="icon-fallback">${escapeHtml(fallback)}</span></span>`;
}

function roleAvatar(role, className = "avatar") {
  const avatarRatio = state.activeGame === "starrail" ? "160 / 188" : "1 / 1";
  return `<div class="${className}" style="--avatar-bg:${safeColor(role?.color)};--avatar-ratio:${avatarRatio}">${iconMarkup(roleIconCandidates(state.activeGame, role), role?.name || "角色", roleInitial(role?.name), "avatar-icon")}</div>`;
}

function inventoryIcon(item, className = "inventory-icon") {
  return iconMarkup(inventoryIconCandidates(state.activeGame, item), item?.name || "装备", roleInitial(item?.name), className);
}

function relicSetIconCandidates(gameKey, value) {
  const option = findRelicSetOption(gameKey, value);
  const fileName = option?.icon || wikiRelicFiles[gameKey]?.[option?.value || value];
  if (!fileName) return [];
  return unique([wikiImageUrl(gameKey, fileName), wikiFileUrl(gameKey, fileName)]);
}

function relicSetIconMarkup(gameKey, value, className = "set-picker-icon") {
  const label = relicSetLabel(gameKey, value) || "套装";
  return iconMarkup(relicSetIconCandidates(gameKey, value), label, roleInitial(label), className);
}

function relicSlotIconMarkup(gameKey, slotKey, setValue = "", className = "set-picker-icon") {
  const label = relicSlotLabel(gameKey, slotKey) || "部位";
  const candidates = relicPieceIconCandidates(gameKey, { slot: slotKey, sets: [setValue] });
  return iconMarkup(candidates, label, roleInitial(label), className);
}

function bindIconFallbacks(root = document) {
  $$('img[data-icon-fallbacks]', root).forEach((image) => {
    if (image.dataset.iconBound === "true") return;
    image.dataset.iconBound = "true";
    const frame = image.parentElement;
    const markLoaded = () => frame?.classList.add("has-image");
    image.addEventListener("load", markLoaded, { once: true });
    image.addEventListener("error", () => {
      const candidates = String(image.dataset.iconFallbacks || "").split("|").filter(Boolean);
      if (candidates.length) {
        image.dataset.iconFallbacks = candidates.slice(1).join("|");
        image.src = candidates[0];
      } else {
        frame?.classList.add("is-fallback");
        image.remove();
      }
    });
    const checkLoaded = () => {
      if (image.complete && image.naturalWidth > 0) markLoaded();
    };
    checkLoaded();
    window.setTimeout(checkLoaded, 0);
    window.setTimeout(checkLoaded, 500);
  });
}

function inventoryTypeForGame(gameKey = state.activeGame) {
  return inventoryMeta[gameKey] || inventoryMeta.genshin;
}

function inventoryTypeLabel(kind, gameKey = state.activeGame) {
  return inventoryTypeForGame(gameKey).find((entry) => entry.key === kind)?.label || "装备";
}

function equipmentKindForGame(gameKey = state.activeGame) {
  return gameKey === "wuthering" ? "echo" : gameKey === "endfield" ? "equipment" : "relic";
}

function inventoryForGame(gameKey = state.activeGame) {
  const game = state.data?.games?.[gameKey];
  if (!game) return { items: [] };
  if (!game.inventory || !Array.isArray(game.inventory.items)) game.inventory = { items: [] };
  return game.inventory;
}

function inventoryItems(gameKey = state.activeGame) {
  return inventoryForGame(gameKey).items;
}

function roleGearIds(role) {
  if (!role) return {};
  if (!role.gearIds || typeof role.gearIds !== "object" || Array.isArray(role.gearIds)) role.gearIds = {};
  return role.gearIds;
}

function roleEquipmentItem(role, kind, slotKey = "") {
  if (!role) return null;
  const field = kind === "weapon" ? "weaponId" : slotKey ? roleGearIds(role)[slotKey] : role.gearId;
  const items = inventoryItems();
  const byId = field && items.find((item) => item.id === field && item.kind === kind && (!slotKey || item.slot === slotKey));
  if (byId) return byId;
  const name = kind === "weapon" ? role.weapon?.name : role.gear?.set;
  if (!name || name === "未配置") return null;
  if (kind === "relic") {
    const targetSets = role.gear?.sets || parseLegacyBuildTarget(state.activeGame, name).sets;
    return items.find((item) => item.kind === kind
      && (!slotKey || item.slot === slotKey)
      && (item.name === name
        || (targetSets.length && targetSets.every((set, index) => !set || item.sets?.[index] === set)))) || null;
  }
  return items.find((item) => item.kind === kind && item.name === name) || null;
}

function normalizeRelicItem(gameKey, raw, fallbackName = "") {
  const config = relicConfig(gameKey);
  const sourceName = String(raw?.name || fallbackName || "").trim();
  const legacySets = parseLegacyRelicSets(gameKey, raw?.set ?? sourceName);
  const explicitSets = Array.isArray(raw?.sets)
    ? raw.sets
    : raw?.set != null
      ? legacySets.length || raw?.customName || raw?.mainStats || raw?.main_stats
        ? (legacySets.length ? legacySets : String(raw.set).split(/\s*[+＋·]\s*/).filter(Boolean))
        : []
      : [];
  const sourceSets = explicitSets.length ? explicitSets : legacySets;
  const sets = config.setFields.map((field, index) => normalizeRelicSetValue(gameKey, sourceSets[index] || ""));
  const sourceStats = raw?.mainStats || raw?.main_stats || {};
  const mainStats = {};
  variableRelicSlots(gameKey).forEach((slot) => {
    const value = normalizeStatValue(slot, sourceStats[slot.key]);
    if (value) mainStats[slot.key] = value;
  });
  const slot = normalizeRelicSlot(gameKey, raw?.slot ?? raw?.part ?? raw?.position) || inferRelicSlotFromName(gameKey, sourceName);
  const defaultName = slot
    ? relicItemDefaultName(gameKey, sets, slot)
    : sets.filter(Boolean).map((value) => relicSetLabel(gameKey, value)).join(" + ");
  const name = raw?.customName ? sourceName : (defaultName || sourceName || "未命名遗器");
  return {
    ...raw,
    name,
    slot,
    sets,
    set: sets.filter(Boolean).join(" + "),
    mainStats,
    substats: String(raw?.substats ?? raw?.subStats ?? ""),
    customName: Boolean(raw?.customName),
  };
}

function ensureInventory(data) {
  if (!data?.games) return data;
  Object.entries(gameMeta).forEach(([gameKey]) => {
    const game = data.games[gameKey];
    if (!game) return;
    game.characters = Array.isArray(game.characters) ? game.characters : [];
    game.teams = Array.isArray(game.teams) ? game.teams : [];
    const rawExisting = Array.isArray(game.inventory?.items) ? game.inventory.items : [];
    const existing = [];
    rawExisting.forEach((raw, index) => {
      if (!raw) return;
      if (isRelicKind(gameKey, raw.kind) && !normalizeRelicSlot(gameKey, raw.slot ?? raw.part ?? raw.position)) {
        const normalized = normalizeRelicItem(gameKey, raw);
        const sourceId = raw.id || `${gameKey}-inventory-${index}`;
        if (!isLegacyStatOnlyRelic(gameKey, raw) && normalized.sets.some(Boolean)) {
          relicSlots(gameKey).forEach((slot, slotIndex) => {
            const sourceStats = raw.mainStats || raw.main_stats || {};
            const mainStats = {};
            if (!slot.fixed && sourceStats[slot.key]) mainStats[slot.key] = sourceStats[slot.key];
            existing.push({
              ...raw,
              id: `${sourceId}-${slot.key}`,
              aggregateId: sourceId,
              name: relicItemDefaultName(gameKey, normalized.sets, slot.key) || `${normalized.name}·${slot.label}`,
              customName: false,
              wikiFile: "",
              slot: slot.key,
              sets: normalized.sets,
              set: normalized.set,
              mainStats,
              score: Array.isArray(raw.pieces) && raw.pieces[slotIndex] != null ? raw.pieces[slotIndex] : null,
              aggregateScore: raw.score == null ? null : Number(raw.score),
              pieces: [],
              holderId: raw.holderId || null,
            });
          });
          return;
        }
      }
      existing.push(raw);
    });
    const items = [];
    const usedIds = new Set();
    const addItem = (raw, fallbackId, holderId = null) => {
      const item = {
        ...raw,
        id: raw?.id && !usedIds.has(raw.id) ? raw.id : fallbackId,
        kind: raw?.kind || "weapon",
        name: String(raw?.name || "未命名装备"),
        rarity: Number(raw?.rarity) || 4,
        level: raw?.level == null ? null : Number(raw.level),
        maxLevel: Number(raw?.maxLevel) || gameMeta[gameKey].levelMax,
        refinement: Number(raw?.refinement) || 1,
        score: raw?.score == null ? null : Number(raw.score),
        pieces: Array.isArray(raw?.pieces) ? raw.pieces : [],
        holderId: holderId || raw?.holderId || null,
        locked: Boolean(raw?.locked),
        iconCached: raw?.iconCached == null ? Boolean(holderId || raw?.holderId) : Boolean(raw.iconCached),
        note: String(raw?.note || ""),
      };
      if (isRelicKind(gameKey, item.kind)) {
        Object.assign(item, normalizeRelicItem(gameKey, item));
      }
      if (usedIds.has(item.id)) item.id = `${fallbackId}-${items.length}`;
      usedIds.add(item.id);
      items.push(item);
      return item;
    };
    const findAvailable = (role, kind, name, sets = [], slotKey = "") => {
      const requestedId = kind === "weapon" ? role.weaponId : slotKey ? roleGearIds(role)[slotKey] : role.gearId;
      const isMatchingSlot = (item) => kind !== "relic" || !slotKey || item.slot === slotKey;
      const isAvailable = (item) => item.kind === kind && isMatchingSlot(item) && (!item.holderId || item.holderId === role.id);
      return (requestedId && existing.find((item) => isAvailable(item) && (item.id === requestedId || item.aggregateId === requestedId)))
        || existing.find((item) => isAvailable(item) && (
          item.name === name
          || (kind === "relic" && sets.length && sets.every((set, index) => !set || item.sets?.[index] === set))
        ));
    };
    game.characters.forEach((role) => {
      normalizeBuildTarget(gameKey, role);
      const weaponName = role.weapon?.name;
      if (weaponName && weaponName !== "未配置") {
        const source = findAvailable(role, "weapon", weaponName) || {
          id: `${gameKey}-weapon-${role.id}`,
          kind: "weapon",
          name: weaponName,
          rarity: role.weapon.rarity,
          level: role.weapon.level,
          refinement: role.weapon.refinement,
          iconCached: true,
        };
        const item = addItem(source, `${gameKey}-weapon-${role.id}`, role.id);
        role.weaponId = item.id;
        role.weapon = { name: item.name, rarity: item.rarity, refinement: item.refinement, level: item.level || 0, maxLevel: item.maxLevel };
      } else {
        role.weaponId = null;
      }
      const gearKind = equipmentKindForGame(gameKey);
      const gearName = role.gear?.set;
      const legacyGear = isRelicKind(gameKey, gearKind) ? normalizeRelicItem(gameKey, role.gear || {}, gearName) : null;
      const gearSets = legacyGear?.sets || [];
      const canonicalGearName = gearSets.filter(Boolean).map((value) => relicSetLabel(gameKey, value)).join(" + ") || legacyGear?.name || gearName;
      const hasRelicSet = gearSets.some(Boolean);
      const hasExplicitRelicItem = Boolean(role.gearId || Object.values(roleGearIds(role)).some(Boolean) || role.gear?.customName || role.gear?.name || (Array.isArray(role.gear?.sets) && role.gear.sets.some(Boolean)));
      const legacyTargetOnly = isLegacyStatOnlyRelic(gameKey, role.gear);
      const canCreateRelicItem = !isRelicKind(gameKey, gearKind) || (!legacyTargetOnly && (hasRelicSet || hasExplicitRelicItem));
      const legacyScore = role.gear?.score ?? null;
      const legacyPieces = Array.isArray(role.gear?.pieces) ? role.gear.pieces : [];
      if (canonicalGearName && canonicalGearName !== "未配置" && canCreateRelicItem) {
        if (isRelicKind(gameKey, gearKind)) {
          const assigned = {};
          relicSlots(gameKey).forEach((slot, slotIndex) => {
            const source = findAvailable(role, gearKind, canonicalGearName, gearSets, slot.key) || {
              id: `${gameKey}-${gearKind}-${role.id}-${slot.key}`,
              kind: gearKind,
              name: relicItemDefaultName(gameKey, gearSets, slot.key) || `${canonicalGearName}·${slot.label}`,
              rarity: 5,
              score: legacyPieces[slotIndex] ?? null,
              pieces: [],
              sets: gearSets,
              set: gearSets.join(" + "),
              slot: slot.key,
              mainStats: {},
              substats: legacyGear?.substats || "",
              iconCached: true,
            };
            source.kind = gearKind;
            source.slot = slot.key;
            source.aggregateId = source.aggregateId || role.gearId || `${gameKey}-${gearKind}-${role.id}`;
            const sourceStats = source.mainStats || {};
            source.mainStats = {};
            if (!slot.fixed && (sourceStats[slot.key] || legacyGear?.mainStats?.[slot.key])) {
              source.mainStats[slot.key] = sourceStats[slot.key] || legacyGear.mainStats[slot.key];
            }
            source.substats = source.substats || legacyGear?.substats || "";
            source.score = source.score ?? legacyPieces[slotIndex] ?? null;
            const item = addItem(source, `${gameKey}-${gearKind}-${role.id}-${slot.key}`, role.id);
            item.holderId = role.id;
            assigned[slot.key] = item.id;
          });
          role.gearIds = assigned;
          role.gearId = assigned[relicSlots(gameKey)[0]?.key] || null;
          role.gear = {
            ...(role.gear || {}),
            set: gearSets.filter(Boolean).join(" + ") || canonicalGearName,
            sets: gearSets,
            score: legacyScore,
            pieces: legacyPieces,
            mainStats: legacyGear?.mainStats || role.gear?.mainStats || {},
            substats: legacyGear?.substats || role.gear?.substats || "",
          };
        } else {
          const source = findAvailable(role, gearKind, canonicalGearName) || {
            id: `${gameKey}-${gearKind}-${role.id}`,
            kind: gearKind,
            name: canonicalGearName,
            rarity: 5,
            score: role.gear?.score,
            pieces: role.gear?.pieces,
            iconCached: true,
          };
          source.kind = gearKind;
          const item = addItem(source, `${gameKey}-${gearKind}-${role.id}`, role.id);
          role.gearId = item.id;
          role.gear = {
            set: item.name,
            score: item.score,
            pieces: item.pieces,
          };
        }
      } else {
        role.gearId = null;
        role.gearIds = {};
        role.gear = {
          ...(role.gear || {}),
          set: "未配置",
          sets: [],
          score: legacyScore,
          pieces: legacyPieces,
          mainStats: role.gear?.mainStats || {},
          substats: role.gear?.substats || "",
        };
      }
    });
    existing.forEach((item, index) => {
      if (!item?.id || usedIds.has(item.id)) return;
      if (isLegacyStatOnlyRelic(gameKey, item)) return;
      addItem(item, `${gameKey}-inventory-${index}`);
    });
    game.inventory = { ...(game.inventory || {}), items };
  });
  data.version = Math.max(Number(data.version) || 1, DATA_VERSION);
  return data;
}

function saveData(message = "自动保存已开启") {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state.data));
    $("#syncStatus").textContent = "本地数据 · 已同步";
    $("#lastSaved").textContent = message;
    $("#heroUpdatedAt").textContent = `最后更新 · ${new Intl.DateTimeFormat("zh-CN", { hour: "2-digit", minute: "2-digit" }).format(new Date())}`;
  } catch {
    $("#syncStatus").textContent = "本地数据 · 仅当前会话";
  }
}

function loadData() {
  let parsed = null;
  try {
    parsed = JSON.parse(localStorage.getItem(STORAGE_KEY) || "null");
  } catch {
    parsed = null;
  }
  if (parsed?.games?.genshin && parsed?.games?.starrail) {
    state.data = parsed;
  } else {
    state.data = clone(sampleData);
  }
  ensureInventory(state.data);
  state.activeGame = state.data.activeGame && state.data.games[state.data.activeGame] ? state.data.activeGame : "genshin";
  state.selectedRoleId = null;
  state.selectedTeamId = state.data.games[state.activeGame]?.teams?.[0]?.id || null;
}

function renderGameSwitcher() {
  const container = $("#gameSwitcher");
  container.innerHTML = Object.entries(gameMeta).map(([key, game]) => {
    const count = state.data.games[key]?.characters?.length || 0;
    const active = key === state.activeGame;
    return `<div class="game-tab-wrap" style="--tab-accent:${safeColor(game.accent)}">
      <button class="game-tab ${active ? "is-active" : ""}" type="button" role="tab" aria-selected="${active}" data-game="${key}">
        <span class="game-tab-art" style="background-image:url('${game.cover}')"></span>
        <span class="game-tab-copy"><span class="game-tab-name">${escapeHtml(game.name)}</span><span class="game-tab-meta">${count} 位角色</span></span>
      </button>
    </div>`;
  }).join("");
}

function renderStats() {
  const roster = roles();
  const maxed = roster.filter((role) => roleStatus(role).key === "maxed").length;
  const fiveStars = roster.filter((role) => Number(role.rarity) >= 5).length;
  const readiness = Math.round(average(roster.map(roleReadiness)));
  const teamCount = teams().length;
  const inventoryCount = inventoryItems().length;
  const cards = [
    { label: "角色总数", value: roster.length, caption: `${meta().name} 档案`, accent: meta().accent },
    { label: "已毕业", value: maxed, caption: `占比 ${roster.length ? Math.round((maxed / roster.length) * 100) : 0}%`, accent: "#71c8b8" },
    { label: "五星角色", value: fiveStars, caption: `共 ${roster.length} 位角色`, accent: "#d8ad6b" },
    { label: "平均完成度", value: `${readiness}%`, caption: `${teamCount} 组配队 · ${inventoryCount} 件装备`, accent: "#aaa4ef" },
  ];
  $("#statsGrid").innerHTML = cards.map((card) => `<article class="stat-card" style="--stat-accent:${safeColor(card.accent)}">
    <span class="stat-label">${escapeHtml(card.label)}</span>
    <strong class="stat-value">${escapeHtml(card.value)}</strong>
    <span class="stat-caption">${escapeHtml(card.caption)}</span>
  </article>`).join("");
  $("#navCharacterCount").textContent = roster.length;
  $("#navTeamCount").textContent = teamCount;
  $("#navInventoryCount").textContent = inventoryCount;
}

function renderFilterOptions() {
  const select = $("#rarityFilter");
  const rarities = [...new Set(roles().map((role) => Number(role.rarity)).filter(Number.isFinite))].sort((a, b) => b - a);
  const current = state.rarity;
  select.innerHTML = `<option value="all">全部星级</option>${rarities.map((rarity) => `<option value="${rarity}">${rarity} 星</option>`).join("")}`;
  select.value = rarities.some((rarity) => String(rarity) === current) ? current : "all";
  state.rarity = select.value;
}

function filteredRoles() {
  const query = state.query.trim().toLowerCase();
  let result = roles().filter((role) => {
    const target = roleBuildTarget(role, state.activeGame);
    const searchable = [
      role.name,
      role.weapon?.name,
      role.gear?.set,
      target?.sets?.map((value) => relicSetLabel(state.activeGame, value)).join(" "),
      buildTargetSummary(role, state.activeGame),
      target?.substats,
      role.note,
      role.role,
      role.element,
    ].filter(Boolean).join(" ").toLowerCase();
    const queryMatch = !query || searchable.includes(query);
    const rarityMatch = state.rarity === "all" || String(role.rarity) === state.rarity;
    const statusMatch = state.status === "all" || roleStatus(role).key === state.status;
    return queryMatch && rarityMatch && statusMatch;
  });
  result.sort((a, b) => {
    if (state.sort === "name") return a.name.localeCompare(b.name, "zh-CN");
    if (state.sort === "score") return (Number(b.gear?.score) || 0) - (Number(a.gear?.score) || 0);
    return roleReadiness(b) - roleReadiness(a);
  });
  return result;
}

function roleCard(role) {
  const status = roleStatus(role);
  const readiness = roleReadiness(role);
  const game = meta();
  const talentText = (role.talents || []).slice(0, 4).map((talent) => `<span class="talent-chip">${escapeHtml(talent)}</span>`).join("");
  const duplicate = Number(role.duplicate) || 0;
  const roleSubline = [role.element, role.role, getEquipmentName(role)].filter(Boolean).join(" · ");
  const targetSummary = buildTargetSummary(role, state.activeGame);
  return `<article class="roster-item ${state.layout === "compact" ? "compact" : ""} ${state.selectedRoleId === role.id ? "is-selected" : ""}" data-role-id="${escapeHtml(role.id)}" tabindex="0" aria-label="查看 ${escapeHtml(role.name)}">
    ${roleAvatar(role)}
    <div class="role-main">
      <div class="role-name-row"><span class="role-name">${escapeHtml(role.name)}</span><span class="rarity-stars" aria-label="${role.rarity} 星">${stars(role.rarity)}</span></div>
      <span class="role-subline">${escapeHtml(roleSubline)}</span>
      ${targetSummary ? `<span class="role-target-line"><span>目标</span>${escapeHtml(targetSummary)}</span>` : ""}
    </div>
    <div class="level-block">
      <div class="metric-line"><span>等级</span><strong>${escapeHtml(formatLevel(role))}</strong></div>
      <div class="progress-track"><div class="progress-fill" style="--bar-color:${safeColor(game.accent)};width:${Math.round(((Number(role.level) || 0) / (Number(role.maxLevel) || game.levelMax)) * 100)}%"></div></div>
    </div>
    <div class="gear-block">
      <div class="metric-line"><span>${escapeHtml(game.duplicateLabel)}</span><strong>${duplicate > 0 ? `+${duplicate}` : "未解锁"}</strong></div>
      <div class="talent-row">${talentText || `<span class="talent-chip">待补</span>`}</div>
    </div>
    <div class="score-block"><span class="score-value" style="--score-color:${readinessColor(readiness)}">${displayScore(role)}</span><span class="status-label">${escapeHtml(status.label)}</span></div>
  </article>`;
}

function renderRoster() {
  const result = filteredRoles();
  if (!result.length) {
    state.selectedRoleId = null;
  } else if (!result.some((role) => role.id === state.selectedRoleId)) {
    state.selectedRoleId = result[0].id;
  }
  $("#rosterResultCount").textContent = `显示 ${result.length} 位角色`;
  $("#rosterList").innerHTML = result.map(roleCard).join("");
  $("#rosterEmpty").hidden = result.length > 0;
  bindIconFallbacks($("#rosterList"));
}

function detailStat(label, value, suffix = "") {
  return `<div class="detail-stat"><span>${escapeHtml(label)}</span><strong>${escapeHtml(value)}${suffix ? `<em>${escapeHtml(suffix)}</em>` : ""}</strong></div>`;
}

function equipmentSlots() {
  const game = meta();
  if (state.activeGame === "wuthering") return [{ kind: "echo", field: "gearId", label: game.equipmentLabel, symbol: "◌" }];
  const relicKind = equipmentKindForGame();
  const relicSlotsForGame = isRelicKind(state.activeGame, relicKind)
    ? relicSlots(state.activeGame).map((relicSlot) => ({
      kind: relicKind,
      field: `gearIds.${relicSlot.key}`,
      slotKey: relicSlot.key,
      label: relicSlot.label,
      symbol: "✦",
    }))
    : [{ kind: relicKind, field: "gearId", label: game.gearLabel, symbol: "✦" }];
  return [{ kind: "weapon", field: "weaponId", label: game.equipmentLabel, symbol: "◇" }, ...relicSlotsForGame];
}

function equipmentSelectMarkup(role, slot) {
  const current = roleEquipmentItem(role, slot.kind, slot.slotKey);
  const selectedId = slot.slotKey ? roleGearIds(role)[slot.slotKey] || current?.id || "" : role[slot.field] || current?.id || "";
  const options = inventoryItems().filter((item) => item.kind === slot.kind && (!slot.slotKey || item.slot === slot.slotKey)).map((item) => {
    const holder = item.holderId && item.holderId !== role.id ? findRole(item.holderId) : null;
    const suffix = holder ? ` · ${holder.name}` : "";
    const lockSuffix = item.locked ? " · 已锁定" : "";
    const relicSuffix = isRelicKind(state.activeGame, item.kind)
      ? ` · ${relicItemStatSummary(state.activeGame, item)}${item.score == null ? "" : ` · ${Number(item.score).toFixed(1).replace(".0", "")}分`}`
      : "";
    const disabled = item.locked && item.id !== selectedId ? "disabled" : "";
    return `<option value="${escapeHtml(item.id)}" ${item.id === selectedId ? "selected" : ""} ${disabled}>${escapeHtml(item.name)}${escapeHtml(relicSuffix)}${escapeHtml(suffix)}${escapeHtml(lockSuffix)}</option>`;
  }).join("");
  return `<label class="equipment-select-row"><span>${escapeHtml(slot.label)}</span><select data-equipment-field="${escapeHtml(slot.field)}" data-equipment-kind="${escapeHtml(slot.kind)}" ${slot.slotKey ? `data-equipment-slot="${escapeHtml(slot.slotKey)}"` : ""}><option value="">未选择</option>${options}</select></label>`;
}

function equipmentLineMarkup(role, slot) {
  const item = roleEquipmentItem(role, slot.kind, slot.slotKey);
  const fallback = item ? roleInitial(item.name) : slot.symbol;
  const icon = item ? inventoryIcon(item, "equipment-icon") : iconMarkup([], slot.label, fallback, "equipment-icon");
  let detail = "未配置";
  if (item && slot.kind === "weapon") detail = `${slot.label} · ${item.rarity || "—"} 星 · 精炼 ${item.refinement || "—"} · ${item.level || "—"}/${item.maxLevel || meta().levelMax}`;
  else if (item && isRelicKind(state.activeGame, item.kind)) detail = `${slot.label} · ${relicItemStatSummary(state.activeGame, item)} · ${item.score == null ? "未评分" : `${Number(item.score).toFixed(1).replace(".0", "")} 分`}`;
  else if (item) detail = `${slot.label} · ${item.score == null ? "未评分" : `${Number(item.score).toFixed(1).replace(".0", "")} 分`}`;
  return `<div class="equipment-line">${icon}<span class="equipment-copy"><strong>${escapeHtml(item?.name || "未配置")}</strong><span>${escapeHtml(detail)}</span></span></div>`;
}

function relicSetOptionsMarkup(gameKey, selected = "") {
  return `<option value="">未选择</option>${relicSetOptions(gameKey).map((option) => `<option value="${escapeHtml(option.value)}" ${option.value === selected ? "selected" : ""}>${escapeHtml(option.label)}</option>`).join("")}`;
}

function targetSetSelectMarkup(role, gameKey, field, index) {
  const target = roleBuildTarget(role, gameKey) || defaultBuildTarget(gameKey);
  const value = target.sets?.[index] || "";
  return `<label class="target-field"><span class="target-field-heading"><span>${escapeHtml(field.label)}</span>${relicSetIconMarkup(gameKey, value)}</span><select data-target-set="${index}">${relicSetOptionsMarkup(gameKey, value)}</select></label>`;
}

function targetStatSelectMarkup(role, gameKey, slot) {
  const target = roleBuildTarget(role, gameKey) || defaultBuildTarget(gameKey);
  const value = target.mainStats?.[slot.key] || "";
  const options = statOptionsForSlot(slot);
  return `<label class="target-field"><span>${escapeHtml(slot.label)}</span><select data-target-stat="${escapeHtml(slot.key)}"><option value="">未选择</option>${options.map((option) => `<option value="${escapeHtml(option.value)}" ${option.value === value ? "selected" : ""}>${escapeHtml(option.label)}</option>`).join("")}</select></label>`;
}

function buildTargetMarkup(role, gameKey = state.activeGame) {
  const config = relicConfig(gameKey);
  if (!config) return "";
  const target = roleBuildTarget(role, gameKey) || defaultBuildTarget(gameKey);
  const setFields = config.setFields.map((field, index) => targetSetSelectMarkup(role, gameKey, field, index)).join("");
  const fixed = config.slots.filter((slot) => slot.fixed).map((slot) => `<div class="target-fixed-stat"><span>${escapeHtml(slot.label)}</span><strong>固定 · ${escapeHtml(slot.fixed.label)}</strong></div>`).join("");
  const variable = config.slots.filter((slot) => !slot.fixed).map((slot) => targetStatSelectMarkup(role, gameKey, slot)).join("");
  const summary = buildTargetSummary(role, gameKey) || "尚未设置";
  return `<div class="detail-section build-target-section"><div class="detail-section-title"><span>养成目标</span><span>${escapeHtml(summary)}</span></div>
    <p class="target-hint">套装与主词条属于角色目标；当前仓库装备只记录实际持有的条目。</p>
    <div class="target-set-grid">${setFields}</div>
    <div class="target-stat-heading"><span>主词条</span><span>固定部位无需选择</span></div>
    <div class="target-fixed-grid">${fixed}</div>
    <div class="target-stat-grid">${variable}</div>
    <label class="target-substats-field"><span>副词条（手填）</span><textarea class="note-box" id="roleTargetSubstats" rows="2" maxlength="180" placeholder="例如：暴击率 70%+，暴击伤害 200%+，元素充能 140%">${escapeHtml(target.substats || "")}</textarea></label>
  </div>`;
}

function saveRoleBuildTarget(role, gameKey = state.activeGame, message = "养成目标已保存") {
  const target = roleBuildTarget(role, gameKey);
  if (!target) return;
  target.set = target.sets.filter(Boolean).join(" + ");
  saveData(message);
  renderStats();
  renderRoster();
  renderDetail();
  renderTeams();
}

function mainStatsCompact(gameKey, values) {
  return variableRelicSlots(gameKey).map((slot) => statShortForSlot(slot, values?.[slot.key])).filter(Boolean).join("") || "主词条未设";
}

function relicItemStatSummary(gameKey, item) {
  const slot = findRelicSlot(gameKey, item?.slot);
  if (slot) {
    const value = slot.fixed?.value || item?.mainStats?.[slot.key];
    return value ? statLabelForSlot(slot, value) : "主词条未设";
  }
  return mainStatsSummary(gameKey, item?.mainStats, true) || "主词条未设";
}

function relicAttributesMarkup(gameKey, item) {
  if (!isRelicKind(gameKey, item?.kind)) return "";
  const sets = (item.sets || []).filter(Boolean).map((value) => relicSetLabel(gameKey, value)).join(" + ") || item.set || item.name;
  const mainStats = item.slot
    ? relicItemStatSummary(gameKey, item)
    : mainStatsSummary(gameKey, item.mainStats, true) || "尚未设置";
  return `<div class="inventory-attribute-block"><div class="inventory-attribute-row"><span>套装</span><strong>${escapeHtml(sets)}</strong></div><div class="inventory-attribute-row"><span>部位</span><strong>${escapeHtml(relicSlotLabel(gameKey, item.slot))}</strong></div><div class="inventory-attribute-row"><span>主词条</span><strong>${escapeHtml(mainStats)}</strong></div><div class="inventory-attribute-row"><span>副词条</span><strong>${escapeHtml(item.substats || "手动填写")}</strong></div></div>`;
}

function syncRoleGearSnapshot(role, gameKey = state.activeGame) {
  if (!role || !isRelicKind(gameKey, equipmentKindForGame(gameKey))) return;
  const kind = equipmentKindForGame(gameKey);
  const ids = roleGearIds(role);
  const config = relicConfig(gameKey);
  const previous = role.gear || {};
  const assigned = config.slots.map((slot) => ({ slot, item: inventoryItems(gameKey).find((entry) => entry.id === ids[slot.key] && entry.kind === kind) }));
  const sets = Array(config.setFields.length).fill("");
  const setSeen = Array(config.setFields.length).fill(false);
  assigned.forEach(({ item }) => {
    (item?.sets || []).forEach((value, index) => {
      if (value && !setSeen[index]) {
        sets[index] = value;
        setSeen[index] = true;
      }
    });
  });
  if (!assigned.some(({ item }) => Boolean(item))) {
    previous.sets?.forEach((value, index) => {
      if (index < sets.length) sets[index] = value || "";
    });
  } else {
    sets.forEach((value, index) => {
      if (!value) sets[index] = previous.sets?.[index] || "";
    });
  }
  const mainStats = {};
  const pieces = config.slots.map((slot, index) => {
    const item = assigned[index].item;
    if (item?.mainStats?.[slot.key]) mainStats[slot.key] = item.mainStats[slot.key];
    return item?.score ?? previous.pieces?.[index] ?? null;
  });
  const hasItems = assigned.some(({ item }) => Boolean(item));
  const substats = assigned.find(({ item }) => item?.substats)?.item?.substats || previous.substats || "";
  role.gear = {
    ...previous,
    set: hasItems ? sets.filter(Boolean).join(" + ") || "未配置" : "未配置",
    sets: hasItems ? sets : [],
    pieces: hasItems ? pieces : [],
    mainStats: hasItems ? mainStats : {},
    substats: hasItems ? substats : "",
    score: hasItems ? previous.score : null,
  };
  role.gearId = assigned.find(({ item }) => item)?.item?.id || null;
}

function clearRoleEquipment(role, kind, slotKey = "") {
  if (!role) return;
  if (kind === "weapon") {
    role.weaponId = null;
    role.weapon = null;
  } else if (isRelicKind(state.activeGame, kind)) {
    const ids = roleGearIds(role);
    const keys = slotKey ? [slotKey] : relicSlots(state.activeGame).map((slot) => slot.key);
    keys.forEach((key) => {
      const item = inventoryItems().find((entry) => entry.id === ids[key] && entry.kind === kind);
      if (item && item.holderId === role.id) item.holderId = null;
      delete ids[key];
    });
    syncRoleGearSnapshot(role);
  } else {
    role.gearId = null;
    role.gear = {
      ...(role.gear || {}),
      set: "未配置",
      sets: [],
      score: null,
      pieces: [],
      mainStats: {},
      substats: "",
    };
  }
}

function bindRoleEquipment(roleId, kind, itemId, slotKey = "") {
  const role = findRole(roleId);
  if (!role) return;
  const items = inventoryItems();
  const previous = roleEquipmentItem(role, kind, slotKey);
  const item = items.find((entry) => entry.id === itemId && entry.kind === kind && (!slotKey || entry.slot === slotKey)) || null;
  if (previous?.locked && previous.id !== item?.id) {
    showToast("当前装备已锁定，请先解锁");
    renderDetail();
    return;
  }
  if (item?.locked && item.holderId !== role.id) {
    showToast("目标装备已锁定，请先解锁");
    renderDetail();
    return;
  }
  if (previous && previous.id !== item?.id) previous.holderId = null;
  if (item) {
    const previousHolder = item.holderId && item.holderId !== role.id ? findRole(item.holderId) : null;
    if (previousHolder) clearRoleEquipment(previousHolder, kind, slotKey);
    item.holderId = role.id;
    if (kind === "weapon") {
      role.weaponId = item.id;
      role.weapon = { name: item.name, rarity: item.rarity, refinement: item.refinement, level: item.level || 0, maxLevel: item.maxLevel };
    } else if (isRelicKind(state.activeGame, kind)) {
      roleGearIds(role)[slotKey] = item.id;
      syncRoleGearSnapshot(role);
    } else {
      role.gearId = item.id;
      role.gear = {
        set: item.name,
        sets: item.sets || [],
        score: item.score,
        pieces: item.pieces || [],
        mainStats: item.mainStats || {},
        substats: item.substats || "",
      };
    }
  } else {
    clearRoleEquipment(role, kind, slotKey);
  }
  saveData("装备配置已保存");
  renderAll();
  showToast(item ? `${item.name} 已装备` : `${inventoryTypeLabel(kind)} 已卸下`);
}

function renderDetail() {
  const role = findRole(state.selectedRoleId);
  const detail = $("#detailContent");
  if (!role) {
    detail.innerHTML = `<div class="empty-team-detail"><span class="empty-icon">◈</span><strong>选择一个角色</strong></div>`;
    return;
  }
  state.selectedRoleId = role.id;
  const game = meta();
  const status = roleStatus(role);
  const readiness = roleReadiness(role);
  const duplicate = Number(role.duplicate) || 0;
  const talentMax = talentMaxForGame();
  const detailSubline = [stars(role.rarity), role.element, role.role].filter(Boolean).join(" · ");
  const talentRows = (role.talents || []).length
    ? role.talents.map((value, index) => `<div class="metric-line"><span>${escapeHtml(game.talentLabels[index] || `技能 ${index + 1}`)}</span><strong>${escapeHtml(value)}</strong></div><div class="progress-track"><div class="progress-fill" style="--bar-color:${safeColor(game.accent)};width:${Math.round((Number(value) / talentMax) * 100)}%"></div></div>`).join("")
    : `<div class="role-subline">技能数据尚未录入</div>`;
  detail.innerHTML = `<div class="detail-hero">
      ${roleAvatar(role)}
      <div class="detail-hero-copy"><h4 class="detail-name">${escapeHtml(role.name)}</h4><div class="detail-subline">${escapeHtml(detailSubline)}</div><div class="detail-status" style="--status-color:${status.color}">${escapeHtml(status.label)} · ${readiness}%</div></div>
      <div class="detail-hero-actions"><button class="icon-button subtle" type="button" data-edit-role="${escapeHtml(role.id)}" title="编辑角色详情" aria-label="编辑角色详情">✎</button></div>
    </div>
    <div class="detail-section"><div class="detail-section-title"><span>核心指标</span><span>${escapeHtml(game.name)}</span></div><div class="detail-stats">
      ${detailStat("等级", role.level || 0, ` / ${role.maxLevel || game.levelMax}`)}
      ${detailStat(game.duplicateLabel, duplicate ? `+${duplicate}` : "未解锁")}
      ${detailStat(game.scoreLabel, displayScore(role))}
      ${detailStat("完成度", `${readiness}%`)}
    </div><div class="detail-progress-row"><div class="metric-line"><span>整体完成度</span><strong>${readiness}%</strong></div><div class="progress-track"><div class="progress-fill" style="--bar-color:${readinessColor(readiness)};width:${readiness}%"></div></div></div></div>
    <div class="detail-section"><div class="detail-section-title"><span>技能 / 节点</span><span>当前等级</span></div><div class="editable-controls">${talentRows}</div></div>
    ${buildTargetMarkup(role, state.activeGame)}
    <div class="detail-section"><div class="detail-section-title"><span>装备</span><span>仓库关联</span></div><div class="equipment-picker">${equipmentSlots().map((slot) => equipmentSelectMarkup(role, slot)).join("")}</div><div class="equipment-lines">${equipmentSlots().map((slot) => equipmentLineMarkup(role, slot)).join("")}</div></div>
    <div class="detail-section"><div class="detail-section-title"><span>快速调整</span><span>自动保存</span></div><div class="editable-controls">
      <div class="editable-row"><span>等级</span><div class="stepper"><button type="button" data-adjust="level" data-direction="-1" aria-label="降低等级">−</button><output>${escapeHtml(role.level || 0)}</output><button type="button" data-adjust="level" data-direction="1" aria-label="提高等级">＋</button></div></div>
      <div class="editable-row"><span>${escapeHtml(game.duplicateLabel)}</span><div class="stepper"><button type="button" data-adjust="duplicate" data-direction="-1" aria-label="降低${escapeHtml(game.duplicateLabel)}">−</button><output>${duplicate}</output><button type="button" data-adjust="duplicate" data-direction="1" aria-label="提高${escapeHtml(game.duplicateLabel)}">＋</button></div></div>
      <label class="editable-row" style="align-items:start"><span>补充备注</span><textarea class="note-box" id="roleNoteInput" rows="2" placeholder="记录角色备注，不用于套装词条">${escapeHtml(role.note)}</textarea></label>
    </div></div>`;
  bindIconFallbacks(detail);
}

function teamReadiness(team) {
  const memberRoles = (team.members || []).map(findRole).filter(Boolean);
  return Math.round(average(memberRoles.map(roleReadiness)));
}

function miniAvatar(role) {
  if (!role) return `<span class="mini-avatar" title="未选择">＋</span>`;
  return `<span class="mini-avatar" style="--avatar-bg:${safeColor(role.color)}" title="${escapeHtml(role.name)}">${iconMarkup(roleIconCandidates(state.activeGame, role), role.name, roleInitial(role.name), "mini-avatar-icon")}</span>`;
}

function renderTeamList() {
  const list = $("#teamList");
  const currentTeams = teams();
  $("#teamLibraryCount").textContent = `${currentTeams.length} 组`;
  list.innerHTML = currentTeams.map((team) => {
    const score = teamReadiness(team);
    const memberRoles = (team.members || []).map(findRole);
    return `<article class="team-card ${team.id === state.selectedTeamId ? "is-selected" : ""}" data-team-id="${escapeHtml(team.id)}" tabindex="0">
      <div class="team-card-copy"><strong>${escapeHtml(team.name)}</strong><span>${escapeHtml(team.note || "未添加备注")}</span><div class="team-mini-avatars">${memberRoles.map(miniAvatar).join("")}</div></div>
      <div class="team-card-score"><strong>${score}%</strong><span>完成度</span></div>
    </article>`;
  }).join("");
  $("#teamEmpty").hidden = currentTeams.length > 0;
  bindIconFallbacks(list);
}

function renderTeamDetail() {
  const currentTeams = teams();
  const team = currentTeams.find((item) => item.id === state.selectedTeamId) || currentTeams[0];
  const container = $("#teamDetailContent");
  if (!team) {
    container.innerHTML = `<div class="empty-team-detail"><span class="empty-icon">▦</span><strong>选择或创建一组配队</strong><span>当前游戏还没有保存方案</span></div>`;
    return;
  }
  state.selectedTeamId = team.id;
  const memberRoles = (team.members || []).map(findRole).filter(Boolean);
  const score = teamReadiness(team);
  const memberCards = memberRoles.map((role) => `<article class="team-member" data-open-role="${escapeHtml(role.id)}" tabindex="0">${roleAvatar(role, "avatar team-member-avatar")}<div class="team-member-name">${escapeHtml(role.name)}</div><div class="team-member-meta"><span>${escapeHtml(formatLevel(role))}</span><span style="color:${readinessColor(roleReadiness(role))}">${roleReadiness(role)}%</span></div></article>`).join("");
  container.innerHTML = `<div class="team-detail-content"><div class="team-detail-header"><div><p class="eyebrow">ACTIVE SQUAD</p><h3>${escapeHtml(team.name)}</h3><p>${escapeHtml(team.note || "未添加备注")}</p></div><div class="team-detail-actions"><button class="icon-button subtle" type="button" data-edit-team="${escapeHtml(team.id)}" title="编辑配队详情" aria-label="编辑配队详情">✎</button><button class="icon-button subtle" type="button" data-delete-team="${escapeHtml(team.id)}" title="删除配队" aria-label="删除配队">⌫</button></div></div>
    <div class="team-score-block"><strong>${score}%</strong><div class="team-score-copy"><span>队伍完成度</span><span>${memberRoles.length} 位角色 · ${escapeHtml(meta().name)}</span></div></div>
    <div class="team-members">${memberCards || `<div class="empty-state"><strong>尚未选择角色</strong></div>`}</div>
    <div class="synergy-row">${(team.tags || []).map((tag) => `<span class="synergy-tag">${escapeHtml(tag)}</span>`).join("")}</div></div>`;
  bindIconFallbacks(container);
}

function renderTeams() {
  if (!state.selectedTeamId || !teams().some((team) => team.id === state.selectedTeamId)) state.selectedTeamId = teams()[0]?.id || null;
  renderTeamList();
  renderTeamDetail();
}

function filteredInventory() {
  const query = state.inventoryQuery.trim().toLowerCase();
  const result = inventoryItems().filter((item) => {
    const holder = item.holderId ? findRole(item.holderId) : null;
    const setText = item.sets?.map((value) => relicSetLabel(state.activeGame, value)).join(" ");
    const statText = mainStatsSummary(state.activeGame, item.mainStats, true);
    const slotText = isRelicKind(state.activeGame, item.kind) ? relicSlotLabel(state.activeGame, item.slot, "") : "";
    const searchable = [item.name, setText, slotText, statText, item.substats, item.note, holder?.name, inventoryTypeLabel(item.kind)].filter(Boolean).join(" ").toLowerCase();
    return (state.inventoryKind === "all" || item.kind === state.inventoryKind) && (!query || searchable.includes(query));
  });
  result.sort((a, b) => {
    if (state.inventorySort === "rarity") return (Number(b.rarity) || 0) - (Number(a.rarity) || 0) || a.name.localeCompare(b.name, "zh-CN");
    if (state.inventorySort === "level") return (Number(b.level) || 0) - (Number(a.level) || 0) || a.name.localeCompare(b.name, "zh-CN");
    return a.name.localeCompare(b.name, "zh-CN");
  });
  return result;
}

function inventoryCard(item) {
  const holder = item.holderId ? findRole(item.holderId) : null;
  const scoreText = item.score == null ? "未评分" : `${Number(item.score).toFixed(1).replace(".0", "")} 分`;
  const stat = item.kind === "weapon"
    ? `Lv.${item.level || 0}/${item.maxLevel || meta().levelMax} · 精炼 ${item.refinement || 1}`
    : isRelicKind(state.activeGame, item.kind)
      ? `${relicSlotLabel(state.activeGame, item.slot, "未指定部位")} · ${relicItemStatSummary(state.activeGame, item)} · ${scoreText}`
      : item.score == null ? "尚未评分" : scoreText;
  return `<article class="inventory-item ${state.selectedInventoryId === item.id ? "is-selected" : ""}" data-inventory-id="${escapeHtml(item.id)}" tabindex="0" aria-label="查看 ${escapeHtml(item.name)}">
    ${inventoryIcon(item)}
    <div class="inventory-item-copy"><div class="inventory-item-title"><strong>${escapeHtml(item.name)}</strong><span class="rarity-stars" aria-label="${item.rarity} 星">${stars(item.rarity)}</span></div><span class="inventory-item-type">${escapeHtml(inventoryTypeLabel(item.kind))}</span><span class="inventory-item-stat">${escapeHtml(stat)}</span><span class="inventory-item-holder ${holder ? "is-held" : ""}">${holder ? `装备中 · ${escapeHtml(holder.name)}` : "未装备"}</span></div>
    <span class="inventory-lock" aria-label="${item.locked ? "已锁定" : "未锁定"}">${item.locked ? "◆" : "◇"}</span>
  </article>`;
}

function renderInventoryTabs() {
  const tabs = [{ key: "all", label: "全部" }, ...inventoryTypeForGame()];
  if (!tabs.some((tab) => tab.key === state.inventoryKind)) state.inventoryKind = "all";
  $("#inventoryKindTabs").innerHTML = tabs.map((tab) => `<button class="inventory-kind-tab ${tab.key === state.inventoryKind ? "is-selected" : ""}" type="button" role="tab" aria-selected="${tab.key === state.inventoryKind}" data-inventory-kind="${escapeHtml(tab.key)}">${escapeHtml(tab.label)}<span>${inventoryItems().filter((item) => tab.key === "all" || item.kind === tab.key).length}</span></button>`).join("");
}

function renderInventoryDetail() {
  const container = $("#inventoryDetailContent");
  const item = inventoryItems().find((entry) => entry.id === state.selectedInventoryId);
  if (!item) {
    container.innerHTML = `<div class="empty-team-detail"><span class="empty-icon">◇</span><strong>选择仓库条目</strong><span>查看装备信息或分配给角色</span></div>`;
    return;
  }
  const holder = item.holderId ? findRole(item.holderId) : null;
  const relicAttributes = relicAttributesMarkup(state.activeGame, item);
  const statRows = item.kind === "weapon"
    ? `${detailStat("等级", item.level == null ? "未强化" : `${item.level} / ${item.maxLevel || meta().levelMax}`)}${detailStat("精炼", item.refinement || 1)}${detailStat("稀有度", `${item.rarity || "—"} 星`)}`
    : `${detailStat("评分", item.score == null ? "未评分" : Number(item.score).toFixed(1).replace(".0", ""))}${detailStat("稀有度", `${item.rarity || "—"} 星`)}${detailStat("持有状态", holder ? "已装备" : "空闲")}`;
  const wikiHref = wikiHomes[state.activeGame] || "https://wiki.biligame.com/";
  container.innerHTML = `<div class="inventory-detail"><div class="inventory-detail-head">${inventoryIcon(item, "inventory-detail-icon")}<div><p class="eyebrow">${escapeHtml(inventoryTypeLabel(item.kind).toUpperCase())}</p><h3>${escapeHtml(item.name)}</h3><div class="rarity-stars">${stars(item.rarity)}</div></div><div class="inventory-detail-actions"><button class="icon-button subtle" type="button" data-edit-inventory="${escapeHtml(item.id)}" title="编辑装备详情" aria-label="编辑装备详情">✎</button><button class="icon-button subtle" type="button" data-toggle-lock="${escapeHtml(item.id)}" title="${item.locked ? "取消锁定" : "锁定条目"}" aria-label="${item.locked ? "取消锁定" : "锁定条目"}">${item.locked ? "◆" : "◇"}</button></div></div>
    <div class="detail-stats inventory-detail-stats">${statRows}</div>
    ${relicAttributes}
    <div class="inventory-holder"><span>当前角色</span>${holder ? `<button class="text-action" type="button" data-open-inventory-holder="${escapeHtml(holder.id)}">${escapeHtml(holder.name)} · ${escapeHtml(meta().name)}</button><button class="button button-quiet" type="button" data-unassign-inventory="${escapeHtml(item.id)}">卸下</button>` : `<span class="inventory-unassigned">未装备</span>`}</div>
    <p class="inventory-note">${escapeHtml(item.note || "暂无备注")}</p>
    <a class="wiki-attribution" href="${escapeHtml(wikiHref)}" target="_blank" rel="noreferrer">图标来源 · BWiki Wiki ↗</a></div>`;
  bindIconFallbacks(container);
}

function renderInventory() {
  renderInventoryTabs();
  $("#inventorySourceLink").href = wikiHomes[state.activeGame] || "https://wiki.biligame.com/";
  const result = filteredInventory();
  if (!result.length) state.selectedInventoryId = null;
  else if (!result.some((item) => item.id === state.selectedInventoryId)) state.selectedInventoryId = result[0].id;
  $("#inventoryResultCount").textContent = `显示 ${result.length} 件装备`;
  $("#inventoryLibraryCount").textContent = `${inventoryItems().length} 件`;
  $("#inventoryList").innerHTML = result.map(inventoryCard).join("");
  $("#inventoryEmpty").hidden = result.length > 0;
  const sortLabel = state.inventorySort === "name" ? "按名称" : state.inventorySort === "rarity" ? "按稀有度" : "按等级";
  $("#inventorySortButton").innerHTML = `${sortLabel} <span>↕</span>`;
  renderInventoryDetail();
  bindIconFallbacks($("#inventoryList"));
}

function renderHero() {
  const game = meta();
  $("#heroBackdrop").style.backgroundImage = `url('${game.cover}')`;
  document.documentElement.style.setProperty("--accent", game.accent);
  document.documentElement.style.setProperty("--accent-strong", game.accent);
}

function renderView() {
  $("#overviewView").hidden = state.view !== "overview";
  $("#teamsView").hidden = state.view !== "teams";
  $("#inventoryView").hidden = state.view !== "inventory";
  $$(".nav-item").forEach((button) => button.classList.toggle("is-active", button.dataset.view === state.view));
}

function renderAll() {
  renderHero();
  renderGameSwitcher();
  renderStats();
  renderFilterOptions();
  renderRoster();
  renderDetail();
  renderTeams();
  renderInventory();
  renderView();
}

function showToast(message) {
  const toast = $("#toast");
  toast.textContent = message;
  toast.classList.add("is-visible");
  clearTimeout(state.toastTimer);
  state.toastTimer = setTimeout(() => toast.classList.remove("is-visible"), 2200);
}

function setGame(gameKey) {
  if (!state.data.games[gameKey]) return;
  state.activeGame = gameKey;
  state.data.activeGame = gameKey;
  state.query = "";
  state.rarity = "all";
  state.status = "all";
  state.inventoryQuery = "";
  state.inventoryKind = "all";
  state.inventorySort = "name";
  $("#searchInput").value = "";
  $("#rarityFilter").value = "all";
  $("#statusFilter").value = "all";
  $("#inventorySearch").value = "";
  state.selectedRoleId = null;
  state.selectedInventoryId = null;
  state.editingInventoryId = null;
  state.selectedTeamId = state.data.games[gameKey].teams?.[0]?.id || null;
  saveData();
  renderAll();
}

function roleEditTalentMarkup(role) {
  const game = meta();
  const talentMax = talentMaxForGame();
  const values = Array.isArray(role?.talents) ? role.talents : [];
  const count = Math.max(game.talentLabels.length, values.length);
  return Array.from({ length: count }, (_, index) => {
    const label = game.talentLabels[index] || `技能 ${index + 1}`;
    const value = values[index] == null ? "" : values[index];
    return `<label class="form-field"><span>${escapeHtml(label)}</span><input data-role-edit-talent="${index}" type="number" min="0" max="${talentMax}" step="1" value="${escapeHtml(value)}" /></label>`;
  }).join("");
}

function openRoleEditDialog(roleId = state.selectedRoleId) {
  const role = findRole(roleId);
  const dialog = $("#roleEditDialog");
  if (!role || !dialog) return;
  state.editingRoleId = role.id;
  const game = meta();
  const talentMax = talentMaxForGame();
  const maxLevel = Number(role.maxLevel) || game.levelMax;
  $("#roleEditNameInput").value = role.name || "";
  $("#roleEditRarityInput").value = Number(role.rarity) || 1;
  $("#roleEditElementInput").value = role.element || "";
  $("#roleEditRoleInput").value = role.role || "未分类";
  $("#roleEditLevelInput").value = Number(role.level) || 1;
  $("#roleEditLevelInput").max = "200";
  $("#roleEditMaxLevelInput").value = maxLevel;
  $("#roleEditDuplicateLabel").textContent = game.duplicateLabel;
  $("#roleEditDuplicateInput").value = clamp(Number(role.duplicate) || 0, 0, 6);
  $("#roleEditColorInput").value = /^#[0-9a-f]{6}$/i.test(String(role.color || "")) ? role.color : "#4a5265";
  $("#roleEditTalentHint").textContent = `0-${talentMax} 级`;
  $("#roleEditTalentGrid").innerHTML = roleEditTalentMarkup(role);
  $("#roleEditNoteInput").value = role.note || "";
  if (typeof dialog.showModal === "function") dialog.showModal();
  else dialog.setAttribute("open", "");
}

function saveRoleFromDialog() {
  const role = findRole(state.editingRoleId);
  if (!role) {
    showToast("未找到要编辑的角色");
    return false;
  }
  const name = $("#roleEditNameInput").value.trim();
  if (!name) {
    showToast("请先填写角色名称");
    return false;
  }
  const game = meta();
  const readNumber = (selector, fallback) => {
    const value = Number($(selector).value);
    return Number.isFinite(value) ? value : fallback;
  };
  const maxLevel = clamp(Math.round(readNumber("#roleEditMaxLevelInput", Number(role.maxLevel) || game.levelMax)), 1, 200);
  role.name = name;
  role.rarity = clamp(Math.round(readNumber("#roleEditRarityInput", Number(role.rarity) || 1)), 1, 6);
  role.element = $("#roleEditElementInput").value.trim();
  role.role = $("#roleEditRoleInput").value.trim() || "未分类";
  role.maxLevel = maxLevel;
  role.level = clamp(Math.round(readNumber("#roleEditLevelInput", Number(role.level) || 1)), 1, maxLevel);
  role.duplicate = clamp(Math.round(readNumber("#roleEditDuplicateInput", Number(role.duplicate) || 0)), 0, 6);
  role.color = safeColor($("#roleEditColorInput").value, role.color || "#4a5265");
  const talentInputs = $$('[data-role-edit-talent]', $("#roleEditTalentGrid"));
  const talentValues = talentInputs.map((input) => input.value.trim());
  role.talents = talentValues.every((value) => value === "")
    ? []
    : talentValues.map((value) => clamp(Math.round(Number(value) || 0), 0, talentMaxForGame()));
  role.note = $("#roleEditNoteInput").value.trim();
  state.selectedRoleId = role.id;
  saveData("角色详情已保存");
  renderAll();
  $("#roleEditDialog").close();
  showToast("角色详情已保存");
  return true;
}

function renderTeamSlotGrid(selector, selectedMembers = []) {
  const grid = $(selector);
  if (!grid) return;
  const roster = roles();
  const selected = Array.isArray(selectedMembers) ? selectedMembers : [];
  const selectedOnly = selected
    .filter((id) => id && !roster.some((role) => role.id === id))
    .map((id) => ({ id, name: `未知角色 (${id})` }));
  const optionsRoles = [...roster, ...selectedOnly];
  grid.innerHTML = Array.from({ length: Math.max(4, selected.length) }, (_, index) => {
    const selectedId = selected[index] || "";
    return `<label class="slot-field"><span>位置 ${index + 1}</span><select data-team-slot="${index}"><option value="">未选择</option>${optionsRoles.map((role) => `<option value="${escapeHtml(role.id)}" ${role.id === selectedId ? "selected" : ""}>${escapeHtml(role.name)}</option>`).join("")}</select></label>`;
  }).join("");
}

function openTeamDialog() {
  const dialog = $("#teamDialog");
  $("#teamNameInput").value = "";
  $("#teamNoteInput").value = "";
  renderTeamSlotGrid("#teamSlotGrid");
  if (typeof dialog.showModal === "function") dialog.showModal();
  else dialog.setAttribute("open", "");
}

function saveTeamFromDialog() {
  const name = $("#teamNameInput").value.trim();
  const note = $("#teamNoteInput").value.trim();
  const members = $$("[data-team-slot]", $("#teamSlotGrid")).map((select) => select.value).filter(Boolean);
  if (!name) {
    showToast("请先填写方案名称");
    return false;
  }
  if (members.length < 2) {
    showToast("至少选择 2 位角色");
    return false;
  }
  const uniqueMembers = [...new Set(members)];
  if (uniqueMembers.length !== members.length) {
    showToast("同一角色不能重复加入配队");
    return false;
  }
  const team = { id: `${state.activeGame}-team-${Date.now()}`, name, note, members: uniqueMembers, tags: ["自定义", meta().name] };
  activeGame().teams = activeGame().teams || [];
  activeGame().teams.unshift(team);
  state.selectedTeamId = team.id;
  saveData("已保存配队");
  renderStats();
  renderTeams();
  $("#teamDialog").close();
  showToast("配队已保存");
  return true;
}

function openTeamEditDialog(teamId = state.selectedTeamId) {
  const team = teams().find((item) => item.id === teamId);
  const dialog = $("#teamEditDialog");
  if (!team || !dialog) return;
  state.editingTeamId = team.id;
  $("#teamEditNameInput").value = team.name || "";
  $("#teamEditNoteInput").value = team.note || "";
  $("#teamEditTagsInput").value = Array.isArray(team.tags) ? team.tags.join(", ") : String(team.tags || "");
  renderTeamSlotGrid("#teamEditSlotGrid", team.members || []);
  if (typeof dialog.showModal === "function") dialog.showModal();
  else dialog.setAttribute("open", "");
}

function parseTeamTags(value) {
  return unique(String(value || "").replaceAll("，", ",").split(/[,\n]+/).map((tag) => tag.trim()).filter(Boolean));
}

function saveTeamFromEditDialog() {
  const team = teams().find((item) => item.id === state.editingTeamId);
  if (!team) {
    showToast("未找到要编辑的配队");
    return false;
  }
  const name = $("#teamEditNameInput").value.trim();
  const members = $$('[data-team-slot]', $("#teamEditSlotGrid")).map((select) => select.value).filter(Boolean);
  if (!name) {
    showToast("请先填写方案名称");
    return false;
  }
  if (members.length < 2) {
    showToast("至少选择 2 位角色");
    return false;
  }
  const uniqueMembers = [...new Set(members)];
  if (uniqueMembers.length !== members.length) {
    showToast("同一角色不能重复加入配队");
    return false;
  }
  team.name = name;
  team.note = $("#teamEditNoteInput").value.trim();
  team.tags = parseTeamTags($("#teamEditTagsInput").value);
  team.members = uniqueMembers;
  state.selectedTeamId = team.id;
  saveData("配队详情已保存");
  renderStats();
  renderTeams();
  $("#teamEditDialog").close();
  showToast("配队详情已保存");
  return true;
}

function updateInventoryNameFromSet() {
  const input = $("#inventoryNameInput");
  const fields = $("#inventoryRelicSetFields");
  if (!input || !fields) return;
  const values = $$("[data-inventory-set]", fields).map((select) => select.value).filter(Boolean);
  const slotKey = $("#inventoryRelicSlotInput")?.value || "";
  const defaultName = relicItemDefaultName(state.activeGame, values, slotKey)
    || values.map((value) => relicSetLabel(state.activeGame, value)).join(" + ");
  if (input.dataset.autoName === "true" || !input.value.trim()) {
    input.value = defaultName;
    input.dataset.autoName = defaultName ? "true" : "false";
  }
}

function updateInventoryRelicSlotIcon() {
  const host = $("[data-inventory-slot-icon]");
  const slotKey = $("#inventoryRelicSlotInput")?.value || "";
  const fields = $("#inventoryRelicSetFields");
  const values = fields ? $$(`[data-inventory-set]`, fields).map((select) => select.value).filter(Boolean) : [];
  if (!host) return;
  host.innerHTML = slotKey
    ? relicSlotIconMarkup(state.activeGame, slotKey, relicSetValueForSlot(state.activeGame, values, slotKey))
    : relicSlotIconMarkup(state.activeGame, "");
  bindIconFallbacks(host);
}

function renderInventoryRelicStatFields() {
  const statFields = $("#inventoryRelicStatFields");
  const slotKey = $("#inventoryRelicSlotInput")?.value || "";
  const slot = relicSlots(state.activeGame).find((entry) => entry.key === slotKey);
  if (!statFields) return;
  if (!slot) {
    statFields.innerHTML = `<div class="relic-slot-placeholder">先选择部位，再填写该部位的主词条</div>`;
    return;
  }
  if (slot.fixed) {
    statFields.innerHTML = `<div class="target-fixed-stat inventory-fixed-stat"><span>${escapeHtml(slot.label)}</span><strong>固定 · ${escapeHtml(slot.fixed.label)}</strong></div>`;
    return;
  }
  statFields.innerHTML = `<label class="form-field"><span>${escapeHtml(slot.label)}主词条</span><select data-inventory-stat="${escapeHtml(slot.key)}"><option value="">未选择</option>${statOptionsForSlot(slot).map((option) => `<option value="${escapeHtml(option.value)}">${escapeHtml(option.label)}</option>`).join("")}</select></label>`;
}

function renderInventoryRelicFields() {
  const section = $("#inventoryRelicFields");
  const setFields = $("#inventoryRelicSetFields");
  const slotFields = $("#inventoryRelicSlotFields");
  const statFields = $("#inventoryRelicStatFields");
  const kind = $("#inventoryKindInput")?.value;
  const config = isRelicKind(state.activeGame, kind) ? relicConfig(state.activeGame) : null;
  if (!section || !setFields || !slotFields || !statFields) return;
  section.hidden = !config;
  if (!config) {
    setFields.innerHTML = "";
    slotFields.innerHTML = "";
    statFields.innerHTML = "";
    return;
  }
  setFields.innerHTML = config.setFields.map((field, index) => `<label class="form-field"><span class="target-field-heading"><span>${escapeHtml(field.label)}</span><span data-inventory-set-icon="${index}">${relicSetIconMarkup(state.activeGame, "")}</span></span><select data-inventory-set="${index}">${relicSetOptionsMarkup(state.activeGame)}</select></label>`).join("");
  slotFields.innerHTML = `<label class="form-field"><span class="target-field-heading"><span>部位</span><span data-inventory-slot-icon>${relicSlotIconMarkup(state.activeGame, "")}</span></span><select id="inventoryRelicSlotInput" name="inventoryRelicSlot">${relicSlotOptionsMarkup(state.activeGame)}</select></label>`;
  renderInventoryRelicStatFields();
  $$("[data-inventory-set]", setFields).forEach((select) => select.addEventListener("change", (event) => {
    updateInventoryNameFromSet();
    const iconHost = $(`[data-inventory-set-icon="${event.target.dataset.inventorySet}"]`, setFields);
    if (iconHost) {
      iconHost.innerHTML = relicSetIconMarkup(state.activeGame, event.target.value);
      bindIconFallbacks(iconHost);
    }
    updateInventoryRelicSlotIcon();
  }));
  $("#inventoryRelicSlotInput")?.addEventListener("change", () => {
    updateInventoryNameFromSet();
    renderInventoryRelicStatFields();
    updateInventoryRelicSlotIcon();
  });
  bindIconFallbacks(section);
}

function setInventoryDialogMode(editing, kind = "") {
  const typeLabel = kind ? inventoryTypeLabel(kind) : "装备";
  const eyebrow = $("#inventoryDialogEyebrow");
  const title = $("#inventoryDialogTitle");
  const saveButton = $("#saveInventoryButton");
  const kindInput = $("#inventoryKindInput");
  if (eyebrow) eyebrow.textContent = editing ? "EDIT INVENTORY ITEM" : "NEW INVENTORY ITEM";
  if (title) title.textContent = editing ? `编辑${typeLabel}` : "加入仓库";
  if (saveButton) saveButton.textContent = editing ? "保存修改" : "加入仓库";
  if (kindInput) kindInput.disabled = Boolean(editing);
}

function populateInventoryDialog(item) {
  const kindInput = $("#inventoryKindInput");
  if (!kindInput || !item) return;
  kindInput.value = item.kind || kindInput.options[0]?.value || "";
  $("#inventoryNameInput").value = item.name || "";
  $("#inventoryRarityInput").value = Number(item.rarity) || 5;
  $("#inventoryLevelInput").value = item.level == null ? "" : item.level;
  $("#inventoryRefinementInput").value = Number(item.refinement) || 1;
  $("#inventoryScoreInput").value = item.score == null ? "" : item.score;
  $("#inventoryWikiFileInput").value = item.wikiFile || "";
  $("#inventoryNoteInput").value = item.note || "";
  $("#inventorySubstatsInput").value = item.substats || "";
  $("#inventoryNameInput").dataset.autoName = item.customName ? "false" : "true";
  renderInventoryRelicFields();
  if (!isRelicKind(state.activeGame, item.kind)) return;

  const setFields = $("#inventoryRelicSetFields");
  const rawSets = Array.isArray(item.sets) ? item.sets : parseLegacyRelicSets(state.activeGame, item.set);
  $$('[data-inventory-set]', setFields).forEach((select, index) => {
    select.value = normalizeRelicSetValue(state.activeGame, rawSets[index] || "");
    const iconHost = $(`[data-inventory-set-icon="${index}"]`, setFields);
    if (iconHost) {
      iconHost.innerHTML = relicSetIconMarkup(state.activeGame, select.value);
      bindIconFallbacks(iconHost);
    }
  });
  const slotInput = $("#inventoryRelicSlotInput");
  if (slotInput) slotInput.value = normalizeRelicSlot(state.activeGame, item.slot || item.part || item.position);
  renderInventoryRelicStatFields();
  const slot = findRelicSlot(state.activeGame, slotInput?.value || "");
  if (slot && !slot.fixed) {
    const statInput = $(`[data-inventory-stat="${slot.key}"]`, $("#inventoryRelicStatFields"));
    if (statInput) statInput.value = normalizeStatValue(slot, item.mainStats?.[slot.key]);
  }
  updateInventoryNameFromSet();
  updateInventoryRelicSlotIcon();
  bindIconFallbacks($("#inventoryRelicFields"));
}

function openInventoryDialog() {
  const dialog = $("#inventoryDialog");
  state.editingInventoryId = null;
  $("#inventoryNameInput").value = "";
  $("#inventoryRarityInput").value = "5";
  $("#inventoryLevelInput").value = "";
  $("#inventoryRefinementInput").value = "1";
  $("#inventoryScoreInput").value = "";
  $("#inventoryWikiFileInput").value = "";
  $("#inventoryNoteInput").value = "";
  $("#inventorySubstatsInput").value = "";
  $("#inventoryNameInput").dataset.autoName = "false";
  $("#inventoryKindInput").innerHTML = inventoryTypeForGame().map((entry) => `<option value="${escapeHtml(entry.key)}">${escapeHtml(entry.label)}</option>`).join("");
  setInventoryDialogMode(false, $("#inventoryKindInput").value);
  renderInventoryRelicFields();
  if (typeof dialog.showModal === "function") dialog.showModal();
  else dialog.setAttribute("open", "");
}

function openInventoryEditDialog(itemId = state.selectedInventoryId) {
  const item = inventoryItems().find((entry) => entry.id === itemId);
  const dialog = $("#inventoryDialog");
  if (!item || !dialog) {
    showToast("未找到要编辑的仓库条目");
    return;
  }
  state.editingInventoryId = item.id;
  $("#inventoryKindInput").innerHTML = inventoryTypeForGame().map((entry) => `<option value="${escapeHtml(entry.key)}">${escapeHtml(entry.label)}</option>`).join("");
  populateInventoryDialog(item);
  setInventoryDialogMode(true, item.kind);
  if (typeof dialog.showModal === "function") dialog.showModal();
  else dialog.setAttribute("open", "");
}

function readInventoryRelicForm(gameKey = state.activeGame) {
  const config = relicConfig(gameKey);
  if (!config) return null;
  const sets = config.setFields.map((field, index) => $(`[data-inventory-set="${index}"]`, $("#inventoryRelicSetFields"))?.value || "");
  const slot = normalizeRelicSlot(gameKey, $("#inventoryRelicSlotInput")?.value || "");
  const mainStats = {};
  const selectedSlot = relicSlots(gameKey).find((entry) => entry.key === slot);
  if (selectedSlot && !selectedSlot.fixed) {
    const value = $(`[data-inventory-stat="${selectedSlot.key}"]`, $("#inventoryRelicStatFields"))?.value || "";
    if (value) mainStats[selectedSlot.key] = normalizeStatValue(selectedSlot, value);
  }
  return {
    sets,
    slot,
    mainStats,
    defaultName: relicItemDefaultName(gameKey, sets, slot) || sets.filter(Boolean).map((value) => relicSetLabel(gameKey, value)).join(" + "),
    substats: $("#inventorySubstatsInput").value.trim(),
  };
}

function moveHeldRelicAssignment(item, nextSlot) {
  if (!item?.holderId || !nextSlot) return true;
  const holder = findRole(item.holderId);
  if (!holder) return true;
  const ids = roleGearIds(holder);
  const occupyingId = ids[nextSlot];
  if (occupyingId && occupyingId !== item.id) {
    const occupying = inventoryItems().find((entry) => entry.id === occupyingId);
    if (occupying?.locked) {
      showToast("目标部位装备已锁定，请先解锁");
      return false;
    }
    if (occupying?.holderId && occupying.holderId !== holder.id) {
      const occupyingHolder = findRole(occupying.holderId);
      if (occupyingHolder) clearRoleEquipment(occupyingHolder, "relic", nextSlot);
      else occupying.holderId = null;
    } else if (occupying) occupying.holderId = null;
  }
  Object.keys(ids).forEach((key) => {
    if (key !== nextSlot && ids[key] === item.id) delete ids[key];
  });
  ids[nextSlot] = item.id;
  return true;
}

function saveInventoryFromDialog() {
  const editingItem = state.editingInventoryId
    ? inventoryItems().find((entry) => entry.id === state.editingInventoryId)
    : null;
  if (state.editingInventoryId && !editingItem) {
    showToast("未找到要编辑的仓库条目");
    return false;
  }
  const kind = editingItem?.kind || $("#inventoryKindInput").value;
  const relicValues = isRelicKind(state.activeGame, kind) ? readInventoryRelicForm(state.activeGame) : null;
  let name = $("#inventoryNameInput").value.trim();
  if (relicValues) {
    const selectedSet = relicSetValueForSlot(state.activeGame, relicValues.sets, relicValues.slot);
    if (!relicValues.slot || !selectedSet) {
      showToast("请选择套装和部位");
      return false;
    }
    if (!name) name = relicValues.defaultName;
  }
  if (!name || !kind) {
    showToast(relicValues ? "请填写条目名称" : "请填写装备名称和类型");
    return false;
  }
  const levelText = $("#inventoryLevelInput").value.trim();
  const scoreText = $("#inventoryScoreInput").value.trim();
  const parsedScore = scoreText ? Number(scoreText) : null;
  const common = {
    kind,
    name,
    rarity: clamp(Number($("#inventoryRarityInput").value) || 4, 1, 6),
    level: levelText ? clamp(Number(levelText) || 1, 1, meta().levelMax) : null,
    maxLevel: Number(editingItem?.maxLevel) || meta().levelMax,
    refinement: clamp(Number($("#inventoryRefinementInput").value) || 1, 1, 5),
    score: parsedScore == null || Number.isFinite(parsedScore) ? parsedScore : null,
    iconCached: editingItem ? editingItem.iconCached !== false : Boolean(relicValues),
    wikiFile: $("#inventoryWikiFileInput").value.trim(),
    note: $("#inventoryNoteInput").value.trim(),
  };
  const relicFields = relicValues ? {
    slot: relicValues.slot,
    sets: relicValues.sets,
    set: relicValues.sets.filter(Boolean).join(" + "),
    mainStats: relicValues.mainStats,
    substats: relicValues.substats,
    customName: Boolean(name !== relicValues.defaultName),
  } : {};

  if (editingItem) {
    if (relicValues && editingItem.holderId && editingItem.slot !== relicValues.slot && !moveHeldRelicAssignment(editingItem, relicValues.slot)) return false;
    const holderId = editingItem.holderId;
    Object.assign(editingItem, common, relicFields);
    state.selectedInventoryId = editingItem.id;
    const holder = holderId ? findRole(holderId) : null;
    if (holder && relicValues) syncRoleGearSnapshot(holder);
    else if (holder && kind === "weapon") {
      holder.weapon = {
        name: editingItem.name,
        rarity: editingItem.rarity,
        refinement: editingItem.refinement,
        level: editingItem.level || 0,
        maxLevel: editingItem.maxLevel,
      };
    } else if (holder) {
      holder.gear = {
        ...(holder.gear || {}),
        set: editingItem.name,
        sets: editingItem.sets || [],
        score: editingItem.score,
        pieces: editingItem.pieces || [],
        mainStats: editingItem.mainStats || {},
        substats: editingItem.substats || "",
      };
    }
    saveData("仓库条目已更新");
    state.editingInventoryId = null;
    setInventoryDialogMode(false, kind);
    $("#inventoryDialog").close();
    renderAll();
    showToast("装备详情已保存");
    return true;
  }

  const item = {
    id: `${state.activeGame}-inventory-${Date.now()}`,
    ...common,
    pieces: [],
    holderId: null,
    locked: false,
    ...relicFields,
  };
  inventoryForGame().items.unshift(item);
  state.selectedInventoryId = item.id;
  saveData("仓库条目已保存");
  state.editingInventoryId = null;
  setInventoryDialogMode(false, kind);
  $("#inventoryDialog").close();
  renderAll();
  showToast("已加入仓库");
  return true;
}

function unassignInventory(itemId) {
  const item = inventoryItems().find((entry) => entry.id === itemId);
  if (!item) return;
  if (item.locked) {
    showToast("当前装备已锁定，请先解锁");
    return;
  }
  if (item.holderId) {
    const holder = findRole(item.holderId);
    if (holder) clearRoleEquipment(holder, item.kind, item.slot || "");
    item.holderId = null;
  }
  saveData("装备已卸下");
  renderAll();
  showToast("装备已卸下");
}

function adjustRole(field, direction) {
  const role = findRole(state.selectedRoleId);
  if (!role) return;
  const game = meta();
  if (field === "level") role.level = clamp((Number(role.level) || 0) + direction, 1, Number(role.maxLevel) || game.levelMax);
  if (field === "duplicate") role.duplicate = clamp((Number(role.duplicate) || 0) + direction, 0, 6);
  saveData("刚刚保存");
  renderStats();
  renderRoster();
  renderDetail();
  renderTeams();
}

function exportData() {
  const output = JSON.stringify(state.data, null, 2);
  const blob = new Blob([output], { type: "application/json;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `nebula-progression-${state.activeGame}.json`;
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 500);
  showToast("数据已导出");
}

function importData(file) {
  if (!file) return;
  const reader = new FileReader();
  reader.onload = () => {
    try {
      const imported = JSON.parse(String(reader.result || ""));
      if (!imported?.games?.genshin) throw new Error("invalid");
      state.data = imported;
      ensureInventory(state.data);
      state.activeGame = imported.activeGame && imported.games[imported.activeGame] ? imported.activeGame : "genshin";
      state.selectedRoleId = null;
      state.selectedInventoryId = null;
      state.editingInventoryId = null;
      state.inventoryQuery = "";
      state.inventoryKind = "all";
      state.inventorySort = "name";
      $("#searchInput").value = "";
      $("#rarityFilter").value = "all";
      $("#statusFilter").value = "all";
      $("#inventorySearch").value = "";
      state.selectedTeamId = state.data.games[state.activeGame]?.teams?.[0]?.id || null;
      saveData("已导入文件");
      renderAll();
      showToast("数据已导入");
    } catch {
      showToast("文件格式无法识别");
    }
  };
  reader.readAsText(file, "utf-8");
}

function resetSample() {
  if (!window.confirm("恢复后会覆盖当前本地修改，是否继续？")) return;
  state.data = clone(sampleData);
  ensureInventory(state.data);
  state.activeGame = "genshin";
  state.selectedRoleId = null;
  state.selectedInventoryId = null;
  state.editingInventoryId = null;
  state.inventoryQuery = "";
  state.inventoryKind = "all";
  state.inventorySort = "name";
  $("#searchInput").value = "";
  $("#rarityFilter").value = "all";
  $("#statusFilter").value = "all";
  $("#inventorySearch").value = "";
  state.selectedTeamId = state.data.games.genshin.teams[0].id;
  saveData("已恢复示例");
  renderAll();
  showToast("示例数据已恢复");
}

function bindEvents() {
  $("#primaryNav").addEventListener("click", (event) => {
    const button = event.target.closest("[data-view]");
    if (!button) return;
    state.view = button.dataset.view;
    renderView();
  });

  $("#gameSwitcher").addEventListener("click", (event) => {
    const button = event.target.closest("[data-game]");
    if (button) setGame(button.dataset.game);
  });

  $("#rosterList").addEventListener("click", (event) => {
    const item = event.target.closest("[data-role-id]");
    if (!item) return;
    state.selectedRoleId = item.dataset.roleId;
    $("#detailSurface").classList.add("is-open");
    renderRoster();
    renderDetail();
  });

  $("#rosterList").addEventListener("keydown", (event) => {
    if (event.key !== "Enter" && event.key !== " ") return;
    const item = event.target.closest("[data-role-id]");
    if (!item) return;
    event.preventDefault();
    state.selectedRoleId = item.dataset.roleId;
    $("#detailSurface").classList.add("is-open");
    renderRoster();
    renderDetail();
  });

  $("#searchInput").addEventListener("input", (event) => {
    state.query = event.target.value;
    renderRoster();
    renderDetail();
  });
  $("#rarityFilter").addEventListener("change", (event) => { state.rarity = event.target.value; renderRoster(); renderDetail(); });
  $("#statusFilter").addEventListener("change", (event) => { state.status = event.target.value; renderRoster(); renderDetail(); });
  $("#sortButton").addEventListener("click", () => {
    state.sort = state.sort === "readiness" ? "score" : state.sort === "score" ? "name" : "readiness";
    $("#sortButton").innerHTML = state.sort === "readiness" ? "按完成度 <span>↕</span>" : state.sort === "score" ? "按评分 <span>↕</span>" : "按名称 <span>↕</span>";
    renderRoster();
  });
  $$("[data-layout]").forEach((button) => button.addEventListener("click", () => {
    state.layout = button.dataset.layout;
    $$("[data-layout]").forEach((item) => item.classList.toggle("is-selected", item === button));
    renderRoster();
  }));

  $("#detailContent").addEventListener("click", (event) => {
    const editButton = event.target.closest("[data-edit-role]");
    if (editButton) {
      openRoleEditDialog(editButton.dataset.editRole);
      return;
    }
    const button = event.target.closest("[data-adjust]");
    if (!button) return;
    adjustRole(button.dataset.adjust, Number(button.dataset.direction));
  });
  $("#detailContent").addEventListener("input", (event) => {
    if (event.target.id === "roleTargetSubstats") {
      const role = findRole(state.selectedRoleId);
      const target = roleBuildTarget(role, state.activeGame);
      if (!target) return;
      target.substats = event.target.value;
      saveData("养成目标已保存");
      return;
    }
    if (event.target.id !== "roleNoteInput") return;
    const role = findRole(state.selectedRoleId);
    if (!role) return;
    role.note = event.target.value;
    saveData("刚刚保存");
  });
  $("#detailContent").addEventListener("change", (event) => {
    const targetSet = event.target.closest("[data-target-set]");
    if (targetSet) {
      const role = findRole(state.selectedRoleId);
      const target = roleBuildTarget(role, state.activeGame);
      if (!target) return;
      const index = Number(targetSet.dataset.targetSet);
      target.sets[index] = normalizeRelicSetValue(state.activeGame, targetSet.value);
      saveRoleBuildTarget(role);
      return;
    }
    const targetStat = event.target.closest("[data-target-stat]");
    if (targetStat) {
      const role = findRole(state.selectedRoleId);
      const target = roleBuildTarget(role, state.activeGame);
      const slot = relicSlots(state.activeGame).find((entry) => entry.key === targetStat.dataset.targetStat);
      if (!role || !target || !slot) return;
      if (targetStat.value) target.mainStats[slot.key] = normalizeStatValue(slot, targetStat.value);
      else delete target.mainStats[slot.key];
      saveRoleBuildTarget(role);
      return;
    }
    const select = event.target.closest("[data-equipment-kind]");
    if (!select) return;
    bindRoleEquipment(state.selectedRoleId, select.dataset.equipmentKind, select.value, select.dataset.equipmentSlot || "");
  });
  $("#closeDetailButton").addEventListener("click", () => $("#detailSurface").classList.remove("is-open"));

  $("#inventorySearch").addEventListener("input", (event) => {
    state.inventoryQuery = event.target.value;
    renderInventory();
  });
  $("#inventoryKindTabs").addEventListener("click", (event) => {
    const tab = event.target.closest("[data-inventory-kind]");
    if (!tab) return;
    state.inventoryKind = tab.dataset.inventoryKind;
    renderInventory();
  });
  $("#inventorySortButton").addEventListener("click", () => {
    state.inventorySort = state.inventorySort === "name" ? "rarity" : state.inventorySort === "rarity" ? "level" : "name";
    renderInventory();
  });
  $("#inventoryList").addEventListener("click", (event) => {
    const item = event.target.closest("[data-inventory-id]");
    if (!item) return;
    state.selectedInventoryId = item.dataset.inventoryId;
    renderInventory();
  });
  $("#inventoryList").addEventListener("keydown", (event) => {
    if (event.key !== "Enter" && event.key !== " ") return;
    const item = event.target.closest("[data-inventory-id]");
    if (!item) return;
    event.preventDefault();
    state.selectedInventoryId = item.dataset.inventoryId;
    renderInventory();
  });
  $("#inventoryDetailContent").addEventListener("click", (event) => {
    const editButton = event.target.closest("[data-edit-inventory]");
    if (editButton) {
      openInventoryEditDialog(editButton.dataset.editInventory);
      return;
    }
    const holderButton = event.target.closest("[data-open-inventory-holder]");
    if (holderButton) {
      state.selectedRoleId = holderButton.dataset.openInventoryHolder;
      state.view = "overview";
      state.query = "";
      state.rarity = "all";
      state.status = "all";
      $("#searchInput").value = "";
      $("#rarityFilter").value = "all";
      $("#statusFilter").value = "all";
      renderAll();
      $("#detailSurface").classList.add("is-open");
      return;
    }
    const unassignButton = event.target.closest("[data-unassign-inventory]");
    if (unassignButton) {
      unassignInventory(unassignButton.dataset.unassignInventory);
      return;
    }
    const lockButton = event.target.closest("[data-toggle-lock]");
    if (lockButton) {
      const item = inventoryItems().find((entry) => entry.id === lockButton.dataset.toggleLock);
      if (!item) return;
      item.locked = !item.locked;
      saveData(item.locked ? "仓库条目已锁定" : "仓库条目已解锁");
      renderInventory();
    }
  });
  $("#inventoryKindInput").addEventListener("change", () => {
    $("#inventoryNameInput").value = "";
    $("#inventoryNameInput").dataset.autoName = "false";
    renderInventoryRelicFields();
  });
  $("#inventoryNameInput").addEventListener("input", () => {
    $("#inventoryNameInput").dataset.autoName = "false";
  });
  $("#newInventoryButton").addEventListener("click", openInventoryDialog);
  $("#inventoryForm").addEventListener("submit", (event) => {
    if (!event.submitter || event.submitter.value === "default") {
      event.preventDefault();
      saveInventoryFromDialog();
    }
  });

  $("#teamsView").addEventListener("click", (event) => {
    const teamCard = event.target.closest("[data-team-id]");
    if (teamCard) {
      state.selectedTeamId = teamCard.dataset.teamId;
      renderTeamList();
      renderTeamDetail();
      return;
    }
    const editButton = event.target.closest("[data-edit-team]");
    if (editButton) {
      openTeamEditDialog(editButton.dataset.editTeam);
      return;
    }
    const member = event.target.closest("[data-open-role]");
    if (member) {
      state.selectedRoleId = member.dataset.openRole;
      state.view = "overview";
      state.query = "";
      state.rarity = "all";
      state.status = "all";
      $("#searchInput").value = "";
      $("#rarityFilter").value = "all";
      $("#statusFilter").value = "all";
      renderAll();
      $("#detailSurface").classList.add("is-open");
      return;
    }
    const deleteButton = event.target.closest("[data-delete-team]");
    if (deleteButton) {
      if (!window.confirm("删除这组配队？")) return;
      activeGame().teams = activeGame().teams.filter((team) => team.id !== deleteButton.dataset.deleteTeam);
      state.selectedTeamId = activeGame().teams[0]?.id || null;
      saveData("配队已删除");
      renderStats();
      renderTeams();
      showToast("配队已删除");
    }
  });
  $("#newTeamButton").addEventListener("click", openTeamDialog);
  $("#teamForm").addEventListener("submit", (event) => {
    if (event.submitter?.value === "default") {
      event.preventDefault();
      saveTeamFromDialog();
    }
  });
  $("#roleEditForm").addEventListener("submit", (event) => {
    if (!event.submitter || event.submitter.value === "default") {
      event.preventDefault();
      saveRoleFromDialog();
    }
  });
  $("#teamEditForm").addEventListener("submit", (event) => {
    if (!event.submitter || event.submitter.value === "default") {
      event.preventDefault();
      saveTeamFromEditDialog();
    }
  });
  $("#exportButton").addEventListener("click", exportData);
  $("#importButton").addEventListener("click", () => $("#importInput").click());
  $("#importInput").addEventListener("change", (event) => { importData(event.target.files?.[0]); event.target.value = ""; });
  $("#resetSampleButton").addEventListener("click", resetSample);

  $$(".side-filter").forEach((button) => button.addEventListener("click", () => {
    state.view = "overview";
    state.status = button.dataset.filter;
    $("#statusFilter").value = state.status;
    renderView();
    renderRoster();
    renderDetail();
  }));
}

function init() {
  loadData();
  bindEvents();
  saveData();
  renderAll();
}

document.addEventListener("DOMContentLoaded", init);
