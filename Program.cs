using System;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

namespace SudaWebLogger {
    internal class Program {
        static void Main(string[] args) {
            ShowTitle();

            ShowProfilePath();

            if (!ProfileReader) {
                ShowProfileExample();
                Paused();
                return;
            }

            AnsiConsole.Write("\n\n");
            IP = GetLogIp();

            if (IP == "-1") {
                Paused();
                return;
            }

            ShowProfile();
            AnsiConsole.Markup("\n");
            if (!GetUserOperate()) {
                AnsiConsole.Markup("[black on white]取消登录[/] ");
                AnsiConsole.Markup("好吧, 什么也没干\n\n");
                Paused();
                return;
            }

            AnsiConsole.Markup("[black on white]按配置文件登录[/] ");

            complementNum = Random(1, 999).ToString();
            AnsiConsole.Markup(string.Format("已生成 {0} 位随机" +
                "数 [bold]{1}[/] 补全请求 URL",
                complementNum.Length, complementNum));
            AnsiConsole.Markup("\n");

            SendConnectRequest();
        }

        private static void ShowTitle() {
            AnsiConsole.Write(new FigletText("SudaWebLogger").Centered());
            AnsiConsole.Write(new Rule("[yellow]苏州大学校园网登录器[/]"));
        }

        private static void ShowProfilePath() {
            AnsiConsole.Markup("[black on white]读取的配置文件路径[/]  ");
            AnsiConsole.Write(new TextPath("./cofig.ini")
                .RootStyle(new Style(foreground: Color.Grey35))
                .StemStyle(new Style(foreground: Color.Grey35))
                .SeparatorStyle(new Style(foreground: Color.Grey35))
                .LeafStyle(new Style(foreground: Color.White)));
        }

        private static bool ProfileReader {
            get {
                IConfiguration config;
                try {
                    config = new ConfigurationBuilder()
                        .SetBasePath(Environment.CurrentDirectory)
                        .AddIniFile("config.ini").Build();
                } catch {
                    AnsiConsole.Markup("\n[red]配置文件不存在, 请检查程序根目录下是否" +
                        "含有名为 config.ini 的配置文件[/]\n\n");
                    return false;
                }

                netType = config["运营商:netType"];
                if (string.IsNullOrEmpty(netType)) {
                    AnsiConsole.Markup("\n[red]配置文件中未填写运营商[/]\n\n");
                    return false;
                }

                userName = config["账户&密码:userName"];
                if (string.IsNullOrEmpty(userName)) {
                    AnsiConsole.Markup("\n[red]配置文件中未填写账号(学工号)[/]\n\n");
                    return false;
                }

                password = config["账户&密码:password"];
                if (string.IsNullOrEmpty(password)) {
                    AnsiConsole.Markup("\n[red]配置文件中未填写密码[/]\n\n");
                    return false;
                }

                return true;
            }
        }

        private static int Random(int lowNum, int maxNum) {
            return new Random().Next(lowNum, maxNum);
        }

        private static void ShowProfile() {
            var infoGrid = new Grid()
                            .AddColumn(new GridColumn().NoWrap().PadRight(6))
                            .AddColumn();
            var infoPanel = new Panel(infoGrid).RoundedBorder().Header("载入" +
                "的配置文件", Justify.Center);
            AnsiConsole.Live(infoPanel)
                    .AutoClear(false)
                    .Overflow(VerticalOverflow.Ellipsis)
                    .Cropping(VerticalOverflowCropping.Top)
                    .Start(ctx => {
                        void Update(int delay, Action action) {
                            action();
                            ctx.Refresh();
                            Thread.Sleep(delay);
                        }
                        Update(0, () => infoGrid!.AddRow("[b]账号(学工号)[/]",
                            $"{userName}"));
                        Update(10, () => infoGrid.AddRow("[b]密码[/]",
                            "".PadRight(password!.Length, '*')));
                        Update(20, () => infoGrid!.AddRow("[b]运营商[/]",
                            $"{netType}"));
                        Update(30, () => infoGrid!.AddRow("[b]当前登录 IP 地址[/]",
                            $"[yellow]{IP}[/]"));
                    });
        }

