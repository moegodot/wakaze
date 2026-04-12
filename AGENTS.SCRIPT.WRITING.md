# 脚本编写指南

本文件描述 `wakaze` 仓库当前脚本编写约定。修改 `eng/scripts`、`scripts`、脚本模板或脚本使用契约时，要根据情况同步更新本文件以及对应的 `AGENTS.ENG.md` / `AGENTS.SCRIPTS.md`。

## 脚本分类

- 工程脚本放在 `eng/scripts/`
  - 面向项目结构、测试、restore、solution 维护等仓库开发流程
- 运维 / 供应链脚本放在 `scripts/`
  - 面向 vendored 工具、第三方源码、外部依赖和安装产物管理

不要把二者混用：

- 会改动项目结构、solution、测试入口的流程脚本通常属于 `eng/scripts/`
- 会下载外部工具、下载第三方源码、构建 vendored 产物的脚本通常属于 `scripts/`

## 当前推荐形式

- 简单脚本优先使用 `sh`
- 非平凡、需要仓库上下文、参数解析、结构化日志或跨平台逻辑的脚本，优先使用 C# file-based script

当前仓库里已经存在两种典型形式：

- `#!/usr/bin/env sh`
- `#!/usr/bin/env dotnet`

## 当前常见实现模式

### C# file-based script

当前仓库里的复杂脚本普遍使用：

- `Serilog` + `Serilog.Sinks.Console` 输出结构化日志
- 通过 `CallerFilePath` 和 `wakaze.root` 定位仓库根目录
- 从 `vendors/*.json` 读取版本源或构建配置
- 在需要时引用仓库内共享项目，例如 `Kawayi.Wakaze.IO`、`Kawayi.Wakaze.Process`

当前脚本里的常见日志模板如下：

```csharp
#:package Serilog
#:package Serilog.Sinks.Console
using Serilog;

using var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Log.Logger = log;
Log.Information("Processing {Path}", path);
Log.Error(ex, "Unhandled error while running script.");
```

### Shell script

- 适用于非常短、逻辑简单、外部依赖少的操作
- 当前 `eng/scripts/updateNugetLockFiles` 就是 `sh` 脚本

## 编写建议

- 先判断脚本属于工程脚本还是运维脚本，再决定目录
- 先判断逻辑是否足够简单；只有简单到 shell 更清晰时才用 `sh`
- 需要参数校验、结构化日志、仓库根定位、平台分支或 JSON 配置读取时，优先使用 C# file-based script
- 脚本若依赖仓库内共享能力，优先复用现有项目，而不是在脚本里重复实现
- 需要对外暴露使用契约时，把参数、输入配置、输出目录和平台前置条件写清楚

## 文档同步要求

- 新增、删除或重命名脚本时，更新对应的 `AGENTS.ENG.md` 或 `AGENTS.SCRIPTS.md`
- 变更脚本参数、输入配置、输出目录、平台约束或调用约定时，更新对应文档
- 如果脚本编写约定本身发生变化，再更新本文件
