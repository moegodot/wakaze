namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Marks a schema projector method that can project values into a target schema.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ProjectToAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectToAttribute"/> class.
    /// </summary>
    /// <param name="targetSchema">The target schema definition type.</param>
    public ProjectToAttribute(Type targetSchema)
    {
        TargetSchema = targetSchema;
    }

    /// <summary>
    /// Gets the target schema definition type.
    /// </summary>
    public Type TargetSchema { get; }
}
