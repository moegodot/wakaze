namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// a globally unique revision number
/// </summary>
/// <param name="ContainerId">the container of the object,this should be globally unique and created by <see cref="Guid.CreateVersion7"/></param>
/// <param name="EpochId">this may be not globally unique,and may be not unique for changes</param>
/// <param name="RevisionId">this may be not globally unique,but should be unique between for any changes</param>
public readonly record struct Revision(Guid ContainerId, ulong EpochId, ulong RevisionId)
{
}
