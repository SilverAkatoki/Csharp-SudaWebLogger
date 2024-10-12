using Spectre.Console;
using System.ComponentModel;

namespace SudaEasyWebLogger {
  internal class App {
    enum UserAction { LogInOrRetry, ChangeProfile, Exit }

    private LoginProfile _profile;
    private readonly IProfileService _profileService = new ProfileService();
    private readonly ILoginService _loginService = new LoginService();
    private bool _hasReadedProfile = false;
    private bool _hasChangedProfile = false;

    public void Run() {
      while (true) {
        DisplayTitle();
        EnsureProfileLoaded();
        DisplayProfile(_profile);

        UserAction userAction = GetUserAction();

        switch (userAction) {
          case UserAction.Exit:
            HandleExit();
            return;
          case UserAction.ChangeProfile:
            HandleChangeProfile();
            break;
          case UserAction.LogInOrRetry:
            HandleLogin();
            break;
          default:
            throw new InvalidEnumArgumentException();
        }
      }
    }

    private void EnsureProfileLoaded() {
      if (!_hasReadedProfile) {
        _hasReadedProfile = true;
        if (!_profileService.TryReadProfile(ref _profile)) {
          AnsiConsole.MarkupLine("[red]当前未检测到配置文件 / 配置文件错误[/]");
          PromptForNewProfile();
          DisplayTitle();
        }
      }
    }

    private void PromptForNewProfile() {
      AnsiConsole.MarkupLine("正在开始创建新的配置文件");
      AnsiConsole.MarkupLine("\n[gray]请按任意键继续...[/]");
      Console.ReadKey(true);
      AnsiConsole.Clear();
      _profile = CreateLoginProfile();
      _hasChangedProfile = true;
    }

    private void HandleChangeProfile() {
      AnsiConsole.Clear();
      _profile = CreateLoginProfile();
      _hasChangedProfile = true;
    }

    private void HandleLogin() {
      string ip = string.Empty;
      if (!TryRetrieveIp(ref ip)) {
        DisplayIpError();
        return;
      }

      DisplayIp(ip);

      if (!TryLogin(ip)) {
        DisplayLoginError();
      } else {
        AnsiConsole.MarkupLine("\n[green]登录成功[/]");
        Thread.Sleep(500);
      }
    }

    private bool TryRetrieveIp(ref string ip) {
      string tempIp = ip;
      bool result = AnsiConsole.Status()
          .Spinner(Spinner.Known.Dots)
          .Start("正在获取 IP 地址...", ctx => {
            return _loginService.TryGetIp(ref tempIp);
          });
      ip = tempIp;
      return result;
    }

    private static void DisplayIpError() {
      AnsiConsole.MarkupLine("\n[red]无法获取本机 IP 地址[/]\n");
      AnsiConsole.MarkupLine("请检查你的[yellow]互联网连接情况[/]" +
        "（未连接 / 已登录时无法获取）, 或者是你的[yellow]网络资费[/]不足");
      AnsiConsole.MarkupLine("\n[gray]请按任意键重试...[/]");
      Console.ReadKey(true);
      AnsiConsole.Clear();
    }

    private bool TryLogin(string ip) {
      return AnsiConsole.Status()
          .Spinner(Spinner.Known.Dots)
          .Start("正在登录...", ctx => {
            return _loginService.TryLogin(_profile, ip);
          });
    }

    private static void DisplayLoginError() {
      AnsiConsole.MarkupLine("\n[red]登录失败[/]\n");
      AnsiConsole.MarkupLine(
          "请检查你的[yellow]配置信息[/]是否正确，以及你的互联网连接情况，"
          + "并确认你的[yellow]网络资费[/]充足"
      );
      AnsiConsole.MarkupLine("\n[gray]请按任意键重试...[/]");
      Console.ReadKey(true);
      AnsiConsole.Clear();
    }

    private void HandleExit() {
      if (_hasChangedProfile) {
        SaveProfile();
      }
    }

