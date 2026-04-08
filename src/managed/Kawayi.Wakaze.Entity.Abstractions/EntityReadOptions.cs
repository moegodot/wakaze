
namespace Kawayi.Wakaze.Entity.Abstractions;

public readonly record struct EntityReadOptions(
    bool IncludeDeleted = false);
