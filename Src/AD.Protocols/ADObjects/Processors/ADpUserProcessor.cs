using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;
using AD.Protocols.ADObjects.Fields;

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
    public ADpUserProcessor(LdapConnection ldapConnection) : base(ADpCommon.OBJ_CLASS_USER, "user", ldapConnection) { }


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


    public void AttrRetrieval_Office()
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
        Result x;
        Result y;
        Result final = new();
        if (obj.PasswordHasBeenSet)
        {
            x = AfterSave_PasswordUpdate(obj);
            if (x.IsFailed)
                final.AddError(new Error("Failure in AfterSave_PasswordUpdate").CausedBy(x.Errors));
        }

        if (obj.MemberOfGroups.Additions.Count > 0 || obj.MemberOfGroups.Removals.Count > 0)
        {
            y = AfterSave_ChangeGroupMembership(obj);
            if (y.IsFailed)
                final.AddError(new Error("Failure in AfterSave_ChangeGroupMembership").CausedBy(y.Errors));
        }

        return final;
    }


    /// <summary>
    /// Changes the user password if it was requested.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected Result AfterSave_PasswordUpdate(ADpUser obj)
    {
        DirectoryAttributeModification passwordMod = new DirectoryAttributeModification
        {
            Name      = "unicodePwd",
            Operation = DirectoryAttributeOperation.Replace
        };
        passwordMod.Add(obj.GetPasswordBytes);

        // Formulate and send the ModifyRequest
        ModifyRequest request = new ModifyRequest(obj.DistinguishedName, passwordMod);

        ModifyResponse response = (ModifyResponse)_ldapConnection.SendRequest(request);

        if (response.ResultCode == ResultCode.Success)
        {
            obj.PasswordHasBeenSet = false;
            return Result.Ok();
        }

        return Result.Fail(response.ErrorMessage);

    }


    /// <summary>
    /// Performs changes to Group Memberships for the user.  This is done after the user has been saved to AD, and is done in a
    /// separate operation because group membership changes cannot be done in the same operation as other changes.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected Result AfterSave_ChangeGroupMembership(ADpUser obj)
    {
        return obj.MemberOfGroups.SaveChangesToActiveDirectory(_ldapConnection, obj.DistinguishedName);
    }


    /// <inheritdoc cref="GetAllChildUsers(ADSPath, SearchScope)"/>
    public List<ADpUser> GetAllChildUsers(ADpOrgUnit parentOu,
                                          SearchScope searchScope = SearchScope.OneLevel)
    {
        return GetAllChildUsers(parentOu.Path, searchScope);
    }


    /// <summary>
    /// Returns a list of users located at a particular path in Active Directory.  This is a one-level search, so it will only return users that are direct children of the specified path.
    /// </summary>
    /// <param name="parentDn"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public List<ADpUser> GetAllChildUsers(ADSPath parentDn,
                                          SearchScope searchScope = SearchScope.OneLevel)
    {
        string                searchFilter = $"(&(objectClass={ADpCommon.OBJ_CLASS_USER}))";
        Result<List<ADpUser>> result       = Find(parentDn.Path, searchScope, searchFilter);

        if (result.IsSuccess)
            return result.Value;

        if (result.ErrorTitle == "NotFound")
            return new List<ADpUser>();

        throw new Exception($"Failed to retrieve child users under {parentDn.Path}. Error: {result.ToStringErrorOnly()}");
    }


    /// <inheritdoc cref="FindByAmbiguosNameResolution(string, ADSPath, SearchScope)"/>
    /// <param name="searchName"></param>
    /// <param name="orgUnit">The Ou object that is the starting point for the search</param>
    /// <param name="searchScope"></param>
    /// <returns></returns>

    public Result<List<ADpUser>> FindByAmbiguosNameResolution(string searchName,
                                                              ADpOrgUnit orgUnit,
                                                              SearchScope searchScope = SearchScope.Subtree)
    {
        return FindByAmbiguosNameResolution(searchName, orgUnit.Path, searchScope);
    }


    /// <summary>
    /// Attempts to find a user by their name using Ambiguous Name Resolution (ANR).  This is a search that will return any user that
    /// has a name that matches the search string.  It is not an exact match, and may return multiple users.  It is a wildcard search,
    /// so it will return any user that has a name that contains the search string.
    /// </summary>
    /// <param name="searchName"></param>
    /// <param name="startingSearchPath"></param>
    /// <param name="searchScope"></param>
    /// <returns></returns>
    public Result<List<ADpUser>> FindByAmbiguosNameResolution(string searchName,
                                                              ADSPath startingSearchPath,
                                                              SearchScope searchScope = SearchScope.Subtree)
    {
        string                searchFilter = $"(&(objectCategory=person)(objectClass={ADpCommon.OBJ_CLASS_USER})(anr={searchName}))";
        Result<List<ADpUser>> result       = Find(startingSearchPath.Path, searchScope, searchFilter);
        return result;
    }


    /// <inheritdoc cref="GetBy_UPN(string, ADSPath, SearchScope)"/>
    /// <param name="upnName"></param>
    /// <param name="startingOu">The Ou object that is the starting point for the search</param>
    /// <param name="searchScope"></param>
    /// <returns></returns>


    /// <inheritdoc cref="GetBy_UPN(string, ADSPath, SearchScope)"/>
    /// <param name="upnName"></param>
    /// <param name="startingSearchPath"></param>
    /// <param name="searchScope"></param>
    /// <returns></returns>
    public Result<ADpUser> GetBy_UPN(string upnName,
                                     ADpOrgUnit startingOu,
                                     SearchScope searchScope = SearchScope.Subtree)
    {
        return GetBy_UPN(upnName, startingOu.Path, searchScope);
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


    /// <inheritdoc cref="GetBy_SAMAccount(string, ADSPath, SearchScope)"/>
    /// <param name="samAccount"></param>
    /// <param name="startingOu"></param>
    /// <param name="searchScope"></param>
    /// <returns></returns>
    public Result<ADpUser> GetBy_SAMAccount(string samAccount,
                                            ADpOrgUnit startingOu,
                                            SearchScope searchScope = SearchScope.Subtree)
    {
        return GetBy_SAMAccount(samAccount, startingOu.Path, searchScope);
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


    /// <inheritdoc cref="GetBy_Attribute(string, string, ADSPath, SearchScope)"/>
    public Result<List<ADpUser>> GetBy_Attribute(string attributeName,
                                                 string attributeValue,
                                                 ADpOrgUnit startingOu,
                                                 SearchScope searchScope = SearchScope.Subtree)
    {
        return GetBy_Attribute(attributeName,
                               attributeValue,
                               startingOu.Path,
                               searchScope);
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
        return user.MemberOfGroups.GetMembersFromActiveDirectory(user.DistinguishedName, _ldapConnection);
/*        Result<HashSet<string>> result = AD_RangeRetrieval(user.DistinguishedName, "memberOf", GroupsRetrievedPerRequest);
        if (result.IsFailed)
            return Result.Fail(result.Errors);

        user.MemberOfGroups = new MultiValuedDNAttribute("memberOf", false, true);
        foreach (var member in result.Value)
        {
            user.MemberOfGroups.AddMember(member);
        }
        return Result.Ok();
    }
*/
    }
    
    

}