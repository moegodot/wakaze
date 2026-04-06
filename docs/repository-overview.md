# 仓库总览

本文档描述 `wakaze` 仓库在当前提交状态下已经落地的内容与边界，目的是帮助贡献者和阅读者快速判断哪些能力已经存在，哪些区域仍然只是未来空间。

## 当前阶段

`Wakaze` 目前是一个以 .NET 10 为目标的多项目仓库，但现阶段的有效实现仍然集中在底层基础库：

- `Kawayi.Wakaze.Digest`
- `Kawayi.Wakaze.Cas.Abstractions`
- `Kawayi.Wakaze.Cas.Local`

这意味着仓库目前更接近“基础构件集合”，而不是一套完整的应用平台。

## 当前有效项目

### `src/managed/Kawayi.Wakaze.Digest`

- 提供 `Blake3` 摘要值类型
- 关注值相等性、哈希码和基于 `Span` 的字节访问
- 当前不承担完整哈希工作流框架的职责

### `src/managed/Kawayi.Wakaze.Cas.Abstractions`

- 提供 CAS 的公共模型和接口契约
- 当前主要类型包括 `BlobId`、`BlobRange`、`ReadRequest`、`PutResult`、`BlobStat`
- 当前主要接口包括 `ICasReader`、`ICasWriter`、`ICasQuerier`、`ICas`

### `src/managed/Kawayi.Wakaze.Cas.Local`

- 提供基于本地文件系统的 CAS 实现 `FileSystemCas`
- 负责 blob 路径布局、临时文件写入、去重提交与范围读取
- 这些实现细节只属于本地 CAS，不属于公共抽象层

### `tests/managed`

- `Kawayi.Wakaze.Digest.Tests` 覆盖摘要值类型的基本值语义
- `Kawayi.Wakaze.Cas.Local.Tests` 覆盖本地 CAS 的写入、去重、并发写入、范围读取与缺失对象语义
- `RunManagedTests.cs` 是聚合入口，会遍历 `tests/managed` 下的 `*.Tests.csproj`

## 当前 CAS 行为事实

以当前实现和测试为准，本地 CAS 已确认的行为包括：

- blob 按内容摘要寻址，`BlobId` 基于 `Blake3` 值类型建模
- 本地文件存储路径来自摘要十六进制字符串，并按前两级前缀做目录分片
- `PutAsync` 会把输入流内容写入临时文件并计算摘要，然后以摘要路径提交
- 相同内容重复写入会去重，不会生成多个最终 blob 文件
- 并发写入相同内容时，调用方仍会得到相同的 `BlobId`
- `OpenReadAsync` 支持整 blob 读取和基于 `BlobRange` 的范围读取
- `StatAsync` 对缺失对象返回 `null`
- `OpenReadAsync` 对缺失对象抛出 `FileNotFoundException`

## 目录状态说明

当前源码树中，以下目录或项目不应被解读为既有能力：

- `src/managed/Kawayi.Wakaze.Abstractions`：当前基本为空
- `src/managed/Kawayi.Wakaze.Core`：当前基本为空
- `samples`：当前为空
- `benchmarks`：当前为空
- `src/native`：当前为空

这些区域目前只能视为未来扩展空间，不能在设计、文档或代码中被当成稳定依赖。

## 开发与测试入口

当前仓库推荐的测试入口是 `dotnet run`：

```bash
dotnet run --project tests/managed/Kawayi.Wakaze.Digest.Tests/Kawayi.Wakaze.Digest.Tests.csproj --
dotnet run --project tests/managed/Kawayi.Wakaze.Cas.Local.Tests/Kawayi.Wakaze.Cas.Local.Tests.csproj --
dotnet run --file tests/managed/RunManagedTests.cs --
```

需要筛选测试时，优先使用 TUnit 的 `--treenode-filter`，并把参数放在 `--` 之后。

## 文档使用原则

阅读或编写本仓库文档时，建议始终遵循两条原则：

- 文档必须以当前源码实现为真相来源
- 当“最小清晰实现”与“遥远未来预留设计”冲突时，优先记录当前已经证明需要的方案
