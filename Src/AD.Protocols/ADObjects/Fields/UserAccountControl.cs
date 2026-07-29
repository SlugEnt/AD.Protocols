
namespace AD.Protocols.ADObjects;

/// <summary>
/// Provides the ability to interpret and set the UserAccountControl for a user object
/// </summary>
public class UserAccountControl
{
    // Used to detect if the value has changed from original.
    private int _originalValue = -1;
    private int _currentValue;
    private readonly Action<int> _onChange;
    
    /// <summary>
    /// Returns true if the value of this object has changed from the original value.  This is used to determine if the object needs to be updated in Active Directory.
    /// </summary>
    public bool HasChangedValue {get
    {
        if (_originalValue != _currentValue)
            return true;
        return false;   
    }}
    
    /// <summary>
    /// Represents the UserAccountControl flags for an Active Directory user account.
    /// </summary>
    public enum UserAccountControlFlags
    {
        /// <summary>
        /// The account is enabled.
        /// </summary>
        None = 0,

        /// <summary>
        /// The account is disabled.
        /// </summary>
        AccountDisabled = 0x0002,

        /// <summary>
        /// The user must change the password at the next logon.
        /// </summary>
        PasswordMustChange = 0x0001,

        /// <summary>
        /// The user's password has expired.
        /// </summary>
        PasswordExpired = 0x800000,

        /// <summary>
        /// The account is a workstation logon account.
        /// </summary>
        WorkstationAccount = 0x0010,

        /// <summary>
        /// The account is a server logon account.
        /// </summary>
        ServerAccount = 0x0020,

        /// <summary>
        /// The account cannot be verified (cannot be verified or does not exist).
        /// </summary>
        InvalidSyntax = 0x0008,

        /// <summary>
        /// The account is locked out.
        /// </summary>
        AccountLockout = 0x0010, // Note: This is duplicated with WorkstationAccount in some contexts. Consider context when using.

        /// <summary>
        /// The user is a guest user.
        /// </summary>
        UserAccountIsGuest = 0x0100,

        /// <summary>
        /// The user is a smart card required user.
        /// </summary>
        UserMustChangePassword = 0x10000,

        /// <summary>
        /// The user cannot change the password.
        /// </summary>
        PasswordNeverExpires = 0x10000,

        /// <summary>
        /// The account is a trusted for delegation account.
        /// </summary>
        TrustedForDelegation = 0x08000,

        /// <summary>
        /// The account is not trusted for delegation.
        /// </summary>
        NotTrustedForDelegation = 0x10000,

        /// <summary>
        /// The account is sensitive and cannot be delegated.
        /// </summary>
        SensitiveNotDelegated = 0x20000,

        /// <summary>
        /// The user is enabled for Kerberos pre-authentication.
        /// </summary>
        DoNotRequirePreAuthentication = 0x0040,

        /// <summary>
        /// The user is a principal whose password never expires.
        /// </summary>
        PasswordNeverExpiresFlag = 0x10000,

        /// <summary>
        /// The user is enabled for scheduled tasks.
        /// </summary>
        EnabledForScheduledTasks = 0x20, // This is actually ServerAccount, but often used for scheduled tasks.

        /// <summary>
        /// The user account is a temporary duplicate account.
        /// </summary>
        TemporaryDuplicateAccount = 0x0400,

        /// <summary>
        /// The account is a normal account.
        /// </summary>
        NormalAccount = 0x0200,

        /// <summary>
        /// The account has a special domain account.
        /// </summary>
        InterdomainTrustAccount = 0x0800,

        /// <summary>
        /// The account has a trusted domain account.
        /// </summary>
        WorkstationTrustAccount = 0x1000,

        /// <summary>
        /// The account has a server trust account.
        /// </summary>
        ServerTrustAccount = 0x2000,

        /// <summary>
        /// The account is enabled for all logon types.
        /// </summary>
        EnabledForAllLogonTypes = 0x4000,

