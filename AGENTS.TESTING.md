# 测试指南

本文件汇总 `wakaze` 仓库中的测试项目、运行方式、筛选规则和当前已验证的命令。修改测试项目、测试入口、筛选方式或测试前置条件时，要同步更新本文件。

## 测试框架与运行方式

- managed 测试当前使用 TUnit，并通过 Microsoft Testing Platform 运行
- 命令行场景下，优先使用 `dotnet run --project ... --` 或 `dotnet run --file ... --`
- 测试程序参数必须放在 `--` 之后
- `eng/scripts/runManagedTests` 只聚合 `tests/managed` 下的测试项目
- Analyzer / Generator 自测项目不在 `eng/scripts/runManagedTests` 的聚合范围内

## 当前测试项目

`tests/managed` 下当前有 8 个测试项目：

- `tests/managed/Kawayi.Wakaze.Abstractions.Tests`
- `tests/managed/Kawayi.Wakaze.Cas.Local.Tests`
- `tests/managed/Kawayi.Wakaze.Db.PostgreSql.Tests`
- `tests/managed/Kawayi.Wakaze.Digest.Tests`
- `tests/managed/Kawayi.Wakaze.Entity.Sqlite.Tests`
- `tests/managed/Kawayi.Wakaze.IO.Tests`
- `tests/managed/Kawayi.Wakaze.Process.Tests`
- `tests/managed/Kawayi.Wakaze.Semantics.Abstractions.Tests`

Roslyn 相关自测项目单独位于 `src/managed`：

- `src/managed/Kawayi.Wakaze.Analyzer/Kawayi.Wakaze.Analyzer.Tests`
- `src/managed/Kawayi.Wakaze.Generator/Kawayi.Wakaze.Generator.Tests`

## 当前已验证命令

以下命令已经在当前工作区执行并验证：

- 查看单个 managed 测试程序帮助：
    - `dotnet run --project tests/managed/Kawayi.Wakaze.Digest.Tests/Kawayi.Wakaze.Digest.Tests.csproj -- --help`
- 运行全部 `tests/managed`：
    - `dotnet run --file eng/scripts/runManagedTests --`
- 运行带树节点筛选的 `tests/managed`：
    - `dotnet run --file eng/scripts/runManagedTests -- --treenode-filter "/*/*/Blake3Tests/*"`
- 查看 PostgreSQL 测试发现结果：
    -
    `dotnet run --project tests/managed/Kawayi.Wakaze.Db.PostgreSql.Tests/Kawayi.Wakaze.Db.PostgreSql.Tests.csproj -- --list-tests`
- 查看 Analyzer 自测发现结果：
    -
    `dotnet run --project src/managed/Kawayi.Wakaze.Analyzer/Kawayi.Wakaze.Analyzer.Tests/Kawayi.Wakaze.Analyzer.Tests.csproj -- --list-tests`
- 查看 Generator 自测发现结果：
    -
    `dotnet run --project src/managed/Kawayi.Wakaze.Generator/Kawayi.Wakaze.Generator.Tests/Kawayi.Wakaze.Generator.Tests.csproj -- --list-tests`

## 当前已验证行为

- `dotnet run --file eng/scripts/runManagedTests --` 在当前工作区可通过，并覆盖 `tests/managed` 下全部 8 个测试项目
- `--treenode-filter` 在当前仓库可工作
- 通过 `eng/scripts/runManagedTests` 传入筛选参数时，未命中的测试项目按跳过处理，不会把整个聚合运行标记为失败
- PostgreSQL 测试当前可通过，因为工作区中已经存在 `vendors/install/postgresql`
- `tests/managed/Kawayi.Wakaze.Db.PostgreSql.Tests` 包含 provider 行为测试和 PostgreSQL 安装 / 生命周期测试
- Analyzer / Generator 测试存在且可被单独发现，但需要直接运行对应测试项目

## 过滤与选择建议

- 需要缩小范围时，优先使用 TUnit 的 `--treenode-filter`
- 不要默认写成旧式 VSTest `--filter`
- 聚合运行 `tests/managed` 时，优先用 `eng/scripts/runManagedTests`
- 修改某个模块时，先运行最接近的测试项目；只有当改动跨多个模块或需要回归时，才跑全部 `tests/managed`
- 修改 Analyzer / Generator 时，不要误以为 `eng/scripts/runManagedTests` 会覆盖它们

## 其他注意事项

- 不要手动编辑 `TestResults/` 下的生成文件
- 如果测试是否可运行取决于 vendored 产物、平台或脚本前置条件，要把这些前置条件写回文档

## 修改

维护这些测试或它们的使用契约时，要同步更新本文件。
