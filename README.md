# SudaWebLogger  

适用于苏州大学校园网的命令行登录器，只需要一路按 Enter 就能完成登录 *已有配置的情况下*  
初学 C# 的银晓写的练手项目  

**Tip**: 当前仅支持**中国移动**的登录请求, 欢迎提PR修复 :)

## 使用方法

确保你的电脑上安装有 [.net 8.0 运行库](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0#:~:text=ASP.NET%20Co)  
如果没有，点进去刚刚的超链接，然后按照自己的系统下载 **.NET Desktop Runtime 8.0.8** 或是 **.NET Runtime 8.0.8**

---

下载完成后，直接双击可执行文件即可  
第一次启动时，会在同一级运行目录下创建一个登录配置文件 `Login.config`，里面应该长这样：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="Account" value="..." />
    <add key="AccountType" value="..." />
    <add key="Password" value="..." />
  </appSettings>
</configuration>
```

*省略号就是你的登录配置内容*

如果手贱删除了，也会在下次启动时重新创建一个新的配置文件

## 使用的开源项目

CUI 框架: [Spectre.Console](https://github.com/spectreconsole/spectre.console)



