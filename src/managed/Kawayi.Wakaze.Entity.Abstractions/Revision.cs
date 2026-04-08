
namespace Kawayi.Wakaze.Entity.Abstractions;

internal readonly record struct Revision(Guid EntityStoreId, ulong EpochId, ulong RevisionId)
{
}
