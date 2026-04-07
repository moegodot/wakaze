namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// global unique identity on behalf of an actor.
/// Actor can be user, system, plugin or external accessor like AI agent.
/// </summary>
/// <param name="Id">The unique id</param>
public readonly record struct ActorId(Guid Id)
{
    /// <summary>
    /// Generate a new <see cref="ActorId"/>.
    ///
    /// This is the recommended way to generate a new <see cref="ActorId"/>.
    /// </summary>
    /// <returns>A new unique <see cref="ActorId"/></returns>
    public static ActorId GenerateNew()
    {
        return new ActorId(Guid.CreateVersion7());
    }
}
