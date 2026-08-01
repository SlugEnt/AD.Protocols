
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
        throw new NotImplementedException();
    }


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

            // (Optional) Add control to bypass password history limits if necessary
            // request.Controls.Add(new PasswordPolicyControl()); 

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
        if (result.IsFailed)
            throw new Exception($"Failed to retrieve child users under {parentDn.Path}. Error: {result.Errors[0].Message}");

        return result.Value;
    }
}

