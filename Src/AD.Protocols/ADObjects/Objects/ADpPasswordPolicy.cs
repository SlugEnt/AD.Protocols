using AD.Protocols.ADObjects.Fields;
using SlugEnt.AD.Protocols;
using SlugEnt.AD.Protocols.Attributes;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;

namespace AD.Protocols.ADObjects.Objects;

public class ADpPasswordPolicy : ADpBaseObject
{
    
    
    public override string ObjectClassName
    {
        get { return "msDS-PasswordSettings"; }
    }

    public override string ObjectTypeDescription
    {
        get { return "Password Policy"; }
    }


    /// <summary>
    /// Constructs a new Password Policy object with the specified name and parent path.  This constructor is used when creating a new Password Policy in Active Directory.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="parentPath"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ADpPasswordPolicy(string name, ADSPath parentPath)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        InCreationMode = true;

        Name       = name;
        CommonName = name;
        IsNew      = true;
        ParentPath = parentPath;

        InCreationMode = false;
    }

    

    /// <summary>
    /// Constructs a Password Policy from Active Directory Attributes.
    /// </summary>
    /// <param name="attributes"></param>
    /// <exception cref="ArgumentException"></exception>
    public ADpPasswordPolicy(SearchResultAttributeCollection attributes)
    {
        InCreationMode = true;
        bool    distinguishedNameFound = false;

        foreach (DirectoryAttribute dirObj in attributes.Values)
        {
                switch (dirObj.Name)
                {
                    case "description": Description = dirObj[0].ToString(); break;
                    case "distinguishedName":
                        DistinguishedName = dirObj[0].ToString();
                        distinguishedNameFound      = true;
                        break;
                    case "displayName": DisplayName = dirObj[0].ToString(); break;
                    case "whenChanged": WhenChanged = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!); break;
                    case "whenCreated": WhenCreated = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!); break;
                    case "name":        Name        = dirObj[0].ToString(); break;
                    case "msDS-LockoutObservationWindow":
                        Result<TimeSpan> lockoutObsWindow = ADFunctions.GetTimeSpan_FromAD(dirObj[0].ToString()!);
                        LockoutObservationWindow = lockoutObsWindow.Value;

                        break;
                    case "msDS-MinimumPasswordLength": MinimumLength = int.Parse(dirObj[0].ToString()!); break;
                    case "msDS-MinimumPasswordAge":
                        Result<TimeSpan> minAgeResult = ADFunctions.GetTimeSpan_FromAD(dirObj[0].ToString()!);
                        if (minAgeResult.IsFailed)
                            throw new ArgumentException($"Failed to parse msDS-MinimumPasswordAge: [ {dirObj[0].ToString()} ] into a TimeSpan");

                        MinimumAge = minAgeResult.Value;
                        break;

                    case "msDS-MaximumPasswordAge":
                        Result<TimeSpan> maxAgeResult = ADFunctions.GetTimeSpan_FromAD(dirObj[0].ToString()!);
                        if (maxAgeResult.IsFailed)
                            throw new ArgumentException($"Failed to parse msDS-MaximumPasswordAge: [ {dirObj[0].ToString()} ] into a TimeSpan");
                        MaximumAge = maxAgeResult.Value;
                        break;
                    
                    case "msDS-PasswordHistoryLength": HistoryCount = int.Parse(dirObj[0].ToString()!); break;
                    case "msDS-LockoutThreshold":      LockoutThreshold = int.Parse(dirObj[0].ToString()!); break;
                    case "msDS-LockoutDuration":
                        Result<TimeSpan> lockoutDurationResult = ADFunctions.GetTimeSpan_FromAD(dirObj[0].ToString()!);
                        if (lockoutDurationResult.IsFailed)
                            throw new ArgumentException($"Failed to parse msDS-LockoutDuration: [ {dirObj[0].ToString()} ] into a TimeSpan");
                        LockoutDuration = lockoutDurationResult.Value;
                        break;
                    case "msDS-PasswordComplexityEnabled":           ComplexityEnabled = bool.Parse(dirObj[0].ToString()!); break;
                    case "msDS-PasswordReversibleEncryptionEnabled": ReversibleEncryptionEnabled = bool.Parse(dirObj[0].ToString()!); break;
                    case "msDS-PasswordSettingsPrecedence":          Precedence          = int.Parse(dirObj[0].ToString()!); break;
/*                    case "msDS-PSOAppliesTo":
                        AppliesTo = new List<string>();
                        for (int i = 0; i < dirObj.Count; i++)
                        {
                            AppliesTo.Add(dirObj[i].ToString()!);
                        }

                        break;
*/
                    case "cn": CommonName = dirObj[0].ToString(); break;

                    default:
                        string msg = "Unexpected data - " + dirObj.Name;
                        break;
                }
        }

        if (DistinguishedName == null | DistinguishedName == string.Empty)
            throw new
                ArgumentException("No Distinguished Name found in the orgUnit object.  Anytime you retrieve an object from Active Directory you must retrieve this attribute.");
        
        // Calculate ParentPath
        Result<ADSPath> parentPathResult = new ADSPath(DistinguishedName).GetParent();
        ParentPath = parentPathResult.IsSuccess ? parentPathResult.Value : null;

        InCreationMode = false;

    }


    public MultiValuedDNAttribute AppliesTo { get; internal set; } = new MultiValuedDNAttribute("msDS-PSOAppliesTo");


    #region Attributes

    /// <summary>
    /// The Precedence of the policy over other polices.  Lower numbers have higher precedence.  The default is 0, which is the lowest precedence.
    /// </summary>
    public int? Precedence
    {
        get;
        set
        {
            int val = value != null ? (int)value : 0;
            field = val;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string                 key       = ADpCommon.ATN_PASSPOL_PRECEDENCE;
            AttrPasswordPrecedence attrValue = new(val, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    } = null;


    /// <summary>
    /// Minimum allowed length of password
    /// </summary>
    public int? MinimumLength
    {
        get;
        set
        {
            int val = value != null ? (int)value : 0;
            field = val;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string             key       = ADpCommon.ATN_PASSPOL_MINLENGTH;
            AttrPasswordLength attrValue = new(val, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    } = null;



    /// <summary>
    /// Number of failed logins before triggering a lockout
    /// </summary>
    public int? LockoutThreshold
    {
        get;
        set
        {
            int val = value != null ? (int)value : 0;
            field = val;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string                       key       = ADpCommon.ATN_PASSPOL_LOCKOUTTHRESHOLD;
            AttrPasswordLockoutThreshold attrValue = new(val, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    } = null;



    public TimeSpan? LockoutDuration
    {
        get;
        set
        {
            if (value == null)
                return;

            TimeSpan nonNullTimeSpan = (TimeSpan)value;
            long     tickValue       = ADFunctions.SetAD_TimeSpan(nonNullTimeSpan);
            field = nonNullTimeSpan;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string                      key       = ADpCommon.ATN_PASSPOL_LOCKOUTDURATION;
            AttrPasswordLockoutDuration attrValue = new(nonNullTimeSpan, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    } = null;



    public TimeSpan? LockoutObservationWindow
    {
        get;
        set
        {
            if (value == null)
                return;

            TimeSpan nonNullTimeSpan = (TimeSpan)value;
            long     tickValue       = ADFunctions.SetAD_TimeSpan(nonNullTimeSpan);
            field = nonNullTimeSpan;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string                       key       = ADpCommon.ATN_PASSPOL_LOCKOUTOBSERVATIONWINDOW;
            AttrPasswordLockOutObsWindow attrValue = new(nonNullTimeSpan, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    } = null;


    /// <summary>
    /// Maximum Allowed age of password
    /// </summary>
    public TimeSpan? MaximumAge
    {
        get;
        set
        {
            if (value == null)
                return;

            TimeSpan nonNullTimeSpan = (TimeSpan)value;
            long     tickValue       = ADFunctions.SetAD_TimeSpan(nonNullTimeSpan);
            field = nonNullTimeSpan;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string             key       = ADpCommon.ATN_PASSPOL_MAXAGE;
            AttrPasswordMaxAge attrValue = new(nonNullTimeSpan, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    } = null;

    /// <summary>
    /// Minimum Allowed age of password
    /// </summary>
    public TimeSpan? MinimumAge
    {
        get;
        set
        {
            if (value == null)
                return;

            TimeSpan nonNullTimeSpan = (TimeSpan)value;
            long     tickValue       = ADFunctions.SetAD_TimeSpan(nonNullTimeSpan);
            field = nonNullTimeSpan;


            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string             key       = ADpCommon.ATN_PASSPOL_MINAGE;
            AttrPasswordMinAge attrValue = new(nonNullTimeSpan, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    } = null;



    /// <summary>
    /// Amount of password history to keep
    /// </summary>
    public int? HistoryCount
    {
        get;
        set
        {
            int val = value != null ? (int)value : 0;
            field = val;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string                   key       = ADpCommon.ATN_PASSPOL_HISTLEN;
            AttrPasswordHistoryCount attrValue = new(val, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    } = null;



    /// <summary>
    /// Number of failed login attempts before account is locked out
    /// </summary>
    public int? LockOutThreshold
    {
        get;
        set
        {
            int val = value != null ? (int)value : 0;
            field = val;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string                       key       = ADpCommon.ATN_PASSPOL_LOCKOUTTHRESHOLD;
            AttrPasswordLockoutThreshold attrValue = new(val, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    } = null;



    /// <summary>
    /// Whether Password complexity should be enforced
    /// </summary>
    public bool? ComplexityEnabled
    {
        get;
        set
        {
            bool val = value != null ? (bool)value : true;
            field = val;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string                 key       = ADpCommon.ATN_PASSPOL_COMPLEXITYENABLED;
            AttrPasswordComplexity attrValue = new(val, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    } = null;


    /// <summary>
    /// Whether reversible password encryption should be used
    /// </summary>
    public bool? ReversibleEncryptionEnabled
    {
        get;
        set
        {
            bool val = value != null ? (bool)value : true;
            field = val;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string                 key       = ADpCommon.ATN_PASSPOL_REVERSEDENCRYPTIONENABLED;
            AttrPasswordReversible attrValue = new(val, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    } = null;
    #endregion



    #region "Attribute Classes"

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

    #endregion


}
