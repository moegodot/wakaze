# Wakaze

![Wakaze banner](images/readme_image_1.jpg)

Wakaze is a small .NET 10 repository focused on low-level content-addressed storage building blocks.

At the current stage, the repository implements:

- A compact `Blake3` digest value type with value semantics
- CAS abstractions such as `BlobId`, `BlobRange`, `ReadRequest`, `PutResult`, and `ICas*`
- A local file system CAS implementation with deduplicated writes and range reads

This repository is not yet a full application framework. Empty or placeholder areas in the tree should not be read as shipped features.

## Current Capabilities

### `Kawayi.Wakaze.Digest`

- Models a fixed 32-byte BLAKE3 digest value
- Provides equality, hash code generation, and span-based access to the digest bytes
- Does not implement a general-purpose hashing workflow API

### `Kawayi.Wakaze.Cas.Abstractions`

- Defines the public CAS contract surface
- Keeps the shared model intentionally small and explicit
- Avoids leaking file system layout details into the abstraction boundary

### `Kawayi.Wakaze.Cas.Local`

- Stores blobs on the local file system under digest-derived paths
- Deduplicates repeated writes of identical content
- Supports full reads and range reads
- Returns `null` for missing metadata queries and throws `FileNotFoundException` for missing read requests

## Project Layout

- `src/managed/Kawayi.Wakaze.Digest`: digest value type
- `src/managed/Kawayi.Wakaze.Cas.Abstractions`: shared CAS models and interfaces
- `src/managed/Kawayi.Wakaze.Cas.Local`: local file system CAS implementation
- `tests/managed/Kawayi.Wakaze.Digest.Tests`: digest behavior tests
- `tests/managed/Kawayi.Wakaze.Cas.Local.Tests`: local CAS behavior tests
- `tests/managed/RunManagedTests.cs`: aggregate managed test runner
- `src/managed/Kawayi.Wakaze.Abstractions`: currently a placeholder
- `src/managed/Kawayi.Wakaze.Core`: currently a placeholder
- `samples`, `benchmarks`, `src/native`: currently empty in this repository state

## Requirements

- .NET 10 SDK
- A local development environment able to run `dotnet run`

## Test Commands

Run the digest tests:

```bash
dotnet run --project tests/managed/Kawayi.Wakaze.Digest.Tests/Kawayi.Wakaze.Digest.Tests.csproj --
```

Run the local CAS tests:

```bash
dotnet run --project tests/managed/Kawayi.Wakaze.Cas.Local.Tests/Kawayi.Wakaze.Cas.Local.Tests.csproj --
```

Run all managed test projects:

```bash
dotnet run --file tests/managed/RunManagedTests.cs --
```

Run a filtered test selection with TUnit tree filters:

```bash
dotnet run --project tests/managed/Kawayi.Wakaze.Digest.Tests/Kawayi.Wakaze.Digest.Tests.csproj -- --treenode-filter "/*/*/Blake3Tests/*"
```

## Documentation

- [Contributing guide](CONTRIBUTING.md)
- [Repository overview](docs/repository-overview.md)
- [Agent-specific repository guidance](AGENTS.md)

## Non-goals

The current repository state does not provide:

- A higher-level application framework
- A networked CAS service
- A plugin system
- Finished samples or benchmark suites
- Native components under active implementation
