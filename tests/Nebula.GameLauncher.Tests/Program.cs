using Nebula.Core;
using Nebula.Features.GameLauncher;

int passed = 0;
void Check(string name, GameBiz biz, string? arguments, string? expected, bool thirdPartyTool = false)
{
    string? actual = GameLaunchArguments.Prepare(biz, arguments, thirdPartyTool);
    if (actual != expected)
    {
        throw new Exception($"{name}: expected '{expected}', got '{actual}'.");
    }
    Console.WriteLine($"PASS {name}");
    passed++;
}

Check("CN direct launch supplies the required HD argument", GameBiz.wutheringwaves_cn, null, "-krqlv=hd");
Check("Global direct launch supplies the required HD argument", GameBiz.wutheringwaves_global, "", "-krqlv=hd");
Check("Blank settings use the default", GameBiz.wutheringwaves_cn, " \t ", "-krqlv=hd");
Check("Custom graphics arguments are preserved", GameBiz.wutheringwaves_cn, " -dx11 -windowed ", "-dx11 -windowed -krqlv=hd");
Check("The existing local workaround is not duplicated", GameBiz.wutheringwaves_cn, "-krqlv=hd", "-krqlv=hd");
Check("Explicit quality overrides are preserved", GameBiz.wutheringwaves_cn, "-dx11 -krqlv=sd", "-dx11 -krqlv=sd");
Check("Quality flag matching ignores case", GameBiz.wutheringwaves_global, "-KRQLV=HD -dx11", "-KRQLV=HD -dx11");
Check("Quoted quality argument is preserved", GameBiz.wutheringwaves_cn, "\"-krqlv=hd\" -dx11", "\"-krqlv=hd\" -dx11");
Check("Quoted quality value is preserved", GameBiz.wutheringwaves_cn, "-krqlv=\"sd\"", "-krqlv=\"sd\"");
Check("Separate explicit quality value is preserved", GameBiz.wutheringwaves_cn, "-krqlv sd", "-krqlv sd");
Check("Similar argument names do not suppress the default", GameBiz.wutheringwaves_cn, "-krqlv-extra=sd", "-krqlv-extra=sd -krqlv=hd");
Check("A flag inside a quoted value is not a quality override", GameBiz.wutheringwaves_cn,
    "-log=\"notes -krqlv=sd\"", "-log=\"notes -krqlv=sd\" -krqlv=hd");
Check("Official Arknights CN launch arguments are supplied", GameBiz.arknights_cn, null,
    "-launcher_lang=zh-cn -launcher_sub_channel=837");
Check("Official Arknights CN launch arguments preserve custom arguments", GameBiz.arknights_cn,
    "-popupwindow", "-popupwindow -launcher_lang=zh-cn -launcher_sub_channel=837");
Check("Existing Arknights launcher arguments are not duplicated", GameBiz.arknights_cn,
    "-launcher_lang=zh-cn -launcher_sub_channel=837", "-launcher_lang=zh-cn -launcher_sub_channel=837");
Check("Custom Arknights language is preserved", GameBiz.arknights_cn,
    "-launcher_lang=en-us", "-launcher_lang=en-us -launcher_sub_channel=837");
Check("Bilibili Arknights keeps its channel arguments", GameBiz.arknights_bilibili,
    null, null);
Check("Global Arknights keeps its channel arguments", GameBiz.arknights_global,
    "-windowed", "-windowed");
Check("Third-party tools keep their own command line", GameBiz.wutheringwaves_cn, " --profile custom ", "--profile custom", thirdPartyTool: true);
Check("Third-party tools with no arguments receive no default", GameBiz.wutheringwaves_cn, null, null, thirdPartyTool: true);
Check("Other games with no arguments receive no default", GameBiz.hk4e_cn, null, null);
Check("Other games retain their arguments", GameBiz.endfield_cn, " -windowed ", "-windowed");

Console.WriteLine($"{passed} game launch argument checks passed.");
