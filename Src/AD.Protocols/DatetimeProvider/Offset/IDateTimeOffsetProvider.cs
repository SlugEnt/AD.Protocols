namespace SlugEnt.AD.Protocols.DatetimeProvider;

/// <summary>
///     Provides an Interface for being able to override DateTime for testing
/// </summary>
public interface IDateTimeOffsetProvider
{
    /// <summary>
    /// Gets Current Date and Time
    /// </summary>
    DateTimeOffset Now { get; }

    /// <summary>
    /// Gets Current Date and Time in UTC
    /// </summary>
    DateTimeOffset UtcNow { get; }
}