        private static void ShowProfileExample() {
            AnsiConsole.Markup("下面是一个[yellow]配置文件[/]的示例:\n");
            AnsiConsole.Write(new Panel(new Grid()
                    .AddColumn()
                    .AddRow("[[账户&密码]]")
                    .AddRow("userName = 114514")
                    .AddRow("password = 1919810")
                    .AddRow("[[运营商]]")
                    .AddRow("netType = 中国移动")
                    .AddRow(";运营商类型只能为 校园网, 中国移动, 中国电信, " +
                    "中国联通 其中之一")
                    )
                .Header(new PanelHeader("config.ini").RightJustified())
                );
            AnsiConsole.Markup("[bold]Tip[/]:配置文件请放置" +
                "在程序[yellow]根目录[/]下.\n\n");
        }

        private static void Paused() {
            [DllImport("msvcrt.dll")]
            static extern bool system(string str);
            system("pause");
        }

        private static string GetLogIp() {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string url = "http://10.9.1.3/";
            try {
                using HttpClient client = new();
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                string responseBody = Encoding.GetEncoding("gb2312")
                    .GetString(response.Content.ReadAsByteArrayAsync().Result);
                return Regex.Matches(responseBody,
                        "ss5=\\\"(\\d+.\\d+.\\d+.\\d+)\\\"")[0].Groups[1]
                        .ToString();
            } catch (ArgumentOutOfRangeException) {
                AnsiConsole.Markup("[red]无法从网页获取登录 IP 地址[/]\n");
                AnsiConsole.Markup("你可能[yellow]已经[/]登录过了\n\n");
                return "-1";
            } catch (AggregateException) {
                AnsiConsole.Markup("[red]无法从网页获取登录 IP 地址[/]\n");
                AnsiConsole.Markup("好像没有[yellow]连接校园网[/]呢\n\n");
                return "-1";
            }
        }

        private static bool GetUserOperate() {
            return AnsiConsole.Confirm("按照此配置文件登录？");
        }

        private static void SendConnectRequest() {
            HttpClient client = new();
            string requestUrl = string.Format("http://10.9.1.3:801/eportal" +
            "/?c=Portal&a=login&callback=dr1003&login_method=1&user_" +
            "account=%2C0%2C{0}%40zgyd&user_password={1}&wlan_user_ip=" +
            "{2}&wlan_user_ipv6=&wlan_user_mac=000000000000&wlan_ac_ip=" +
            "&wlan_ac_name=&jsVersion=3.3.3&v={3}\r\n",
            userName, password, IP, complementNum);
            try {
                HttpResponseMessage response =
                    client.GetAsync(requestUrl).Result;
                response.EnsureSuccessStatusCode();
                string responseBody =
                response.Content.ReadAsStringAsync().Result;
                string returnCode = Regex.Matches(responseBody,
                    "\\\"result\\\":\\\"(\\d)\\\"")[0].Groups[1].ToString();
                switch (returnCode) {
                    case "0":
                        AnsiConsole.Markup("\n[red]登录失败![/]\n\n");
                        AnsiConsole.Markup("你可能[yellow]已经[/]登录过了, " +
                            "或者是[yellow]账号密码[/]错误\n\n");
                        break;
                    case "1":
                        AnsiConsole.Markup("\n[yellow]已成功登录![/]\n\n");
                        break;
                }
            } catch (HttpRequestException e) {
                AnsiConsole.Markup("[red]登录请求发送错误[/]\n");
                AnsiConsole.Markup(e.ToString());
            }
        }

        private static string? userName;
        private static string? password;
        private static string? netType;
        private static string? IP;
        private static string? complementNum;
    }
}
