namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Defines optional behaviors for entity reads.
/// </summary>
/// <param name="IncludeDeleted">
/// <see langword="true"/> to allow reads to observe deleted entities when the implementation supports it;
/// otherwise, <see langword="false"/> to limit reads to currently visible entities.
/// </param>
public readonly record struct EntityReadOptions(
    bool IncludeDeleted = false);
