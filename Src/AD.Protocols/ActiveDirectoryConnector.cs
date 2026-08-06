using FluentResults.Reasons;
using Microsoft.Extensions.Logging;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;
using System.Net;
using AD.Protocols;
using AD.Protocols.ADObjects;
using AD.Protocols.ADObjects.Processors;
using SlugEnt.AD.Protocols.Attributes;

using SearchOption = System.DirectoryServices.Protocols.SearchOption;

namespace SlugEnt.AD.Protocols;

/// <summary>
///     This class is the primary engine that allows access to Active Directory via the legacy Ldap protocol.
///     This is required if you wish to run applications that interface with AD on Non-Windows platforms.
/// </summary>
public class ActiveDirectoryConnector : EngineBase
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public const string ADOU                                           = "organizationalUnit";
    public const string EXISTS                                         = "Exists";
    public const string NOT_FOUND                                      = "NotFound";
    public const string SEARCH_FILTER_ALL_COMPUTERS                    = "(objectCategory=computer)";
    public const string SEARCH_FILTER_ALL_DISABLED_USERS               = "(&(objectCategory=person)(objectClass=user)(userAccountControl:1.2.840.113556.1.4.803:=2))";
    public const string SEARCH_FILTER_ALL_GROUPS                       = "(objectCategory=group)";
    public const string SEARCH_FILTER_ALL_OU                           = "(objectCategory=organizationalUnit)";
    public const string SEARCH_FILTER_ALL_USERS                        = "(sAMAccountType=805306368)";
    public const string SEARCH_FILTER_ALL_USERS_PASSWORD_NEVER_EXPIREa = "(&(objectCategory=person)(objectClass=user)(userAccountControl:1.2.840.113556.1.4.803:=65536))";
