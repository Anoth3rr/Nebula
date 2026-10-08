# 鸣潮启动参数

鸣潮 3.7 HD 版本在没有画质启动参数时可能报错：

```text
Fatal Error: [File:Unknown] [Line: 54]
kuro: Use launcher to start game!
```

官方启动器会传入 `-krqlv=hd`。Nebula 现在也会在直接启动鸣潮时补上这个默认参数；已经设置的 `-krqlv` 参数优先，其他自定义启动参数会保留。使用第三方工具启动时，由该工具管理启动参数。

旧版 Nebula 可在鸣潮的启动参数设置中填写 `-krqlv=hd`。已在本机鸣潮 3.7 HD 上验证：通过 Nebula 的“开始游戏”按钮能够进入“点击连接”界面。

参数处理回归检查：

```powershell
dotnet run --project tests/Nebula.GameLauncher.Tests
```
