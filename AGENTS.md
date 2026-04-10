# AGENTS.md

本文件面向在本仓库中工作的编码代理与贡献者。它参考同类仓库协作文档的结构，但内容以当前 `wakaze` 仓库的实际实现为准。

## 项目背景

- 产品名：`Wakaze`
- 仓库类型：C# / .NET 10 多项目仓库
- 当前重点：内容寻址存储（CAS）抽象、本地文件系统 CAS 实现、BLAKE3 摘要值类型
- 当前仓库阶段：底层基础库阶段，范围较小，尚未发展出更高层的完整应用框架

将当前源码树视为实现真相来源，不要把占位文档当作现有能力说明。

## 当前仓库状态

当前仓库已经落地的核心内容主要集中在以下几个模块：

- `src/managed/Kawayi.Wakaze.Digest` 提供 `Blake3` 摘要值类型与其值语义
- `src/managed/Kawayi.Wakaze.Cas.Abstractions` 提供 CAS 相关公共契约，包括 `BlobId`、`BlobRange`、`ReadRequest`、
  `PutResult`、`BlobStat` 以及 `ICas*` 接口
- `src/managed/Kawayi.Wakaze.Cas.Local` 提供基于本地文件系统的 CAS 实现，包括 blob 路径策略、临时文件写入与范围读取支持
- `tests/managed/Kawayi.Wakaze.Digest.Tests` 覆盖摘要值类型的基本行为
- `tests/managed/Kawayi.Wakaze.Cas.Local.Tests` 覆盖本地 CAS 的写入、去重、并发写入、范围读取与缺失对象语义

当前也存在尚未形成有效实现的区域：

- `src/managed/Kawayi.Wakaze.Abstractions` 目前基本为空，可视为占位区
- `src/managed/Kawayi.Wakaze.Core` 目前基本为空，可视为占位区
- `README.md` 与 `CONTRIBUTING.md` 当前仍主要是占位内容，不应被视为实现真相

不要凭空假设尚未落地的上层框架、服务层、网络层、插件层或工具链基础设施已经存在。

## 架构方向

除非任务明确要求调整边界，否则遵循以下职责划分：

- 摘要值类型与其通用值语义属于 `Kawayi.Wakaze.Digest`
- CAS 公共模型与读取 / 写入 / 查询契约属于 `Kawayi.Wakaze.Cas.Abstractions`
- 本地文件系统存储细节属于 `Kawayi.Wakaze.Cas.Local`
- 未落地的新模块只能被视为未来空间，不能在当前任务中被当成稳定依赖

更具体地说：

- 不要把文件系统实现细节泄漏到抽象层
- 不要把本地路径布局策略硬编码进公共接口契约
- 如果某个类型只服务于本地 CAS 实现，它通常应留在 `Kawayi.Wakaze.Cas.Local`
- 如果某个概念对所有 CAS 实现都成立，它更可能属于 `Kawayi.Wakaze.Cas.Abstractions`

## Digest 指南

`Kawayi.Wakaze.Digest` 目前承担的是值类型建模责任，而不是完整的哈希工作流框架。

- 保持 `Blake3` 作为紧凑、明确的摘要值类型
- 优先维护值相等性、哈希码、Span 转换等底层语义的一致性
- 没有明确需求时，不要在该模块中扩展与当前仓库无关的大型摘要工具集

## CAS 指南

当前仓库的 CAS 设计已经形成了清晰的“抽象 + 本地实现”边界。

- 公共调用方应优先面向 `ICas`、`ICasReader`、`ICasWriter`、`ICasQuerier`
- `BlobId`、`BlobRange`、`ReadRequest`、`PutResult`、`BlobStat` 等模型应保持小而清晰
- 本地实现中的文件布局、临时文件提交、去重策略与范围读取细节应保留在 `Kawayi.Wakaze.Cas.Local`
- 没有明确调用方需求时，不要过早引入额外的存储后端抽象层或注册框架

如果任务涉及新增 CAS 实现：

- 先复用现有抽象契约
- 仅在现有契约确实不足时才扩展公共接口
- 新增公共 API 时保持最小、显式、可测试

## 语言策略

本仓库对不同类型内容使用不同语言策略，必须严格区分：

- 代码内联注释使用英文
- XML 文档注释使用英文
- XML 中的异常说明、参数说明、返回值说明使用英文
- 测试中的注释与测试相关 XML 文档注释使用英文
- 除 `README.md` 外，仓库中的独立文档默认使用中文，例如 `AGENTS.md`、未来的 `cas.md`、`CONTRIBUTING.md`
- `README.md` 必须保持英文

补充约束：

- 当你修改 `src/` 或 `tests/` 中已有成员，若周边代码已使用 XML 文档注释，新增或补齐的 XML 注释必须写英文
- 不要把中文写进代码注释、XML 注释或异常契约说明
- 代码标识符、命令、路径、项目名等技术符号保持原样

## 实务编辑规则

- 优先在现有有效模块内扩展，而不是轻易新增顶层项目
- 保持公共 API 小而显式
- 优先做小而连贯的改动，不要为了假想未来场景引入大而空的框架
- 修改源码时，若相关成员已经有文档风格约束，应同步补齐英文 XML 文档
- 对于 `Kawayi.Wakaze.Abstractions` 与 `Kawayi.Wakaze.Core` 这类当前占位区域，优先保持克制，避免凭空堆砌概念

## 当出现权衡时

当“当前最小可用实现”与“面向遥远未来的预留设计”之间出现张力时，优先选择面向遥远未来的预留设计。

当“本地实现细节”与“抽象层公共语义”之间出现张力时，优先把细节留在实现层，把公共边界保持干净。

## 其他有用的文件

`AGENTS.ENG.md`中枚举出了工程脚本和使用说明。在功能重合的时候，优先运行脚本而非自己运行命令；
`AGENTS.TESTING.md`中有测试相关约定、命令模板与筛选规则。

做架构规划而非修改项目的时候无需读取上述文件。
