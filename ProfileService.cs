using System.Configuration;
using System.ComponentModel;


namespace SudaEasyWebLogger {
  /// <summary>
  /// 用户登录账号的类别
  /// </summary>
  public enum AccountType { ChinaTelecom, ChinaMobile, ChinaUnicom, Suda }

  /// <summary>
  /// 用户登录配置
  /// </summary>
  public struct LoginProfile {
    public AccountType accountType;
    public string account;
    public string password;
  }

  /// <summary>
  /// 配置管理的接口类型
  /// </summary>
  public interface IProfileService {
    bool TryReadProfile(ref LoginProfile profile);
    bool TryWriteProfile(LoginProfile profile);
  }

  /// <summary>
  /// 配置管理的接口具体实现
  /// </summary>
  public class ProfileService : IProfileService {
    private readonly Configuration? _config;
    private readonly string _configFilePath;

    public ProfileService() {
      // 手动指定 Login.config 文件路径
      _configFilePath
        = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Login.config");

      try {
        _config = ConfigurationManager.OpenMappedExeConfiguration(
            new ExeConfigurationFileMap { ExeConfigFilename = _configFilePath },
            ConfigurationUserLevel.None);
      } catch (Exception) {
        _config = null;
      }
    }

    public bool TryReadProfile(ref LoginProfile profile) {
      if (_config == null || !File.Exists(_configFilePath)) {
        return false;
      }

      try {
        profile.account = GetConfigValue("Account", string.Empty);
        profile.accountType = GetConfigValue("AccountType", string.Empty) switch {
          "ChinaTelecom" => AccountType.ChinaTelecom,
          "ChinaMobile" => AccountType.ChinaMobile,
          "ChinaUnicom" => AccountType.ChinaUnicom,
          "Suda" => AccountType.Suda,
          _ => throw new InvalidEnumArgumentException(),
        };
        profile.password = GetConfigValue("Password", string.Empty);
      } catch (Exception) {
        return false;
      }

      return true;
    }

    public bool TryWriteProfile(LoginProfile profile) {
      try {
        if (_config == null || !File.Exists(_configFilePath)) {
          CreateProfile(profile);
          return true;
        }

        SetConfigValue("Account", profile.account);
        SetConfigValue("AccountType", profile.accountType.ToString());
        SetConfigValue("Password", profile.password);

        _config.Save(ConfigurationSaveMode.Modified);

        // 反正改完就关，也没必要强制刷新了
        //  ConfigurationManager.RefreshSection("appSettings");

      } catch (Exception) {
        return false;
      }
      return true;
    }

    private string GetConfigValue(string key, string defaultValue) {
      return _config?.AppSettings.Settings[key]?.Value ?? defaultValue;
    }

    private void SetConfigValue(string key, string value) {
      if (_config!.AppSettings.Settings[key] == null) {
        _config!.AppSettings.Settings.Add(key, value);
      } else {
        _config!.AppSettings.Settings[key].Value = value;
      }
    }

    private void CreateProfile(LoginProfile profile) {
      string configContent = $@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <appSettings>
    <add key=""Account"" value=""{profile.account}"" />
    <add key=""AccountType"" value=""{profile.accountType}"" />
    <add key=""Password"" value=""{profile.password}"" />
  </appSettings>
</configuration>";

      File.WriteAllText(_configFilePath, configContent);
    }
  }

}
