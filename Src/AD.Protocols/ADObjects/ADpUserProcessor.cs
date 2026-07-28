
using SlugEnt.AD.Protocols;
using SlugEnt.AD.Protocols.Attributes;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;

namespace AD.Protocols.ADObjects;

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
        AttributeRetrieverMgr.AddAttribute("userAccountControl");
    }

    public void AttrRetrieval_Office ()
    {
        AttributeRetrieverMgr.AddAttribute("title");
        AttributeRetrieverMgr.AddAttribute("department");
        AttributeRetrieverMgr.AddAttribute("mail");
        AttributeRetrieverMgr.AddAttribute("telephoneNumber");
        AttributeRetrieverMgr.AddAttribute("manager");
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
        AttributeRetrieverMgr.AddAttribute("lastLogon");
        AttributeRetrieverMgr.AddAttribute("lastLogoff");
        AttributeRetrieverMgr.AddAttribute("lastLogonTimestamp");
        // TODO need to add this to the ADpUser object as a DateTime property.  It is currently a long.
//        AttributeRetrieverMgr.AddAttribute("accountExpires");
        AttributeRetrieverMgr.AddAttribute("msDS-UserPasswordExpiryTimeComputed");
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
}

