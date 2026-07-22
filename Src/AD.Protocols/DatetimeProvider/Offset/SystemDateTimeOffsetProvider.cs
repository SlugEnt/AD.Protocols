namespace SlugEnt.AD.Protocols.DatetimeProvider;

/// <summary>
///     Provides the .Net Core DateTime Standard Methods
/// </summary>
public class SystemDateTimeOffsetProvider : IDateTimeOffsetProvider
{
    /// <summary>
    ///    Gets the current date and time
    /// </summary>
    public DateTimeOffset Now => DateTimeOffset.Now;

    /// <summary>
    ///   Gets the current date and time in UTC
    /// </summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}