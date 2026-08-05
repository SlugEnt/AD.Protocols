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
                RootDSE = RootDSE.BuildChildADSPath("OU=" + rootOU);
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
    ///     Very Basic Sample Query OU.
    /// </summary>
    public void SampleDisplayUsers()
    {
        string hostOrDomainName = _activeDirConfig.Domain;
        string startingDn       = "OU=SystemAccounts,OU=YCY4FUsers,DC=ycy4y,DC=local"; 

        // for returning up to 5 entries in each page
        int pageSize = 5;

        // for tracking the pages returned by the search request
        int pageCount = 0;

        // establish a connection to the directory

        LdapConnection connection = LdapConnection; //new(hostOrDomainName);

        try
        {
            Console.WriteLine("\nPerforming a paged search ...");

            // this search filter does not limit the returned results
            string ldapSearchFilter = "(objectClass=*)";

            // create a SearchRequest object
            SearchRequest searchRequest = new(startingDn,
                                              ldapSearchFilter,
                                              SearchScope.Subtree,
                                              null);

            // create the PageResultRequestControl object 
            // pass it the size of each page.
            PageResultRequestControl pageRequest = new(pageSize);

            // add the PageResultRequestControl object to the
            // SearchRequest object's directory control collection 
            // to enable a paged search request
            searchRequest.Controls.Add(pageRequest);

            // turn off referral chasing so that data from other partitions is
            // not returned. This is necessary when scoping a search
            // to a single naming context, such as a domain or the 
            // configuration container
            SearchOptionsControl searchOptions = new(SearchOption.DomainScope);

            // add the SearchOptionsControl object to the
            // SearchRequest object's directory control collection 
            // to disable referral chasing
            searchRequest.Controls.Add(searchOptions);

            // loop through the pages until there are no more 
            // to retrieve
            while (true)
            {
                // increment the pageCount by 1
                pageCount++;

                // cast the directory response into a 
                // SearchResponse object
                SearchResponse searchResponse =
                    (SearchResponse)connection.SendRequest(searchRequest);

                // verify support for this advanced search operation
                if (searchResponse.Controls.Length != 1 ||
                    !(searchResponse.Controls[0] is PageResultResponseControl))
                {
                    Console.WriteLine("The server cannot page the result set");
                    return;
                }

                // cast the diretory control into 
                // a PageResultResponseControl object.
                PageResultResponseControl pageResponse =
                    (PageResultResponseControl)searchResponse.Controls[0];

                // display the retrieved page number and the number of 
                // directory entries in the retrieved page                    
                Console.WriteLine("\nPage:{0} contains {1} response entries",
                                  pageCount,
                                  searchResponse.Entries.Count);

                // display the entries within this page
                foreach (SearchResultEntry entry in searchResponse.Entries)
                {
                    Console.WriteLine("{0}:{1}",
                                      searchResponse.Entries.IndexOf(entry) + 1,
                                      entry.DistinguishedName);
                }

                // if this is true, there 
                // are no more pages to request
                if (pageResponse.Cookie.Length == 0)
                {
                    break;
                }

                // set the cookie of the pageRequest equal to the cookie 
                // of the pageResponse to request the next page of data
                // in the send request
                pageRequest.Cookie = pageResponse.Cookie;
            }

            Console.WriteLine("\nPaged search completed.");
        }

        catch (Exception e)
        {
            Console.WriteLine("\nUnexpected exception occured:\n\t{0}: {1}",
                              e.GetType().Name,
                              e.Message);
        }
    }


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
    ///    Converts the EnumAttributeOperation to the DirectoryAttributeOperation
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    public static DirectoryAttributeOperation ToOperation(EnumAttributeOperation operation)
    {
        return operation switch
        {
            EnumAttributeOperation.Add    => DirectoryAttributeOperation.Add,
            EnumAttributeOperation.Delete => DirectoryAttributeOperation.Delete,
            EnumAttributeOperation.Modify => DirectoryAttributeOperation.Replace,
            _                             => DirectoryAttributeOperation.Add
        };
    }


    /// <summary>
    ///     Finds a single password policy in the directory.  If more than one is found, it returns an error.
    /// </summary>
    /// <param name="searchContainerDn"></param>
    /// <param name="searchScope"></param>
    /// <param name="searchFilter"></param>
    /// <param name="attributesToReturn">
    ///     The attributes that should be returned.  Must contain at a minimum UPN and
    ///     SAMAccountName.  If none are provided a default base set is automatically added.
    /// </param>
    /// <returns></returns>
    public Result<ADpReadOnlyPasswordPolicy> PasswordPolicyFindSingle(string searchContainerDn,
                                                      SearchScope searchScope,
                                                      string searchFilter,
                                                      List<string> attributesToReturn)
    {
        try
        {
            if (attributesToReturn.Count == 0)
            {
                // Add in some of the basic ones.
                ADpReadOnlyPasswordPolicy.AddBaseAttributes(attributesToReturn);
            }


            Result<List<SearchResponse>> resultResponse = SearchDirectory(searchContainerDn,
                                                                          searchFilter,
                                                                          searchScope,
                                                                          attributesToReturn.ToArray());
            if (resultResponse.IsFailed)
            {
                return Result.Fail(resultResponse.Errors);
            }

            if (resultResponse.Value.Count == 0)
            {
                return Result.Fail(NOT_FOUND);
            }

            if (resultResponse.Value[0].Entries.Count == 0)
            {
                return Result.Fail(NOT_FOUND);
            }

            if (resultResponse.Value[0].Entries.Count > 1)
            {
                return Result.Fail("More than one password policy found.  Request was for a single user");
            }

            Result<ADpReadOnlyPasswordPolicy> policyCreation = ADpReadOnlyPasswordPolicy.CreatePasswordPolicyObj(resultResponse.Value[0].Entries[0].Attributes);
            if (policyCreation.IsFailed)
            {
                return Result.Fail(policyCreation.Errors);
            }


            return Result.Ok(policyCreation.Value);
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Unexpected exception occured", e));
        }
    }


    /// <summary>
    ///   Updates the given Password Policy in AD.  The policy must already exist.
    /// </summary>
    /// <param name="policy"></param>
    /// <returns></returns>
    public Result<ModifyResponse> PasswordPolicyUpdate(ADpPasswordPolicyUpdater policy)
    {
        try
        {
            if (!string.IsNullOrEmpty(policy.DistinquishedName))
            {
                return PasswordPolicyUpdate(policy.DistinquishedName, policy.GetAttributes());
            }

            return Result.Fail("Failed to update Password Policy: DistinquishedName is blank");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to update Password Policy  : " + policy.DistinquishedName + " Error: " + e.Message, e));
        }
    }


    /// <summary>
    ///     Updates the given password policy in Active Directory
    /// </summary>
    /// <param name="dnUser"></param>
    /// <param name="attributes"></param>
    /// <returns></returns>
    public Result<ModifyResponse> PasswordPolicyUpdate(string? dnPolicy,
                                             AttributeBase[] attributes)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dnPolicy))
                return Result.Fail("Must provide a valid distinguished Password policy value");

            DirectoryAttributeModification[] modifications = new DirectoryAttributeModification[attributes.Length];
            int i = 0;

            foreach (AttributeBase attribute in attributes)
            {
                // Determine the type of modification to perform on the attribute.
                DirectoryAttributeModification modification = new()
                {
                    Operation = ToOperation(attribute.OperationMode),
                    Name = attribute.Name
                };
                if (attribute is AttributeStringSingle ass)
                    modification.Add(ass.Value);

                else if (attribute is AttributeByteArray aba)
                    modification.Add(aba.Value);
                else if (attribute is AttributeInt aint)
                    modification.Add(aint.Value);
                else if (attribute is AttributeTimeSpan ats)
                    modification.Add(ats.Value);
                else if (attribute is AttributeList atList)
                    foreach (string atListValue in atList.Values)
                    {
                        modification.Add(atListValue);
                    }
                    
                else
                {
                    modification.AddRange((string[])attribute.Value);
                }

                modifications[i++] = modification;
            }

            ModifyRequest modifyRequest = new(dnPolicy, modifications);
            PermissiveModifyControl permissiveModify = new();
            modifyRequest.Controls.Add(permissiveModify);

            ModifyResponse modifyResponse = (ModifyResponse)LdapConnection.SendRequest(modifyRequest);
            if (modifyResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok(modifyResponse);
            }

            return Result.Fail("Failed to update Password Policy: " + modifyResponse.ErrorMessage + " [ " + modifyResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to update Password Policy in AD: " + e.Message, e));
        }
    }



    /// <summary>
    /// Finds one or more Password Policies within the specified parent container and matching the search filter.
    /// </summary>
    /// <param name="searchContainerDn"></param>
    /// <param name="searchScope"></param>
    /// <param name="searchFilter"></param>
    /// <param name="attributesToReturn"></param>
    /// <returns></returns>
    private Result<List<ADpReadOnlyPasswordPolicy>> PasswordPolicyFindOneOrMore(string searchContainerDn,
                                                       SearchScope searchScope,
                                                       string searchFilter,
                                                       List<string> attributesToReturn)
    {
        try
        {
            if (attributesToReturn.Count == 0)
            {
                // Add in some of the basic ones.
                ADpReadOnlyPasswordPolicy.AddBaseAttributes(attributesToReturn);
            }


            Result<List<SearchResponse>> resultResponse = SearchDirectory(searchContainerDn,
                                                                          searchFilter,
                                                                          searchScope,
                                                                          [.. attributesToReturn]);
            if (resultResponse.IsFailed)
            {
                return Result.Fail(resultResponse.Errors);
            }

            if (resultResponse.Value.Count == 0)
            {
                return Result.Fail("No active directory password policies found", EnumReasonCode.NotFound);
            }

            if (resultResponse.Value[0].Entries.Count == 0)
            {
                return Result.Fail("No active directory password policies found", EnumReasonCode.NotFound);
            }

            List<ADpReadOnlyPasswordPolicy> users = new();
            foreach (SearchResultEntry searchResultEntry in resultResponse.Value[0].Entries)
            {
                Result<ADpReadOnlyPasswordPolicy> resultUser = ADpReadOnlyPasswordPolicy.CreatePasswordPolicyObj(searchResultEntry.Attributes);
                if (resultUser.IsFailed)
                {
                    return Result.Fail("Failed during conversion of Password Policy from LDAP Search Result to ADpReadOnlyPasswordPolicy  Error: " + resultUser.ToStringWithLineFeeds());
                }

                users.Add(resultUser.Value);
            }


            return Result.Ok(users);
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Unexpected exception occured", e));
        }
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
        string searchContainerDn = GetPasswordPolicyOU().Path;
        
        return PasswordPolicyFindOneOrMore(searchContainerDn, searchScope, searchFilter, attributesToReturn);
    }



    /// <summary>
    /// Retrieves a Password Policy by its Common Name (CN) value.
    /// Assumes that the password policy is in the standard AD Location
    /// </summary>
    /// <param name="commonNameValue"></param>
    /// <returns></returns>
    public Result<ADpReadOnlyPasswordPolicy> PasswordPolicyGetByCn(string commonNameValue, SearchScope searchScope = SearchScope.Subtree)
    {
        try
        {
            List<string> attributesToReturn = new();
            ADpReadOnlyPasswordPolicy.AddBaseAttributes(attributesToReturn);

            string searchFilter = "(&(objectClass=msDS-PasswordSettings)(cn=" + commonNameValue + "))";
            string searchContainerDn = GetPasswordPolicyOU().Path;
            Result<ADpReadOnlyPasswordPolicy> result =
                PasswordPolicyFindSingle(searchContainerDn, searchScope, searchFilter, attributesToReturn);
            return result;
        }
        catch (Exception e)
        {
            return Result.Fail($"Error: {e.Message} |  Error Data: {e.ToString()}");
        }
    }



    /// <summary>
    /// Saves the given Password Policy to the specified parent OU.
    /// </summary>
    /// <param name="parentOu"></param>
    /// <param name="policy"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public Result PasswordPolicyAdd(ADpPasswordPolicyUpdater policy)

    {
        try
        {
            ADSPath parentOu =   GetPasswordPolicyOU();
            string     policyDN    = "CN=" + policy.NameChg + "," + parentOu;

            DirectoryAttribute[] attributesToLoad = policy.GetDirectoryAttributesNew();

            AddRequest  addRequest  = new(policyDN, attributesToLoad);
#if DEBUG
            foreach (DirectoryAttribute directoryAttribute in attributesToLoad)
            {
                if (directoryAttribute.Count > 0)
                    Console.WriteLine($"Attr: {directoryAttribute.Name} | { directoryAttribute[0].ToString()}");
                else 
                    Console.WriteLine($"Attr: {directoryAttribute.Name} | Null");
            }
#endif
            AddResponse addResponse = (AddResponse)LdapConnection.SendRequest(addRequest);
            if (addResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }

            return Result.Fail("Failed to add Password Policy: " + addResponse.ErrorMessage + " [ " + addResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to add Password Policy: " + e.Message, e));
        }
    }






    /// <summary>
    ///     Deletes a Password Policy
    /// </summary>
    /// <param name="distinguishedName"></param>
    /// <returns></returns>
    public Result PasswordPolicyDelete(string distinguishedName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(distinguishedName))
                return Result.Fail(
                                   "Must provide a valid distinguished name for the password policy to delete:  Provided Value: " +
                                   distinguishedName);

            DeleteRequest  deleteRequest  = new(distinguishedName);
            DeleteResponse deleteResponse = (DeleteResponse)LdapConnection.SendRequest(deleteRequest);
            if (deleteResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }

            return Result.Fail("Failed to delete Password Policy: " + deleteResponse.ErrorMessage + " [ " + deleteResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to delete Password Policy " + distinguishedName + " Error: " + e.Message, e));
        }
    }


    /// <summary>
    /// Returns the Password Policy Active Directory OU path.
    /// </summary>
    /// <returns></returns>
    public ADSPath GetPasswordPolicyOU()
    {
        ADSPath ouPath = DomainRoot.BuildChildADSPath(ADpReadOnlyPasswordPolicy.ROOT_OU_PATH);
        return ouPath;
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