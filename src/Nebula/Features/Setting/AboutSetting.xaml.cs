using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nebula.Features.Update;
using Nebula.Frameworks;
using Nebula.Setup.Core.Github;
using System;
using System.Threading.Tasks;


namespace Nebula.Features.Setting;

public sealed partial class AboutSetting : PageBase
{


    private readonly ILogger<AboutSetting> _logger = AppConfig.GetLogger<AboutSetting>();


    public AboutSetting()
    {
        this.InitializeComponent();
    }




    /// <summary>
    /// 预览版
    /// </summary>
    public bool EnablePreviewRelease
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.EnablePreviewRelease = value;
                AppConfig.LastUpdateCheckTime = default;
            }
        }
    } = AppConfig.EnablePreviewRelease;

    public bool AutomaticallyCheckForUpdates
    {
        get => AppConfig.AutomaticallyCheckForUpdates;
        set
        {
            AppConfig.AutomaticallyCheckForUpdates = value;
            OnPropertyChanged();
        }
    }

    public bool AutomaticallyInstallUpdates
    {
        get => AppConfig.AutomaticallyInstallUpdates;
        set
        {
            AppConfig.AutomaticallyInstallUpdates = value;
            OnPropertyChanged();
        }
    }

    public bool AutoRestartWhenUpdateFinished
    {
        get => AppConfig.AutoRestartWhenUpdateFinished;
        set => AppConfig.AutoRestartWhenUpdateFinished = value;
    }

    public string? UpdateStatusText { get; set => SetProperty(ref field, value); }


    /// <summary>
    /// 是最新版
    /// </summary>
    public string? LatestVersion { get; set => SetProperty(ref field, value); }


    /// <summary>
    /// 更新错误文本
    /// </summary>
    public string? UpdateErrorText { get; set => SetProperty(ref field, value); }


    /// <summary>
    /// 检查更新
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        try
        {
            LatestVersion = null;
            UpdateErrorText = null;
            UpdateStatusText = null;
            var release = await AppConfig.GetService<UpdateService>().GetLatestVersionAsync();
            AppConfig.LastUpdateCheckTime = DateTimeOffset.UtcNow;
            if (release is null)
            {
                UpdateStatusText = Lang.Update_NoPublishedRelease;
                return;
            }
            var currentVersion = GithubReleaseSource.ParseVersion(AppConfig.AppVersion);
            var newVersion = GithubReleaseSource.ParseVersion(release.Version);
            if (currentVersion is null || newVersion is null)
                throw new FormatException("Invalid application or release version.");
            if (newVersion > currentVersion)
            {
                UpdateWindow.ShowRelease(release);
            }
            else
            {
                LatestVersion = AppConfig.AppVersion;
            }
        }
        catch (Exception ex)
        {
            UpdateErrorText = ex.Message;
            _logger.LogError(ex, "Check update");
        }
    }




}
