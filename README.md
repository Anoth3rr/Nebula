English | [简体中文](./docs/README.zh-CN.md) | [日本語](./docs/README.ja-JP.md)


# Nebula

Nebula is an open-source third-party launcher built to integrate games from multiple publishers. It supports all PC games from HoYoverse, all PC games from Hypergryph, and Wuthering Waves, with the goal of integrating selected domestic anime-style games. In addition to the basic functions of a launcher, I will also add some extra features based on personal needs, such as:

-  Record game time
-  Switch game accounts
-  View game screenshots
-  Save gacha records
-  Switch game servers

More features are left for you to explore.


## Fork Notice

This project is forked from [Scighost/Starward](https://github.com/Scighost/Starward). It also references ideas and implementations from [wuwatracker/wuwatracker](https://github.com/wuwatracker/wuwatracker) and [bhaoo/endfield-gacha](https://github.com/bhaoo/endfield-gacha).


## Changelog

### v0.0.0

- Added support entries for Arknights, Arknights: Endfield, and Wuthering Waves, including Mainland China and Global game identifiers where available.
- Added Wuthering Waves gacha record support, with item metadata and query logic referenced from [wuwatracker/wuwatracker](https://github.com/wuwatracker/wuwatracker).
- Added Arknights: Endfield gacha record support, including account binding, token-based query flow, character and weapon pool parsing, and import/statistics integration, with reference to [bhaoo/endfield-gacha](https://github.com/bhaoo/endfield-gacha).
- Added Endfield item metadata synchronization from Skland Wiki, local `EndfieldGachaInfo` storage, icon matching, and item-name refresh for existing gacha records.
- Extended URL Protocol documentation with `endfield_cn`, `endfield_global`, `wutheringwaves_cn`, and `wutheringwaves_global` game identifiers.
- Added support for switching between official and Bilibili servers for Genshin Impact, Honkai: Star Rail, Genshin Impact, and Arknights.


## Install

First, your device needs to meet the following requirements:

- Windows 10 1809 (17763) and above.
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2) installed.
- [WebP Image Extension](https://apps.microsoft.com/detail/9pg2dk419drg) installed.
- For better experience, please enable **Transparency effects** and **Animation effects** in the system settings.

>[WebP Imaging Extension](https://apps.microsoft.com/detail/9pg2dk419drg) is typically bundled with your system. If the application isn't displaying background images correctly, please ensure it's installed.

Next, download the package for your CPU architecture from [GitHub Release](https://github.com/Anoth3rr/Nebula/releases). Extract it, then run `Nebula.exe` and follow the prompts.


## Development

To compile the project locally, you need to install Visual Studio 2022 and select the following workloads:

-  .NET Desktop Development
-  C++ Desktop Development
-  Universal Windows Platform Development


## Thanks

This project references and benefits from the following projects. Sincere thanks to their authors and communities:

-  [Scighost/Starward](https://github.com/Scighost/Starward): the fork source of this project.
-  [CollapseLauncher/Collapse](https://github.com/CollapseLauncher/Collapse): referenced for launcher design and implementation ideas.
-  [DGP-Studio/Snap.Hutao](https://github.com/DGP-Studio/Snap.Hutao): referenced during development and provided related help.
-  [wuwatracker/wuwatracker](https://github.com/wuwatracker/wuwatracker): referenced for Wuthering Waves gacha records and item metadata.
-  [bhaoo/endfield-gacha](https://github.com/bhaoo/endfield-gacha): referenced for Arknights: Endfield gacha records.

And the [third-party libraries](./docs/ThirdParty.md) used in this project.
