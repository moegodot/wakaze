# 运维与供应链脚本指南

本文件汇总仓库中 `scripts/` 下的脚本、用途、前置条件和当前已验证的只读检查方式。这里的脚本主要处理外部工具和第三方源码 /
产物；工程流程脚本见 `AGENTS.ENG.md`。

## 当前配置来源

以下配置文件已经被脚本实际使用：

- `vendors/vendors_source.json`
    - 记录 PostgreSQL 源码和 `uv` 发布源信息
- `vendors/pgsql_options.json`
    - 记录 PostgreSQL Meson 配置和 macOS Homebrew 依赖列表

## 当前脚本清单

- `scripts/downloadUv`
    - 下载与当前平台架构匹配的 `uv` 发行包
    - 将产物展开到 `vendors/binary/uv`
    - 读取 `vendors/vendors_source.json`
- `scripts/runUv`
    - 运行仓库内 vendored `uv`
    - 默认查找 `vendors/binary/uv/uv` 或 Windows 下的 `uv.exe`
    - 如果 vendored `uv` 不存在，会提示先运行 `scripts/downloadUv`
- `scripts/downloadPostgresqlSource`
    - 下载 PostgreSQL 源码归档
    - 将源码展开到 `vendors/native/postgresql`
    - 读取 `vendors/vendors_source.json`
- `scripts/buildPostgresql`
    - 使用 `uv run meson ...` 配置、编译并安装 PostgreSQL
    - 读取 `vendors/pgsql_options.json`
    - 依赖 `vendors/native/postgresql`、`pyenv/`、`scripts/runUv`
    - 产物目录为 `vendors/build/postgresql` 和 `vendors/install/postgresql`
    - 在 macOS 上会额外尝试升级配置里列出的 Homebrew formula，并在安装后调用 `scripts/relocatePostgresqlInstall`
- `scripts/relocatePostgresqlInstall`
    - 仅支持 macOS
    - 重写 PostgreSQL 安装树中的 Mach-O 依赖和 install name
    - 依赖 `otool`、`install_name_tool`、`codesign`
    - 读取 `vendors/pgsql_options.json`

## 当前已验证的只读检查命令

以下命令已经在当前工作区执行并验证：

- 验证 vendored `uv` 可运行：
    - `dotnet run --file scripts/runUv -- --version`
- 验证当前 PostgreSQL 安装中的 `postgres`：
    - `vendors/install/postgresql/bin/postgres --version`
- 验证当前 PostgreSQL 安装中的 `psql`：
    - `vendors/install/postgresql/bin/psql --version`

这些命令适合用于确认当前工作区产物是否可用，不会修改仓库跟踪内容。

## 前置条件与边界

- `scripts/downloadUv` 和 `scripts/downloadPostgresqlSource` 会改写 `vendors/` 下的内容
- `scripts/buildPostgresql` 会改写 `vendors/build/postgresql` 与 `vendors/install/postgresql`
- `scripts/relocatePostgresqlInstall` 会改写 `vendors/install/postgresql`
- 文档里不要把上述变更型脚本写成“可随手运行”的示例命令，除非已经在当前任务中实际执行并核验
- `scripts/buildPostgresql` 依赖仓库内 `pyenv` 项目存在且可被 `uv run` 使用
- `scripts/relocatePostgresqlInstall` 只适用于 macOS；非 macOS 环境不应把它写成默认流程

## 使用建议

- 需要确认 vendored 工具是否已安装时，优先用本文件中的只读检查命令
- 需要修改下载源、版本或构建选项时，先核对 `vendors/vendors_source.json` 与 `vendors/pgsql_options.json`
- 需要描述 PostgreSQL 安装行为时，区分“下载源码”“构建安装”“macOS 重定位”三个阶段，不要混成一个模糊步骤

## 修改

维护这些脚本或它们的使用契约时，要同步更新本文件。
