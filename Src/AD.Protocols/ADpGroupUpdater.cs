using SlugEnt.AD.Protocols.Attributes;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.AD.Protocols;

/// <summary>
/// Used to add or update an existing group.
/// </summary>
public class ADpGroupUpdater {
    private string?       _CommonName;
    private string?       _Description;
    private string?       _DisplayName;
    private string?       _Email;
    private EnumGroupType _GroupType = EnumGroupType.NotSpecified;
    private string?       _Name;
    private string?       _SAMAccount;

    /// <summary>
    ///     The list of attributes to update
    /// </summary>
    protected Dictionary<string, AttributeBase> AttributesToUpdate = new();


    /// <summary>
    ///     This is the most preferred constructor if you have already read the user from AD as it almost 100% assuredly will
    ///     pull the user without error.
    /// </summary>
    /// <param name="readOnlyUser"></param>
    public ADpGroupUpdater(ADpReadOnlyGroup group)
    {
        string? value = group.DistinguishedName;
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException(nameof(value), "Distinguished Name cannot be null or empty.");
        DistinquishedName = value;
        IsNew             = false;
    }


    /// <summary>
    /// Creates a mew group with the given name and of the given type.  You can override SAMAccount, CommonName, DisplayNams by
    /// setting the respective properties.
    /// </summary>
    /// <param name="groupName"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ADpGroupUpdater(string groupName, EnumGroupType groupType)
    {
        if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));

        IsNew         = true;
        NameChg       = groupName;
        SAMAccountChg   = groupName;
        CommonNameChg = groupName;
        DisplayNameChg = groupName;
        GroupTypeChg = groupType;
    }

    /// <summary>
    /// If true, this will be a creation, not an update.
    /// </summary>
    protected bool IsNew { get; set; }


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
            string         key       = ADpCommon.ATN_CN;
            AttrCommonName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    /// SAM Account Name of the group.  This is the name that will be used to create the group in AD.
    /// </summary>
    public string? SAMAccountChg
    {
        get => _SAMAccount;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value), "SAM Account Name cannot be null or empty.");

            _SAMAccount = value;
            string         key       = ADpCommon.ATN_SAM;
            AttrSamAccount attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    /// The Name of the group.
    /// </summary>
    public string? NameChg
    {
        get => _Name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value), "Name cannot be null or empty.");
            _Name = value;
            string         key       = ADpCommon.ATN_NAME;
            AttrName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }



    /// <summary>
    /// Description of the user.  This is a free form text field that can be used to describe the user.
    /// </summary>
    public string? DescriptionChg
    {
        get => _Description;
        set
        {
            if (value == null) value = "";
            _Description = value;
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
    public string? DisplayNameChg
    {
        get => _DisplayName;
        set
        {
            if (value == null) value = "";
            _DisplayName = value;
            string          key       = ADpCommon.ATN_DISPLAYNAME;
            AttrDisplayName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    ///     Distinquished Name.  This is the unique identifier of the group in Active Directory.
    /// </summary>
    public string DistinquishedName { get; protected set; }


    /// <summary>
    /// Email Address of Group if it has one.
    /// </summary>
    public string? EmailChg
    {
        get => _Email;
        set
        {
            _Email = value;
            string    key       = ADpCommon.ATN_EMAIL;
            AttrEmail attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    public EnumGroupType GroupTypeChg
    {
        get => _GroupType;
        set
        {
            _GroupType = value;
            string key   = ADpCommon.ATN_GROUPTYPE;
            string typeValue = "";

            switch (value)
            {
                case EnumGroupType.Universal_Security:
                    typeValue = "-2147483640";
                    break;
                case EnumGroupType.Universal_Distribution:
                    typeValue = "8";
                    break;
                case EnumGroupType.Global_Security:
                    typeValue = "-2147483646";
                    break;
                case EnumGroupType.Global_Distribution:
                    typeValue = "2";
                    break;
                case EnumGroupType.Domain_Local_Security:
                    typeValue = "-2147483644";
                    break;
                case EnumGroupType.Domain_Local_Distribution:
                    typeValue = "4";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid GroupType value specfied.");
            }

            AttrGroupType attrValue = new(typeValue, EnumAttributeOperation.Modify);
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
        // / If this is a new group, we need to set the object class to group
        if (IsNew)
        {
            AttrObjectClass objectClass = new AttrObjectClass("group");
            if (!AttributesToUpdate.TryAdd(ADpCommon.ATN_OBJECT_CLASS, objectClass))
            {
                AttributesToUpdate[ADpCommon.ATN_OBJECT_CLASS] = objectClass;
            }
        }
        return AttributesToUpdate.Values.ToArray();
    }



    public DirectoryAttribute[] GetDirectoryAttributes()
    {
        return GetAttributes().Select(x => x.DA).ToArray();
    }
}



