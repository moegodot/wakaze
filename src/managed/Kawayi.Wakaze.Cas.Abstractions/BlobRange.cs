namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Defines how a blob read range should be interpreted.
/// </summary>
public enum BlobRangeKind : byte
{
    /// <summary>
    /// Reads the entire blob.
    /// </summary>
    Full = 0,

    /// <summary>
    /// Reads a fixed-length slice that starts at an offset.
    /// </summary>
    Slice = 1,

    /// <summary>
    /// Reads from an offset to the end of the blob.
    /// </summary>
    From = 2
}

/// <summary>
/// Represents a requested range over a blob.
/// </summary>
/// <param name="Kind">The interpretation of the range values.</param>
/// <param name="A">The first range argument. For <see cref="BlobRangeKind.Slice"/> and <see cref="BlobRangeKind.From"/>, this is the starting offset.</param>
/// <param name="B">The second range argument. For <see cref="BlobRangeKind.Slice"/>, this is the requested length.</param>
public readonly record struct BlobRange(
    BlobRangeKind Kind,
    ulong A,
    ulong B)
{
    /// <summary>
    /// Gets a range that covers the entire blob.
    /// </summary>
    public static BlobRange Full => new(BlobRangeKind.Full, 0, 0);

    /// <summary>
    /// Creates a range that reads a fixed-length slice.
    /// </summary>
    /// <param name="offset">The zero-based start offset within the blob.</param>
    /// <param name="length">The number of bytes to read.</param>
    /// <returns>A range that reads <paramref name="length"/> bytes from <paramref name="offset"/>.</returns>
    public static BlobRange Slice(ulong offset, ulong length)
    {
        return new BlobRange(BlobRangeKind.Slice, offset, length);
    }

    /// <summary>
    /// Creates a range that reads from the specified offset to the end of the blob.
    /// </summary>
    /// <param name="offset">The zero-based start offset within the blob.</param>
    /// <returns>A range that reads from <paramref name="offset"/> to the end of the blob.</returns>
    public static BlobRange From(ulong offset)
    {
        return new BlobRange(BlobRangeKind.From, offset, 0);
    }

    /// <summary>
    /// Resolves the requested range against a concrete blob length.
    /// </summary>
    /// <param name="blobLength">The total length of the blob in bytes.</param>
    /// <returns>The resolved offset and length for the read operation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the requested range exceeds the blob length.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the range kind is not recognized.</exception>
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
/// Represents a concrete blob range after it has been validated against a blob length.
/// </summary>
/// <param name="Offset">The zero-based start offset of the resolved range.</param>
/// <param name="Length">The length of the resolved range in bytes.</param>
public readonly record struct ResolvedBlobRange(
    ulong Offset,
    ulong Length);
