using SlugEnt.FluentResults;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.AD.Protocols;


/// <summary>
/// Represents an AD group object that is used to read group information from Active Directory.  This is a Read Only Object.
/// </summary>
public class ADpReadOnlyGroup
{
    public string? AD_CommonName { get; protected set; }

    /// <summary>
    ///     This is the same as the SAM Account
    /// </summary>
    public string? ADAccount { get; protected set; }
    
    /// <summary>
    /// The SAM Account
    /// </summary>
    public string? SAMAccount { get { return ADAccount;} set { ADAccount = value; } }

    /// <summary>
    /// Name of the group
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description of the groups purpose
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Display Name for the group
    /// </summary>
    public string? DisplayName { get; protected set; }

    /// <summary>
    /// The full DN of the group
    /// </summary>
    public string? DistinguishedName { get; protected set; }

    /// <summary>
    /// Email address for the group
    /// </summary>
    public string? Email { get; protected set; }


    /// <summary>
    /// When group was last changed
    /// </summary>
    public DateTimeOffset WhenChanged { get; protected set; }

    /// <summary>
    /// When group was created
    /// </summary>
    public DateTimeOffset WhenCreated { get; protected set; }


    /// <summary>
    /// Type of Group, IE. ,Distribution, Security.  And Universal, Global, Domain Local
    /// </summary>
    public EnumGroupType GroupType { get; set; } = EnumGroupType.NotSpecified;

    /// <summary>
    /// Members of the group
    /// </summary>
    public List<string> Members { get; set; } = [];


    /// <summary>
    /// Adds the Members attribute to the list of attributes to be retrieved
    /// </summary>
    /// <param name="attributeList"></param>
    public static void AddMemberAttribute(List<string> attributeList)
    {
        attributeList.Add("member");
    }


    /// <summary>
    /// Adds the Base attributes - sAMAccountName, displayName, mail, cn, distinguishedName, groupType
    /// </summary>
    /// <param name="attributeList"></param>
    public static void AddBaseAttributes(List<string> attributeList)
    {
        attributeList.Add("sAMAccountName");
        attributeList.Add("displayName");
        attributeList.Add("mail");
        attributeList.Add("cn");
        attributeList.Add("distinguishedName");
        attributeList.Add("groupType");
        attributeList.Add("name");
    }


    /// <summary>
    /// Adds the Informational attributes - Description
    /// </summary>
    /// <param name="attributeList"></param>
    public static void AddInfoAttributes (List<string> attributeList)
    {
        attributeList.Add("description");
    }


    /// <summary>
    /// Adds the when created and last updated attributes
    /// </summary>
    /// <param name="attributeList"></param>
    public static void AddStatisticAttributes(List<string> attributeList)
    {
        attributeList.Add("whenCreated");
        attributeList.Add("whenChanged");
    }



    public static Result<ADpReadOnlyGroup> CreateGroupObj(SearchResultAttributeCollection attributes)
    {
        ADpReadOnlyGroup group           = new();
        bool             samAccountFound = false;
        bool distinguishedNameFound = false;

        foreach (DirectoryAttribute dirObj in attributes.Values)
        {
            
            switch (dirObj.Name)
            {
                case "sAMAccountName":
                    samAccountFound = true;
                    group.ADAccount  = dirObj[0].ToString();
                    break;
                case "description":
                    group.Description = dirObj[0].ToString();
                    break;
                case "distinguishedName":
                    group.DistinguishedName = dirObj[0].ToString();
                    distinguishedNameFound = true;
                    break;
                case "name": group.Name = dirObj[0].ToString(); break;
                
                case "cn":
                    group.AD_CommonName = dirObj[0].ToString();
                    break;
                case "displayName":
                    group.DisplayName = dirObj[0].ToString();
                    break;
                case "mail":
                    group.Email = dirObj[0].ToString();
                    break;
                case "groupType":
                    string val  = dirObj[0].ToString();
                    switch (val)
                    {
                        case "-2147483646":
                            group.GroupType = EnumGroupType.Global_Security;
                            break;
                        case "2": group.GroupType = EnumGroupType.Global_Distribution;
                            break;
                        case "8":
                            group.GroupType = EnumGroupType.Universal_Distribution;
                            break;
                        case "-2147483640":
                            group.GroupType = EnumGroupType.Universal_Security;
                            break;

                        case "4":
                            group.GroupType = EnumGroupType.Domain_Local_Distribution;
                            break;
                        case "-2147483644":
                            group.GroupType = EnumGroupType.Domain_Local_Security;
                            break;
                    }
                    break;
                case "whenChanged":
                    group.WhenChanged = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!);
                    break;
                case "whenCreated":
                    group.WhenCreated = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!);
                    break;
                case "member":
                    for (int i = 0; i < dirObj.Count; i++)
                    {
                        group.Members.Add(dirObj[i].ToString()!);
                    }

                    break;

            }
        }
        if (!samAccountFound)
        {
            return Result.Fail<ADpReadOnlyGroup>("No SAM Account found in the group.  It is a required attribute.");
        }
        if (!distinguishedNameFound)
        {
            return Result.Fail<ADpReadOnlyGroup>("No Distinguished Name found in the group.  It is a required attribute.");
        }

        return Result.Ok(group);
    }
}
