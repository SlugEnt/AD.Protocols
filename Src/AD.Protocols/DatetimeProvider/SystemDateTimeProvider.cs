using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.AD.Protocols.DatetimeProvider;


/// <summary>
/// Provides access to the Date Time object.  This can be used to override the value of DateTime.Now and DateTime.UtcNow
/// for unit testing purposes.
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    /// <summary>
    /// Get the current date time.
    /// </summary>
    public DateTime Now => DateTime.Now;

    /// <summary>
    /// Get the current UTC date time
    /// </summary>
    public DateTime UtcNow => DateTime.UtcNow;
}