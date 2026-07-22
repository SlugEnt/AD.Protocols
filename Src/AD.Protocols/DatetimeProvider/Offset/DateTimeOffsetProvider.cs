namespace SlugEnt.AD.Protocols.DatetimeProvider;

/// <summary>
///     Provides the current date and time as a DateTimeOffset.  Enables the overriding of the
/// System DateTimeOffset.Now and UtcNow properties for testing purposes.  Classes however must integrate it.
/// </summary>
public class DateTimeOffsetProvider : IDateTimeOffsetProvider
{
    /// <summary>
    ///    Gets the current date and time as a DateTimeOffset
    /// </summary>
    public DateTimeOffset Now => DateTimeOffset.Now;

    /// <summary>
    ///   Gets the current date and time as a DateTimeOffset in UTC
    /// </summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}