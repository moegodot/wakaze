# AGENTS.ENG.md

本文件汇总 `wakaze` 仓库中 `eng/` 目录下的工程脚本与相关辅助文件说明。

从仓库根目录执行这些脚本，并优先使用仓库内脚本而不是手写等价命令。

- `runManagedTests`：聚合发现并逐个运行 `tests/managed` 下的 `*.Tests.csproj`，并将筛选参数透传给各测试项目。
    - 运行全部 managed 测试：`dotnet run --file eng/scripts/runManagedTests --`
    - 按筛选条件运行 managed 测试：
      `dotnet run --file eng/scripts/runManagedTests -- --treenode-filter "/*/*/Blake3Tests/*"`
- `newManagedProject`：按仓库约定创建新的 `src/managed` 项目，可选配套 `tests/managed` 测试项目，并自动接入`Wakaze.slnx`。
    - 创建新的 managed 项目：
      `dotnet run --file eng/scripts/newManagedProject -- --name Kawayi.Wakaze.Example --kind library --with-tests yes`
- `updateNugetLockFiles`: 在添加，删除，修改依赖之后更新nuget lock file，这个脚本可以不经用户允许就调用。
    - 更新 NuGet lock files：`sh eng/scripts/updateNugetLockFiles`
