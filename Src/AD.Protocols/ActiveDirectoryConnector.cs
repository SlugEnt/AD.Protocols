using FluentResults.Reasons;
using Microsoft.Extensions.Logging;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;
using System.Net;
using AD.Protocols;
using SlugEnt.AD.Protocols.Attributes;
using SlugEnt.IS;
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
                RootDSE = RootDSE.NewChildADSPath("OU=" + rootOU);
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
    protected LdapConnection LdapConnection { get; set; }


    /// <summary>
    ///     The current root of the Directory.  Note, this can be changed to another OU if needed.
    /// </summary>
    public ADSPath RootDSE { get; set; }



    /// <summary>
    ///     Finds the requested OU.  Note, it will only find it if under the Parent.  If you specifiy subtree for
    ///     search scope, then it will return the FIRST matching OU.  It returns Result.Fail if it did not find
    ///     any matching OUs and sets the error message to constant NOT_FOUND.
    /// </summary>
    /// <param name="parentPath"></param>
    /// <param name="ouName"></param>
    /// <returns>Returns Success nd the OU object or Failure if errors.  Returns Constant NOT_FOUND if it did not find it.</returns>
    public Result<SearchResultEntry> FindSingleOuAtPath(ADSPath parentPath,
                                                        string ouName,
                                                        SearchScope searchScope = SearchScope.OneLevel)
    {
        string ldapSearchFilter = "(&(objectClass=organizationalUnit)(ou=" + ouName + "))";

        try
        {
            ADSPath findOU = parentPath.NewChildADSPath("OU=" + ouName);
            Result<List<SearchResponse>> resultResponse = SearchDirectory(parentPath.Path,
                                                                          ldapSearchFilter,
                                                                          searchScope,
                                                                          "distinguishedName");
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

            return Result.Ok(resultResponse.Value[0].Entries[0]);
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Unexpected exception occured", e));
        }
    }



    /// <summary>
    ///     Creates the OU under the given parent.
    /// </summary>
    /// <param name="ouName"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public Result OuCreate(string ouName,
                           ADSPath parent)
    {
        try
        {
            ADSPath     newOU       = parent.NewChildADSPath("OU=" + ouName);
            AddRequest  addRequest  = new(newOU.Path, ADOU);
            AddResponse addResponse = (AddResponse)LdapConnection.SendRequest(addRequest);
            if (addResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }

            return Result.Fail("Failed to add OU: " + addResponse.ErrorMessage + " [ " + addResponse.ResultCode + " ]");
        }
        catch (Exception e)

        {
            if (e.Message.Contains("ENTRY_EXISTS"))
            {
                return Result.Fail(EXISTS);
            }

            return Result.Fail(new ExceptionalError("Failed to add OU: " + e.Message, e));
        }
    }

    
    
    /// <summary>
    /// Saves the provided OU either as a new OU or updates an existing one based on the ADpOrgUnitUpdater object.
    /// </summary>
    /// <param name="orgUnit"></param>
    /// <returns></returns>
    private Result OuSave(ADpOrgUnitUpdater orgUnit)
    {
        try
        {
            string dn;
            if (orgUnit.IsNew)
            {
                // Need to build Distinguished Name
                dn                        = "OU=" + orgUnit.NameChg + "," + orgUnit.ParentPath.Path;
                orgUnit.DistinquishedName = dn;
            }
            else
            {
                dn = orgUnit.DistinquishedName;
            }

            // If this is a new OU, we need to set the object class to organizationalUnit

            ADSPath              newOU            = orgUnit.ParentPath.NewChildADSPath("OU=" + orgUnit.NameChg);
            DirectoryAttribute[] attributesToLoad = orgUnit.GetDirectoryAttributesNew();

            AddRequest addRequest = new(dn, attributesToLoad);


            AddResponse addResponse = (AddResponse)LdapConnection.SendRequest(addRequest);
            if (addResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }

            return Result.Fail("Failed to add OrgUnit: " + addResponse.ErrorMessage + " [ " + addResponse.ResultCode + " ]");


        }
        catch (Exception ex)
        {
            return Result.Fail(new ExceptionalError("Failed to add OrgUnit: " + ex.Message, ex));
        }
    }


    /// <summary>
    /// Creates an Active Directory Organizational Unit (OU) based on the provided ADpOrgUnitUpdater object.
    /// If the OU is new, it constructs the Distinguished Name and sets the necessary attributes before adding it to Active Directory.
    /// If the OU already exists, it uses the existing Distinguished Name for the update.
    /// </summary>
    /// <param name="orgUnit"></param>
    /// <returns></returns>
    public Result OuCreate(ADpOrgUnitUpdater orgUnit)
    {
        return OuSave(orgUnit);
    }

    
    public Result OuUpdate (ADpOrgUnitUpdater orgUnit)
    {
        return OuSave(orgUnit);
    }
    

    /// <summary>
    ///     Deletes an OU and all child objects
    /// </summary>
    /// <param name="ouPath"></param>
    /// <returns></returns>
    public Result OuDelete(ADSPath ouPath)
    {
        try
        {
            DeleteRequest  deleteRequest  = new(ouPath.Path);
            DeleteResponse deleteResponse = (DeleteResponse)LdapConnection.SendRequest(deleteRequest);
            if (deleteResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }

            return Result.Fail("Failed to delete OU: " + deleteResponse.ErrorMessage + " [ " + deleteResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to delete OU: " + e.Message, e));
        }
    }



    public Result<bool> OuExists(ADSPath parentOuPath, string ouName)
    {
        try
        {
            ADSPath ouToFind         = parentOuPath.NewChildADSPath("ou=" + ouName);
            string  ldapSearchFilter = "(&(objectClass=organizationalUnit)(distinguishedName=" + ouToFind + "))";
            List<string> ouAttrList = new()
            {
                "distinguishedName",
                "name"
            };

            Result<List<SearchResponse>> resultResponse = SearchDirectory(parentOuPath.Path,
                                                                          ldapSearchFilter,
                                                                          SearchScope.OneLevel, ouAttrList.ToArray());
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
                return Result.Fail(NOT_FOUND, EnumReasonCode.NotFound);
            }

            return Result.Ok(true);

        }
        catch (Exception e)
        {
            
            _logger.LogDebug("Failed to find OU: {@ou} at path {@rootPath} {@ExceptionMsg}" ,ouName, parentOuPath.Path, e.Message);
            return Result.Fail(new ExceptionalError("Failure while checking for OU existence: " + e.Message, e));
        }
    }


    /// <summary>
    ///     Returns a list of users who match the criteria.  If no users are found, it returns an empty list.
    /// </summary>
    /// <param name="searchContainerDn"></param>
    /// <param name="searchScope"></param>
    /// <param name="searchFilter"></param>
    /// <param name="attributesToReturn"></param>
    /// <returns></returns>
    public Result<List<ADpReadOnlyOrgUnit>> OuFindOneOrMore(string searchContainerDn,
                                                           SearchScope searchScope,
                                                           List<string> attributesToReturn = null, 
                                                           string searchFilter = "(objectClass=organizationalUnit)")
    {
        try
        {
            if (attributesToReturn == null)
                attributesToReturn = new List<string>();
            
            if (attributesToReturn.Count == 0)
            {
                // Add in some of the basic ones.
                ADpReadOnlyOrgUnit.AddBaseAttributes(attributesToReturn);
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
                return Result.Fail("No active directory org units found", EnumReasonCode.NotFound);
            }

            if (resultResponse.Value[0].Entries.Count == 0)
            {
                return Result.Fail("No active directory org units found", EnumReasonCode.NotFound);
            }

            // Create list of Org Unit objects
            List<ADpReadOnlyOrgUnit> orgUnits = new();
            foreach (SearchResultEntry searchResultEntry in resultResponse.Value[0].Entries)
            {
                Result<ADpReadOnlyOrgUnit> result = ADpReadOnlyOrgUnit.CreateOrgUnitObj(searchResultEntry.Attributes);
                if (result.IsFailed)
                {
                    return Result.Fail($"Failed to convert AD object to Org Unit Object {searchResultEntry.DistinguishedName}");
                }

                orgUnits.Add(result.Value);
            }


            return Result.Ok(orgUnits);
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Unexpected exception occured", e));
        }
    }


    public Result<SearchResultEntryCollection> OuGetChildrenOus(ADSPath parentPath,string ouName)
    {
        ADSPath ouPath = parentPath.NewChildADSPath("ou=" + ouName);

        // Bring back the password related attributes
        List<string> attrToGet = new()
        {
            "distinguishedName",
            "name"
        };

        // First Find all OU's under the Root OU.
        Result<List<SearchResponse>> result = SearchDirectory(ouPath.Path,
                                                                            "(objectClass=organizationalUnit)",
                                                                            SearchScope.Subtree,
                                                                            attrToGet.ToArray());
        if (result.IsFailed)
        {
            _logger.LogError("Failed to retrieve children OU's under the OU [ {@ou} ]  This was caused by: {@reason}", ouPath.Path, result.ToStringErrorOnly());
            return Result.Fail(new Error($"Failed to retrieve children OU's from the {parentPath.Path}").CausedBy(result.Errors));
        }


        return Result.Ok(result.Value[0].Entries);  // .Select(x => x.DistinguishedName).ToList());
    }


    /// <summary>
    /// Deletes the given OU and all chidlren OU's and objects.
    /// </summary>
    /// <param name="ouPath"></param>
    /// <returns></returns>
    public Result OuDeleteAll(ADSPath ouPath)
    {
        try
        {
            DeleteRequest deleteRequest = new(ouPath.Path);
            deleteRequest.Controls.Add(new TreeDeleteControl());
            DeleteResponse deleteResponse = (DeleteResponse)LdapConnection.SendRequest(deleteRequest);

            if (deleteResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }

            return Result.Fail("Failed to delete OU: " + deleteResponse.ErrorMessage + " [ " + deleteResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to delete OU: " + e.Message, e));
        }
    }


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
        SearchResponse?       response            = null;
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
    /// Retrieves the user from Active Directory with the matching attribute.  This is a shortcut method, that eliminates most of the hassle of having to read a ReadOnly object and then convert it to an Updater object.
    /// </summary>
    /// <param name="attributeName">Name of the attribute in AD to base search on</param>
    /// <param name="attributeValue">The value of the attribute to search for</param>
    /// <param name="searchContainerDn">Where to start the search from</param>
    /// <param name="searchScope">At what level to search</param>
    /// <param name="attributesToReturn">Which attributes to return.  If not provided then the Basic and Info attributes are returned.</param>
    /// <returns>An Editable AD object</returns>
    public Result<ADpUserEditable> GetUserFromADViaAttribute(string attributeName,
                                                       string attributeValue,
                                                       string searchContainerDn,
                                                       SearchScope searchScope = SearchScope.Subtree,
                                                       List<string> attributesToReturn = null)
    {
        try
        {
            // Search for the userFromAdRo in Active Directory using the specified attribute and value.  If not found return Result.Failed.
            Result<ADpUserFromAD_RO> findResult = UserGetByAttribute(searchContainerDn,
                                                                    attributeName,
                                                                    attributeValue,
                                                                    searchScope,
                                                                    attributesToReturn);
            if (findResult.IsFailed)
            {
                _logger.LogError("Failed to find userFromAdRo by attribute [ {attributeName} ] with value [ {attributeValue} ]  Error: {error}",
                                 attributeName,
                                 attributeValue,
                                 findResult.ToStringErrorOnly());

                return Result.Fail(new Error($"Failed to find userFromAdRo by attribute [ {attributeName} ] with value [ {attributeValue} ]  Error: {findResult.ToStringErrorOnly()}"));
            }

            // Pull User Object out
            ADpUserFromAD_RO userFromAdRoFromAd = findResult.Value;


            // Create a new ADpUserEditable object using the found userFromAdRo object.
            ADpUserEditable updateUser = new(userFromAdRoFromAd);
            return Result.Ok(updateUser);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to create an updatable user object from AD via attribute [ {attributeName} ] with value [ {attributeValue} ]  Error: {error}",
                             attributeName,
                             attributeValue,
                             e.Message);
            return Result.Fail(new ExceptionalError($"Failed to create user updater from AD via attribute [ {attributeName} ] with value [ {attributeValue} ]  Error: {e.Message}", e));
        }
    }


    public Result UserAddNew(ADpUserEditable user, string parentPath="")
    {
        try
        {
            //ADSPath parentOu = GetPasswordPolicyOU();
            string userDn = "";
            if (parentPath == string.Empty)
                userDn = user.DistinquishedName;
            else
                userDn = "CN=" + user.CommonNameChg + "," + parentPath;

            user.DistinquishedName = userDn;

            
            DirectoryAttribute[] attributesToLoad = user.GetDirectoryAttributesNew();

            AddRequest addRequest = new(userDn, attributesToLoad);
            foreach (DirectoryAttribute directoryAttribute in attributesToLoad)
            {
                Console.WriteLine($"W: {directoryAttribute.Name} ");
            }

            AddResponse addResponse = (AddResponse)LdapConnection.SendRequest(addRequest);
            if (addResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }

            return Result.Fail("Failed to add User: " + addResponse.ErrorMessage + " [ " + addResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to add User: " + e.Message, e));
        }
    }


    /// <summary>
    ///     Adds a user object to the directory. Attributes are also added
    /// </summary>
    /// <param name="userPath"></param>
    /// <returns></returns>
    internal Result UserAdd(string userPath,
                                DirectoryAttribute[] attributes)
    {
        try
        {
            AddRequest addRequest = new(userPath, attributes);

            AddResponse addResponse = (AddResponse)LdapConnection.SendRequest(addRequest);
            if (addResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }

            return Result.Fail("Failed to add User: " + addResponse.ErrorMessage + " [ " + addResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to add User: " + e.Message, e));
        }
    }



    /// <summary>
    /// Retrieves a user by their Common Name (CN) value.
    /// </summary>
    /// <param name="commonNameValue"></param>
    /// <returns></returns>
    public Result<ADpReadOnlyOrgUnit> OuGetByName(string searchContainerDn,
                                                string name,
                                                SearchScope searchScope = SearchScope.Subtree,
                                                List<string> attributesToReturn = null)
    {
        try
        {
            return OuGetByAttribute(searchContainerDn,
                                      "cn",
                                      name,
                                      searchScope,
                                      attributesToReturn);
        }
        catch (Exception e)
        {
            return Result.Fail($"Failed to retrieve organizational unit with cn attribute [ {name} ]  | Error:  {e.Message}");
        }
    }



    /// <summary>
    /// Retrieves a user by their Common Name (CN) value.
    /// </summary>
    /// <param name="commonNameValue"></param>
    /// <returns></returns>
    public Result<ADpUserFromAD_RO> UserGetByCn(string searchContainerDn,string commonNameValue, SearchScope searchScope = SearchScope.Subtree,
                                               List<string> attributesToReturn = null)
    {
        try
        {
            return UserGetByAttribute(searchContainerDn,
                                      "cn",
                                      commonNameValue,
                                      searchScope,
                                      attributesToReturn);
        }
        catch (Exception e)
        {
            return Result.Fail($"Failed to retrieve user with cn attribute [ {commonNameValue} ]  | Error:  {e.Message}");
        }
    }




    /// <summary>
    /// Retrieves a user by their UPN Name.
    /// </summary>
    /// <param name="commonNameValue"></param>
    /// <returns></returns>
    public Result<ADpUserFromAD_RO> UserGetByUPN(string searchContainerDn,
                                               string upn,
                                               SearchScope searchScope = SearchScope.Subtree,
                                               List<string> attributesToReturn = null)
    {
        try
        {
            return UserGetByAttribute(searchContainerDn,
                                      "userprincipalname",
                                      upn,
                                      searchScope,
                                      attributesToReturn);
        }
        catch (Exception e)
        {
            return Result.Fail($"Failed to retrieve user with UPN attribute [ {upn} ]  | Error:  {e.Message}");
        }
    }




    /// <summary>
    /// Retrieves a user by their UPN Name.
    /// </summary>
    /// <param name="commonNameValue"></param>
    /// <returns></returns>
    public Result<ADpUserFromAD_RO> UserGetBySAMAccount(string searchContainerDn,
                                                string SAMAccount,
                                                SearchScope searchScope = SearchScope.Subtree,
                                                List<string> attributesToReturn = null)
    {
        try
        {
            return UserGetByAttribute(searchContainerDn,
                                      "sAMAccountName",
                                      SAMAccount,
                                      searchScope,
                                      attributesToReturn);
        }
        catch (Exception e)
        {
            return Result.Fail($"Failed to retrieve user with SAMAcccount attribute [ {SAMAccount} ]  | Error:  {e.Message}");
        }
    }



    /// <summary>
    /// Retrieves a user by their UPN Name.
    /// </summary>
    /// <param name="commonNameValue"></param>
    /// <returns></returns>
    public Result<ADpUserFromAD_RO> UserGetByDn(string searchContainerDn,
                                                string dn,
                                                SearchScope searchScope = SearchScope.Subtree,
                                                List<string> attributesToReturn = null)
    {
        try
        {
            return UserGetByAttribute(searchContainerDn,
                                      "distinguishedName",
                                      dn,
                                      searchScope,
                                      attributesToReturn);
        }
        catch (Exception e)
        {
            return Result.Fail($"Failed to retrieve user with Distinguished Name attribute [ {dn} ]  | Error:  {e.Message}");
        }
    }



    /// <summary>
    /// Retrieves a single user object based upon the provided attribute.
    /// </summary>
    /// <param name="searchContainerDn">Where to start the search from</param>
    /// <param name="attributeName">Name of the attribute in AD to base search on</param>
    /// <param name="attributeValue">The value of the attribute to search for</param>
    /// <param name="searchScope">At what level to search</param>
    /// <param name="attributesToReturn">Which attributes to return.  If not provided then the Basic and Info attributes are returned.</param>
    /// <returns></returns>
    public Result<ADpUserFromAD_RO> UserGetByAttribute(string searchContainerDn,
                                                      string attributeName,
                                                      string attributeValue,
                                                      SearchScope searchScope = SearchScope.Subtree,
                                                      List<string> attributesToReturn = null)
    {
        try
        {
            // If no attributes are provided we will add the base attributes.
            if (attributesToReturn == null)
            {
                attributesToReturn = new();
                ADpUserFromAD_RO.AddAllAttributes(attributesToReturn);
            }


            string searchFilter = $"(&(objectClass=Person)({attributeName}={attributeValue}))";
            Result<ADpUserFromAD_RO> result = UserFindSingleUser(searchContainerDn,
                                                                searchScope,
                                                                searchFilter,
                                                                attributesToReturn);
            return result;
        }
        catch (Exception e)
        {
            return Result.Fail($"Failed to retrieve user with attribute [ {attributeName} ]  containing value [{attributeValue} | Error:  {e.Message}");
        }

    }


    /// <summary>
    /// Retrieves a single user object based upon the provided attribute.
    /// </summary>
    /// <param name="searchContainerDn">Where to start the search from</param>
    /// <param name="attributeName">Name of the attribute in AD to base search on</param>
    /// <param name="attributeValue">The value of the attribute to search for</param>
    /// <param name="searchScope">At what level to search</param>
    /// <param name="attributesToReturn">Which attributes to return.  If not provided then the Basic and Info attributes are returned.</param>
    /// <returns></returns>
    public Result<ADpReadOnlyOrgUnit> OuGetByAttribute(string searchContainerDn,
                                                      string attributeName,
                                                      string attributeValue,
                                                      SearchScope searchScope = SearchScope.Subtree,
                                                      List<string> attributesToReturn = null)
    {
        try
        {
            // If no attributes are provided we will add the base attributes.
            if (attributesToReturn == null)
            {
                attributesToReturn = new();
                ADpReadOnlyOrgUnit.AddBaseAttributes(attributesToReturn);
            }


            string searchFilter = $"(&(objectClass=OrganizationalUnit)({attributeName}={attributeValue}))";
            Result<ADpReadOnlyOrgUnit> result = OuFindSingleOrgUnit(searchContainerDn,
                                                                searchScope,
                                                                searchFilter,
                                                                attributesToReturn);
            return result;
        }
        catch (Exception e)
        {
            return Result.Fail($"Failed to retrieve organizational unit  with attribute [ {attributeName} ]  containing value [{attributeValue} | Error:  {e.Message}");
        }

    }



    /// <summary>
    ///     Adds a user object to the directory.  No attributes are set, just the base object is added.  
    /// </summary>
    /// <param name="userPath"></param>
    /// <returns></returns>
    internal Result UserAddBase(string userPath)
    {
        try
        {
            AddRequest addRequest = new(userPath, "user");

            //AddRequest addRequest = new("CN=John Doe,OU=techwriters,dc=fabrikam,dc=com",
            //                            "user");
            AddResponse addResponse = (AddResponse)LdapConnection.SendRequest(addRequest);
            if (addResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }

            return Result.Fail("Failed to add User: " + addResponse.ErrorMessage + " [ " + addResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to add User: " + e.Message, e));
        }
    }



    /// <summary>
    ///     Deletes a User
    /// </summary>
    /// <param name="distinguishedName"></param>
    /// <returns></returns>
    public Result<DeleteResponse> UserDelete(string distinguishedName)
    {
        try
        {
            DeleteRequest  deleteRequest  = new(distinguishedName);
            DeleteResponse deleteResponse = (DeleteResponse)LdapConnection.SendRequest(deleteRequest);
            if (deleteResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok(deleteResponse);
            }

            return Result.Fail("Failed to delete User: " + deleteResponse.ErrorMessage + " [ " + deleteResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to delete User: " + distinguishedName + " Error: " + e.Message, e));
        }
    }


    /// <summary>
    ///     Returns a list of users who match the criteria.  If no users are found, it returns an empty list.
    /// </summary>
    /// <param name="searchContainerDn"></param>
    /// <param name="searchScope"></param>
    /// <param name="searchFilter"></param>
    /// <param name="attributesToReturn"></param>
    /// <returns></returns>
    public Result<List<ADpUserFromAD_RO>> UserFindOneOrMore(string searchContainerDn,
                                                           SearchScope searchScope,
                                                           string searchFilter,
                                                           List<string> attributesToReturn)
    {
        try
        {
            if (attributesToReturn.Count == 0)
            {
                // Add in some of the basic ones.
                ADpUserFromAD_RO.AddBaseAttributes(attributesToReturn);
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
                return Result.Fail("No active directory users found", EnumReasonCode.NotFound);
            }

            if (resultResponse.Value[0].Entries.Count == 0)
            {
                return Result.Fail("No active directory users found", EnumReasonCode.NotFound);
            }

            List<ADpUserFromAD_RO> users = new();
            foreach (SearchResultEntry searchResultEntry in resultResponse.Value[0].Entries)
            {
                Result<ADpUserFromAD_RO> resultUser = ADpUserFromAD_RO.CreateUserObj(searchResultEntry.Attributes);
                if (resultUser.IsFailed)
                {
                    return Result.Fail("Failed during conversion of user from LDAP Search Result to ADpUserFromAD_RO  Error: " + resultUser.ToStringWithLineFeeds());
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
    ///     Returns a list of users who match the criteria.  If no users are found, it returns an empty list.
    /// </summary>
    /// <param name="searchContainerDn"></param>
    /// <param name="searchScope"></param>
    /// <param name="searchFilter"></param>
    /// <param name="attributesToReturn"></param>
    /// <returns></returns>
    public Result<List<ADpReadOnlyGroup>> GroupFindOneOrMore(string searchContainerDn,
                                                           SearchScope searchScope,
                                                           string searchFilter,
                                                           List<string> attributesToReturn)
    {
        try
        {
            if (attributesToReturn.Count == 0)
            {
                // Add in some of the basic ones.
                ADpUserFromAD_RO.AddBaseAttributes(attributesToReturn);
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
                return Result.Fail("No active directory users found", EnumReasonCode.NotFound);
            }

            if (resultResponse.Value[0].Entries.Count == 0)
            {
                return Result.Fail("No active directory users found", EnumReasonCode.NotFound);
            }





            List<ADpReadOnlyGroup> groups = new();
            foreach (SearchResultEntry searchResultEntry in resultResponse.Value[0].Entries)
            {
                Result<ADpReadOnlyGroup> result = ADpReadOnlyGroup.CreateGroupObj(searchResultEntry.Attributes);
                if (result.IsFailed)
                {
                    return Result.Fail($"Failed to convert AD object to Group Object {searchResultEntry.DistinguishedName}");
                }

                groups.Add(result.Value);
            }


            return Result.Ok(groups);
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Unexpected exception occured", e));
        }
    }


    /// <summary>
    ///     Finds a single user in the directory.  If more than one is found, it returns an error.
    /// </summary>
    /// <param name="searchContainerDn"></param>
    /// <param name="searchScope"></param>
    /// <param name="searchFilter"></param>
    /// <param name="attributesToReturn">
    ///     The attributes that should be returned.  Must contain at a minimum UPN and
    ///     SAMAccountName.  If none are provided a default base set is automatically added.
    /// </param>
    /// <returns></returns>
    public Result<ADpUserFromAD_RO> UserFindSingleUser(string searchContainerDn,
                                                      SearchScope searchScope,
                                                      string searchFilter,
                                                      List<string> attributesToReturn)
    {
        try
        {
            if (attributesToReturn.Count == 0)
            {
                // Add in some of the basic ones.
                ADpUserFromAD_RO.AddBaseAttributes(attributesToReturn);
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
                return Result.Fail("More than one user found.  Request was for a single user");
            }

            Result<ADpUserFromAD_RO> userCreation = ADpUserFromAD_RO.CreateUserObj(resultResponse.Value[0].Entries[0].Attributes);
            if (userCreation.IsFailed)
            {
                return Result.Fail(userCreation.Errors);
            }


            return Result.Ok(userCreation.Value);
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Unexpected exception occured", e));
        }
    }

    /// <summary>
    /// Locates a single Organizational Unit (OU) in the directory based on the provided search criteria. If more than one OU is found, an error is returned.
    /// </summary>
    /// <param name="searchContainerDn"></param>
    /// <param name="searchScope"></param>
    /// <param name="searchFilter"></param>
    /// <param name="attributesToReturn"></param>
    /// <returns></returns>
    public Result<ADpReadOnlyOrgUnit> OuFindSingleOrgUnit(string searchContainerDn,
                                                  SearchScope searchScope,
                                                  string searchFilter,
                                                  List<string> attributesToReturn)
    {
        try
        {
            if (attributesToReturn.Count == 0)
            {
                // Add in some of the basic ones.
                ADpUserFromAD_RO.AddBaseAttributes(attributesToReturn);
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
                return Result.Fail("More than one Ou found.  Request was for a single Ou");
            }

            Result<ADpReadOnlyOrgUnit> orgUnitCreation = ADpReadOnlyOrgUnit.CreateOrgUnitObj(resultResponse.Value[0].Entries[0].Attributes);
            if (orgUnitCreation.IsFailed)
            {
                return Result.Fail(orgUnitCreation.Errors);
            }


            return Result.Ok(orgUnitCreation.Value);
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Unexpected exception occured", e));
        }
    }




    /// <summary>
    ///     Finds a single group in the directory and returns a new ReadOnlyGroup object with the requested attributes.
    ///   If more than one is found, it returns an error.
    /// </summary>
    /// <param name="searchContainerDn"></param>
    /// <param name="searchScope"></param>
    /// <param name="searchFilter"></param>
    /// <param name="attributesToReturn">
    ///     The attributes that should be returned.  Must contain at a minimum SAMAccountName.
    /// If none are provided a default base set is automatically added.
    /// </param>
    /// <returns></returns>
    public Result<ADpReadOnlyGroup> GroupFindSingle(string searchContainerDn,
                                                    SearchScope searchScope,
                                                    string searchFilter,
                                                    List<string> attributesToReturn)
    {
        try
        {
            if (attributesToReturn.Count == 0)
            {
                // Add in some of the basic ones.
                ADpUserFromAD_RO.AddBaseAttributes(attributesToReturn);
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
                return Result.Fail("More than one Group found.  Request was for a single group");
            }

            Result<ADpReadOnlyGroup> result = ADpReadOnlyGroup.CreateGroupObj(resultResponse.Value[0].Entries[0].Attributes);
            if (result.IsFailed)
            {
                return Result.Fail(result.ToStringWithLineFeeds());
            }


            return Result.Ok(result.Value);
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Unexpected exception occured", e));
        }
    }


    



    /// <summary>
    ///     Moves a userFromAdRo to another OU.
    /// </summary>
    /// <param name="userFromAdRo"></param>
    /// <param name="newParentDn"></param>
    /// <returns></returns>
    public Result<string> UserMove(ADpUserFromAD_RO userFromAdRo,
                                   string newParentDn)
    {
        try
        {
            string           fullCN           = "CN=" + userFromAdRo.AD_CommonName;
            ModifyDNRequest  modifyDNRequest  = new(userFromAdRo.DistinguishedName, newParentDn, fullCN);
            ModifyDNResponse modifyDNResponse = (ModifyDNResponse)LdapConnection.SendRequest(modifyDNRequest);
            if (modifyDNResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok(fullCN + "," + newParentDn);

                //return Result.Ok(modifyDNResponse);
            }

            return Result.Fail("Failed to move User: " + modifyDNResponse.ErrorMessage + " [ " + modifyDNResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to move User: " + userFromAdRo.DistinguishedName + " Error: " + e.Message, e));
        }
    }



    /// <summary>
    ///     Renames the User (Common Name cn)
    /// </summary>
    /// <param name="userFromAdRo"></param>
    /// <param name="newCommonName"></param>
    /// <returns>The full Distinguished name of the AD userFromAdRo.  DN = CN + parent path</returns>
    public Result<string> UserRename(ADpUserFromAD_RO userFromAdRo,
                                     string newCommonName)
    {
        try
        {
            string           curerntCN         = "CN=" + userFromAdRo.AD_CommonName;
            ADSPath          currentParentPath = new ADSPath(userFromAdRo.DistinguishedName).GetParent();
            string           parentPath        = currentParentPath.Path;
            string           fullNewCN         = "CN=" + newCommonName;
            ModifyDNRequest  modifyDNRequest   = new(userFromAdRo.DistinguishedName, parentPath, fullNewCN);
            ModifyDNResponse modifyDNResponse  = (ModifyDNResponse)LdapConnection.SendRequest(modifyDNRequest);
            if (modifyDNResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok(fullNewCN + "," + parentPath);
            }

            return Result.Fail("Failed to move User: " + modifyDNResponse.ErrorMessage + " [ " + modifyDNResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to move User: " + userFromAdRo.DistinguishedName + " Error: " + e.Message, e));
        }
    }



    /// <summary>
    ///     Updates the given user in AD/
    /// </summary>
    /// <param name="dnUser"></param>
    /// <param name="attributes"></param>
    /// <returns></returns>
    public Result<ModifyResponse> UserUpdate(string? dnUser,
                                             AttributeBase[] attributes)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dnUser))
                return Result.Fail("Must provide a valid dnUser value");

            DirectoryAttributeModification[] modifications = new DirectoryAttributeModification[attributes.Length];
            int                              i             = 0;

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
                else if (attribute is AttributeDateTimeOffset ado)
                    modification.Add(ado.Value);
                else if (attribute is AttributeDateTimeOffset adt)
                    modification.Add(adt.Value);
                else if (attribute is AttributeInt ain)
                    modification.Add(ain.Value);
                else
                {
                    modification.AddRange((string[])attribute.Value);
                }

                modifications[i++] = modification;
            }

            ModifyRequest           modifyRequest    = new(dnUser, modifications);
            PermissiveModifyControl permissiveModify = new();
            modifyRequest.Controls.Add(permissiveModify);

            ModifyResponse modifyResponse = (ModifyResponse)LdapConnection.SendRequest(modifyRequest);
            if (modifyResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok(modifyResponse);
            }

            return Result.Fail("Failed to update User: " + modifyResponse.ErrorMessage + " [ " + modifyResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to update User: " + e.Message, e));
        }
    }



    /// <summary>
    ///     Updates the user from the UserUpdater Object, which is the preferred way.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public Result<ModifyResponse> UserUpdate(ADpUserEditable user)
    {
        try
        {
            if (!string.IsNullOrEmpty(user.DistinquishedName))
            {
                return UserUpdate(user.DistinquishedName, user.GetAttributes());
            }

            return Result.Fail("Failed to update User: DistinquishedName is blank");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to update User: " + user.DistinquishedName + " Error: " + e.Message, e));
        }
    }

    /*
    public void zFindOu(string ouName)
    {
        //string ldapSearchFilter = "(objectClass=*)";
        string ldapSearchFilter = "(&(objectClass=organizationalUnit)(ou=" + ouName + "))";

        try
        {
            string rootPath = "OU=YCY4FUsers,DC=ycy4y,DC=local";
            Result<List<SearchResponse>> resultResponse = SearchDirectory(rootPath,
                                                                          ldapSearchFilter,
                                                                          SearchScope.Subtree,
                                                                          "distinguishedName");
            if (resultResponse.IsFailed)
            {
                Console.WriteLine(resultResponse.Errors[0].Message);
                return;
            }


            // display the entries within this page
            foreach (SearchResponse searchResponse in resultResponse.Value)
            {
                foreach (SearchResultEntry entry in searchResponse.Entries)
                {
                    Console.WriteLine("{0}:{1}",
                                      searchResponse.Entries.IndexOf(entry) + 1,
                                      entry.DistinguishedName);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("\nUnexpected exception occured:\n\t{0}: {1}",
                              e.GetType().Name,
                              e.Message);
        }
    }
    */



    /// <summary>
    /// Add a userFromAdRo to a group.
    /// </summary>
    /// <param name="groupDN"></param>
    /// <param name="userFromAdRo"></param>
    /// <returns></returns>
    public Result UserAddToGroup(string groupDN,
                                 ADpUserFromAD_RO userFromAdRo)
    {
        try
        {
            ModifyRequest modifyRequest = new(groupDN,
                                              DirectoryAttributeOperation.Add,
                                              "member",
                                              userFromAdRo.DistinguishedName);
            PermissiveModifyControl permissiveModify = new();
            modifyRequest.Controls.Add(permissiveModify);
            ModifyResponse modifyResponse = (ModifyResponse)LdapConnection.SendRequest(modifyRequest);
            if (modifyResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }

            return Result.Fail("Failed to add userFromAdRo to group: " + modifyResponse.ErrorMessage + " [ " + modifyResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to add userFromAdRo to group: " + e.Message, e));
        }
    }


    /// <summary>
    /// Removes a userFromAdRo from a group.
    /// </summary>
    /// <param name="groupDN"></param>
    /// <param name="userFromAdRo"></param>
    /// <returns></returns>
    public Result UserRemoveFromGroup(string groupDN,
                                      ADpUserFromAD_RO userFromAdRo)
    {
        try
        {
            ModifyRequest modifyRequest = new(groupDN,
                                              DirectoryAttributeOperation.Delete,
                                              "member",
                                              userFromAdRo.DistinguishedName);
            PermissiveModifyControl permissiveModify = new();
            modifyRequest.Controls.Add(permissiveModify);
            ModifyResponse modifyResponse = (ModifyResponse)LdapConnection.SendRequest(modifyRequest);
            if (modifyResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }

            return Result.Fail("Failed to remove userFromAdRo from group: " + modifyResponse.ErrorMessage + " [ " + modifyResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to remove userFromAdRo from group: " + e.Message, e));
        }
    }


    // TODO this method can create a group that already exists sometimes.  Need to make a recursive call - maybe 3x to get a 
    // valid group name.
    public Result GroupAdd(string parentOu,
                           ADpGroupUpdater group)

    {
        try
        {
            if (string.IsNullOrEmpty(parentOu))
                throw new ArgumentNullException(nameof(parentOu));

            string     groupDN    = "CN=" + group.CommonNameChg + "," + parentOu;
            AddRequest addRequest = new(groupDN, group.GetDirectoryAttributes());

            AddResponse addResponse = (AddResponse)LdapConnection.SendRequest(addRequest);
            if (addResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }

            return Result.Fail("Failed to add Group: " + addResponse.ErrorMessage + " [ " + addResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to add Group: " + e.Message, e));
        }
    }



    /// <summary>
    /// Updates the specified group in AD.  The group must already exist.
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    public Result<ModifyResponse> GroupUpdate(ADpGroupUpdater group)
    {
        try
        {
            if (!string.IsNullOrEmpty(group.DistinquishedName))
            {
                return UserUpdate(group.DistinquishedName, group.GetAttributes());
            }

            return Result.Fail("Failed to update User: DistinquishedName is blank");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to update Group: " + group.DistinquishedName + " Error: " + e.Message, e));
        }
    }




    /// <summary>
    ///     Deletes a Group
    /// </summary>
    /// <param name="distinguishedName"></param>
    /// <returns></returns>
    public Result<DeleteResponse> GroupDelete(string distinguishedName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(distinguishedName))
                return Result.Fail(
                    "Must provide a valid distinguished name for the group to delete:  Provided Value: " +
                    distinguishedName);
            DeleteRequest  deleteRequest  = new(distinguishedName);
            DeleteResponse deleteResponse = (DeleteResponse)LdapConnection.SendRequest(deleteRequest);
            if (deleteResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok(deleteResponse);
            }

            return Result.Fail("Failed to delete Group: " + deleteResponse.ErrorMessage + " [ " + deleteResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to delete Group: " + distinguishedName + " Error: " + e.Message, e));
        }
    }


    /// <summary>
    ///     Moves a group to another OU.
    /// </summary>
    /// <param name="group"></param>
    /// <param name="newParentDn"></param>
    /// <returns></returns>
    public Result<string> GroupMove(ADpReadOnlyGroup group,
                                   string newParentDn)
    {
        try
        {
            string           fullCN           = "CN=" + group.AD_CommonName;
            ModifyDNRequest  modifyDNRequest  = new(group.DistinguishedName, newParentDn, fullCN);
            ModifyDNResponse modifyDNResponse = (ModifyDNResponse)LdapConnection.SendRequest(modifyDNRequest);
            if (modifyDNResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok(fullCN + "," + newParentDn);

                //return Result.Ok(modifyDNResponse);
            }

            return Result.Fail("Failed to move User: " + modifyDNResponse.ErrorMessage + " [ " + modifyDNResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to move Group: " + group.DistinguishedName + " Error: " + e.Message, e));
        }
    }



    /// <summary>
    ///     Renames the Group (Common Name cn)
    /// </summary>
    /// <param name="group"></param>
    /// <param name="newCommonName"></param>
    /// <returns>The full Distinguished name of the AD user.  DN = CN + parent path</returns>
    public Result<string> GroupRename(ADpReadOnlyGroup group,
                                     string newCommonName)
    {
        try
        {
            string           curerntCN         = "CN=" + group.AD_CommonName;
            ADSPath          currentParentPath = new ADSPath(group.DistinguishedName).GetParent();
            string           parentPath        = currentParentPath.Path;
            string           fullNewCN         = "CN=" + newCommonName;
            ModifyDNRequest  modifyDNRequest   = new(group.DistinguishedName, parentPath, fullNewCN);
            ModifyDNResponse modifyDNResponse  = (ModifyDNResponse)LdapConnection.SendRequest(modifyDNRequest);
            if (modifyDNResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok(fullNewCN + "," + parentPath);
            }

            return Result.Fail("Failed to rename group: " + modifyDNResponse.ErrorMessage + " [ " + modifyDNResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to rename Group: " + group.DistinguishedName + " Error: " + e.Message, e));
        }
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
        ADSPath ouPath = DomainRoot.NewChildADSPath(ADpReadOnlyPasswordPolicy.ROOT_OU_PATH,false);
        return ouPath;
    }
}