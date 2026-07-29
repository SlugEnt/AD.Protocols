namespace AD.Protocols.ADObjects.Fields;

public class MsDsUserAccountControl
{
    // Used to detect if the value has changed from original.
    private          int         _originalValue = -1;
    private          int         _currentValue;
    private readonly Action<int> _onChange;

    /// <summary>
    /// Returns true if the value of this object has changed from the original value.  This is used to determine if the object needs to be updated in Active Directory.
    /// </summary>
    public bool HasChangedValue
    {
        get
        {
            if (_originalValue != _currentValue)
                return true;

            return false;
        }
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="UserAccountControl"/> class.
    /// </summary>
    public MsDsUserAccountControl(Action<int> onChange)
    {
        _currentValue  = 0;
        _originalValue = 0;
        _onChange      = onChange;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="UserAccountControl"/> class with a specific integer value.
    /// </summary>
    /// <param name="value">The integer value representing the UserAccountControl flags.</param>
    /// <param name="onChange">The action to invoke when the value changes.</param>
    public MsDsUserAccountControl(int value)
                              //Action<int> onChange)
    {
        _currentValue = value;
        if (_originalValue == -1)
            _originalValue = value;
        //_onChange = onChange;
    }


    /// <summary>
    /// Gets or sets the integer value of the UserAccountControl flags.
    /// </summary>
    public int Value
    {
        get => _currentValue;
        set
        {
            _currentValue = value;
            _onChange?.Invoke(value);
        }
    }


    public enum UserAccountControlFlags
    {
        /// <summary>
        /// The account is enabled.
        /// </summary>
        None = 0,

        Script = 0x0001,    
        
        /// <summary>
        /// The account is disabled.
        /// </summary>
        AccountIsDisabled = 0x0002,

        /// <summary>
        /// The account is locked out
        /// </summary>
        AccountLocked = 0x0010,

        /// <summary>
        /// The account does not need a password
        /// </summary>
        PasswordNotRequired = 0x0020,

        /// <summary>
        /// The password cannot be changed
        /// </summary>
        PasswordCannotBeChanged = 0x0040,

        /// <summary>
        /// The user's password has expired.
        /// </summary>
        PasswordExpired = 0x800000,

        /// <summary>
        /// Account is a regular account
        /// </summary>
        NormalAccount = 0x0200,

        /// <summary>
        /// The password does not expire </summary>
        PasswordDoesNotExpire = 0x10000,
    }

    public bool IsDisabled => (_currentValue & (int)UserAccountControlFlags.AccountIsDisabled) != 0;
    public bool IsEnabled => (_currentValue & (int)UserAccountControlFlags.AccountIsDisabled) == 0;
    /*
    public bool IsPasswordRequired => (_currentValue & (int)UserAccountControlFlags.PasswordCannotBeChanged) != 0;
    public bool IsNotPasswordRequired => (_currentValue & (int)UserAccountControlFlags.PasswordCannotBeChanged) == 0;
    public bool IsPasswordNeverChanged => (_currentValue & (int)UserAccountControlFlags.PasswordCannotBeChanged) != 0;
    public bool IsAccountLockedOut => (_currentValue & (int)UserAccountControlFlags.AccountLocked) != 0;
    public bool IsNormalUserAccount => (_currentValue & (int)UserAccountControlFlags.NormalAccount) != 0;
    public bool IsSmartCardRequired => (_currentValue & (int)UserAccountControlFlags.Script) != 0; // Placeholder, not a standard UAC flag
    public bool IsWorkstationOrServer => (_currentValue & (int)UserAccountControlFlags.NormalAccount) != 0;
    public bool HasPassword => (_currentValue & (int)UserAccountControlFlags.PasswordNotRequired) == 0;
    public bool CannotBeChanged => (_currentValue & (int)UserAccountControlFlags.PasswordCannotBeChanged) != 0;
    public bool IsPasswordChangeable => (_currentValue & (int)UserAccountControlFlags.PasswordCannotBeChanged) == 0;
    */

    /*

    public bool MustChangePassword => (_currentValue & (int)UserAccountControlFlags.PasswordCannotBeChanged) == 0;

    public bool IsPasswordExpired => (_currentValue & (int)UserAccountControlFlags.PasswordExpired) != 0;

    public bool IsWorkstationAccount => (_currentValue & (int)UserAccountControlFlags.NormalAccount) != 0;

    public bool IsServerAccount => (_currentValue & (int)UserAccountControlFlags.NormalAccount) != 0;

    public bool HasInvalidSyntax => (_currentValue & (int)UserAccountControlFlags.Script) != 0;

    public bool IsGuestUser => (_currentValue & (int)UserAccountControlFlags.Script) != 0;

    public bool UserMustChangePassword => (_currentValue & (int)UserAccountControlFlags.PasswordCannotBeChanged) != 0;

    public bool PasswordNeverExpires => (_currentValue & (int)UserAccountControlFlags.PasswordDoesNotExpire) != 0;

    public bool IsTrustedForDelegation => (_currentValue & (int)UserAccountControlFlags.Script) != 0;

    public bool IsNotTrustedForDelegation => (_currentValue & (int)UserAccountControlFlags.Script) != 0;

    public bool IsSensitiveNotDelegated => (_currentValue & (int)UserAccountControlFlags.Script) != 0;  

    public bool DoNotRequirePreAuthentication => (_currentValue & (int)UserAccountControlFlags.Script) != 0;

    public bool PasswordNeverExpiresFlag => (_currentValue & (int)UserAccountControlFlags.PasswordDoesNotExpire) != 0;

    public bool IsEnabledForScheduledTasks => (_currentValue & (int)UserAccountControlFlags.Script) != 0;

    public bool IsTemporaryDuplicateAccount => (_currentValue & (int)UserAccountControlFlags.Script) != 0;

    public bool IsNormalAccount => (_currentValue & (int)UserAccountControlFlags.NormalAccount) != 0;

    public bool IsInterdomainTrustAccount => (_currentValue & (int)UserAccountControlFlags.Script) != 0;

    public bool IsWorkstationTrustAccount => (_currentValue & (int)UserAccountControlFlags.Script) != 0;

    public bool IsServerTrustAccount => (_currentValue & (int)UserAccountControlFlags.Script) != 0;

    public bool IsEnabledForAllLogonTypes => (_currentValue & (int)UserAccountControlFlags.Script) != 0;

    public bool IsComputerAccount => (_currentValue & (int)UserAccountControlFlags.Script) != 0;
    */
    
    /// <summary>
    /// Checks if the account is locked out.
    /// </summary>
    public bool IsLockedOut => (_currentValue & (int)UserAccountControlFlags.AccountLocked) != 0;

    /// <summary>
    /// Checks if the account is not locked out.
    /// </summary>
    public bool IsNotLockedOut => (_currentValue & (int)UserAccountControlFlags.AccountLocked) == 0;


    /// <summary>
    /// Gets a string representation of the current UserAccountControl flags.
    /// </summary>
    /// <returns>A string listing the active flags.</returns>
    public override string ToString()
    {
        var flags = new System.Collections.Generic.List<string>();

        if (IsLockedOut)
            flags.Add("AccountLockedOut");
        if (IsDisabled)
            flags.Add("Disabled");
/*        
        if (MustChangePassword)
            flags.Add("PasswordMustChange");
        if (IsPasswordExpired)
            flags.Add("PasswordExpired");
        if (IsWorkstationAccount)
            flags.Add("WorkstationAccount");
        if (IsServerAccount)
            flags.Add("ServerAccount");
        if (HasInvalidSyntax)
            flags.Add("InvalidSyntax");
        //        if (IsAccountLockedOut)
        //            flags.Add("AccountLockout");
        if (IsGuestUser)
            flags.Add("GuestUser");
        if (UserMustChangePassword)
            flags.Add("UserMustChangePassword");
        if (PasswordNeverExpires)
            flags.Add("PasswordNeverExpires");
        if (IsTrustedForDelegation)
            flags.Add("TrustedForDelegation");
        if (IsNotTrustedForDelegation)
            flags.Add("NotTrustedForDelegation");
        if (IsSensitiveNotDelegated)
            flags.Add("SensitiveNotDelegated");
        if (DoNotRequirePreAuthentication)
            flags.Add("DoNotRequirePreAuthentication");
        if (PasswordNeverExpiresFlag)
            flags.Add("PasswordNeverExpiresFlag");
        if (IsEnabledForScheduledTasks)
            flags.Add("EnabledForScheduledTasks");
        if (IsTemporaryDuplicateAccount)
            flags.Add("TemporaryDuplicateAccount");
        if (IsNormalAccount)
            flags.Add("NormalAccount");
        if (IsInterdomainTrustAccount)
            flags.Add("InterdomainTrustAccount");
        if (IsWorkstationTrustAccount)
            flags.Add("WorkstationTrustAccount");
        if (IsServerTrustAccount)
            flags.Add("ServerTrustAccount");
        if (IsEnabledForAllLogonTypes)
            flags.Add("EnabledForAllLogonTypes");
        if (IsComputerAccount)
            flags.Add("ComputerAccount");
*/
        if (flags.Count == 0)
        {
            return "None";
        }

        return string.Join(", ", flags);
    }

}

