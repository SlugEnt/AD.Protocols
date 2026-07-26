using SlugEnt.AD.Protocols.Attributes;
using System.DirectoryServices.Protocols;
using System.Text;

namespace SlugEnt.AD.Protocols;

/// <summary>
///     Creates an Active Directory User object that can be used to update an existing user in Active Directory or create a new one.
/// </summary>
public class ADpUserEditable
{
    internal readonly UserAccountControlManager _uacManager;
    internal bool? _accountEnable;
    internal bool? _accountEnableOrig;
    private string _distinguishedName = "";
    private string _samAccount = "";
    private string _upn = "";
    private string _CommonName = "";
    private string _DepartmentFullName = "";
    private string _DepartmentShortCode = "";
    private string _Description = "";
    private string _DisplayName = "";
    private string _Email = "";
    private string _FirstName = "";
    private string _LastName = "";
    private string _Manager = "";
    private byte[] _password = [];
    private DateTimeOffset _passwordLastSet = DateTime.MinValue;
    private string _Phone = "";
    private string _Title = "";
    private int _userAccountControl = 0; // Default to 0, which is normal user account with no special flags set.
    private bool _isAccountDisabled = true;
    private bool _isPasswordSetToNeverExpires = false; // Default to false, meaning the password is set to expire.
    private bool _isAccountLockedOut = false; // Default to false, meaning the account is not locked out.
    private bool _isNormalAccount = true;  // This cannot be set to anything other than true. 


    /// <summary>
    ///     The list of attributes to update
    /// </summary>
    protected Dictionary<string, AttributeBase> AttributesToUpdate = [];



    /// <summary>
    ///     This is the most preferred constructor if you have already read the userFromAdRo from AD as it almost 100% assuredly will
    ///     pull the userFromAdRo without error.
    /// <para>Important, it will only load fields that were pulled during the userFromAdRo retrieval, so if you did not pull password fields, they will not contain the users password information</para>
    /// </summary>
    /// <param name="userFromAdRo"></param>
    public ADpUserEditable(ADpUserFromAD_RO userFromAdRo)
    {
        IsNew          = false;
        InCreationMode = true;

        // Load the non-updatable fields from the read only userFromAdRo.
        DistinquishedName  = userFromAdRo.DistinguishedName;
        UPN                = userFromAdRo.UPN;
        PasswordExpiration = userFromAdRo.PasswordExpiryDateTime; // This is not an updatable field, just a read only one.

        // Load the updatable fields from the read only userFromAdRo.
        _Description         = userFromAdRo.Description;
        _CommonName          = userFromAdRo.AD_CommonName;
        _FirstName           = userFromAdRo.FirstName;
        _LastName            = userFromAdRo.LastName;
        _Email               = userFromAdRo.Email;
        _Phone               = userFromAdRo.Phone;
        _Title               = userFromAdRo.Title;
        _Manager             = userFromAdRo.Manager;
        _DepartmentFullName  = userFromAdRo.DepartmentFullName;
        _DepartmentShortCode = userFromAdRo.DepartmentShortCode;
        _passwordLastSet     = userFromAdRo.PasswordLastSet;


        if (userFromAdRo.UserAccountControl != null)
        {
            _uacManager = new UserAccountControlManager(userFromAdRo.UserAccountControl);
            _isAccountDisabled = !_uacManager.IsEnabled;
            //_accountEnable     = _uacManager.IsEnabled;
            //_accountEnableOrig = _accountEnable;
        }

        InCreationMode = false;
    }


    /// <summary>
    /// Creates a new user, one that does not yet exist in AD
    /// </summary>
    /// <param name="commonName"></param>
    public ADpUserEditable(string commonName)
    {
        if (string.IsNullOrEmpty(commonName))
            throw new ArgumentNullException(nameof(commonName));

        IsNew = true;

        // Set default values
        CommonNameChg = commonName;
    }



    /// <summary>
    /// Creates the UserAccountControl value based on the current state of the user object.  UAC is a bitwise value that contains flags for the user account.
    /// </summary>
    /// <returns></returns>
    public int Calculate_UserAccountControlValue()
    {
        int uac = 0;

        if (_isAccountDisabled)
            uac |= 0x0002; // ACCOUNTDISABLE

        if (_isPasswordSetToNeverExpires)
            uac |= 0x10000; // DONT_EXPIRE_PASSWORD

        if (_isAccountLockedOut)
            uac |= 0x0010; // LOCKOUT

        if (_isNormalAccount)
            uac |= 0x0200; // NORMAL_ACCOUNT

        UserAccountControl = uac; // Set the UserAccountControl value
        return uac;
    }



