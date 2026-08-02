using AD.Protocols.ADObjects.Objects;
using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;

namespace AD.Protocols.ADObjects;

/// <summary>
/// Allows for the processing of Active Directory User objects.  This includes creating, reading, updating, and deleting users in Active Directory.
/// </summary>
public class ADpUserProcessor : ADpGenericProcessor<ADpUser>
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="ldapConnection"></param>
    public ADpUserProcessor(LdapConnection ldapConnection) : base (ADpCommon.OBJ_CLASS_USER,"user",ldapConnection)
    { }

    
    protected override Result<ADpUser> CreateObjectFromAttributes(SearchResultAttributeCollection attributes)
    {
        ADpUser user = new(attributes);
        return Result.Ok(user);
    }



    /// <summary>
    /// How many members are retrieved per request.  Active Directory has a limit of 1500 members per request, so this value should be set to 1500 or less.  The default is 1400.
    /// </summary>
    internal int GroupsRetrievedPerRequest { get; set; } = 1400;

    /// <summary>
    /// Set Default Attributes to be retrieved if none are defined at time of retrieval from AD
    /// DisplayName, UserPrincipalName, sAMAccountName, GivenName, SN, Description, CN and DistinguishedName
    /// </summary>
    internal override void AttrRetrieval_Default()
    {
        AttributeRetrieverMgr.AddAttribute("displayName");
        AttributeRetrieverMgr.AddAttribute("userPrincipalName");
        AttributeRetrieverMgr.AddAttribute("sAMAccountName");
        AttributeRetrieverMgr.AddAttribute("givenName");
        AttributeRetrieverMgr.AddAttribute("sn");
        AttributeRetrieverMgr.AddAttribute("description");
        AttributeRetrieverMgr.AddAttribute("msDS-User-Account-Control-Computed");
        AttributeRetrieverMgr.AddAttribute("userAccountControl");
    }

    public void AttrRetrieval_Office ()
    {
        AttributeRetrieverMgr.AddAttribute("title");
        AttributeRetrieverMgr.AddAttribute("department");
        AttributeRetrieverMgr.AddAttribute("mail");
        AttributeRetrieverMgr.AddAttribute("telephoneNumber");
        AttributeRetrieverMgr.AddAttribute("manager");
        AttributeRetrieverMgr.AddAttribute("physicalDeliveryOfficeName");
    }


    /// <summary>
    /// Set of attributes to retrieve for password and logon information.  This includes:
    /// badPwdCount, badPasswordTime, lockoutTime, lockoutDuration, pwdLastSet, lastLogon, lastLogoff, lastLogonTimestamp, accountExpires, msDS-UserPasswordExpiryTimeComputed
    /// </summary>
    public void AttrRetrieval_PasswordLogonInfo()
    {
        AttributeRetrieverMgr.AddAttribute("badPwdCount");
        AttributeRetrieverMgr.AddAttribute("badPasswordTime");
        AttributeRetrieverMgr.AddAttribute("lockoutTime");
        AttributeRetrieverMgr.AddAttribute("lockoutDuration");
        AttributeRetrieverMgr.AddAttribute("pwdLastSet");
        // Do not appear to be updated in AD....
        //AttributeRetrieverMgr.AddAttribute("lastLogon");
        //AttributeRetrieverMgr.AddAttribute("lastLogoff");
        AttributeRetrieverMgr.AddAttribute("lastLogonTimestamp");
        // TODO need to add this to the ADpUser object as a DateTime property.  It is currently a long.
        //        AttributeRetrieverMgr.AddAttribute("accountExpires");
        AttributeRetrieverMgr.AddAttribute("userAccountControl");
        AttributeRetrieverMgr.AddAttribute("msDS-UserPasswordExpiryTimeComputed");
        AttributeRetrieverMgr.AddAttribute("msDS-User-Account-Control-Computed");
    }


    /// <summary>
    /// Derived classes should override this if they need to do anything after saving an object to AD.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected override Result AfterSave(ADpUser obj)
    {
        // Password changes cannot be done in the same operation as other changes.
        // If the password has been set, then we need to do a separate operation to set it.
        if (obj.PasswordHasBeenSet)
        {
            DirectoryAttributeModification passwordMod = new DirectoryAttributeModification
            {
                Name      = "unicodePwd",
                Operation = DirectoryAttributeOperation.Replace
            };
            passwordMod.Add(obj.GetPasswordBytes);

            // 4. Formulate and send the ModifyRequest
            ModifyRequest request = new ModifyRequest(obj.DistinguishedName, passwordMod);
            
            ModifyResponse response = (ModifyResponse)_ldapConnection.SendRequest(request);

            if (response.ResultCode == ResultCode.Success)
            {
                obj.PasswordHasBeenSet = false;
                return Result.Ok();
            }

            return Result.Fail(response.ErrorMessage);
        }

        return Result.Ok();
    }


    /// <summary>
    /// Returns a list of users located at a particular path in Active Directory.  This is a one-level search, so it will only return users that are direct children of the specified path.
    /// </summary>
    /// <param name="parentDn"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public List<ADpUser> GetAllChildUsers(ADSPath parentDn,SearchScope searchScope = SearchScope.OneLevel)
    {
        string searchFilter = $"(&(objectClass={ADpCommon.OBJ_CLASS_USER}))";
        Result<List<ADpUser>> result = Find(parentDn.Path, searchScope, searchFilter);

        if (result.IsSuccess)
            return result.Value;

        if (result.ErrorTitle == "NotFound")
            return new List<ADpUser>();

        throw new Exception($"Failed to retrieve child users under {parentDn.Path}. Error: {result.ToStringErrorOnly()}");
    }


    /// <summary>
    /// Retrieves a user by their UPN (User Principal Name).  This is a unique attribute, so it should only return one user.  If multiple users are found, an error will be returned.
    /// </summary>
    /// <param name="upnName"></param>
    /// <param name="startingSearchPath"></param>
    /// <param name="searchScope"></param>
    /// <returns></returns>
    public Result<ADpUser> GetBy_UPN(string upnName,
                                ADSPath startingSearchPath,
                                SearchScope searchScope = SearchScope.Subtree)
    {
        Result<List<ADpUser>> result = FindByAttribute(startingSearchPath.Path,
                                                 ADpCommon.ATN_UPN,
                                                 upnName,
                                                 searchScope);
        if (result.IsFailed)
            return Result.Fail(result.Errors);

        if (result.Value.Count == 0)
            return Result.Fail($"No {ObjectEnglishName} found.", EnumReasonCode.NotFound);

        if (result.Value.Count > 1)
            return Result.Fail($"Expected to only find one match for UPN attribute, but found multiple {ObjectEnglishName}s.");

        // Return the object.
        return Result.Ok(result.Value[0]);
    }


    /// <summary>
    /// Retrieves a user by their SAM Account name.  This is a unique attribute, so it should only return one user.  If multiple users are found, an error will be returned.
    /// </summary>
    /// <param name="samAccount"></param>
    /// <param name="startingSearchPath"></param>
    /// <param name="searchScope"></param>
    /// <returns></returns>
    public Result<ADpUser> GetBy_SAMAccount(string samAccount,
                                     ADSPath startingSearchPath,
                                     SearchScope searchScope = SearchScope.Subtree)
    {
        Result<List<ADpUser>> result = FindByAttribute(startingSearchPath.Path,
                                                       ADpCommon.ATN_SAM,
                                                       samAccount,
                                                       searchScope);
        if (result.IsFailed)
            return Result.Fail(result.Errors);

        if (result.Value.Count == 0)
            return Result.Fail($"No {ObjectEnglishName} found.", EnumReasonCode.NotFound);

        if (result.Value.Count > 1)
            return Result.Fail($"Expected to only find one match for SAM Account attribute, but found multiple {ObjectEnglishName}s.");

        // Return the object.
        return Result.Ok(result.Value[0]);
    }


    /// <summary>
    /// Retrieves a user by any attribute.  This performs an is equal search to the attribute
    /// </summary>
    /// <param name="attributeName"></param>
    /// <param name="attributeValue"></param>
    /// <param name="startingSearchPath"></param>
    /// <param name="searchScope"></param>
    /// <returns></returns>
    public Result<List<ADpUser>> GetBy_Attribute(string attributeName,
                                                 string attributeValue,
                                            ADSPath startingSearchPath,
                                            SearchScope searchScope = SearchScope.Subtree)
    {
        return FindByAttribute(startingSearchPath.Path,
                                                       attributeName,
                                                       attributeValue,
                                                       searchScope);
    }



    /// <summary>
    /// Retrieves all groups that this user is a direct member of.  It does not get nested groups.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public Result GetMemberOfs(ADpUser user)
    {
        int step = GroupsRetrievedPerRequest;
        int startRange = 0;
        bool hasMoreMembers = true;

        user.MemberOfGroups.Clear();

        while (hasMoreMembers)
        {
            int endRange = startRange + step - 1;

            // Format the range attribute query (e.g., "member;range=0-1499")
            string memberAttributeWithRange = $"memberOf;range={startRange}-{endRange}";

            var request = new SearchRequest(
                                            user.DistinguishedName,
                                            "(objectClass=*)",
                                            SearchScope.Base, // Base scope targets only this specific user object
                                            new string[]
                                            {
                                                memberAttributeWithRange
                                            }
                                           );

            var response = (SearchResponse)_ldapConnection.SendRequest(request);

            if (response.Entries.Count == 0)
                break;

            SearchResultEntry searchResult = response.Entries[0];
            bool rangeFoundInThisLoop = false;


            foreach (string attrName in searchResult.Attributes.AttributeNames)
            {
                // Active Directory will return either "memberOf;range=X-Y" or "memberOf;range=X-*"
                if (attrName.StartsWith("memberOf;range=", StringComparison.OrdinalIgnoreCase))
                {
                    rangeFoundInThisLoop = true;
                    DirectoryAttribute attribute = searchResult.Attributes[attrName];

                    // Extract Distinguished Names of the members
                    foreach (object val in attribute.GetValues(typeof(string)))
                    {
                        user.MemberOfGroups.Add(val.ToString());
                    }

                    // If the attribute name ends with "-*", we have reached the final block
                    if (attrName.EndsWith("-*"))
                    {
                        hasMoreMembers = false;
                    }
                    else
                    {
                        startRange += step;
                    }

                    break;
                }
            }

            // Fallback: If the group has < 1500 members, AD ignores ranges and returns a normal "member" attribute
            if (!rangeFoundInThisLoop)
            {
                if (searchResult.Attributes.Contains("memberOf"))
                {
                    DirectoryAttribute attribute = searchResult.Attributes["memberOf"];
                    foreach (object val in attribute.GetValues(typeof(string)))
                    {
                        user.MemberOfGroups.Add(val.ToString());
                    }
                }

                hasMoreMembers = false;
            }
        }

        return Result.Ok();
    }
}

