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



    /// <summary>
    /// Checks if the account is locked out.
    /// </summary>
    public bool IsLockedOut => (_currentValue & (int)UserAccountControlFlags.AccountLocked) != 0;

    /// <summary>
    /// Checks if the account is not locked out.
    /// </summary>
    public bool IsNotLockedOut => (_currentValue & (int)UserAccountControlFlags.AccountLocked) == 0;

}

