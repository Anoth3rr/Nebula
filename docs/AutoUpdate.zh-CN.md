# 软件更新与发布

## 使用方式

在「设置 → 关于」中配置：

- **启动时及每小时自动检查更新**：默认开启。后台按小时检查，本机保留最近检查时间；失败也会限频。开发构建（`DEBUG` / `DONOT_CHECK_UPDATE`）不执行后台检查。
- **发现新版本后自动升级**：默认关闭。打开后，发现新版本会显示更新窗口并自动下载、校验、安装。安装版可能请求管理员权限并关闭当前 Nebula。
- **更新完成后自动重启**：决定安装完成后是否重新启动软件。
- **预览版通道**：允许接收预览版，同时也接收版本更高的稳定版。

「检查更新」按钮随时可用，并忽略「忽略此版本」的限制。自动检查尊重忽略版本设置；没有公开版本时会明确提示，网络错误不会显示成「已是最新版」。

更新来源固定为 [Anoth3rr/Nebula Releases](https://github.com/Anoth3rr/Nebula/releases)。按语义版本比较，支持 `v0.1.0` 和 `0.1.0` 标签，排除草稿。下载包按当前进程的 CPU 架构、安装方式精确匹配。

## 安装过程

- 使用 GitHub 资源元数据中的 `sha256:` digest 校验完整文件及文件大小；缺少有效校验值时只提供手动下载。
- 支持断点续传。如果服务器忽略 Range，会重新下载，避免把完整响应拼接到残留文件后面。
- 便携版 ZIP 先解压到安装目录下的临时目录，校验布局和 `version.ini` 后替换文件，最后切换版本指针。保留旧版本目录和用户数据。
- 安装版下载包含完整载荷的安装器，再使用现有提权更新窗口进行安装，不依赖旧的独立更新服务器。
- 文件替换期间保留备份；替换失败则恢复旧文件。若恢复也失败，保留备份并在错误中显示备份位置。
- 关闭更新窗口会取消尚未完成的下载。下载校验失败不会执行或安装该文件。

## 生成发布包

准备仓库 `global.json` 指定的 .NET SDK、Windows SDK 和 Visual Studio C++ v145 工具链。MSBuild 需要在 PATH 中；7-Zip 可选，缺失时脚本使用 SharpSevenZip。

```powershell
./scripts/package-stable.ps1 -Version 0.1.0 -Architecture x64 -IncludePortable
./scripts/package-stable.ps1 -Version 0.1.0 -Architecture arm64 -IncludePortable
# 预览版
./scripts/package-stable.ps1 -Version 0.2.0-preview.1 -Architecture x64 -IncludePortable -AllowPrerelease
```

输出位于 `publish/stable/`，文件名必须与版本号和架构一致：

```text
Nebula_Setup_0.1.0_x64.exe
Nebula_Portable_0.1.0_x64.zip
Nebula_Setup_0.1.0_arm64.exe
Nebula_Portable_0.1.0_arm64.zip
```

ZIP 根目录为 `Nebula.exe`、`version.ini`、`app-0.1.0/`，不能再包一层 `Nebula/`，也不能混入 `config.ini`、数据库或用户数据。旧 `.7z` 便携包仍可手动下载，不参与 ZIP 自动升级。

也可手动运行 GitHub Actions 的 **Package GitHub Release**，输入版本号。它会测试更新模块、构建 x64/ARM64 包，并创建包含附件的 Release **草稿**。检查后手动发布草稿，客户端才会收到更新；带预览后缀的版本会设置为 prerelease。此工作流不需要旧发布脚本的外部存储或签名服务凭据。

GitHub 为上传附件提供 digest；完整上传所有架构附件后再公开 Release。自动更新只能在用户安装了含新更新器的版本后使用，首次迁移需手动安装。

## 验证

```powershell
dotnet run --project tests/Nebula.Update.Tests -c Release
dotnet build src/Nebula.Setup -c Release -p:PublishAot=false
dotnet build src/Nebula -c Release -p:Platform=x64
```

自动回归覆盖版本通道、架构匹配、空发布列表、HTTP 错误与取消、断点续传、校验失败、ZIP 路径和布局、数据保留、安装失败回滚。正式发布前还需在 Windows 上实际验证安装版的 UAC、退出与重启，以及便携版跨版本升级。