    /// <summary>
    /// Used when creating the object from an existing AD object.  This prevents the attributes from being added to the modified list during initial setting.
    /// </summary>
    protected bool InCreationMode { get; set; }


    /// <summary>
    /// If true, this will be a creation, not an update.
    /// </summary>
    protected bool IsNew { get; set; }



    /// <summary>
    /// The CN or common name of the user in AD.
    /// </summary>
    public string CommonNameChg
    {
        get => _CommonName;
        set
        {
            _CommonName = value;
            string key = ADpCommon.ATN_CN;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            AttrCommonName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    /// Full Name of the Department.
    /// </summary>
    public string DepartmentFullNameChg
    {
        get => _DepartmentFullName;
        set
        {
            _DepartmentFullName = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string         key       = ADpCommon.ATN_DEPARTMENT;
            AttrDepartment attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }

    /// <summary>
    /// Short name of the Department
    /// </summary>
    public string DepartmentShortCodeChg { get; set; }


    /// <summary>
    /// Description of the user.  This is a free form text field that can be used to describe the user.
    /// </summary>
    public string DescriptionChg
    {
        get => _Description;
        set
        {
            _Description = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string          key       = ADpCommon.ATN_DESCRIPTION;
            AttrDescription attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    /// The Display Name of the User.  This is the name that will be displayed in the address book and other places.
    /// </summary>
    public string DisplayNameChg
    {
        get => _DisplayName;
        set
        {
            _DisplayName = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string          key       = ADpCommon.ATN_DISPLAYNAME;
            AttrDisplayName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    ///     Distinquished Name.  This is the unique identifier of the user in Active Directory.  It is used to identify the
    ///     user in Active Directory.
    /// </summary>
    public string DistinquishedName
    {
        get => _distinguishedName;
        set
        {
            _distinguishedName = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string                key       = ADpCommon.ATN_DISTINGUISHED_NAME;
            AttrDistinguishedName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }



    public string EmailChg
    {
        get => _Email;
        set
        {
            _Email = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string    key       = ADpCommon.ATN_EMAIL;
            AttrEmail attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    public string FirstNameChg
    {
        get => _FirstName;
        set
        {
            _FirstName = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string        key       = ADpCommon.ATN_FIRSTNAME;
            AttrFirstName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }



    public string LastNameChg
    {
        get => _LastName;
        set
        {
            _LastName = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string       key       = ADpCommon.ATN_LASTNAME;
            AttrLastName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }

    public string ManagerChg
    {
        get => _Manager;
        set
        {
            _Manager = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string      key       = ADpCommon.ATN_MANAGER;
            AttrManager attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }

    public string PasswordChg
    {
        set
        {
            _password = Encoding.Unicode.GetBytes("\"" + value + "\"");

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string           key       = ADpCommon.ATN_PASSWORD;
            AttrUserPassword attrValue = new(_password, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    /// Sets the User Password Last Set Value.  Can only set it to 0 Ticks (Expired now) OR DateTimeOffset.MaxValue (never expires).
    /// </summary>
    public DateTimeOffset PasswordLastSet
    {
        get
        {
            return _passwordLastSet;
        }
        set
        {
            if (value != DateTimeOffset.MinValue && value != DateTimeOffset.MaxValue)
                throw new ArgumentException("PasswordLastSet can only be set to 0 Ticks (Expired now) OR DateTimeOffset.MaxValue (never expires)", nameof(value));

            // Convert DateTime offset to date time but in UTC Time.
            DateTime utc = value.UtcDateTime;
            _passwordLastSet = utc;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string              key       = ADpCommon.ATN_PASSWORD_LAST_SET;
            AttrPasswordLastSet attrValue = new(_passwordLastSet, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    /// The date the user's password will expire.  This is not an updatable field.  It is calculated based on the password policy that applies for the user.
    /// </summary>
    public DateTimeOffset PasswordExpiration { get; private set; }


    public string PhoneChg
    {
        get => _Phone;
        set
        {
            _Phone = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string        key       = ADpCommon.ATN_PHONE;
            AttrWorkPhone attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    /// The User's Title.  This is a free form text field that can be used to describe the user's job title.
    /// </summary>
    public string TitleChg
    {
        get => _Title;
        set
        {
            _Title = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string    key       = ADpCommon.ATN_TITLE;
            AttrTitle attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }



    /// <summary>
    ///     Universal Principal Name.  This is the email address of the user and is used to identify the user in Active
    ///     Directory.
    /// </summary>
    public string UPN
    {
        get => _upn;
        set
        {
            _upn = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string  key       = ADpCommon.ATN_UPN;
            AttrUPN attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }



    /// <summary>
    /// SAMAccount Id
    /// </summary>
    public string SAMAccount
    {
        get => _samAccount;
        set
        {
            _samAccount = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string         key       = ADpCommon.ATN_SAM;
            AttrSamAccount attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    #region UserAccountControl Properties

    /// <summary>
    /// Indicates if the user account is enabled.  This is used to determine if the user account is active or not.
    /// </summary>
    public bool IsDisabled
    {
        get
        {
            return _isAccountDisabled;
        }
        set
        {
            _isAccountDisabled = value;
            Calculate_UserAccountControlValue();
        }
    }


    /// <summary>
    /// Indicates if the user's password is set to never expire.  This is used to determine if the user account's password will expire or not.
    /// </summary>
    public bool IsPasswordSetToNeverExpires
    {
        get
        {
            return _isPasswordSetToNeverExpires;
        }
        set
        {
            _isPasswordSetToNeverExpires = value;
            Calculate_UserAccountControlValue();
        }
    }

    /// <summary>
    /// Indicates if the user account is locked out.  This is used to determine if the user account is locked out due to too many failed login attempts.
    /// <para>This value cannot be set programmatically.</para>
    /// </summary>
    public bool IsAccountLockedOut
    {
        get { return _isAccountLockedOut; }
    }


    /// <summary>
    /// Indicates if the user is a normal account.  This is used to determine if the user is a service account or a computer account.
    /// This cannot be set to false, it is always true for user accounts.
    /// </summary>
    public bool IsNormalAccount
    {
        get
        {
            return _isNormalAccount;
        }
        private set
        {
            _isNormalAccount = value;
            Calculate_UserAccountControlValue();
        }
    }



    /// <summary>
    /// User Account Control value for the user.  This is a bitwise value that contains flags for the user account.
    /// </summary>
    private int UserAccountControl
    {
        get
        {
            return _userAccountControl;
        }


        set
        {
            _userAccountControl = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string                 key       = ADpCommon.ATN_USER_ACCOUNT_CONTROL;
            AttrUserAccountControl attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }

#endregion



    /// <summary>
    ///     Performs all final settings to prepare the user's attributes to be updated.  For instance it will look at the
    ///     current disposition of AccountEnable to see how it should be set.
    /// </summary>
    /// <returns></returns>
    public AttributeBase[] GetAttributes()
    {
        /* Not needed anymore.  
        // Get UAC value - if the user did not give it to us then we cannot manipulate its fields.
        if (_uacManager != null)
        {
            // The account disablement status has changed then we need to update.
            if (_accountEnable != _accountEnableOrig)
            {
                if ((bool)_accountEnable) // Enable the account
                {
                    _uacManager.IsEnabled = true;
                }
                else // Disable the account
                {
                    _uacManager.IsEnabled = false;
                }

                string                 key       = ADpCommon.ATN_UAC;
                AttrUserAccountControl attrValue = new(_uacManager.ToString(), EnumAttributeOperation.Modify);
                if (!AttributesToUpdate.TryAdd(key, attrValue))
                {
                    AttributesToUpdate[key] = attrValue;
                }
            }
        }
        */

        return [.. AttributesToUpdate.Values];
    }


    /// <summary>
    /// Returns the list of attributes to be updated.  This will be a list of DirectoryAttribute objects that can be used to 
    /// </summary>
    /// <returns></returns>
    public DirectoryAttribute[] GetDirectoryAttributesNew()
    {
        DirectoryAttribute[]     attributes    = Array.Empty<DirectoryAttribute>();
        List<DirectoryAttribute> dirAttributes = new List<DirectoryAttribute>();
        dirAttributes.Add(new DirectoryAttribute("objectClass", "user"));

        foreach (KeyValuePair<string, AttributeBase> attributeBase in AttributesToUpdate)
        {
            dirAttributes.Add(attributeBase.Value.DA);
        }

        return dirAttributes.ToArray();
    }
}