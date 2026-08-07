using System.DirectoryServices.Protocols;
using AD.Protocols.ADObjects.Fields;
using SlugEnt.AD.Protocols;
using SlugEnt.AD.Protocols.Attributes;
using SlugEnt.FluentResults;

namespace AD.Protocols.ADObjects.Objects;

/// <summary>
/// Represents an AD Group object that is used to read and update group information from Active Directory.
/// </summary>
public class ADpGroup : ADpBaseObject
{
    public override string ObjectClassName => ADpCommon.OBJ_CLASS_GROUP;

    public override string ObjectTypeDescription => "group";


    /// <summary>
    /// Creates a basic security group.  You can override SAMAccount, CommonName, DisplayName, etc.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="parentPath"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ADpGroup(string name,
                      ADSPath parentPath)

    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        InCreationMode = true;

        ParentPath = parentPath;
        Name       = name;
        CommonName = name;
        GroupType = EnumGroupType.Global_Security;
        IsNew          = true;
        
        InCreationMode = false;
    }

    
    /// <summary>
    /// Creates a group object from a set of Active Directory attributes.  This constructor is used when retrieving an existing group from Active Directory.
    /// </summary>
    /// <param name="attributes"></param>
    /// <exception cref="ArgumentException"></exception>
    public ADpGroup(SearchResultAttributeCollection attributes)
    {
        InCreationMode = true;
        bool samAccountFound        = false;
        bool distinguishedNameFound = false;
        
        foreach (DirectoryAttribute dirObj in attributes.Values)
        {
            switch (dirObj.Name)
            {
                case "sAMAccountName":
                    samAccountFound = true; 
                    SAMAccount = dirObj[0].ToString();
                    break;
                case "description":
                    Description = dirObj[0].ToString();
                    break;
                case "distinguishedName":
                    DistinguishedName = dirObj[0].ToString();
                    distinguishedNameFound = true;
                    break;
                case "name": Name = dirObj[0].ToString(); break;

                case "cn":
                    CommonName = dirObj[0].ToString();
                    break;
                case "displayName":
                    DisplayName = dirObj[0].ToString();
                    break;
                case "mail":
                    Email = dirObj[0].ToString();
                    break;
                case "groupType":
                    string val = dirObj[0].ToString();
                    switch (val)
                    {
                        case "-2147483646":
                            GroupType = EnumGroupType.Global_Security;
                            break;
                        case "2":
                            GroupType = EnumGroupType.Global_Distribution;
                            break;
                        case "8": 
                            GroupType = EnumGroupType.Universal_Distribution;
                            break;
                        case "-2147483640":
                            GroupType = EnumGroupType.Universal_Security;
                            break;

                        case "4":
                            GroupType = EnumGroupType.Domain_Local_Distribution;
                            break;
                        case "-2147483644":
                            GroupType = EnumGroupType.Domain_Local_Security;
                            break;
                    }
                    break;
                case "whenChanged":
                    WhenChanged = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!);
                    break;
                case "whenCreated":
                    WhenCreated = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!);
                    break;
                
                // This will be hit if the group has < 1500 members.  
                /*
                case "member":
                    for (int i = 0; i < dirObj.Count; i++)
                    {
                        Members.Add(dirObj[i].ToString()!);
                    }

                    break;
                */
                default:
                    // See if it is an edge case where the group has > 1500 members.  In this case, the member attribute will not be returned, but the member;range=0-1499 attribute will be returned instead.
                    /*
                    if (dirObj.Name.StartsWith("member;range="))
                    {
                        for (int i = 0; i < dirObj.Count; i++)
                        {
                            Members.Add(dirObj[i].ToString()!);
                        }
                    }
                    */
                    break;
            }
        }

        if (DistinguishedName == null | DistinguishedName == string.Empty)
            throw new
                ArgumentException("No Distinguished Name found in the orgUnit object.  Anytime you retrieve an object from Active Directory you must retrieve this attribute.");


        // Calculate ParentPath
        Result<ADSPath> pathResult = new ADSPath(DistinguishedName).GetParent();
        ParentPath = pathResult.IsSuccess ? pathResult.Value : null;

        InCreationMode = false;
    }
    
    /// <summary>
    /// Parameterless constructor for creating a new ADpGroup object from an existing Active Directory Group
    /// </summary>
    internal ADpGroup() { }


    #region "Attributes"


    public MultiValuedDNAttribute Members { get; internal set; } = new MultiValuedDNAttribute("member", true, true);
    
    
    // TODO:  Add the ability to add/remove members from a group.  This will require a new attribute type that can handle adding/removing members from a group.
    /// <summary>
    /// Members of the group
    /// </summary>
    //public HashSet<string> Members { get; internal set; } = new HashSet<string>();

    
    /// <summary>
    /// Members to be added to the group.  This is used when updating a group to add new members.  This list will be processed when the group is updated.
    /// </summary>
    //internal HashSet<string> NewMembers { get; set; } = new HashSet<string>();

    
    /// <summary>
    /// Members to be removed from the group.  This is used when updating a group to remove members.  This list will be processed when the group is updated.
    /// </summary>
    //internal HashSet<string> RemovedMembers { get; set; } = new HashSet<string>();

    /// <summary>
    /// The type of AD Group this is.  This is a required attribute for creating a new group.  If this is not set, then the group cannot be created.
    /// </summary>
    public EnumGroupType? GroupType
    {
        get;
        set
        {
            field = value;
            string key       = ADpCommon.ATN_GROUPTYPE;
            string typeValue = "";

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            switch (value)
            {
                case EnumGroupType.Universal_Security:        typeValue = "-2147483640"; break;
                case EnumGroupType.Universal_Distribution:    typeValue = "8"; break;
                case EnumGroupType.Global_Security:           typeValue = "-2147483646"; break;
                case EnumGroupType.Global_Distribution:       typeValue = "2"; break;
                case EnumGroupType.Domain_Local_Security:     typeValue = "-2147483644"; break;
                case EnumGroupType.Domain_Local_Distribution: typeValue = "4"; break;
                default:                                      throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid GroupType value specfied.");
            }

            AttrGroupType attrValue = new(typeValue, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    } = null;


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

    #endregion

    /// <summary>
    /// Adds the specified user to the group.  The user is specified by their distinguished name.  This method adds the user to the NewMembers list, which will be processed when the group is updated.
    /// <para>Note, you must update / save the group after calling this method for the changes to take effect.</para>
    /// </summary>
    /// <param name="userDistinguishedName"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public void AddUserToGroup (string userDistinguishedName)
    {
        if (string.IsNullOrEmpty(userDistinguishedName))
            throw new ArgumentNullException(nameof(userDistinguishedName));
        if (!userDistinguishedName.StartsWith("CN=",StringComparison.CurrentCultureIgnoreCase))
            throw new ArgumentException("The distinguished name must start with 'CN='.", nameof(userDistinguishedName));

        // If for some reason we have previously added this user to the RemovedMembers list, remove them from that list AND DO NOT ADD as NewMember
        if (Members.AddMember(userDistinguishedName)) { return; }
        /*
        if (RemovedMembers.Contains(userDistinguishedName))
            RemovedMembers.Remove(userDistinguishedName);
        else
            NewMembers.Add(userDistinguishedName);
        */
    }


    /// <summary>
    /// Removes the specified user from the group.  The user is specified by their distinguished name.  This method adds the user to the RemovedMembers list, which will be processed when the group is updated.
    /// <para>Note, you must update / save the group after calling this method for the changes to take effect.</para>
    /// </summary>
    /// <param name="userDistinguishedName"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public void RemoveUserFromGroup(string userDistinguishedName)
    {
        if (string.IsNullOrEmpty(userDistinguishedName))
            throw new ArgumentNullException(nameof(userDistinguishedName));
        if (!userDistinguishedName.StartsWith("CN=", StringComparison.CurrentCultureIgnoreCase))
            throw new ArgumentException("The distinguished name must start with 'CN='.", nameof(userDistinguishedName));

        Members.RemoveMember(userDistinguishedName, false);

        // If for some reason we have previously added this user to the NewMembers list, remove them from that list.
/*        if (NewMembers.Contains(userDistinguishedName))
            NewMembers.Remove(userDistinguishedName);
        else
            RemovedMembers.Add(userDistinguishedName);
    }
*/
    }
}