        /// <summary>
        /// The account is a computer account.
        /// </summary>
        ComputerAccount = 0x1000
    }
    

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAccountControl"/> class.
    /// </summary>
    public UserAccountControl(Action<int> onChange)
    {
        _currentValue = 0;
        _originalValue           = 0;
        _onChange = onChange;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="UserAccountControl"/> class with a specific integer value.
    /// </summary>
    /// <param name="value">The integer value representing the UserAccountControl flags.</param>
    /// <param name="onChange">The action to invoke when the value changes.</param>
    public UserAccountControl(int value, Action<int> onChange)
    {
        _currentValue = value;
        if (_originalValue == -1 )
            _originalValue            = value;
        _onChange = onChange;
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

    /// <summary>
    /// Checks if the account is enabled.
    /// </summary>
    public bool IsEnabled => (_currentValue & (int)UserAccountControlFlags.AccountDisabled) == 0;

    /// <summary>
    /// Checks if the account is disabled.
    /// </summary>
    public bool IsDisabled => (_currentValue & (int)UserAccountControlFlags.AccountDisabled) != 0;

    /// <summary>
    /// Checks if the user must change the password at the next logon.
    /// </summary>
    public bool MustChangePassword => (_currentValue & (int)UserAccountControlFlags.PasswordMustChange) != 0;

    /// <summary>
    /// Checks if the user's password has expired.
    /// </summary>
    public bool IsPasswordExpired => (_currentValue & (int)UserAccountControlFlags.PasswordExpired) != 0;

    /// <summary>
    /// Checks if the account is a workstation logon account.
    /// </summary>
    public bool IsWorkstationAccount => (_currentValue & (int)UserAccountControlFlags.WorkstationAccount) != 0;

    /// <summary>
    /// Checks if the account is a server logon account.
    /// </summary>
    public bool IsServerAccount => (_currentValue & (int)UserAccountControlFlags.ServerAccount) != 0;

    /// <summary>
    /// Checks if the account has invalid syntax.
    /// </summary>
    public bool HasInvalidSyntax => (_currentValue & (int)UserAccountControlFlags.InvalidSyntax) != 0;

    /// <summary>
    /// Checks if the account is locked out.
    /// </summary>
    public bool IsAccountLockedOut => (_currentValue & (int)UserAccountControlFlags.AccountLockout) != 0;

    /// <summary>
    /// Checks if the user is a guest user.
    /// </summary>
    public bool IsGuestUser => (_currentValue & (int)UserAccountControlFlags.UserAccountIsGuest) != 0;

    /// <summary>
    /// Checks if the user must change their password.
    /// </summary>
    public bool UserMustChangePassword => (_currentValue & (int)UserAccountControlFlags.UserMustChangePassword) != 0;

    /// <summary>
    /// Checks if the password never expires.
    /// </summary>
    public bool PasswordNeverExpires => (_currentValue & (int)UserAccountControlFlags.PasswordNeverExpires) != 0;

    /// <summary>
    /// Checks if the account is trusted for delegation.
    /// </summary>
    public bool IsTrustedForDelegation => (_currentValue & (int)UserAccountControlFlags.TrustedForDelegation) != 0;

    /// <summary>
    /// Checks if the account is not trusted for delegation.
    /// </summary>
    public bool IsNotTrustedForDelegation => (_currentValue & (int)UserAccountControlFlags.NotTrustedForDelegation) != 0;

    /// <summary>
    /// Checks if the account is sensitive and cannot be delegated.
    /// </summary>
    public bool IsSensitiveNotDelegated => (_currentValue & (int)UserAccountControlFlags.SensitiveNotDelegated) != 0;

    /// <summary>
    /// Checks if the user is enabled for Kerberos pre-authentication.
    /// </summary>
    public bool DoNotRequirePreAuthentication => (_currentValue & (int)UserAccountControlFlags.DoNotRequirePreAuthentication) != 0;

    /// <summary>
    /// Checks if the user is a principal whose password never expires.
    /// </summary>
    public bool PasswordNeverExpiresFlag => (_currentValue & (int)UserAccountControlFlags.PasswordNeverExpiresFlag) != 0;

    /// <summary>
    /// Checks if the user is enabled for scheduled tasks.
    /// </summary>
    public bool IsEnabledForScheduledTasks => (_currentValue & (int)UserAccountControlFlags.EnabledForScheduledTasks) != 0;

    /// <summary>
    /// Checks if the account is a temporary duplicate account.
    /// </summary>
    public bool IsTemporaryDuplicateAccount => (_currentValue & (int)UserAccountControlFlags.TemporaryDuplicateAccount) != 0;

    /// <summary>
    /// Checks if the account is a normal account.
    /// </summary>
    public bool IsNormalAccount => (_currentValue & (int)UserAccountControlFlags.NormalAccount) != 0;

    /// <summary>
    /// Checks if the account has a special domain account.
    /// </summary>
    public bool IsInterdomainTrustAccount => (_currentValue & (int)UserAccountControlFlags.InterdomainTrustAccount) != 0;

    /// <summary>
    /// Checks if the account has a trusted domain account.
    /// </summary>
    public bool IsWorkstationTrustAccount => (_currentValue & (int)UserAccountControlFlags.WorkstationTrustAccount) != 0;

    /// <summary>
    /// Checks if the account has a server trust account.
    /// </summary>
    public bool IsServerTrustAccount => (_currentValue & (int)UserAccountControlFlags.ServerTrustAccount) != 0;

    /// <summary>
    /// Checks if the account is enabled for all logon types.
    /// </summary>
    public bool IsEnabledForAllLogonTypes => (_currentValue & (int)UserAccountControlFlags.EnabledForAllLogonTypes) != 0;

    /// <summary>
    /// Checks if the account is a computer account.
    /// </summary>
    public bool IsComputerAccount => (_currentValue & (int)UserAccountControlFlags.ComputerAccount) != 0;


    /// <summary>
    /// Sets the account to be enabled.
    /// </summary>
    public void EnableAccount() { Value &= ~(int)UserAccountControlFlags.AccountDisabled; }


    /// <summary>
    /// Sets the account to be disabled.
    /// </summary>
    public void DisableAccount() { Value |= (int)UserAccountControlFlags.AccountDisabled; }


    /// <summary>
    /// Sets the flag for the user to change password at next logon.
    /// </summary>
    public void SetPasswordMustChange() { Value |= (int)UserAccountControlFlags.PasswordMustChange; }


    /// <summary>
    /// Clears the flag for the user to change password at next logon.
    /// </summary>
    public void ClearPasswordMustChange() { Value &= ~(int)UserAccountControlFlags.PasswordMustChange; }


    /// <summary>
    /// Sets the flag for password expiration.
    /// </summary>
    public void SetPasswordExpired() { Value |= (int)UserAccountControlFlags.PasswordExpired; }


    /// <summary>
    /// Clears the flag for password expiration.
    /// </summary>
    public void ClearPasswordExpired() { Value &= ~(int)UserAccountControlFlags.PasswordExpired; }


    /// <summary>
    /// Sets the account as a workstation account.
    /// </summary>
    public void SetWorkstationAccount() { Value |= (int)UserAccountControlFlags.WorkstationAccount; }


    /// <summary>
    /// Clears the account as a workstation account.
    /// </summary>
    public void ClearWorkstationAccount() { Value &= ~(int)UserAccountControlFlags.WorkstationAccount; }


    /// <summary>
    /// Sets the account as a server account.
    /// </summary>
    public void SetServerAccount() { Value |= (int)UserAccountControlFlags.ServerAccount; }


    /// <summary>
    /// Clears the account as a server account.
    /// </summary>
    public void ClearServerAccount() { Value &= ~(int)UserAccountControlFlags.ServerAccount; }


    /// <summary>
    /// Sets the account to have invalid syntax.
    /// </summary>
    public void SetInvalidSyntax() { Value |= (int)UserAccountControlFlags.InvalidSyntax; }


    /// <summary>
    /// Clears the account to have invalid syntax.
    /// </summary>
    public void ClearInvalidSyntax() { Value &= ~(int)UserAccountControlFlags.InvalidSyntax; }


    /// <summary>
    /// Sets the account to be locked out.
    /// </summary>
    //public void LockAccount() { Value |= (int)UserAccountControlFlags.AccountLockout; }


    /// <summary>
    /// Unlocks the account.
    /// </summary>
    //public void UnlockAccount() { Value &= ~(int)UserAccountControlFlags.AccountLockout; }


    /// <summary>
    /// Sets the user as a guest user.
    /// </summary>
    public void SetGuestUser() { Value |= (int)UserAccountControlFlags.UserAccountIsGuest; }


    /// <summary>
    /// Clears the user as a guest user.
    /// </summary>
    public void ClearGuestUser() { Value &= ~(int)UserAccountControlFlags.UserAccountIsGuest; }


    /// <summary>
    /// Sets the flag for the user to change their password.
    /// </summary>
    public void SetUserMustChangePassword() { Value |= (int)UserAccountControlFlags.UserMustChangePassword; }


    /// <summary>
    /// Clears the flag for the user to change their password.
    /// </summary>
    public void ClearUserMustChangePassword() { Value &= ~(int)UserAccountControlFlags.UserMustChangePassword; }


    /// <summary>
    /// Sets the password to never expire.
    /// </summary>
    public void SetPasswordNeverExpires() { Value |= (int)UserAccountControlFlags.PasswordNeverExpires; }


    /// <summary>
    /// Clears the password never expires flag.
    /// </summary>
    public void ClearPasswordNeverExpires() { Value &= ~(int)UserAccountControlFlags.PasswordNeverExpires; }


    /// <summary>
    /// Sets the account to be trusted for delegation.
    /// </summary>
    public void SetTrustedForDelegation() { Value |= (int)UserAccountControlFlags.TrustedForDelegation; }


    /// <summary>
    /// Clears the account to be trusted for delegation.
    /// </summary>
    public void ClearTrustedForDelegation() { Value &= ~(int)UserAccountControlFlags.TrustedForDelegation; }


    /// <summary>
    /// Sets the account to not be trusted for delegation.
    /// </summary>
    public void SetNotTrustedForDelegation() { Value |= (int)UserAccountControlFlags.NotTrustedForDelegation; }


    /// <summary>
    /// Clears the account to not be trusted for delegation.
    /// </summary>
    public void ClearNotTrustedForDelegation() { Value &= ~(int)UserAccountControlFlags.NotTrustedForDelegation; }


    /// <summary>
    /// Sets the account to be sensitive and not delegated.
    /// </summary>
    public void SetSensitiveNotDelegated() { Value |= (int)UserAccountControlFlags.SensitiveNotDelegated; }


    /// <summary>
    /// Clears the account to be sensitive and not delegated.
    /// </summary>
    public void ClearSensitiveNotDelegated() { Value &= ~(int)UserAccountControlFlags.SensitiveNotDelegated; }


    /// <summary>
    /// Sets the user to not require pre-authentication for Kerberos.
    /// </summary>
    public void SetDoNotRequirePreAuthentication() { Value |= (int)UserAccountControlFlags.DoNotRequirePreAuthentication; }


    /// <summary>
    /// Clears the flag for not requiring pre-authentication for Kerberos.
    /// </summary>
    public void ClearDoNotRequirePreAuthentication() { Value &= ~(int)UserAccountControlFlags.DoNotRequirePreAuthentication; }


    /// <summary>
    /// Sets the password never expires flag for the principal.
    /// </summary>
    public void SetPasswordNeverExpiresFlag() { Value |= (int)UserAccountControlFlags.PasswordNeverExpiresFlag; }


    /// <summary>
    /// Clears the password never expires flag for the principal.
    /// </summary>
    public void ClearPasswordNeverExpiresFlag() { Value &= ~(int)UserAccountControlFlags.PasswordNeverExpiresFlag; }


    /// <summary>
    /// Sets the user as enabled for scheduled tasks.
    /// </summary>
    public void SetEnabledForScheduledTasks() { Value |= (int)UserAccountControlFlags.EnabledForScheduledTasks; }


    /// <summary>
    /// Clears the user as enabled for scheduled tasks.
    /// </summary>
    public void ClearEnabledForScheduledTasks() { Value &= ~(int)UserAccountControlFlags.EnabledForScheduledTasks; }


    /// <summary>
    /// Sets the account as a temporary duplicate account.
    /// </summary>
    public void SetTemporaryDuplicateAccount() { Value |= (int)UserAccountControlFlags.TemporaryDuplicateAccount; }


    /// <summary>
    /// Clears the account as a temporary duplicate account.
    /// </summary>
    public void ClearTemporaryDuplicateAccount() { Value &= ~(int)UserAccountControlFlags.TemporaryDuplicateAccount; }


    /// <summary>
    /// Sets the account as a normal account.
    /// </summary>
    public void SetNormalAccount() { Value |= (int)UserAccountControlFlags.NormalAccount; }


    /// <summary>
    /// Clears the account as a normal account.
    /// </summary>
    public void ClearNormalAccount() { Value &= ~(int)UserAccountControlFlags.NormalAccount; }


    /// <summary>
    /// Sets the account as a special domain account.
    /// </summary>
    public void SetInterdomainTrustAccount() { Value |= (int)UserAccountControlFlags.InterdomainTrustAccount; }


    /// <summary>
    /// Clears the account as a special domain account.
    /// </summary>
    public void ClearInterdomainTrustAccount() { Value &= ~(int)UserAccountControlFlags.InterdomainTrustAccount; }


    /// <summary>
    /// Sets the account as a trusted domain account.
    /// </summary>
    public void SetWorkstationTrustAccount() { Value |= (int)UserAccountControlFlags.WorkstationTrustAccount; }


    /// <summary>
    /// Clears the account as a trusted domain account.
    /// </summary>
    public void ClearWorkstationTrustAccount() { Value &= ~(int)UserAccountControlFlags.WorkstationTrustAccount; }


    /// <summary>
    /// Sets the account as a server trust account.
    /// </summary>
    public void SetServerTrustAccount() { Value |= (int)UserAccountControlFlags.ServerTrustAccount; }


    /// <summary>
    /// Clears the account as a server trust account.
    /// </summary>
    public void ClearServerTrustAccount() { Value &= ~(int)UserAccountControlFlags.ServerTrustAccount; }


    /// <summary>
    /// Sets the account to be enabled for all logon types.
    /// </summary>
    public void SetEnabledForAllLogonTypes() { Value |= (int)UserAccountControlFlags.EnabledForAllLogonTypes; }


    /// <summary>
    /// Clears the account to be enabled for all logon types.
    /// </summary>
    public void ClearEnabledForAllLogonTypes() { Value &= ~(int)UserAccountControlFlags.EnabledForAllLogonTypes; }


    /// <summary>
    /// Sets the account as a computer account.
    /// </summary>
    public void SetComputerAccount() { Value |= (int)UserAccountControlFlags.ComputerAccount; }


    /// <summary>
    /// Clears the account as a computer account.
    /// </summary>
    public void ClearComputerAccount() { Value &= ~(int)UserAccountControlFlags.ComputerAccount; }


    /// <summary>
    /// Gets a string representation of the current UserAccountControl flags.
    /// </summary>
    /// <returns>A string listing the active flags.</returns>
    public override string ToString()
    {
        var flags = new System.Collections.Generic.List<string>();

        if (IsEnabled)
            flags.Add("Enabled");
        if (IsDisabled)
            flags.Add("Disabled");
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
        if (IsAccountLockedOut)
            flags.Add("AccountLockout");
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

        if (flags.Count == 0)
        {
            return "None";
        }

        return string.Join(", ", flags);
    }
    
}
