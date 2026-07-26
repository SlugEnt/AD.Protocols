namespace SlugEnt.AD.Protocols;

/// <summary>
///     Enumeration of UserAccountControl flags used in Active Directory
/// </summary>
[Flags]
public enum EnumUserAccountControlFlags
{
#pragma warning disable CS1591                 // Missing XML comment for publicly visible type or member
    SCRIPT                         = 0x0001,   // Script login enabled
    ACCOUNTDISABLE                 = 0x0002,   // Account is disabled
    HOMEDIR_REQUIRED               = 0x0008,   // Home directory required
    LOCKOUT                        = 0x0010,   // Account is locked out
    PASSWD_NOTREQD                 = 0x0020,   // Password not required
    PASSWD_CANT_CHANGE             = 0x0040,   // User can't change password
    ENCRYPTED_TEXT_PWD_ALLOWED     = 0x0080,   // Encrypted text password is allowed
    TEMP_DUPLICATE_ACCOUNT         = 0x0100,   // Local user account
    NORMAL_ACCOUNT                 = 0x0200,   // Normal user account
    INTERDOMAIN_TRUST_ACCOUNT      = 0x0800,   // Interdomain trust account
    WORKSTATION_TRUST_ACCOUNT      = 0x1000,   // Workstation trust account
    SERVER_TRUST_ACCOUNT           = 0x2000,   // Server trust account
    DONT_EXPIRE_PASSWORD           = 0x10000,  // Password never expires
    MNS_LOGON_ACCOUNT              = 0x20000,  // MNS logon account
    SMARTCARD_REQUIRED             = 0x40000,  // Smart card required
    TRUSTED_FOR_DELEGATION         = 0x80000,  // Trusted for delegation
    NOT_DELEGATED                  = 0x100000, // Not trusted for delegation
    USE_DES_KEY_ONLY               = 0x200000, // Use DES key only
    DONT_REQ_PREAUTH               = 0x400000, // Preauth not required
    PASSWORD_EXPIRED               = 0x800000, // Password expired
    TRUSTED_TO_AUTH_FOR_DELEGATION = 0x1000000 // Trusted to authenticate for delegation
#pragma warning restore CS1591                 // Missing XML comment for publicly visible type or member
}


/// <summary>
///     Class to manage Active Directory UserAccountControl attribute
/// </summary>
[Obsolete]
public class UserAccountControlManager
{
    /// <summary>
    ///     Constructor that initializes with existing UAC value
    /// </summary>
    /// <param name="currentUacValue">Current UserAccountControl value from AD</param>
    public UserAccountControlManager(int currentUacValue)
    {
        Value = currentUacValue;
    }


    /// <summary>
    ///     Constructor that initializes with default normal account settings
    /// </summary>
    public UserAccountControlManager()
    {
        Value = (int)EnumUserAccountControlFlags.NORMAL_ACCOUNT;
    }


    /// <summary>
    ///     Common operations as properties
    /// </summary>
    public bool IsEnabled
    {
        get => !HasFlag(EnumUserAccountControlFlags.ACCOUNTDISABLE);
        set
        {
            if (value)
            {
                RemoveFlag(EnumUserAccountControlFlags.ACCOUNTDISABLE);
            }
            else
            {
                AddFlag(EnumUserAccountControlFlags.ACCOUNTDISABLE);
            }
        }
    }


    /// <summary>
    /// Used to get or change the status of the account lock out
    /// </summary>
    public bool IsLocked
    {
        get => HasFlag(EnumUserAccountControlFlags.LOCKOUT);
        set
        {
            if (value)
            {
                AddFlag(EnumUserAccountControlFlags.LOCKOUT);
            }
            else
            {
                RemoveFlag(EnumUserAccountControlFlags.LOCKOUT);
            }
        }
    }


    /// <summary>
    ///     Gets or sets whether the password never expires flag is set
    /// </summary>
    public bool IsPasswordNeverExpires
    {
        get => HasFlag(EnumUserAccountControlFlags.DONT_EXPIRE_PASSWORD);
        set
        {
            if (value)
            {
                AddFlag(EnumUserAccountControlFlags.DONT_EXPIRE_PASSWORD);
            }
            else
            {
                RemoveFlag(EnumUserAccountControlFlags.DONT_EXPIRE_PASSWORD);
            }
        }
    }


    /// <summary>
    /// Smart Card required for login
    /// </summary>
    public bool IsSmartCardRequired
    {
        get => HasFlag(EnumUserAccountControlFlags.SMARTCARD_REQUIRED);
        set
        {
            if (value)
            {
                AddFlag(EnumUserAccountControlFlags.SMARTCARD_REQUIRED);
            }
            else
            {
                RemoveFlag(EnumUserAccountControlFlags.SMARTCARD_REQUIRED);
            }
        }
    }

    /// <summary>
    ///     Gets the current UserAccountControl value
    /// </summary>
    public int Value { get; private set; }


    /// <summary>
    ///     Adds a flag to the UserAccountControl value
    /// </summary>
    /// <param name="flag">Flag to add</param>
    public void AddFlag(EnumUserAccountControlFlags flag)
    {
        Value |= (int)flag;
    }


    /// <summary>
    ///     Checks if a specific flag is set
    /// </summary>
    /// <param name="flag">Flag to check</param>
    /// <returns>True if flag is set, false otherwise</returns>
    public bool HasFlag(EnumUserAccountControlFlags flag)
    {
        return (Value & (int)flag) == (int)flag;
    }


    /// <summary>
    ///     Removes a flag from the UserAccountControl value
    /// </summary>
    /// <param name="flag">Flag to remove</param>
    public void RemoveFlag(EnumUserAccountControlFlags flag)
    {
        Value &= ~(int)flag;
    }


    public override string ToString()
    {
        return Value.ToString();
    }
}