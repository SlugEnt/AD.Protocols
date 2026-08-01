using SlugEnt.AD.Protocols;
using SlugEnt.AD.Protocols.Attributes;
using System.DirectoryServices.Protocols;
using System.Text;
using AD.Protocols.ADObjects.Fields;

namespace AD.Protocols.ADObjects;

public class ADpUser : ADpBaseObject
{
    private byte[] _password           = [];
    

    /// <summary>
    /// Used to communicate to detect password has been changed.  Password is a special case because it cannot be updated
    /// at same time as the other attributes.
    /// </summary>
    internal bool PasswordHasBeenSet { get; set; }
    
    public ADpUser(string name, ADSPath parentPath)
    {
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            
            InCreationMode = true;

            UserAccountControlSetter = new UserAccountControl(UserAccountControlHasChanged);
            ParentPath               = parentPath;
            Name                     = name;

            IsNew          = true;
            InCreationMode = false;
        }
    }


    /// <summary>
    /// Constructor that starts the process of creating a new user.
    /// </summary>
    /// <param name="name"></param>
    public ADpUser(string name) : base(name)
    {
        IsNew                    = true;
        UserAccountControlSetter = new UserAccountControl(UserAccountControlHasChanged);
    }

    /// <summary>
    /// Constructor that starts the process of creating a new user.
    /// </summary>
    public ADpUser() : base()
    {
        IsNew                    = true;
        UserAccountControlSetter = new UserAccountControl(UserAccountControlHasChanged);
    }



    /// <summary>
    ///  Creates a User from a set of Active Directory Attributes.
    /// </summary>
    /// <param name="attributes"></param>
    public ADpUser(SearchResultAttributeCollection attributes)
    {
        InCreationMode        = true;
        IsFromActiveDirectory = true;
        
        bool samAccountFound = false;
        bool upnFound = false;

        foreach (DirectoryAttribute dirObj in attributes.Values)
        {
            int ival;

            switch (dirObj.Name)
            {
                case "sAMAccountName":
                    samAccountFound = true;
                    SAMAccount      = dirObj[0].ToString();
                    break;
                case "distinguishedName": DistinguishedName = dirObj[0].ToString(); break;
                case "cn":                CommonName        = dirObj[0].ToString(); break;
                case "displayName":       DisplayName       = dirObj[0].ToString(); break;
                case "givenName":         FirstName         = dirObj[0].ToString(); break;
                case "sn":                LastName          = dirObj[0].ToString(); break;
                case "title":             Title             = dirObj[0].ToString(); break;
                case "userPrincipalName":
                    upnFound = true;
                    UPN      = dirObj[0].ToString();
                    break;
                case "department":      DepartmentFullName = dirObj[0].ToString(); break;
                case "mail":            Email              = dirObj[0].ToString(); break;
                case "manager":         Manager            = dirObj[0].ToString(); break;
                case "telephoneNumber": Phone              = dirObj[0].ToString(); break;
                case "description":     Description        = dirObj[0].ToString(); break;
                case "memberOf":
                    for (int i = 0; i < dirObj.Count; i++)
                    {
                        MemberOf.Add(dirObj[i].ToString());
                    }

                    break;
                case "userAccountControl":
                    if (!int.TryParse(dirObj[0].ToString(), out ival))
                    {
                        throw new ArgumentException("DA is not a int value - key [" + dirObj.Name + "] value: [" + dirObj[0]! + "]");
                    }

                    UserAccountControlSetter = new UserAccountControl(ival, UserAccountControlHasChanged);
                    break;

                case "lastLogon":   LastLogon   = ADFunctions.GetDateTime_FromLDAPPropertyLong(dirObj[0].ToString()!); break;
                case "whenChanged": WhenChanged = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!); break;
                case "whenCreated": WhenCreated = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!); break;
                case "badPwdCount":
                    if (!int.TryParse(dirObj[0].ToString(), out ival))
                    {
                        throw new ArgumentException("DA is not an integer value - key [" + dirObj.Name + "] value: [" + dirObj[0]! + "]");
                    }

                    BadPasswordCount = ival;
                    break;
                case "badPasswordTime": BadPasswordDateTime = ADFunctions.GetDateTime_FromLDAPPropertyLong(dirObj[0].ToString()); break;
                case "lockoutTime":     LockOutDateTime     = ADFunctions.GetDateTime_FromLDAPPropertyLong(dirObj[0].ToString()); break;
                case "lockoutDuration":
                    if (!long.TryParse(dirObj[0].ToString(), out long lval))
                    {
                        throw new ArgumentException("DA is not a long value - key [" + dirObj.Name + "] value: [" + dirObj[0] + "]");
                    }

                    LockOutDurationNS = lval;
                    break;
                case "pwdLastSet": PasswordLastSet = ADFunctions.GetDateTime_FromLDAPPropertyLong(dirObj[0].ToString()); break;
                case "msDS-UserPasswordExpiryTimeComputed":
                    PasswordExpiryDateTime = ConvertADExpirationDates(dirObj);
                    break;
                case "accountExpires":
                    AccountExpirey = ConvertADExpirationDates(dirObj);
                    break;
                // We do not store a download password.
                case "password": break;
                case "physicalDeliveryOfficeName":
                    Office = dirObj[0].ToString();
                    break;
                case "msDS-User-Account-Control-Computed":
                    // This is a read only attribute that is calculated by AD.  It is not settable.
                    if (!int.TryParse(dirObj[0].ToString(), out ival))
                    {
                        throw new ArgumentException("DA is not a int value - key [" + dirObj.Name + "] value: [" + dirObj[0]! + "]");
                    }                                             

                    //UserAccountControlSetter = new UserAccountControl(ival, UserAccountControlHasChanged);
                    MsDsUserAccountControlGetter = new MsDsUserAccountControl(ival);
                    break;
            }
        }
        
        
        if (DistinguishedName == null | DistinguishedName == string.Empty)
            throw new
                ArgumentException("No Distinguished Name found in the orgUnit object.  Anytime you retrieve an object from Active Directory you must retrieve this attribute.");
        
        // Calculate ParentPath
        ParentPath = new ADSPath(DistinguishedName).GetParent();
        
        InCreationMode = false;
    }
    
    
    private ADpExpirationValue ConvertADExpirationDates(DirectoryAttribute adValue)
    {
        // This is a special case for several AD attributes (like msDS-UserPasswordExpiryTimeComputed and accountExpires)
        // which are stored as a long value representing ticks.
        DateTimeOffset calculatedDate;
        
        if (!long.TryParse(adValue[0].ToString(), out long ticks))
        {
            throw new ArgumentException("AD Attribute is not a long value - key [" + adValue.Name + "] value: [" + adValue[0] + "]");
        }

        return new ADpExpirationValue(ticks);
    }

    

    
    /// <summary>
    /// The object class of this object.
    /// </summary>
    public override string ObjectClassName
    {
        get { return ADpCommon.OBJ_CLASS_USER; }
    }


    /// <summary>
    /// Used in prompts and error messages to name the object type we are working with,
    /// </summary>
    public override string ObjectTypeDescription
    {
        get { return "user"; }
    }


    /// <summary>
    /// Some objects have complicated values (for instance - user with UserAccountControl) that need to be synchronized
    /// or have other changes made to the core object before saving.  This method is called before saving the object to Active Directory
    /// to allow the derived object to perform any necessary pre-save operations.
    /// </summary>
    /// <remarks>Is Internal because Processors need access to this.</remarks>
    internal override void SyncPreSave()
    {
        // TODO  - Do not think this is necessary any longer now that we have the UserAccountControlSetter.HasChangedValue property.  But leaving it here for now.

        // See if UserAccountControl has been changed.  If so, we need to add it to the Attributes To Update List
        if (UserAccountControlSetter.HasChangedValue)
        {
            string key = ADpCommon.ATN_USER_ACCOUNT_CONTROL;
            AttrUserAccountControl attrValue = new(UserAccountControlSetter.Value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
        base.SyncPreSave();     
    }


    /// <summary>
    /// Is true, when this object was created from an existing Active Directory object. 
    /// </summary>
    public bool IsFromActiveDirectory { get; protected set; } = false;

    #region "Status Properties"


    /// <summary>
    /// Returns if the user account is disabled.  This is a read only property that is calculated based on the MsDsUserAccountControl attribute.  If the MsDsUserAccountControl attribute
    /// was not retrieved from Active Directory, this property will return null indicating it is unknown.  This property will return the status of the value when it was read from AD.
    /// <para>>It is possible you have set it to re-enabled, but not sent the change to AD yet.  In this case it still shows as disabled.</para>
    /// </summary>
    public bool? IsDisabled
    {
        get
        {
            if (UserAccountControlSetter == null)
                return null;
            return (UserAccountControlSetter.IsDisabled);
        }
    }

    
    /// <summary>
    /// Returns if the user account is enabled.  This is a read only property that is calculated based on the MsDsUserAccountControl attribute.  If the MsDsUserAccountControl attribute
    /// was not retrieved from Active Directory, this property will return null indicating it is unknown.  This property will return the status of the value when it was read from AD.
    /// <para>>It is possible you have set it to disabled, but not sent the change to AD yet.  In this case it still shows as enabled.</para>
    /// </summary>
    public bool? IsEnabled
    {
        get
        {
            if (UserAccountControlSetter == null)
                return null;
            return (UserAccountControlSetter.IsEnabled);
        }
    }
    
    
    /// <summary>
    /// Enables the user account.  This is a convenience method that sets the UserAccountControl attribute to enable the account.
    /// If the account is already enabled, this method does nothing.
    /// </summary>
    public void EnableAccount()
    {
        UserAccountControlSetter.EnableAccount();
    }


    /// <summary>
    /// Disables the user account.  This is a convenience method that sets the UserAccountControl attribute to disable the account.
    /// If the account is already disabled, this method does nothing.
    /// </summary>
    public void DisableAccount()
    {
        UserAccountControlSetter.DisableAccount();
    }
    #endregion


    #region Info Attributes


    /// <summary>
    /// All the groups a user is a member of.
    /// <para> Will be null if this attribute was not read form AD</para>
    /// </summary>
    public HashSet<string>? MemberOf { get; protected set; } = null;


    /// <summary>
    /// Number of bad passwords entered by the user.  This is reset when a valid password is entered.
    /// <para>Will be null if this attribute was not read form AD</para>
    /// </summary>
    public int? BadPasswordCount { get; protected set; }

    /// <summary>
    ///    Date and Time of the last bad password attempt by the user.
    /// <para> Will be null if this attribute was not read form AD</para>
    /// </summary>
    public DateTimeOffset? BadPasswordDateTime { get; protected set; }

    
    /// <summary>
    ///   Date and Time of the last logon by the user.
    /// <para> Will be null if this attribute was not read form AD</para>
    /// </summary>
    public DateTimeOffset? LastLogon { get; protected set; }


    /// <summary>
    ///   Date and Time the user account was locked out in Active Directory.
    /// <para> Will be null if this attribute was not read form AD</para>
    /// </summary>
    public DateTimeOffset? LockOutDateTime { get; protected set; }

    /// <summary>
    ///     Amount of time Account has been locked out in nano seconds.
    /// </summary>
    public long? LockOutDurationNS { get; protected set; }



    /// <summary>
    /// Date and Time the password will expire.  This is computed based on the password policy in Active Directory.
    /// <para>Will be null if this attribute was not read form AD</para>
    /// </summary>
    public ADpExpirationValue? PasswordExpiryDateTime { get; internal set; } = null;

    /// <summary>
    /// Date and time the account expires.
    /// </summary>
    public ADpExpirationValue? AccountExpirey { get; internal set; } = null;

    
    /// <summary>
    /// The date the user's password will expire.  This is not an updatable field.  It is calculated based on the password policy that applies for the user.
    /// </summary>
    public DateTimeOffset? PasswordExpiration { get; private set; }


    #endregion

    #region Attributes

    /// <summary>
    /// Full Name of the Department.
    /// </summary>
    public string DepartmentFullName
    {
        get;
        set
        {
            field = value;

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
    /// The Email Address of the User.  This is the email address that will be used in the address book and other places.
    /// </summary>
    public string Email
    {
        get;
        set
        {
            field = value;

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

    
    /// <summary>
    /// The First Name of the User.  This is the name that will be used in the address book and other places.
    /// </summary>
    public string FirstName
    {
        get;
        set
        {
            field = value;

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


    /// <summary>
    /// The Last Name of the User.  This is the name that will be used in the address book and other places.
    /// </summary>
    public string LastName  
    {
        get;
        set
        {
            field = value;

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

    
    /// <summary>
    /// The Manager of the User.  This is the name that will be used in the address book and other places.
    /// </summary>
    public string Manager
    {
        get;
        set
        {
            field = value;

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
    

    /// <summary>
    /// Office user is considered a part of.
    /// </summary>
    public string Office
    {
        get;
        set
        {
            field = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string         key       = ADpCommon.ATN_OFFICE;
            AttrOffice attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }



    /// <summary>
    /// Password is a special case.  Return the password bytes.
    /// </summary>
    internal byte[] GetPasswordBytes
    {
        get => _password;
    }

    /// <summary>
    /// The Password of the User.  This is the password that will be used to log in to the system.
    /// </summary>
    public string Password          
    {
        set
        {
            string pwd = $"\"{value}\"";
            
            _password           = Encoding.Unicode.GetBytes(pwd);
            PasswordHasBeenSet = true;
            
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
        get;
        set
        {
            if (InCreationMode)
            {
                field = value;

                // Do not add attribute to modification list if in initial creation mode.
                return;
            }
            
            if (value != DateTimeOffset.MinValue && value != DateTimeOffset.MaxValue)
                throw new ArgumentException("PasswordLastSet can only be set to 0 Ticks (Expired now) OR DateTimeOffset.MaxValue (never expires)", nameof(value));

            // Convert DateTime offset to date time but in UTC Time.
            DateTime utc = value.UtcDateTime;
            field = utc;

            string              key       = ADpCommon.ATN_PASSWORD_LAST_SET;
            AttrPasswordLastSet attrValue = new(utc, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }

    /// <summary>
    /// The Phone Number of the User.  This is the phone number that will be used in the address book and other places.
    /// </summary>
    public string Phone
    {
        get;
        set
        {
            field = value;

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
    public string Title
    {
        get;
        set
        {
            field = value;

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
        get;
        set
        {
            field = value;

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
        get;
        set
        {
            field = value;

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


    /// <summary>
    ///  User Account Control value.  Which is actually a bunch of flags that create the value.
    /// </summary>
    /*public int UserAccountControl
    {
        get;
        set
        {
            field = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string         key       = ADpCommon.ATN_USER_ACCOUNT_CONTROL;
            AttrUserAccountControl attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }
    */

    #endregion


    /// <summary>
    /// Provides access to the UserAccountControl object which allows user to manipulate / view all the components
    /// Note:  This will be null IF the object was read from Active Directory, but the UserAccountControl attribute was not retrieved.
    /// You CANNOT set UserAccoutnControl properties on an AD object that did not retrieve it first.
    /// that make up this value.
    /// </summary>
    public UserAccountControl UserAccountControlSetter { get; }
    
    public MsDsUserAccountControl? MsDsUserAccountControlGetter { get; }


    private void UserAccountControlHasChanged(int value)
    {
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
    

    #region "Actions"

    /// <summary>
    /// Sets the lockoutTime attribute to 0, which unlocks the account.
    /// This is a special case because it is not a normal attribute that can be set.
    /// </summary>
    public void UnlockAccount()
    {
        string                 key       = "lockoutTime"; 
        AttrLockOutTime attrValue = new(0, EnumAttributeOperation.Modify);
        
        if (!AttributesToUpdate.TryAdd(key, attrValue))
        {
            AttributesToUpdate[key] = attrValue;
        }

    }



    #endregion

}

