using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Nebula.Core.Gacha.Endfield;
using Nebula.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;

namespace Nebula.Features.Gacha;

public sealed partial class EndfieldGachaLoginDialog : ContentDialog
{
    private readonly EndfieldGachaService _service = AppConfig.GetService<EndfieldGachaService>();

    private readonly List<AccountItem> _accounts = [];


    public EndfieldGachaClient.EndfieldBindingAccount? SelectedAccount { get; private set; }

    public string LoginToken { get; private set; } = "";


    public EndfieldGachaLoginDialog()
    {
        InitializeComponent();
    }


    private string Provider => RadioButton_Gryphline.IsChecked is true ? "gryphline" : "hypergryph";

    private string LoginUrl => Provider == "gryphline" ? "https://user.gryphline.com/" : "https://user.hypergryph.com/";

    private string TokenUrl => Provider == "gryphline" ? "https://web-api.gryphline.com/cookie_store/account_token" : "https://web-api.hypergryph.com/account/info/hg";


    private async void Button_OpenLogin_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri(LoginUrl));
    }


    private async void Button_OpenToken_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri(TokenUrl));
    }


    private void TextBox_Token_TextChanged(object sender, TextChangedEventArgs e)
    {
        Button_LoadAccounts.IsEnabled = !string.IsNullOrWhiteSpace(TextBox_Token.Text);
    }


    private async void Button_LoadAccounts_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetLoading(true);
            LoginToken = EndfieldGachaClient.ExtractAccountToken(TextBox_Token.Text);
            var accounts = await _service.GetAccountsByTokenAsync(LoginToken, Provider);
            _accounts.Clear();
            _accounts.AddRange(accounts.Select(x => new AccountItem(x)));
            ComboBox_Accounts.ItemsSource = null;
            ComboBox_Accounts.ItemsSource = _accounts;
            ComboBox_Accounts.SelectedIndex = _accounts.Count > 0 ? 0 : -1;
            IsPrimaryButtonEnabled = ComboBox_Accounts.SelectedItem is AccountItem;
            if (_accounts.Count == 0)
            {
                InAppToast.MainWindow?.Warning(null, "\u672a\u627e\u5230\u7ed1\u5b9a\u7684\u7ec8\u672b\u5730\u8d26\u53f7\u3002");
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex);
        }
        finally
        {
            SetLoading(false);
        }
    }


    private void ComboBox_Accounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsPrimaryButtonEnabled = ComboBox_Accounts.SelectedItem is AccountItem;
    }


    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (ComboBox_Accounts.SelectedItem is not AccountItem item || string.IsNullOrWhiteSpace(LoginToken))
        {
            args.Cancel = true;
            return;
        }
        SelectedAccount = item.Account;
    }


    private void SetLoading(bool value)
    {
        ProgressRing_Loading.IsActive = value;
        ProgressRing_Loading.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        Button_LoadAccounts.IsEnabled = !value && !string.IsNullOrWhiteSpace(TextBox_Token.Text);
        TextBox_Token.IsEnabled = !value;
        RadioButton_Hypergryph.IsEnabled = !value;
        RadioButton_Gryphline.IsEnabled = !value;
    }


    private sealed class AccountItem
    {
        public EndfieldGachaClient.EndfieldBindingAccount Account { get; }

        public string DisplayText { get; }

        public AccountItem(EndfieldGachaClient.EndfieldBindingAccount account)
        {
            Account = account;
            var name = string.IsNullOrWhiteSpace(account.NickName) ? account.RoleId : account.NickName;
            var server = string.IsNullOrWhiteSpace(account.ServerName) ? account.ServerId : account.ServerName;
            DisplayText = string.IsNullOrWhiteSpace(name)
                ? $"{account.Uid} | {server}"
                : $"{name} ({account.Uid}) | {server}";
        }
    }
}
