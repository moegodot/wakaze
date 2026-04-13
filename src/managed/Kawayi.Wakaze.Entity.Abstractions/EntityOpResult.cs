namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Represents the outcome of a conditional entity mutation.
/// </summary>
public readonly record struct EntityOpResult
{
    /// <summary>
    /// Initializes a new <see cref="EntityOpResult"/>.
    /// </summary>
    /// <param name="succeeded">
    /// <see langword="true"/> when the operation succeeded; otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="failure">
    /// The expected failure when <paramref name="succeeded"/> is <see langword="false"/>; otherwise, <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="succeeded"/> is <see langword="false"/> and <paramref name="failure"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="succeeded"/> is <see langword="true"/> and <paramref name="failure"/> is not <see langword="null"/>.
    /// </exception>
    public EntityOpResult(bool succeeded, EntityOpFailure? failure)
    {
        if (succeeded && failure is not null)
        {
            throw new ArgumentException("A successful result cannot carry a failure.", nameof(failure));
        }

        if (!succeeded && failure is null)
        {
            throw new ArgumentNullException(nameof(failure));
        }

        Succeeded = succeeded;
        Failure = failure;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    /// <value><see langword="true"/> when the operation succeeded; otherwise, <see langword="false"/>.</value>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the expected failure when the operation did not succeed.
    /// </summary>
    /// <value>The expected failure when the operation did not succeed; otherwise, <see langword="null"/>.</value>
    public EntityOpFailure? Failure { get; }

    /// <summary>
    /// Gets a successful entity operation result.
    /// </summary>
    /// <value>A successful entity operation result.</value>
    public static EntityOpResult Success { get; } = new(true, null);

    /// <summary>
    /// Creates a failed entity operation result.
    /// </summary>
    /// <param name="failure">The expected failure that prevented the mutation.</param>
    /// <returns>A failed entity operation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="failure"/> is <see langword="null"/>.</exception>
    public static EntityOpResult Failed(EntityOpFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new EntityOpResult(false, failure);
    }
}
