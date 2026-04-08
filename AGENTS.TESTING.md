# AGENTS.TESTING.md

本文件汇总 `wakaze` 仓库中的测试约定、运行方式与命令模板。测试行为与命令示例以当前源码树和当前测试项目为准。

## 测试框架与运行方式

本仓库测试使用 TUnit，并通过 Microsoft Testing Platform 运行。在命令行场景下，优先使用 `dotnet run --project ... --`，并将测试程序参数放在 `--` 之后。若要一次运行 `tests/managed` 下的全部测试项目，可使用 `tests/managed/RunManagedTests.cs` 这个 file-based C# 入口。

## 当前测试项目

- `tests/managed/Kawayi.Wakaze.Digest.Tests`
- `tests/managed/Kawayi.Wakaze.Cas.Local.Tests`

## 修改代码时

- 在最接近变更模块的测试项目中补充或更新测试
- 除非任务跨越多个模块，否则优先运行相关测试项目，而不是无差别跑全量
- 对行为语义的修改，必须同步更新或新增测试

## 常用命令

- `dotnet run --project tests/managed/Kawayi.Wakaze.Digest.Tests/Kawayi.Wakaze.Digest.Tests.csproj --`
- `dotnet run --project tests/managed/Kawayi.Wakaze.Cas.Local.Tests/Kawayi.Wakaze.Cas.Local.Tests.csproj --`
- `dotnet run --file tests/managed/RunManagedTests.cs --`
- `dotnet run --file tests/managed/RunManagedTests.cs -- --treenode-filter "/*/*/Blake3Tests/*"`

## 过滤建议

- 需要筛选测试时，优先使用 TUnit 的 `--treenode-filter`
- 命令行场景下，优先使用 `dotnet run`，不要默认写成 `dotnet test`
- 使用 `dotnet run` 时，测试程序参数必须放在 `--` 之后
- 不要默认使用旧式 VSTest `--filter` 语法来筛选 TUnit 树节点
- 树节点筛选语法为 `/<Assembly>/<Namespace>/<Class name>/<Test name>`
- 支持 `*` 通配符
- 支持 `=` 精确匹配
- 支持 `!=` 取反
- `&` 表示 AND
- `|` 表示 OR，但必须用括号包裹
- 支持属性筛选，例如 `/*/*/*/*[Category=Value]`
- 不要在本仓库的推荐模板中加入 `-nologo` 之类会被测试可执行程序识别为未知选项的旧式参数
- 推荐模板：`dotnet run --project tests/managed/<Project>/<Project>.csproj -- --treenode-filter "/*/*/ClassName/*"`
- 推荐模板：`dotnet run --project tests/managed/<Project>/<Project>.csproj -- --treenode-filter "/*/*/*/TestName"`
- 推荐模板：`dotnet run --project tests/managed/<Project>/<Project>.csproj -- --treenode-filter "(/*/*/ClassA/*)|(/*/*/ClassB/*)"`
- 推荐模板：`dotnet run --project tests/managed/<Project>/<Project>.csproj -- --treenode-filter "/*/*/*/*[Category=Value]"`
- 即使不加筛选，也优先使用 `dotnet run --project ... --` 这一形式
- 需要聚合运行 `tests/managed` 下所有测试项目时，使用 `dotnet run --file tests/managed/RunManagedTests.cs --`
- 聚合入口会自动发现 `tests/managed` 下的 `*.Tests.csproj`，并将 `--` 之后的参数原样透传给每个测试项目
- 使用 `--treenode-filter` 或 `--filter-uid` 时，未命中的测试项目会按跳过处理，而不是把整个聚合运行标记为失败

## 已验证的当前环境状态

- `dotnet run --project tests/managed/Kawayi.Wakaze.Digest.Tests/Kawayi.Wakaze.Digest.Tests.csproj -- --help` 可正常进入 TUnit 测试程序
- `Kawayi.Wakaze.Digest.Tests` 可通过
- `Kawayi.Wakaze.Cas.Local.Tests` 可通过

## 其他注意事项

不要手动编辑 `TestResults/` 下的生成文件。
