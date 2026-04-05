namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Kind of <see cref="BlobRange"/>
/// </summary>
public enum BlobRangeKind : byte
{
    /// <summary>
    /// All the data
    /// </summary>
    Full = 0,

    /// <summary>
    /// Data bewteen [A,B)
    /// </summary>
    Slice = 1,

    /// <summary>
    /// Data bewteen [A,+∞)
    /// </summary>
    From = 2
}

/// <summary>
/// Range for CAS system
/// </summary>
/// <param name="Kind">The range's kind</param>
/// <param name="A">A index</param>
/// <param name="B">B index</param>
public readonly record struct BlobRange(
    BlobRangeKind Kind,
    ulong A,
    ulong B)
{
    public static BlobRange Full => new(BlobRangeKind.Full, 0, 0);

    public static BlobRange Slice(ulong offset, ulong length)
    {
        return new BlobRange(BlobRangeKind.Slice, offset, length);
    }

    public static BlobRange From(ulong offset)
    {
        return new BlobRange(BlobRangeKind.From, offset, 0);
    }

    public ResolvedBlobRange Resolve(ulong blobLength)
    {
        return Kind switch
        {
            BlobRangeKind.Full => new ResolvedBlobRange(0, blobLength),

            BlobRangeKind.Slice => A <= blobLength && B <= blobLength - A
                ? new ResolvedBlobRange(A, B)
                : throw new ArgumentOutOfRangeException(nameof(BlobRange)),

            BlobRangeKind.From => A <= blobLength
                ? new ResolvedBlobRange(A, blobLength - A)
                : throw new ArgumentOutOfRangeException(nameof(BlobRange)),

            _ => throw new InvalidOperationException("Unknown BlobRangeKind.")
        };
    }
}

/// <summary>
/// Resolved range, with determined data length.
/// </summary>
/// <param name="Offset">Start offset of range</param>
/// <param name="Length">Range's length</param>
public readonly record struct ResolvedBlobRange(
    ulong Offset,
    ulong Length);
