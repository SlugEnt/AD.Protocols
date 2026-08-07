namespace AD.Protocols.ADObjects;

/// <summary>
/// Used for working with Active Directory Expiration Fields.
/// <para>Expiration fields can have a datetime (UTC) value or a flag value of Expired or Never Expires</para>
/// </summary>
public class ADpExpirationValue
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="ticks"></param>
    public ADpExpirationValue(long ticks)
    {
        SetValue(ticks);
    }
    
    
    /// <summary>
    ///  True if the expiration value is expired, false if it is not expired.  
    /// </summary>
    public bool IsExpired { get; private set; }
    
    /// <summary>
    ///  True if the expiration value is set to never expire, false otherwise.
    /// </summary>
    public bool IsNeverExpires { get; private set; }

    /// <summary>
    /// Sets the value.  If value is DateTime.MaxValue ,
    /// then the IsNeverExpires flag is set to true and the IsExpired flag is set to false.  If the value is less than DateTime.UtcNow, then the
    /// IsExpired flag is set to true and the IsNeverExpires flag is set to false.
    /// </summary>
    public DateTime ExpirationDateTimeUtc
    {
        get;
        set
        {
            field = value;
            
             if (value == DateTime.MaxValue)
             {
                 IsNeverExpires = true;
                 IsExpired = false;
             }
             else
             {
                 IsNeverExpires = false;
                 IsExpired = value < DateTime.UtcNow;
             }
        }
    }
    
    
    /// <summary>
    /// Used to set the value from the AD value.
    /// </summary>
    /// <param name="ticks"></param>
    internal void SetValue (long ticks)
    {
        // 9223372036854775807
        if (ticks == 0 || ticks == long.MaxValue)
        {
            IsNeverExpires        = true;
            IsExpired             = false;
            ExpirationDateTimeUtc = DateTime.MaxValue;
        }
        else
        {
            IsNeverExpires = false;
            ExpirationDateTimeUtc = DateTime.FromFileTimeUtc(ticks);
            IsExpired = ExpirationDateTimeUtc < DateTime.UtcNow;    
        }
    }


    /// <summary>
    /// Sets the expiration value to never expire.
    /// </summary>
    public void SetNeverExpires()
    {
        IsNeverExpires = true;
        IsExpired = false;
        ExpirationDateTimeUtc = DateTime.MaxValue;
    }
}

