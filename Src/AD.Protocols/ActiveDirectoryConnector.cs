using AD.Protocols.ADObjects;
using AD.Protocols.ADObjects.Processors;
using Microsoft.Extensions.Logging;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;
using System.Net;

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
    public ActiveDirectoryConnector(ILogger<ActiveDirectoryConnector> logger,
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
    /// Returns a new OrgUnitProcessor to manage Organization Units in Active Directory.  This is the preferred way to manage OUs.
    /// </summary>
    /// <returns></returns>
    public ADpOrgUnitProcessor GetOrgUnitProcessor()
    {
        return new ADpOrgUnitProcessor(LdapConnection);
    }



    /// <summary>
    /// Returns a new GroupProcessor to manage Groups in Active Directory.  This is the preferred way to manage Groups.
    /// </summary>
    /// <param name="addGroupDefaultRetrievalAttributes">If true, the set of attributes which this library considers the default set of attributes to retrieve from AD for each group object
    /// are set.  If you wish to completely customize this list, you can set this to false OR after processor creation, clear the list and set your own.</param>
    /// <returns></returns>
    public ADpGroupProcessor GetGroupProcessor(bool addGroupDefaultRetrievalAttributes = true) { return new ADpGroupProcessor(LdapConnection,addGroupDefaultRetrievalAttributes); }


    /// <summary>
    /// Returns a new User Processor to manage Users in Active Directory.  This is the preferred way to manage Users.
    /// </summary>
    /// <returns></returns>
    public ADpUserProcessor GetUserProcessor()
    {
        return new ADpUserProcessor(LdapConnection);
    }


    public ADpPasswordPolicyProcessor GetPasswordPolicyProcessor()
    {
        return new ADpPasswordPolicyProcessor(LdapConnection, DomainRoot);
    }
}