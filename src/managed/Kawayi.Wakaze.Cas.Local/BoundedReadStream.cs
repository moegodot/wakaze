namespace Kawayi.Wakaze.Cas.Local;

internal sealed class BoundedReadStream : Stream
{
    private readonly Stream _inner;
    private readonly long _length;
    private readonly long _start;
    private long _position;

    public BoundedReadStream(Stream inner, long length)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        if (!inner.CanRead) throw new ArgumentException("The inner stream must be readable.", nameof(inner));

        if (!inner.CanSeek) throw new ArgumentException("The inner stream must support seeking.", nameof(inner));

        _inner = inner;
        _length = length;
        _start = inner.Position;
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        var available = GetRemainingCount(buffer.Length);
        if (available == 0) return 0;

        var read = _inner.Read(buffer[..available]);
        _position += read;
        return read;
    }

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        var available = GetRemainingCount(buffer.Length);
        if (available == 0) return ValueTask.FromResult(0);

        return ReadCoreAsync(buffer[..available], cancellationToken);
    }

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (target < 0 || target > _length) throw new IOException("Attempted to seek outside the bounded range.");

        _inner.Seek(_start + target, SeekOrigin.Begin);
        _position = target;
        return _position;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        throw new NotSupportedException();
    }

    public override ValueTask DisposeAsync()
    {
        return _inner.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();

        base.Dispose(disposing);
    }

    private int GetRemainingCount(int requestedCount)
    {
        var remaining = _length - _position;
        if (remaining <= 0) return 0;

        return (int)Math.Min(remaining, requestedCount);
    }

    private async ValueTask<int> ReadCoreAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        var read = await _inner.ReadAsync(buffer, cancellationToken);
        _position += read;
        return read;
    }
}
