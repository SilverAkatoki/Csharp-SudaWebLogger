﻿using Spectre.Console;
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

        if (!_hasReadedProfile) {
          _hasReadedProfile = true;
          if (!_profileService.TryReadProfile(ref _profile)) {
            AnsiConsole.MarkupLine("[red]当前未检测到配置文件 / 配置文件错误[/]");
            AnsiConsole.MarkupLine("正在开始创建新的配置文件");
            AnsiConsole.MarkupLine("\n[gray]请按任意键继续...[/]");
            Console.ReadKey(true);
            AnsiConsole.Clear();
            _profile = CreateLoginProfile();
            _hasChangedProfile = true;
            DisplayTitle();
          }
        }

        DisplayProfile(_profile);

        UserAction userAction = GetUserAction();

        if (userAction == UserAction.Exit) {
          break;
        } else if (userAction == UserAction.ChangeProfile) {
          AnsiConsole.Clear();
          _profile = CreateLoginProfile();
          _hasChangedProfile = true;
        } else if (userAction == UserAction.LogInOrRetry) {
          string ip = string.Empty;
          if (!_loginService.TryGetIp(ref ip)) {
            AnsiConsole.MarkupLine("\n[red]无法获取本机 IP 地址[/]\n");
            AnsiConsole.MarkupLine(
              "请检查你的[yellow]互联网连接情况[/], 或者是你[yellow]没充网费[/]"
              );
            AnsiConsole.MarkupLine("\n[gray]请按任意键重试...[/]");
            Console.ReadKey(true);
            AnsiConsole.Clear();
            continue;
          }
          DisplayIp(ip);
          DisplayDelay(1);
          if (!_loginService.TryLogin(_profile, ip)) {
            AnsiConsole.MarkupLine("\n[red]登录失败[/]\n");
            AnsiConsole.MarkupLine(
              "请检查你的[yellow]配置信息[/]是否正确，抑或是你[yellow]没有连网[/]"
               + "或是[yellow]没充网费[/]"
              );
            AnsiConsole.MarkupLine("\n[gray]请按任意键重试...[/]");
            Console.ReadKey(true);
            AnsiConsole.Clear();
            continue;
          } else {
            AnsiConsole.MarkupLine("\n[green]登录成功[/]");
            Thread.Sleep(500);
            break;
          }
        } else {
          throw new InvalidEnumArgumentException();
        }
      }
      if (_hasChangedProfile) {
        if (!_profileService.TryWriteProfile(_profile)) {
          AnsiConsole.MarkupLine("\n[red]保存配置文件错误[/]");
          AnsiConsole.MarkupLine("请于再次创建配置文件时尝试保存");
          AnsiConsole.MarkupLine("\n[gray]按任意键关闭此窗口...[/]");
          Console.ReadKey(true);
        }
      }
    }

    private static void DisplayIp(string ip) {
      AnsiConsole.MarkupLine($"获取到本地 IP 地址: [yellow]{ip}[/]");
    }

    private static void DisplayDelay(int seconds = 1) {
      AnsiConsole.Status()
              .Spinner(Spinner.Known.Dots)
              .Start("登录中...", ctx => {
                for (int i = 0; i < seconds * 10; i++) {
                  Thread.Sleep(100);
                }
              });
    }

    private static void DisplayTitle() {
      AnsiConsole.Write(new FigletText("SudaWebLogger").Centered());
      AnsiConsole.Write(new Rule("[yellow]苏州大学校园网登录器[/]"));
    }

    private static void DisplayProfile(LoginProfile profile) {
      string account = profile.Account ?? "------";
      string accountType = profile.AccountType switch {
        AccountType.ChinaTelecom => "中国电信",
        AccountType.ChinaMobile => "中国移动",
        AccountType.ChinaUnicom => "中国联通",
        AccountType.Suda => "校园网",
        _ => throw new InvalidEnumArgumentException("运营商类型错误"),
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
        _ => throw new InvalidCastException("操作未完全对应枚举值"),
      };
    }

    private static LoginProfile CreateLoginProfile() {
      AnsiConsole.Write(
        new Rule("[yellow]创建 / 更改登录配置[/]").LeftJustified());
      var profile = new LoginProfile();
      var accountType = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .PageSize(10)
            .Title("选择你的运营商：\n[gray]使用 ↑ 和 ↓ 来切换选项，Enter 键确认[/]")
            .AddChoices(["中国电信", "中国移动", "中国联通", "校园网"]));
      profile.AccountType = accountType switch {
        "中国电信" => AccountType.ChinaTelecom,
        "中国移动" => AccountType.ChinaMobile,
        "中国联通" => AccountType.ChinaUnicom,
        "校园网" => AccountType.Suda,
        _ => throw new InvalidCastException(),
      };
      AnsiConsole.Markup($"选择你的运营商:{accountType}\n");
      profile.Account = AnsiConsole.Prompt(
            new TextPrompt<string>("输入你的登录账号(一般是学号):")).ToString();
      profile.Password = AnsiConsole.Prompt(
            new TextPrompt<string>("输入你的密码(为了防窥没有显示密码字符):")
              .Secret('\0'));
      AnsiConsole.MarkupLine("\n[green]登录配置创建完成！[/]");
      Thread.Sleep(500);
      AnsiConsole.Clear();
      return profile;
    }
  }
}
