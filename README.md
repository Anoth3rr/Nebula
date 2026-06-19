English | [简体中文](./docs/README.zh-CN.md) | [Tiếng Việt](./docs/README.vi-VN.md) | [日本語](./docs/README.ja-JP.md) | [ภาษาไทย](./docs/README.th-TH.md) | [Русский](./docs/README.ru-RU.md)


# Nebula

> **Nebula** comes from the slogan of Star Rail: May This Journey Lead Us **Nebula**, which is very suitable to be used as an app name.

Nebula is an open-source third-party launcher developed to address the shortcomings of HoYoPlay (miHoYo Launcher). It supports all PC games on of HoYoverse and aims to completely replace the official launcher. In addition to the basic functions of a launcher, I will also incorporate some additional features based on individual needs, such as:

-  Record game time
-  Switch game accounts
-  View game screenshots
-  Save gacha records

More features are left for you to explore...


## Fork Notice

This project is fork from [Scighost/Starward](https://github.com/Scighost/Starward). It also references ideas and implementations from [wuwatracker/wuwatracker](https://github.com/wuwatracker/wuwatracker) and [bhaoo/endfield-gacha](https://github.com/bhaoo/endfield-gacha).


## Changelog

### v0.0.0

Compared with [Scighost/Starward](https://github.com/Scighost/Starward):

- Rebranded the project as Nebula and updated project, release, issue, update, metadata, documentation, and installer links to [Anoth3rr/Nebula](https://github.com/Anoth3rr/Nebula).
- Added support entries for Arknights, Arknights: Endfield, and Wuthering Waves, including Mainland China and Global game identifiers where available.
- Added Wuthering Waves gacha record support, with item metadata and query logic referenced from [wuwatracker/wuwatracker](https://github.com/wuwatracker/wuwatracker).
- Added Arknights: Endfield gacha record support, including account binding, token-based query flow, character and weapon pool parsing, and import/statistics integration, with reference to [bhaoo/endfield-gacha](https://github.com/bhaoo/endfield-gacha).
- Added Endfield item metadata synchronization from Skland Wiki, local `EndfieldGachaInfo` storage, icon matching, and item-name refresh for existing gacha records.
- Extended URL protocol documentation with `endfield_cn`, `endfield_global`, `wutheringwaves_cn`, and `wutheringwaves_global` game identifiers.


## Install

First, your device needs to meet the following requirements:

- Windows 10 1809 (17763) and above.
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2) installed.
- [WebP Image Extension](https://apps.microsoft.com/detail/9pg2dk419drg) installed.
- For better experience, please enable **Transparency effects** and **Animation effects** in the system settings.

>[WebP Imaging Extension](https://apps.microsoft.com/detail/9pg2dk419drg) is typically bundled with your system. If the application isn’t displaying background images correctly, please ensure it’s installed.

Next, download the package for your CPU architecture from [GitHub Release](https://github.com/Anoth3rr/Nebula/releases). Extract it, then run `Nebula.exe` and follow the prompts.


## Localization

[![de-DE translation](https://img.shields.io/badge/dynamic/json?color=blue&label=de-DE&style=flat&logo=crowdin&query=%24.progress.0.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/nebula/de)
[![en-US translation](https://img.shields.io/badge/any_text-100%25-blue?logo=crowdin&label=en-US)](https://crowdin.com/project/nebula)
[![it-IT translation](https://img.shields.io/badge/dynamic/json?color=blue&label=it-IT&style=flat&logo=crowdin&query=%24.progress.2.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/nebula/it)
[![ja-JP translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ja-JP&style=flat&logo=crowdin&query=%24.progress.3.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/nebula/ja)
[![ko-KR translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ko-KR&style=flat&logo=crowdin&query=%24.progress.4.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/nebula/ko)
[![ru-RU translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ru-RU&style=flat&logo=crowdin&query=%24.progress.5.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/nebula/ru)
[![th-TH translation](https://img.shields.io/badge/dynamic/json?color=blue&label=th-TH&style=flat&logo=crowdin&query=%24.progress.6.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/nebula/th)
[![vi-VN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=vi-VN&style=flat&logo=crowdin&query=%24.progress.7.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/nebula/vi)
[![zh-CN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-CN&style=flat&logo=crowdin&query=%24.progress.8.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/nebula/zh-CN)
[![zh-TW translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-TW&style=flat&logo=crowdin&query=%24.progress.9.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/nebula/zh-TW)

Nebula uses [Crowdin](https://crowdin.com/project/nebula) for in-app text localization work. You can contribute by helping us translate and proofread content in your local language. We look forward to having more people join us.

[Localization Guide](./docs/Localization.md)


## Development

To compile the project locally, you need to install Visual Studio 2022 and select the following workloads:

-  .NET Desktop Development
-  C++ Desktop Development
-  Universal Windows Platform Development


## Donation

Development is not easy. If you think Nebula useful, you cloud donate me at https://donate.scighost.com.


## Thanks

<picture>
    <source srcset="https://github.com/Anoth3rr/Nebula/assets/61003590/9d369ec3-ab7c-408f-88c2-11bfe4453208" type="image/avif" />
    <img src="https://github.com/Anoth3rr/Nebula/assets/61003590/44552992-e2c5-451f-9c2a-73176e8e4e93" width="240px" />
</picture>

First of all, I would like to express my sincerest thanks to all the contributors and translators of this project. Nebula can only become better because of you.

Then, I want to express my special thanks to [@neon-nyan](https://github.com/neon-nyan). The inspiration and design for this project come directly from his project [Collapse](https://github.com/neon-nyan/Collapse). I have gained a lot of knowledge from the Collapse code, and with such a valuable reference, my development process has been much smoother.

Next, a big thanks to the main developer of [Snap Hutao](https://github.com/DGP-Studio/Snap.Hutao), [@Lightczx](https://github.com/Lightczx). His assistance has been invaluable during the development of Nebula.

Additionally, thanks [CloudFlare](https://www.cloudflare.com/) for providing free CDN services and [SignPath Foundation](https://signpath.org/) for providing free code signing for open-source projects.

<img alt="cloudflare" height="72px" src="https://github.com/user-attachments/assets/c1fba88e-4cd1-45df-b681-bf5634215f41" />&nbsp;&nbsp;&nbsp;&nbsp;<img alt="signpath foundation" height="72px" src="https://github.com/user-attachments/assets/052c654a-13fa-4e7a-8f1d-9f57c83f438b" />

And the [third-party libraries](./docs/ThirdParty.md) used in this project.


## Screenshot

<img width="1200" src="https://github.com/user-attachments/assets/d1704d44-fadd-4672-aade-c09584b7f16c" />
