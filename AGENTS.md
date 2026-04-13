# AGENTS.md

本文件面向在 `wakaze` 仓库中工作的编码代理与贡献者。仓库中的源码树、项目文件、脚本和已验证命令是当前实现真相来源；README、贡献文档和其他说明性文档只作为补充，不应覆盖源码事实。

## 项目背景

- 产品名：`Wakaze`
- 仓库类型：C# / .NET 10 多项目仓库
- 当前仓库同时包含运行时库、测试项目、Roslyn Analyzer / Source Generator、工程脚本和运维脚本
- 当前尚未形成完整终端产品；现阶段主要是底层模型、存储、数据库、实体、语义和工具链基础设施

不要假设仓库已经存在尚未落地的服务层、网络层、插件系统或完整 CLI 工作流。遇到能力边界时，以当前项目与测试为准。

## 当前实现范围

当前源码树中已经有明确实现的区域包括：

- `src/Kawayi.Wakaze.Abstractions`
    - 提供 schema / typed-object 相关抽象，例如 `SchemaId`、`SchemaFamily`、`ISchemaDefinition*`、`ISchemaProjector`、
      `ITypedObject`
- `src/Kawayi.Wakaze.Digest`
    - 提供 `Blake3` 摘要值类型与值语义
- `src/Kawayi.Wakaze.Cas.Abstractions`
    - 提供 CAS 公共模型与接口，例如 `BlobId`、`BlobRange`、`ReadRequest`、`PutResult`、`BlobStat`、`ICas*`
- `src/Kawayi.Wakaze.Cas.Local`
    - 提供基于本地文件系统的 CAS 实现
- `src/Kawayi.Wakaze.Db.Abstractions`
    - 提供数据库描述、连接、健康检查、维护、转储 / 恢复等抽象
- `src/Kawayi.Wakaze.Db.PostgreSql`
    - 提供 PostgreSQL provider、数据库对象和 DI 扩展
- `src/Kawayi.Wakaze.Entity.Abstractions`
    - 提供实体 current visible state、逻辑引用、修订、快照、历史访问、原子读写上下文和实体存储抽象
- `src/Kawayi.Wakaze.Entity.Sqlite`
    - 提供基于 SQLite / EF Core 的实体存储实现
- `src/Kawayi.Wakaze.Semantics.Abstractions`
    - 提供语义读取、投影、提交、索引和会话相关抽象
- `src/Kawayi.Wakaze.IO`
    - 提供目录树复制、递归删除、仓库根定位等共享文件系统工具
- `src/Kawayi.Wakaze.Process`
    - 提供子进程启动、输出捕获和退出码处理等共享进程工具
- `src/Kawayi.Wakaze.Analyzer`
    - 提供 Roslyn Analyzer
- `src/Kawayi.Wakaze.Generator`
    - 提供 Roslyn Source Generator
- `src/Kawayi.Wakaze.Cli`
    - 当前存在可构建 CLI 入口，但实现仍然非常轻，当前行为仅输出 `Hello, World!`

当前接近占位区的主要是：

- `src/Kawayi.Wakaze.Core`
    - 目前只有项目骨架和依赖，尚无实际源码成员

不要再把 `Kawayi.Wakaze.Abstractions` 视为“基本为空”；它已经是当前 schema 抽象的重要组成部分。

## 架构边界

除非任务明确要求调整边界，否则遵循以下职责划分：

