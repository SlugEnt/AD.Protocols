using System.DirectoryServices.Protocols;
using SlugEnt.FluentResults;

namespace AD.Protocols.ADObjects.Fields;

public class MultiValuedUserMemberOf : MultiValuedDNAttribute
{
    public MultiValuedUserMemberOf(string attributeName) : base(attributeName, false,true)
    {
    }


    
    private Result AddUserToGroup (string groupDn, string userDn, LdapConnection ldapConnection)
    {
        try
        {
            var request = new ModifyRequest(groupDn, DirectoryAttributeOperation.Add, "member", userDn);
            ModifyResponse response = (ModifyResponse)ldapConnection.SendRequest(request);
            if (response.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }
            else
            {
                return Result.Fail($"Failed to add user {userDn} to group {groupDn}: {response.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            return Result.Fail($"Exception occurred while adding user {userDn} to group {groupDn}: {ex.Message}");
        }
    }
    
    private Result RemoveUserFromGroup (string groupDn, string userDn, LdapConnection ldapConnection)
    {
        try
        {
            var request = new ModifyRequest(groupDn, DirectoryAttributeOperation.Delete, "member", userDn);
            ModifyResponse response = (ModifyResponse)ldapConnection.SendRequest(request);
            if (response.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }
            else
            {
                return Result.Fail($"Failed to remove user {userDn} from group {groupDn}: {response.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            return Result.Fail($"Exception occurred while removing user {userDn} from group {groupDn}: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves the requested changes (additions and removals) to the Active Directory attribute using the provided LDAP connection.
    /// </summary>
    /// <param name="ldapConnection">The LDAP connection to use for the modification request.</param>
    /// <param name="parentDistinguishedName">The distinguished name (DN) of the parent object in Active Directory.</param>
    /// <returns>A Result indicating success or failure of the operation.</returns>
    internal override Result SaveChangesToActiveDirectory(LdapConnection ldapConnection,
                                                          string parentDistinguishedName) 
    {
        // There is no direct way to update the members of attribute.  Instead we must go thru all the members and update them with the user...
        foreach (string additionDn in Additions)
        {
            Result addResult = AddUserToGroup(additionDn, parentDistinguishedName, ldapConnection);
            if (addResult.IsFailed) 
                    return addResult;
        }

        foreach (string removalDn in Removals)
        {
            Result removeResult = RemoveUserFromGroup(removalDn, parentDistinguishedName, ldapConnection);
            if (removeResult.IsFailed)
                return removeResult;
        }   
        
        return Result.Ok();
    }
}
