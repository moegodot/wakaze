namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Represents an exact schema identifier constrained to a compile-time URI scheme definition.
/// </summary>
/// <typeparam name="TScheme">The compile-time scheme definition.</typeparam>
public readonly struct SchemaId<TScheme> : IEquatable<SchemaId<TScheme>>
    where TScheme : ISchemaUriSchemeDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaId{TScheme}"/> struct.
    /// </summary>
    /// <param name="family">The versionless schema family identifier.</param>
    /// <param name="version">The exact schema version.</param>
    /// <exception cref="ArgumentException">Thrown when the schema family does not belong to <typeparamref name="TScheme"/>.</exception>
    public SchemaId(SchemaFamily family, SchemaVersion version)
        : this(new SchemaId(family, version))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaId{TScheme}"/> struct from an untyped schema identifier.
    /// </summary>
    /// <param name="value">The untyped schema identifier.</param>
    /// <exception cref="ArgumentException">Thrown when the schema does not belong to <typeparamref name="TScheme"/>.</exception>
    public SchemaId(SchemaId value)
    {
        Value = EnsureMatchingScheme(value, nameof(value));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaId{TScheme}"/> struct from a versioned schema identifier string.
    /// </summary>
    /// <param name="value">The versioned schema URI string.</param>
    public SchemaId(string value)
        : this(new SchemaId(value))
    {
    }

    /// <summary>
    /// Gets the underlying untyped schema identifier.
    /// </summary>
    public SchemaId Value { get; }

    /// <summary>
    /// Gets the versionless schema family identifier.
    /// </summary>
    public SchemaFamily Family => Value.Family;

    /// <summary>
    /// Gets the exact schema version.
    /// </summary>
    public SchemaVersion Version => Value.Version;

    /// <summary>
    /// Gets the compile-time URI scheme declared by <typeparamref name="TScheme"/>.
    /// </summary>
    public static string DeclaredUriScheme => TScheme.UriScheme;

    /// <summary>
    /// Determines whether the current schema equals another schema.
    /// </summary>
    /// <param name="other">The other schema.</param>
    /// <returns><see langword="true"/> when both schemas are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(SchemaId<TScheme> other)
    {
        return Value.Equals(other.Value);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current schema.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when the object is an equal schema; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is SchemaId<TScheme> other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current schema.
    /// </summary>
    /// <returns>A hash code for the current schema.</returns>
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// Returns the canonical versioned URI text for the current schema.
    /// </summary>
    /// <returns>The canonical versioned URI text.</returns>
    public override string ToString()
    {
        return Value.ToString();
    }

    /// <summary>
    /// Compares two schemas for equality.
    /// </summary>
    /// <param name="left">The first schema.</param>
    /// <param name="right">The second schema.</param>
    /// <returns><see langword="true"/> when both schemas are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(SchemaId<TScheme> left, SchemaId<TScheme> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two schemas for inequality.
    /// </summary>
    /// <param name="left">The first schema.</param>
    /// <param name="right">The second schema.</param>
    /// <returns><see langword="true"/> when the schemas are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(SchemaId<TScheme> left, SchemaId<TScheme> right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Converts a typed schema identifier to its untyped representation.
    /// </summary>
    /// <param name="schema">The typed schema identifier.</param>
    public static implicit operator SchemaId(SchemaId<TScheme> schema)
    {
        return schema.Value;
    }

    /// <summary>
    /// Converts an untyped schema identifier to a typed representation.
    /// </summary>
    /// <param name="schema">The untyped schema identifier.</param>
    /// <exception cref="ArgumentException">Thrown when the schema does not belong to <typeparamref name="TScheme"/>.</exception>
    public static explicit operator SchemaId<TScheme>(SchemaId schema)
    {
        return new SchemaId<TScheme>(schema);
    }

    /// <summary>
    /// Attempts to parse a typed schema identifier from text.
    /// </summary>
    /// <param name="value">The URI string to parse.</param>
    /// <param name="schema">
    /// When this method returns <see langword="true"/>, contains the parsed schema identifier;
    /// otherwise, the default value.
    /// </param>
    /// <returns><see langword="true"/> when parsing succeeds and the schema belongs to <typeparamref name="TScheme"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? value, out SchemaId<TScheme> schema)
    {
        schema = default;

        if (!SchemaId.TryParse(value, out var parsed) || parsed.Family.Value.Scheme != TScheme.UriScheme) return false;

        schema = new SchemaId<TScheme>(parsed);
        return true;
    }

    private static SchemaId EnsureMatchingScheme(SchemaId value, string paramName)
    {
        if (value.Family.Value.Scheme != TScheme.UriScheme)
            throw new ArgumentException(
                $"The scheme '{value.Family.Value.Scheme}' does not match expected uri scheme '{TScheme.UriScheme}'.",
                paramName);

        return value;
    }
}
