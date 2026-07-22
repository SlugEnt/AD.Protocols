using SlugEnt.AD.Protocols.Attributes;
using System.DirectoryServices.Protocols;


namespace SlugEnt.AD.Protocols;

/// <summary>
/// Providea  Class that can update an Active Directory Password Policy object.
/// </summary>
public class ADpPasswordPolicyUpdater
{
    private string?      _CommonName;
    private string?      _Name;
    private string?      _Description;
    private string?      _DisplayName;
    private int?         _minLength                   = 10;
    private int?         _lockOutThreshold            = 10;
    private bool?        _complexityEnabled           = true;
    private bool?        _reversibleEncryptionEnabled = false;
    private int?         _historySize                 = 24;
    private int?         _settingsPrecedence;
    private List<string> _appliesTo;

    private TimeSpan? _minAge = new TimeSpan(1,
                                             0,
                                             0,
                                             0);

    private TimeSpan? _maxAge = new TimeSpan(90,
                                             0,
                                             0,
                                             0);

    private TimeSpan? _lockoutDuration = new TimeSpan(0,
                                                      0,
                                                      15,
                                                      0);

    private TimeSpan? _lockoutObservationWindow = new TimeSpan(0,
                                                               0,
                                                               30,
                                                               0);


    /// <summary>
    ///     The list of attributes to update
    /// </summary>
    protected Dictionary<string, AttributeBase> AttributesToUpdate = new();


    /// <summary>
    ///     This is the most preferred constructor if you have already read the policy from AD as it almost 100% assuredly will
    ///     pull the policy without error.  Do not call this if this a new policy, as it will throw an exception.
    /// </summary>
    /// <param name="readOnlyPasswordPolicy"></param>
    public ADpPasswordPolicyUpdater(ADpReadOnlyPasswordPolicy policy)
    {
        string? value = policy.DistinguishedName;
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException(nameof(value), "Distinguished Name cannot be null or empty.");

        IsNew          = false;
        InCreationMode = true;

        // Set fields
        DistinquishedName        = value;
        NameChg                  = policy.Name;
        CommonNameChg            = policy.CommonName;
        DisplayNameChg           = policy.DisplayName;
        DescriptionChg           = policy.Description;
        SettingsPrecedence       = policy.PasswordSettingsPrecedence;
        MinimumLength            = policy.PasswordMinLength;
        MinimumAge               = policy.PasswordMinAge;
        MaximumAge               = policy.PasswordMaxAge;
        LockoutObservationWindow = policy.PasswordLockoutObservationWindow;
        ComplexityEnabled        = policy.PasswordComplexityEnabled;
        HistoryCount = policy.PasswordHistoryLength;
        LockOutThreshold = policy.PasswordLockThreshold;
        LockoutDuration = policy.PasswordLockoutDuration;
        ReversibleEncryptionEnabled = policy.PasswordReversibleEncryptionEnabled;
        AppliesTo = policy.AppliesTo ?? new List<string>();

        // Turn off in Creation mode.
        InCreationMode = false;
    }

    /// <summary>
    /// Used when creating the object from an existing AD object.  This prevents the attributes from being added to the modified list during initial setting.
    /// </summary>
    protected bool InCreationMode { get; set; }


    /// <summary>
    /// Creates a mew Password Policy with the given name.  You can override CommonName, DisplayNams by setting the respective properties.
    /// USe this constructor if you are creating a new Password Policy object in Active Directory.  If you are updating an existing policy, use the constructor that takes an ADpReadOnlyPasswordPolicy object.
    /// </summary>
    /// <param name="policyName"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ADpPasswordPolicyUpdater(string policyName)
    {
        if (string.IsNullOrEmpty(policyName))
            throw new ArgumentNullException(nameof(policyName));

        // Set default Values
        IsNew              = true;
        NameChg            = policyName;
        DisplayNameChg     = policyName;
        
        SettingsPrecedence = 5;
        MinimumAge = new TimeSpan(1,
                                  0,
                                  0,
                                  0);
        MaximumAge = new TimeSpan(180,
                                  0,
                                  0,
                                  0);
        LockoutObservationWindow = new TimeSpan(0,
                                                0,
                                                30,
                                                0);
        ComplexityEnabled = true;
        HistoryCount      = 10;
        LockOutThreshold  = 10;
        LockoutDuration = new TimeSpan(0,
                                       0,
                                       10,
                                       0);
        ReversibleEncryptionEnabled = false;
    }



