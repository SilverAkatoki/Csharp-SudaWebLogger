using System.Text.RegularExpressions;
using System.Text;
using System.ComponentModel;

namespace SudaEasyWebLogger {

  public interface ILoginService {
    bool TryGetIp(ref string ip);
    bool TryLogin(LoginProfile loginProfile, string ip);
  }
  interface ILoginStrategy {
    bool TryLogin(LoginProfile loginProfile, string ip, HttpClient httpClient);
  }

  internal partial class LoginService : ILoginService {
    private ILoginStrategy? _strategy;
    private readonly HttpClient _httpClient = new();

    public bool TryGetIp(ref string ip) {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      string url = "http://10.9.1.3/";

      try {
        HttpResponseMessage response = _httpClient.GetAsync(url).Result;
        response.EnsureSuccessStatusCode();
        string responseBody = Encoding.GetEncoding("gb2312")
            .GetString(response.Content.ReadAsByteArrayAsync().Result);
        ip = IpRegex().Matches(responseBody)[0].Groups[1].ToString();
      } catch (Exception) {
        return false;
      }
      return true;
    }

    public bool TryLogin(LoginProfile loginProfile, string ip) {
      _strategy = loginProfile.accountType switch {
        AccountType.ChinaTelecom => new ChinaTelecomLogin(),
        AccountType.ChinaMobile => new ChinaMobileLogin(),
        AccountType.ChinaUnicom => new ChinaUnicomLogin(),
        AccountType.Suda => new SudaLogin(),
        _ => throw new InvalidEnumArgumentException("枚举值错误"),
      };
      return _strategy.TryLogin(loginProfile, ip, _httpClient);
    }

    [GeneratedRegex("ss5=\\\"(\\d+.\\d+.\\d+.\\d+)\\\"")]
    private static partial Regex IpRegex();
  }
  internal partial class ChinaTelecomLogin : ILoginStrategy {
    public bool TryLogin(LoginProfile loginProfile, string ip,
      HttpClient httpClient) {
      throw new NotImplementedException();
    }
  }
  internal partial class ChinaMobileLogin : ILoginStrategy {
    public bool TryLogin(LoginProfile loginProfile, string ip,
      HttpClient httpClient) {
      string complement = new Random().Next(1, 999).ToString();
      string requestUrl = string.Format("http://10.9.1.3:801/eportal" +
            "/?c=Portal&a=login&callback=dr1003&login_method=1&user_" +
            "account=%2C0%2C{0}%40zgyd&user_password={1}&wlan_user_ip=" +
            "{2}&wlan_user_ipv6=&wlan_user_mac=000000000000&wlan_ac_ip=" +
            "&wlan_ac_name=&jsVersion=3.3.3&v={3}\r\n",
            loginProfile.account,
            loginProfile.password,
            ip,
            complement);
      try {
        HttpResponseMessage response =
            httpClient.GetAsync(requestUrl).Result;
        response.EnsureSuccessStatusCode();
        string responseBody =
        response.Content.ReadAsStringAsync().Result;
        string returnCode = ReturnCodeRegex().Matches(responseBody)[0].Groups[1]
          .ToString();
        if (returnCode == "0") {
          return false;
        } else if (returnCode == "1") {
          return true;
        } else {
          throw new InvalidCastException();
        }
      } catch (Exception) {
        return false;
      }
    }

    [GeneratedRegex("\\\"result\\\":\\\"(\\d)\\\"")]
    private static partial Regex ReturnCodeRegex();
  }
  internal partial class ChinaUnicomLogin : ILoginStrategy {
    public bool TryLogin(LoginProfile loginProfile, string ip,
      HttpClient httpClient) {
      throw new NotImplementedException();
    }
  }
  internal partial class SudaLogin : ILoginStrategy {
    public bool TryLogin(LoginProfile loginProfile, string ip,
      HttpClient httpClient) {
      throw new NotImplementedException();
    }
  }
}
