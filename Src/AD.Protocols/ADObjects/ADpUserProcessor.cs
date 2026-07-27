
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
        base.AttrRetrieval_Default();
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


    public void AttrRetrieval_AddUserStd()
    {

    }

    /*
    public Result Move (ADpUser user, ADSPath newParentPath)
    {
        Result<string> result = base.Move(user.DistinguishedName, newParentPath, user.CommonName);
        if (result.IsSuccess)
        {
            user.DistinguishedName = result.Value;
        }

        return Result.Ok();
    }
    */
}

