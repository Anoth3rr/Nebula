# 终末地官方启动来源适配

核查日期：2026-10-16。

## 使用方式

保留已安装的鹰角启动器（国服）或 GRYPHLINK（国际服），至少正常运行过一次以完成协议注册。在 Nebula 定位终末地游戏目录后，直接点击「开始游戏」；普通权限运行 Nebula 时会请求一次 UAC 授权。启动成功后仍由 Nebula 记录游戏进程的时长。

国服启动时自动补齐缺失的参数：

```text
-launcher_lang=zh-cn -launcher_sub_channel=837
```

用户已配置的语言、渠道、DX11、窗口参数会保留；相同参数不重复追加。国际服保留自己的参数，不套用国服渠道。启用了「第三方工具启动」时，仍由所选工具负责启动链路，需要该工具自身兼容新版终末地。终末地直接启动采用下述原生创建流程，因此不会再经由「使用 CMD 启动游戏」插入中间进程。

## 为什么还需要官方启动器

本机鹰角启动器日志中，2026-10-07 的 `Endfield.exe` 命令行已有上述语言与渠道参数。2026-10-15 更新后的公开适配实现还使用官方 `Games.exe` 作为父进程，仅补齐参数不能替代这一启动来源适配。

Nebula 读取以下 Windows 协议注册来定位已安装的启动器：

| 区服 | 注册协议 | 签名发行者 |
| --- | --- | --- |
| 国服 | `hypergryphlauncher-abYeZZ16BPluCFyT` | `Shanghai Hypergryph Network Technology Co., Ltd.` |
| 国际服 | `gryphlinklauncher-TiaytKBUIEdoEwRT`、旧版 `gryphlinelauncher-YDUTE5gscDZ229CW` | `GRYPH FRONTIER PTE. LTD.` |

启动时按数字版本寻找 `Games.exe`，验证 Authenticode 签名与对应发行者，复用当前 Windows 会话中的官方进程；尚未运行时启动已安装的官方程序并等待最多 15 秒。创建游戏前再次核对进程路径并锁定、验证对应文件，通过 Windows `PROC_THREAD_ATTRIBUTE_PARENT_PROCESS` 创建游戏，取得实际游戏 PID。

官方进程和游戏均需要管理员权限。Nebula 界面可以维持原权限，通过自身的内部辅助命令完成提权启动；辅助进程在 WinUI 初始化之前处理命令，以退出码返回游戏 PID 或失败原因。失败会提示缺少启动器、签名无效、权限不足或超时，不回退到无法满足来源校验的直接启动。辅助进程错误日志位于 `%LOCALAPPDATA%\Nebula\log\endfield-launch.log`。

本机日志中的 `tracking_code` 属于安装来源信息，没有作为通用默认值写入代码。

## 验证

```powershell
dotnet run --project tests/Nebula.GameLauncher.Tests -c Release
dotnet run --project tests/Nebula.EndfieldLauncher.Tests -c Release
# 可选：使用本机已安装的国服官方启动器验证真实签名及篡改检测
dotnet run --project tests/Nebula.EndfieldLauncher.Tests -c Release -- --verify-installed 'D:\Game\Hypergryph Launcher\1.6.0\Games.exe'
```

2026-10-16 验证：35 项参数回归、30 项终末地检查通过（其中 3 项使用本机官方签名）。无害子进程实际验证了父进程选择、PID 回传、工作目录和带引号参数；签名检查覆盖正确国服发行者、错误区服、未签名和已篡改文件。主程序 Release/x64 构建通过；附加 CI 的 `DONOT_CHECK_UPDATE` 常量时也能保留自定义入口。使用实际构建的 Nebula.exe 验证了无效辅助请求返回 `-6`、未提权请求返回 `-4`，两者均在界面初始化前退出。Windows CI 同时运行两组测试，CI 不依赖安装游戏。

本次没有实际启动终末地登录，也没有完成真实 UAC 授权与国际服客户端的端到端验收。

## 来源

- 本机鹰角启动器 `1.6.0` 及其 `games.log` 中的终末地启动记录；日志只读核对，未纳入仓库。
- [Xel Launcher 的终末地适配](https://github.com/lTinchl/Xel-Launcher/blob/01b4ff133fe5c0b496c64a1341c19eb612eaafb5/Helpers/EndfieldLauncher.cs)：用于交叉核对官方进程、区服协议及启动方式。
- [Microsoft：UpdateProcThreadAttribute](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute)：父进程属性及访问权限。
- [Microsoft：WinVerifyTrust](https://learn.microsoft.com/en-us/windows/win32/api/wintrust/nf-wintrust-winverifytrust)：签名验证及状态释放。