#pragma warning restore

   private ActiveDirConfig _activeDirConfig;


    /// <summary>
    ///     Constructor for the ADLDAPEngine
    /// </summary>
    /// <param name="db"></param>
    /// <param name="activeDirConfig"></param>
    /// <param name="rootOU">
    ///     If blank the top of the directory is root, otherwise, it must be
    ///     a proper OU Path, IE OU=xyz,OU=abc
    /// </param>
    public ActiveDirectoryConnector( //   IActiveDirConfig activeDirConfig,
                        ILogger<ActiveDirectoryConnector> logger,
                        string rootOU = "") : base(logger) 
    {
    }


    /// <summary>
    /// Initializes the Active Directory Connector with the provided configuration.
    /// </summary>
    /// <param name="activeDirConfig">The Active Directory configuration to use.</param>
    /// <param name="rootOU">
    ///     If blank the top of the directory is root, otherwise, it must be
    ///     a proper OU Path, IE OU=xyz,OU=abc
    /// </param>
    /// <returns>True if the configuration was correct and the connector was able to connect to the AD Server.</returns>
    public Result Initialize(ActiveDirConfig activeDirConfig,
                           string rootOU = "")
    {
        _activeDirConfig = activeDirConfig;

        try
        {
            LdapDirectoryIdentifier directory = new(_activeDirConfig.Server1Name + "." + _activeDirConfig.Domain + $":{_activeDirConfig.Port}");

            LdapConnection = new LdapConnection(directory);

            // Set Credential
            NetworkCredential credential = new(_activeDirConfig.AdUser, _activeDirConfig.AdPassword);
            LdapConnection.Credential = credential;
            
            DomainRoot = ADSPath.FromDomainName(_activeDirConfig.Domain);
            if (rootOU != "")
            {
                RootDSE = RootDSE.CreateChild("OU=" + rootOU);
            }



            // Use AuthType.Negotiate for Windows domain networks, or AuthType.Basic for standard user/pass over secure lines.
            if (activeDirConfig.IsConnectingFromLinux)
            {
                LdapConnection.AuthType                         = AuthType.Basic;
                LdapConnection.SessionOptions.ProtocolVersion   = 3;
                LdapConnection.SessionOptions.ReferralChasing   = ReferralChasingOptions.None;
                LdapConnection.SessionOptions.SecureSocketLayer = true;
            }
            else
            {
                LdapConnection.AuthType                         = AuthType.Negotiate;
                LdapConnection.SessionOptions.SecureSocketLayer = true;
            }

            LdapConnection.Bind();
            IsConnected = true;

            return Result.Ok();
        }
        catch (LdapException ex)
        {
            // Handle LDAP-specific exceptions
            // Log the exception or perform any necessary error handling
            return Result.Fail(new ExceptionalError($"LDAP exception occurred.  Server Connecting to: {_activeDirConfig.Server1Name}.{_activeDirConfig.Domain}:{_activeDirConfig.Port} | {ex.Message}", ex));
        }
        catch (Exception ex)
        {
            return Result.Fail(new ExceptionalError("Unexpected exception occurred", ex));
        }
    }


    // TODO:  Confirm this is what we want to do...
    // Uses the current config to generate an AD connection with the given user and PAssword and
    // returns the LDAPConnection object.  Mostly for testing user logins for now....
    public Result<LdapConnection> ConnectAsUser(string user,
                                                string password)
    {
        try
        {
            LdapDirectoryIdentifier directory = new(_activeDirConfig.Server1Name + "." + _activeDirConfig.Domain + $":{_activeDirConfig.Port}");

            LdapConnection ldapCX = new LdapConnection(directory);
            

            // Set Credential
            NetworkCredential credential = new(user, password);
            ldapCX.Credential = credential;

            DomainRoot = ADSPath.FromDomainName(_activeDirConfig.Domain);

            // Use AuthType.Negotiate for Windows domain networks, or AuthType.Basic for standard user/pass over secure lines.
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                ldapCX.AuthType = AuthType.Basic;
                ldapCX.SessionOptions.ProtocolVersion = 3;
                ldapCX.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
                ldapCX.SessionOptions.SecureSocketLayer = true;
            }
            else
            {
                ldapCX.AuthType = AuthType.Negotiate;
                ldapCX.SessionOptions.SecureSocketLayer = true;
            }

            ldapCX.Bind();
            

            return Result.Ok(ldapCX);
        }
        catch (LdapException ex)
        {
            // Handle LDAP-specific exceptions
            // Log the exception or perform any necessary error handling
            return Result.Fail(new ExceptionalError($"LDAP exception occurred.  Server Connecting to: {_activeDirConfig.Server1Name}.{_activeDirConfig.Domain}:{_activeDirConfig.Port} | {ex.Message}", ex));
        }
        catch (Exception ex)
        {
            return Result.Fail(new ExceptionalError("Unexpected exception occurred", ex));
        }

    }

    /// <summary>
    /// Returns True if the AD Connector is connected to the AD Server.
    /// Note, this does not mean that the connection is still valid, just that it was able to connect at some point
    /// </summary>
    public bool IsConnected { get; private set; } = false;


    /// <summary>
    ///     Returns the Domain Root, ie DC=abc,DC=xyz
    /// </summary>
    public ADSPath DomainRoot { get; set; }


    /// <summary>
    ///     The LDAP Connection used to communicate to AD Server.
    /// </summary>
    public LdapConnection LdapConnection { get; protected set; }


    /// <summary>
    ///     The current root of the Directory.  Note, this can be changed to another OU if needed.
    /// </summary>
    public ADSPath RootDSE { get; set; }




    /// <summary>
    ///     Searches the directory for the specified entries
    /// </summary>
    /// <param name="searchContainerDn">Distinguished name from which to start the search from</param>
    /// <param name="searchFilter">LDAP Syntax search filter</param>
    /// <param name="searchScope">LDAP Syntax search scope</param>
    /// <param name="attributeList">List of attributes to bring back</param>
    /// <returns>Result Success or Failure (along with error message)</returns>
    public Result<List<SearchResponse>> SearchDirectory(string searchContainerDn,
                                                        string searchFilter,
                                                        SearchScope searchScope,
                                                        params string[] attributeList)
    {
        List<SearchResponse> result              = new();
        SearchResponse?      response            = null;
        int                  maxResultsToRequest = 200;

        try
        {
            PageResultRequestControl pageRequestControl = new(maxResultsToRequest);

            // used to retrieve the cookie to send for the subsequent request
            PageResultResponseControl pageResponseControl;
            SearchRequest searchRequest = new(searchContainerDn,
                                              searchFilter,
                                              searchScope,
                                              attributeList);
            searchRequest.Controls.Add(pageRequestControl);

            while (true)
            {
                response = (SearchResponse)LdapConnection.SendRequest(searchRequest);
                result.Add(response);
                pageResponseControl = (PageResultResponseControl)response.Controls[0];
                if (pageResponseControl.Cookie.Length == 0)
                {
                    break;
                }

                pageRequestControl.Cookie = pageResponseControl.Cookie;
            }
        }
        catch (Exception e)
        {
            if (e.Message.Contains("The object does not exist"))
            {
                return Result.Fail(new ExceptionalError("The starting container does not exist - " + searchContainerDn, e));
            }

            // TODO Log the error
            /*            Console.WriteLine("\nUnexpected exception occured:\n\t{0}: {1}",
                                          e.GetType().Name,
                                          e.Message);*/
            return Result.Fail(new ExceptionalError($"SearchDirectory: [ {searchContainerDn} ]  Had Error. {e.Message}" , e));
        }

        return Result.Ok(result);
    }




    /// <summary>
    /// Returns a list of Password Policies that match the given name prefix.  This is the preferred method to find Password Policy Lists
    /// </summary>
    /// <param name="namePrefix"></param>
    /// <param name="searchScope"></param>
    /// <returns></returns>
    public Result<List<ADpReadOnlyPasswordPolicy>> PasswordPolicyFindOneOrMore(string namePrefix,
                                                       SearchScope searchScope = SearchScope.Subtree)
    {
        List<string> attributesToReturn = new();
        ADpReadOnlyPasswordPolicy.AddBaseAttributes(attributesToReturn);

        string searchFilter      = "(&(objectClass=msDS-PasswordSettings)(cn=" + namePrefix + "*))";
        //string searchContainerDn = GetPasswordPolicyOU().Path;
        string searchContainerDn = "dummy";
        return PasswordPolicyFindOneOrMore(searchFilter, searchScope);
    }





    /// <summary>
    /// Returns a new OrgUnitProcessor to manage Organization Units in Active Directory.  This is the preferred way to manage OUs.
    /// </summary>
    /// <returns></returns>
    public ADpOrgUnitProcessor OrgUnitProcessor()
    {
        return new ADpOrgUnitProcessor(LdapConnection);
    }



    /// <summary>
    /// Returns a new GroupProcessor to manage Groups in Active Directory.  This is the preferred way to manage Groups.
    /// </summary>
    /// <param name="addGroupDefaultRetrievalAttributes">If true, the set of attributes which this library considers the default set of attributes to retrieve from AD for each group object
    /// are set.  If you wish to completely customize this list, you can set this to false OR after processor creation, clear the list and set your own.</param>
    /// <returns></returns>
    public ADpGroupProcessor GroupProcessor(bool addGroupDefaultRetrievalAttributes = true) { return new ADpGroupProcessor(LdapConnection,addGroupDefaultRetrievalAttributes); }


    /// <summary>
    /// Returns a new User Processor to manage Users in Active Directory.  This is the preferred way to manage Users.
    /// </summary>
    /// <returns></returns>
    public ADpUserProcessor UserProcessor()
    {
        return new ADpUserProcessor(LdapConnection);
    }
}