- Schema 标识、兼容性、注册、投影和 typed-object 模型属于 `Kawayi.Wakaze.Abstractions`
- 摘要值类型与其底层值语义属于 `Kawayi.Wakaze.Digest`
- CAS 公共模型与读取 / 写入 / 查询契约属于 `Kawayi.Wakaze.Cas.Abstractions`
- 本地文件系统 blob 布局、临时文件提交、范围读取等细节属于 `Kawayi.Wakaze.Cas.Local`
- 数据库无关的 provider / connection / dump / restore / maintenance 抽象属于 `Kawayi.Wakaze.Db.Abstractions`
- PostgreSQL 特定连接串、工具调用和依赖注入扩展属于 `Kawayi.Wakaze.Db.PostgreSql`
- 实体 current visible state、revision、逻辑引用、快照、历史读取和实体存储抽象属于 `Kawayi.Wakaze.Entity.Abstractions`
- SQLite / EF Core 持久化细节属于 `Kawayi.Wakaze.Entity.Sqlite`
- 语义层通用抽象属于 `Kawayi.Wakaze.Semantics.Abstractions`
- 与具体领域无关的文件系统工具属于 `Kawayi.Wakaze.IO`
- 与具体领域无关的进程执行工具属于 `Kawayi.Wakaze.Process`
- Roslyn 诊断规则属于 `Kawayi.Wakaze.Analyzer`
- Roslyn 代码生成逻辑属于 `Kawayi.Wakaze.Generator`
- 当前 CLI 行为仍然很轻，不要把尚未实现的命令面或工作流写成既有能力

更具体地说：

- 不要把本地文件布局、临时路径或具体数据库工具细节泄漏到抽象层
- 不要把 PostgreSQL、SQLite 或测试编排逻辑下沉到 `Kawayi.Wakaze.IO` 或 `Kawayi.Wakaze.Process`
- 如果一个概念只对某个 provider / backend 成立，应优先留在对应实现层
- 只有当某个概念已被多个实现共同需要时，才提升到公共抽象
- 优先扩展现有有效模块，不要为了假想未来场景新增大而空的顶层项目或注册框架

## 语言策略

本仓库对不同类型内容使用不同语言策略，必须严格区分：

- 代码内联注释使用英文
- XML 文档注释使用英文
- XML 中的异常说明、参数说明、返回值说明使用英文
- 测试中的注释与测试相关 XML 文档注释使用英文
- 除 `README.md` 外，仓库中的独立文档默认使用中文，例如 `AGENTS.md`、`AGENTS.ENG.md`、`AGENTS.TESTING.md`
- `README.md` 必须保持英文

补充约束：

- 当你修改 `src/` 或 `tests/` 中已有成员，若周边代码已使用 XML 文档注释，新增或补齐的 XML 注释必须写英文
- 不要把中文写进代码注释、XML 注释或异常契约说明
- 代码标识符、命令、路径、项目名等技术符号保持原样

## 实务编辑规则

- 优先做小而连贯的改动
- 公共 API 保持小而显式
- 先复用现有抽象和共享工具，再考虑新增层次
- 修改行为语义时，同步补齐最接近模块的测试
- 遇到目录树复制、递归删除、仓库根定位、通用进程启动等重复逻辑时，优先复用 `Kawayi.Wakaze.IO` 和 `Kawayi.Wakaze.Process`
- 做文档修订时，先验证一句话和一个命令是否仍然与当前仓库一致，再写入文档
- 当“当前已证明需要的清晰实现”和“遥远未来的预留设计”冲突时，优先记录和实现当前已被证明需要的方案

## 文档同步要求

- 改动项目结构、模块边界、公共职责划分或仓库架构说明时，要同步更新 `AGENTS.md`
- 改动测试项目、测试入口、测试筛选方式或测试前置条件时，要根据情况更新 `AGENTS.TESTING.md`
- 改动 `eng/scripts` 或其使用契约时，要根据情况更新 `AGENTS.ENG.md`
- 改动 `scripts` 或其使用契约时，要根据情况更新 `AGENTS.SCRIPTS.md`
- 改动脚本编写约定时，要根据情况更新 `AGENTS.SCRIPT.WRITING.md`

## 其他有用的文件

- `AGENTS.ENG.md`：`eng/scripts` 工程脚本与使用说明
- `AGENTS.SCRIPTS.md`：`scripts` 运维 / 供应链脚本与当前已验证的只读检查方式
- `AGENTS.SCRIPT.WRITING.md`：脚本编写约定
- `AGENTS.TESTING.md`：测试项目、运行方式、筛选规则与当前已验证行为

做架构规划之前可以先读本文件；做脚本、测试或工程操作之前，再按需要读对应的 `AGENTS.*.md`。

## 修改

修改项目结构或项目的使用契约时，要同步更新本文件。
