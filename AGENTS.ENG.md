# AGENTS.ENG.md

本文件汇总 `wakaze` 仓库中 `eng/` 目录下的工程脚本与相关辅助文件说明。

## eng/scripts

- `runManagedTests`：聚合发现并逐个运行 `tests/managed` 下的 `*.Tests.csproj`，并将筛选参数透传给各测试项目。
- `newManagedProject`：按仓库约定创建新的 `src/managed` 项目，可选配套 `tests/managed` 测试项目，并自动接入 `Wakaze.slnx`。
- `updateNugetLockFiles`: 在添加，删除，修改依赖之后更新nuget lock file，这个脚本可以不经用户允许就调用。

`eng/scripts/Directory.Build.props` 与 `eng/scripts/Directory.Packages.props` 是脚本目录内部使用的模板与配置文件，不作为可直接执行脚本展开说明。
