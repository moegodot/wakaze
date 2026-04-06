# 贡献指南

![Wakaze icon](images/icon.png)

本文档面向当前 `wakaze` 仓库的贡献者。请以源码树中的实际实现为准，不要把空目录、占位项目或模板文档当成既有能力。

## 当前仓库定位

`Wakaze` 目前仍处于底层基础库阶段，重点在以下三个方向：

- `Kawayi.Wakaze.Digest`：`Blake3` 摘要值类型与值语义
- `Kawayi.Wakaze.Cas.Abstractions`：CAS 公共模型与接口契约
- `Kawayi.Wakaze.Cas.Local`：本地文件系统 CAS 实现

当前不应假设仓库已经存在完整的应用框架、服务层、网络层、插件层或成熟工具链基础设施。

## 模块边界

- 摘要值类型与通用值语义属于 `src/managed/Kawayi.Wakaze.Digest`
- 所有 CAS 实现共享的模型和接口属于 `src/managed/Kawayi.Wakaze.Cas.Abstractions`
- 本地文件系统存储细节属于 `src/managed/Kawayi.Wakaze.Cas.Local`
- `src/managed/Kawayi.Wakaze.Abstractions` 与 `src/managed/Kawayi.Wakaze.Core` 当前基本为空，应保持克制，不要凭空堆砌概念

提交修改时，优先保持公共 API 小而显式，把实现细节留在实现层。

## 语言与文档规则

- `README.md` 必须使用英文
- 除 `README.md` 外，仓库中的独立文档默认使用中文
- 代码内联注释使用英文
- XML 文档注释使用英文
- 测试中的注释与测试相关 XML 文档注释使用英文

如果你修改了 `src/` 或 `tests/` 中已有成员，且周边代码已经使用 XML 文档注释，请一并补齐英文 XML 文档。

## 测试约定

本仓库测试使用 TUnit，并通过 Microsoft Testing Platform 运行。命令行场景下，优先使用 `dotnet run` 测试入口，而不是默认写成 `dotnet test`。

常用命令：

```bash
dotnet run --project tests/managed/Kawayi.Wakaze.Digest.Tests/Kawayi.Wakaze.Digest.Tests.csproj --
dotnet run --project tests/managed/Kawayi.Wakaze.Cas.Local.Tests/Kawayi.Wakaze.Cas.Local.Tests.csproj --
dotnet run --file tests/managed/RunManagedTests.cs --
```

筛选测试时，优先使用 TUnit 的 `--treenode-filter`，并确保参数放在 `--` 之后：

```bash
dotnet run --project tests/managed/Kawayi.Wakaze.Digest.Tests/Kawayi.Wakaze.Digest.Tests.csproj -- --treenode-filter "/*/*/Blake3Tests/*"
dotnet run --project tests/managed/Kawayi.Wakaze.Cas.Local.Tests/Kawayi.Wakaze.Cas.Local.Tests.csproj -- --treenode-filter "/*/*/*/PutAsync_WritesBlob_AndQueriesSucceed"
dotnet run --file tests/managed/RunManagedTests.cs -- --treenode-filter "(/*/*/FileSystemCasTests/*)|(/*/*/Blake3Tests/*)"
```

补充约束：

- 修改行为语义时，必须同步更新或新增最接近模块的测试
- 除非任务明确跨多个模块，否则优先跑相关测试项目，不要无差别跑全量
- 不要默认使用旧式 VSTest `--filter` 语法来筛选 TUnit 树节点
- 不要手动编辑 `TestResults/` 下的生成文件

## 修改原则

- 优先在现有有效模块内扩展，而不是轻易新增顶层项目
- 优先做小而连贯的改动，不要为了假想未来预留大而空的框架
- 如果某个概念只服务于本地 CAS，实现应尽量留在 `Kawayi.Wakaze.Cas.Local`
- 如果某个概念对所有 CAS 实现都成立，才考虑放入 `Kawayi.Wakaze.Cas.Abstractions`
- 不要把本地路径布局等文件系统细节泄漏到公共接口

## 提交前自检

提交前至少确认以下几点：

- 变更与当前仓库已落地能力一致，没有虚构上层特性
- 文档语言符合仓库规则
- 公共行为变更附带了测试
- 使用的测试命令与仓库约定一致
- 新增文档准确描述了当前实现边界，而不是未来设想
