# 脚本编写指南

## 脚本分类

脚本分为运维脚本和工程脚本。区别如下：

- 运维脚本是“周边设施”，运行频率一般较低，甚至只在每台机器上运行一次
- 运维脚本和本项目主题或者C#项目没有明显关系
- 运维脚本一般搬到其他项目上也能用，比较项目无关
- 工程脚本直接管理项目的构建测试等关键操作
- 工程脚本可以执行添加csproj等项目架构层面操作

工程脚本放`eng/scripts/`，运维脚本放`scripts/`.

## 脚本编写

简单脚本写成`#!/usr/bin/env sh`，即shell文件即可。

复杂脚本写成C# file-based脚本。

如果使用C#，需要使用SeriLog：

```csharp
#:package Serilog
#:package Serilog.Sinks.Console
using Serilog;
using var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
Log.Logger = log;
Log.Information("Process {step}", "data");
Log.Error(ex,"Catch an exception when ....")
```
