using Microsoft.Identity.Client.Kerberos;
using SlugEnt.FluentResults;
using System.Globalization;

namespace SlugEnt.AD.Protocols;

/// <summary>
/// Provides some common functions for working with Active Directory
/// </summary>
public static class ADFunctions
{
    /// <summary>
    ///     Converts an AD ResultsProperty Date DA into an official DateTime Object.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static DateTime GetDateTime_From_ResultProperty(long value)
    {
        DateTime dateTime = DateTime.FromFileTimeUtc(value);
        return dateTime;
    }


    /// <summary>
    ///     Some Active Directory LDAP Date Properties store date times in a unique format that includes a
    ///     TimeZone component.  This converts to proper date time.
    /// </summary>
    /// <param name="ldapValue"></param>
    /// <returns></returns>
    public static DateTimeOffset GetDateTime_FromLDAPProperty(string ldapValue)
    {
        string temp = ldapValue;
        if (temp.EndsWith(".0Z"))
        {
            temp = temp[..^3];
        }


        DateTimeOffset d1 = DateTime.ParseExact(temp, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        return d1;
    }


    /// <summary>
    ///     Takes a long number and converts to a date time value
    /// </summary>
    /// <param name="ldapValue"></param>
    /// <returns></returns>
    public static DateTimeOffset GetDateTime_FromLDAPPropertyLong(string ldapValue)
    {
        long           lvalue = long.Parse(ldapValue);
        DateTimeOffset d1     = DateTime.FromFileTimeUtc(lvalue);
        return d1;
    }



    /// <summary>
    /// Convert an Active Directory TimeSpan stored object back into a TimeSpan.
    /// </summary>
    /// <param name="ldapValue"></param>
    /// <returns></returns>
    public static Result<TimeSpan> GetTimeSpan_FromAD (string ldapValue)
    {
        // This is a special case for the msDS-MaximumPasswordAge attribute
        // which is stored as a long value representing ticks.
        if (long.TryParse(ldapValue, out long ticks))
        {
            // No idea why they are stored as negative values, but they are...
            ticks = Math.Abs(ticks); // Ensure ticks are positive
            TimeSpan ts = TimeSpan.FromTicks(ticks);
            return  Result.Ok(ts);
        }

        // If parsing fails, return a zero TimeSpan
        return  Result.Fail(new Error($"Active Directory value [ {ldapValue} ] in not able to be converted into a valid TimeSpan"));
    }


    public static long SetAD_TimeSpan (TimeSpan timeSpan)
    {
        // Convert TimeSpan to ticks and ensure it's positive
        long ticks = Math.Abs(timeSpan.Ticks);
        ticks = ticks * -1; // Active Directory stores these as negative values
        return ticks;
    }
}