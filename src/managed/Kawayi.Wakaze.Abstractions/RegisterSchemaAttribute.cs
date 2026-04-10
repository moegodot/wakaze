namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Marks a schema definition type for source-generated runtime registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RegisterSchemaAttribute : Attribute
{
}
