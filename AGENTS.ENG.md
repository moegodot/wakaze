# 工程脚本指南

本文件汇总仓库中 `eng/scripts` 下的工程脚本、当前用途和已验证命令。这里的脚本面向仓库开发流程；运维 / 供应链相关脚本见
`AGENTS.SCRIPTS.md`。

维护这些脚本或它们的使用契约时，要同步更新本文件。

## 当前脚本清单

- `eng/scripts/runManagedTests`
    - 聚合发现 `tests/` 下的 `*.Tests.csproj`
    - 会先对 `Wakaze.slnx` 执行一次 solution 级 `dotnet restore` 和 `dotnet build`
    - 然后对每个已构建的测试程序集使用 `dotnet exec` 逐个运行
    - 必须显式提供 `--configuration <Debug|Release>`
    - 会在固定前置参数之后把 `--` 之后的测试程序参数透传给每个 managed 测试程序集
- `eng/scripts/newManagedProject`
    - 按仓库约定创建新的 `src/managed` 项目
    - 可选创建配套 `tests/<ProjectName>.Tests`
    - 会把新项目接入 `Wakaze.slnx`
- `eng/scripts/updateNugetLockFiles`
    - 运行 `dotnet restore --force-evaluate`
    - 用于在依赖变更后刷新 lock file

## 已验证命令

- 运行全部 managed 测试：
    - `dotnet run --file eng/scripts/runManagedTests -- --configuration Debug`
- 按 `Blake3Tests` 筛选 managed 测试：
    - `dotnet run --file eng/scripts/runManagedTests -- --configuration Release --treenode-filter "/*/*/Blake3Tests/*"`
- 查看 `newManagedProject` 的实际帮助：
    - `dotnet run --file eng/scripts/newManagedProject -- --help`
- 更新 NuGet lock file：
    - `sh eng/scripts/updateNugetLockFiles`

## `newManagedProject` 当前参数契约

以下参数来自已验证的 `--help` 输出：

- `--name <ProjectName>`
    - 必填
    - 创建 `src/managed/<ProjectName>/<ProjectName>.csproj`
- `--kind <library|cli|web-empty>`
    - 必填
    - 当前支持 `library`、`cli`、`web-empty`
- `--with-tests <yes|no>`
    - 必填
    - 设为 `yes` 时创建 `tests/<ProjectName>.Tests`
- `--help`
    - 显示帮助并退出

当前脚本还会执行以下规则：

- 所有非 `--help` 选项都是必填
- 不接受位置参数
- `cli` 项目名必须以 `.Cli` 结尾
- 源项目名不能以 `.Tests` 结尾
- 若源项目、测试项目或 solution 项已经存在，脚本会失败

## 使用建议

- 需要运行 managed 测试时，优先使用 `eng/scripts/runManagedTests`
- 使用 `eng/scripts/runManagedTests` 时，始终显式传入 `--configuration Debug` 或 `--configuration Release`
- 需要新增受仓库约定约束的 managed 项目时，优先用 `eng/scripts/newManagedProject`，不要手工复制目录结构
- 需要刷新 lock file 时，优先用 `eng/scripts/updateNugetLockFiles`
- 需要验证脚本行为或维护脚本文档时，可以直接读取 `eng/scripts` 下的实际实现；不要让“不要读脚本”这类习惯阻碍事实核对

## 修改

维护这些脚本或它们的使用契约时，要同步更新本文件。