    /// <summary>
    /// If true, this will be a creation, not an update.
    /// </summary>
    protected bool IsNew { get; set; }


    /// <summary>
    /// If this is true, the applies to list has been updated and the SetAppliesTo method should be called to update the policy in AD.  THIS MUST BE MANUALLY SET BY CALLER!
    /// </summary>
    public bool AppliesToSetFlag { get { return _appliesToSetFlag;}
        set
        {
            _appliesToSetFlag = value;
            if (value)
            {
                // If the flag is set, we need to add the AppliesTo attribute to the update list.
                string key = ADpCommon.ATN_PASSPOL_APPLIESTO;
                AttrPasswordAppliesTo attrValue = new(AppliesTo, EnumAttributeOperation.Modify);
                if (!AttributesToUpdate.TryAdd(key, attrValue))
                {
                    AttributesToUpdate[key] = attrValue;
                }
            }
            else
            {
                // If the flag is not set, we need to remove the AppliesTo attribute from the update list.
                AttributesToUpdate.Remove(ADpCommon.ATN_PASSPOL_APPLIESTO);
            }
        } }
    private bool _appliesToSetFlag = false; 



    /// <summary>
    /// Provides the List of Distinguished Names that this Password Policy applies to.  This is a list of DNs of users or security groups that the policy applies to.
    /// <para>Warning:  If you update the list directly YOU must call the SetAppliesToUpdatedFlag to true or else the changes are not propogated</para>
    /// </summary>
    public List<string> AppliesTo
    {
        get => _appliesTo;
        set
        {
            if (value == null || value.Count == 0)
            {
                _appliesTo = new List<string>();
                return;
            }
            _appliesTo = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode) return;

            string         key       = ADpCommon.ATN_PASSPOL_APPLIESTO;
            AttrPasswordAppliesTo attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }
    /// <summary>
    /// The CN or common name of the group.  This is the name that will be used to create the group in AD.
    /// </summary>
    public string? CommonNameChg
    {
        get => _CommonName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value), "Common Name cannot be null or empty.");

            _CommonName = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_CN;
            AttrCommonName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    /// The Name of the Password Policy.
    /// </summary>
    public string? NameChg
    {
        get => _Name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value), "Name cannot be null or empty.");

            _Name = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_NAME;
            AttrName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }



    /// <summary>
    /// Description of the Password Policy.  This is a free form text field that can be used to describe the policy.
    /// </summary>
    public string? DescriptionChg
    {
        get => _Description;
        set
        {
            if (value == null)
                value = "";
            _Description = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_DESCRIPTION;
            AttrDescription attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    /// The Display Name of the Password Policy.  This is the name that will be displayed in the address book and other places.
    /// </summary>
    public string? DisplayNameChg
    {
        get => _DisplayName;
        set
        {
            if (value == null)
                value = "";
            _DisplayName = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_DISPLAYNAME;
            AttrDisplayName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    ///     Distinquished Name.  This is the unique identifier of the Password Policy in Active Directory.
    /// </summary>
    public string DistinquishedName { get; protected set; }


    /// <summary>
    /// The Precedence of the Setting
    /// </summary>
    public int? SettingsPrecedence
    {
        get => _settingsPrecedence;
        set
        {
            int val = value != null ? (int)value : 0;
            _settingsPrecedence = val;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_PASSPOL_PRECEDENCE;
            AttrPasswordPrecedence attrValue = new(val, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    /// Minimum allowed length of password
    /// </summary>
    public int? MinimumLength
    {
        get => _minLength;
        set
        {
            int val = value != null ? (int)value : 0;
            _minLength = val;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_PASSPOL_MINLENGTH;
            AttrPasswordLength attrValue = new(val, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }



    /// <summary>
    /// Number of failed logins before triggering a lockout
    /// </summary>
    public int? LockoutThreshold
    {
        get => _lockOutThreshold;
        set
        {
            int val = value != null ? (int)value : 0;
            _lockOutThreshold = val;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_PASSPOL_LOCKOUTTHRESHOLD;
            AttrPasswordLockoutThreshold attrValue = new(val, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }



    public TimeSpan? LockoutDuration
    {
        get => _lockoutDuration;
        set
        {
            if (value == null)
                return;

            TimeSpan nonNullTimeSpan = (TimeSpan)value;
            long     tickValue       = ADFunctions.SetAD_TimeSpan(nonNullTimeSpan);
            _lockoutDuration = nonNullTimeSpan;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_PASSPOL_LOCKOUTDURATION;
            AttrPasswordLockoutDuration attrValue = new(nonNullTimeSpan, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }



    public TimeSpan? LockoutObservationWindow
    {
        get => _lockoutObservationWindow;
        set
        {
            if (value == null)
                return;

            TimeSpan nonNullTimeSpan = (TimeSpan)value;
            long     tickValue       = ADFunctions.SetAD_TimeSpan(nonNullTimeSpan);
            _lockoutObservationWindow = nonNullTimeSpan;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_PASSPOL_LOCKOUTOBSERVATIONWINDOW;
            AttrPasswordLockOutObsWindow attrValue = new(nonNullTimeSpan, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    /// Maximum Allowed age of password
    /// </summary>
    public TimeSpan? MaximumAge
    {
        get => _maxAge;
        set
        {
            if (value == null)
                return;

            TimeSpan nonNullTimeSpan = (TimeSpan)value;
            long     tickValue       = ADFunctions.SetAD_TimeSpan(nonNullTimeSpan);
            _maxAge = nonNullTimeSpan;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_PASSPOL_MAXAGE;
            AttrPasswordMaxAge attrValue = new(nonNullTimeSpan, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }

    /// <summary>
    /// Minimum Allowed age of password
    /// </summary>
    public TimeSpan? MinimumAge
    {
        get => _minAge;
        set
        {
            if (value == null)
                return;

            TimeSpan nonNullTimeSpan = (TimeSpan)value;
            long     tickValue       = ADFunctions.SetAD_TimeSpan(nonNullTimeSpan);
            _minAge = nonNullTimeSpan;


            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_PASSPOL_MINAGE;
            AttrPasswordMinAge attrValue = new(nonNullTimeSpan, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }



    /// <summary>
    /// Amount of password history to keep
    /// </summary>
    public int? HistoryCount
    {
        get => _historySize;
        set
        {
            int val = value != null ? (int)value : 0;
            _historySize = val;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_PASSPOL_HISTLEN;
            AttrPasswordHistoryCount attrValue = new(val, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }



    /// <summary>
    /// Number of failed login attempts before account is locked out
    /// </summary>
    public int? LockOutThreshold
    {
        get => _lockOutThreshold;
        set
        {
            int val = value != null ? (int)value : 0;
            _lockOutThreshold = val;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_PASSPOL_LOCKOUTTHRESHOLD;
            AttrPasswordLockoutThreshold attrValue = new(val, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }



    /// <summary>
    /// Whether Password complexity should be enforced
    /// </summary>
    public bool? ComplexityEnabled
    {
        get => _complexityEnabled;
        set
        {
            bool val = value != null ? (bool)value : true;
            _complexityEnabled = val;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_PASSPOL_COMPLEXITYENABLED;
            AttrPasswordComplexity attrValue = new(val, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    /// Whether reversible password encryption should be used
    /// </summary>
    public bool? ReversibleEncryptionEnabled
    {
        get => _reversibleEncryptionEnabled;
        set
        {
            bool val = value != null ? (bool)value : true;
            _reversibleEncryptionEnabled = val;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string key       = ADpCommon.ATN_PASSPOL_REVERSEDENCRYPTIONENABLED;
            AttrPasswordReversible attrValue = new(val, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    ///     Performs all final settings to prepare the user's attributes to be updated.  For instance it will look at the
    ///     current disposition of AccountEnable to see how it should be set.
    /// </summary>
    /// <returns></returns>
    public AttributeBase[] GetAttributes()
    {

            AttrObjectClass objectClass = new AttrObjectClass("msDS-PasswordSettings");
            if (!AttributesToUpdate.TryAdd(ADpCommon.ATN_OBJECT_CLASS, objectClass))
            {
                AttributesToUpdate[ADpCommon.ATN_OBJECT_CLASS] = objectClass;
            }

        return AttributesToUpdate.Values.ToArray();
    }



    /// <summary>
    /// Returns the list of attributes to be updated.  This will be a list of DirectoryAttribute objects that can be used to 
    /// </summary>
    /// <returns></returns>
    public DirectoryAttribute[] GetDirectoryAttributesNew()
    {
        DirectoryAttribute[]     attributes    = Array.Empty<DirectoryAttribute>();
        List<DirectoryAttribute> dirAttributes = new List<DirectoryAttribute>();
        dirAttributes.Add(new DirectoryAttribute("objectClass", "msDS-PasswordSettings")); 

        foreach (KeyValuePair<string, AttributeBase> attributeBase in AttributesToUpdate)
        {
            dirAttributes.Add(attributeBase.Value.DA);
        }

        return dirAttributes.ToArray();
    }


    /// <summary>
    /// Password Minimum Length Attribute
    /// </summary>
    public class AttrPasswordLength : AttributeInt
    {
        public AttrPasswordLength(int value,
                                  EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_PASSPOL_MINLENGTH, changeMode)
        {
            DirectoryAttribute.Add(value.ToString());
        }
    }


    /// <summary>
    /// Password Min Age Attribute
    /// </summary>
    public class AttrPasswordMinAge : AttributeTimeSpan
    {
        public AttrPasswordMinAge(TimeSpan value,
                                  EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_PASSPOL_MINAGE, changeMode)
        {
            DirectoryAttribute.Add("-" + value.Ticks);
        }
    }


    /// <summary>
    /// Password Max Age Attribute
    /// </summary>
    public class AttrPasswordMaxAge : AttributeTimeSpan
    {
        public AttrPasswordMaxAge(TimeSpan value,
                                  EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_PASSPOL_MAXAGE, changeMode)
        {
            //DirectoryAttribute.Add(value.ToString("c"));
            DirectoryAttribute.Add("-" + value.Ticks.ToString());
        }
    }



    /// <summary>
    /// Password Lockout Duration
    /// </summary>
    public class AttrPasswordLockoutDuration : AttributeTimeSpan
    {
        public AttrPasswordLockoutDuration(TimeSpan value,
                                           EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_PASSPOL_LOCKOUTDURATION, changeMode)
        {
            //DirectoryAttribute.Add(value.ToString("c"));
            DirectoryAttribute.Add("-" + value.Ticks.ToString());
        }
    }



    /// <summary>
    /// Password Lockout Observation Window
    /// </summary>
    public class AttrPasswordLockOutObsWindow : AttributeTimeSpan
    {
        public AttrPasswordLockOutObsWindow(TimeSpan value,
                                            EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_PASSPOL_LOCKOUTOBSERVATIONWINDOW, changeMode)
        {
            DirectoryAttribute.Add("-" + value.Ticks.ToString());
        }
    }


    /// <summary>
    /// Password History Count Attribute
    /// </summary>
    public class AttrPasswordHistoryCount : AttributeInt
    {
        public AttrPasswordHistoryCount(int value,
                                        EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_PASSPOL_HISTLEN, changeMode)
        {
            DirectoryAttribute.Add(value.ToString());
        }
    }



    /// <summary>
    /// Password Complexity Attribute
    /// </summary>
    public class AttrPasswordComplexity : AttributeStringSingle
    {
        public AttrPasswordComplexity(bool value,
                                      EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_PASSPOL_COMPLEXITYENABLED, changeMode)
        {
            string adBool = value ? "TRUE" : "FALSE";
            DirectoryAttribute.Add(adBool.ToString());
        }
    }



    /// <summary>
    /// Password Complexity Attribute
    /// </summary>
    public class AttrPasswordReversible : AttributeStringSingle
    {
        public AttrPasswordReversible(bool value,
                                      EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_PASSPOL_REVERSEDENCRYPTIONENABLED, changeMode)
        {
            string adBool = value ? "TRUE" : "FALSE";
            DirectoryAttribute.Add(adBool.ToString());
        }
    }


    /// <summary>
    /// Password Lockout Threshold Attribute
    /// </summary>
    public class AttrPasswordLockoutThreshold : AttributeInt
    {
        public AttrPasswordLockoutThreshold(int value,
                                            EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_PASSPOL_LOCKOUTTHRESHOLD, changeMode)
        {
            DirectoryAttribute.Add(value.ToString());
        }
    }


    /// <summary>
    /// Password Precedence Attribute
    /// </summary>
    public class AttrPasswordPrecedence : AttributeInt
    {
        public AttrPasswordPrecedence(int value,
                                      EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_PASSPOL_PRECEDENCE, changeMode)
        {
            DirectoryAttribute.Add(value.ToString());
        }
    }

    public class AttrPasswordAppliesTo : AttributeList
    {
        public AttrPasswordAppliesTo(List<string> values,
                                     EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_PASSPOL_APPLIESTO, changeMode)
        {
            if (values == null || values.Count == 0)
                return;
            
            foreach (string item in values)
            {
                DirectoryAttribute.Add(item);
            }
        }
    }
}