    private void SaveProfile() {
      if (!_profileService.TryWriteProfile(_profile)) {
        AnsiConsole.MarkupLine("\n[red]保存配置文件错误[/]");
        AnsiConsole.MarkupLine("请于再次创建配置文件时尝试保存");
        AnsiConsole.MarkupLine("\n[gray]按任意键关闭此窗口...[/]");
        Console.ReadKey(true);
      }
    }

    private static void DisplayIp(string ip) {
      AnsiConsole.MarkupLine($"获取到本地 IP 地址: [yellow]{ip}[/]");
    }

    private static void DisplayTitle() {
      AnsiConsole.Write(new FigletText("SudaWebLogger").Centered());
      AnsiConsole.Write(new Rule("[yellow]苏州大学校园网登录器[/]"));
    }

    private static void DisplayProfile(LoginProfile profile) {
      string account = profile.account ?? "------";
      string accountType = profile.accountType switch {
        AccountType.ChinaTelecom => "中国电信",
        AccountType.ChinaMobile => "中国移动",
        AccountType.ChinaUnicom => "中国联通",
        AccountType.Suda => "校园网",
        _ => throw new InvalidEnumArgumentException(),
      } ?? "----";

      var profileGrid = new Grid()
                  .AddColumn(new GridColumn().NoWrap().PadRight(6))
                  .AddColumn();
      var profilePanel = new Panel(profileGrid).RoundedBorder()
                            .Header("登录信息", Justify.Center);
      AnsiConsole.Live(profilePanel).AutoClear(false)
          .Overflow(VerticalOverflow.Ellipsis)
          .Cropping(VerticalOverflowCropping.Top)
          .Start(ctx => {
            void Update(int delay, Action action) {
              action();
              ctx.Refresh();
              Thread.Sleep(delay);
            }
            Update(0, () => profileGrid!.AddRow("[b]账号(学工号)[/]",
                      $"[yellow]{account}[/]"));
            Update(10, () => profileGrid!.AddRow("[b]运营商[/]",
                      $"{accountType}"));
          });
    }

    private static UserAction GetUserAction() {
      string userChoice = AnsiConsole.Prompt(
          new SelectionPrompt<string>()
              .PageSize(10)
              .Title("[gray]使用 ↑ 和 ↓ 来切换选项，Enter 键确认[/]")
              .AddChoices(["按此配置登录", "修改配置文件", "退出"]));
      return userChoice switch {
        "按此配置登录" => UserAction.LogInOrRetry,
        "修改配置文件" => UserAction.ChangeProfile,
        "退出" => UserAction.Exit,
        _ => throw new InvalidEnumArgumentException(),
      };
    }

    private static LoginProfile CreateLoginProfile() {
      AnsiConsole.Write(
        new Rule("[yellow]创建 / 更改登录配置[/]").LeftJustified());
      var profile = new LoginProfile();
      string accountType = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .PageSize(10)
            .Title("选择你的运营商：\n[gray]使用 ↑ 和 ↓ 来切换选项，Enter 键确认[/]")
            .AddChoices(["中国电信", "中国移动", "中国联通", "校园网"]));
      profile.accountType = accountType switch {
        "中国电信" => AccountType.ChinaTelecom,
        "中国移动" => AccountType.ChinaMobile,
        "中国联通" => AccountType.ChinaUnicom,
        "校园网" => AccountType.Suda,
        _ => throw new InvalidCastException(),
      };
      AnsiConsole.Markup($"选择你的运营商:{accountType}\n");
      profile.account = AnsiConsole.Prompt(
            new TextPrompt<string>("输入你的登录账号(一般是学号):")).ToString();
      profile.password = AnsiConsole.Prompt(
            new TextPrompt<string>("输入你的密码(为了防窥没有显示密码字符):")
              .Secret('\0'));
      AnsiConsole.MarkupLine("\n[green]登录配置创建完成！[/]");
      Thread.Sleep(500);
      AnsiConsole.Clear();
      return profile;
    }
  }
}
