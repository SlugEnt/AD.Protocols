namespace SlugEnt.AD.Protocols.DatetimeProvider;

/// <summary>
/// Interface for DateTime Provider, which enables testing with mock DateTime values for classes that integrate it.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    DateTime Now { get; }
    }
