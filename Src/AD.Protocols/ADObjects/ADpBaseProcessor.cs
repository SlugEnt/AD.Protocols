using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;

namespace AD.Protocols.ADObjects;

/// <summary>
/// Serves as the base class for All Active Directory Protocol objects.
/// </summary>
public abstract class ADpBaseProcessor
{
    // The Active Directory LDAP Connector.
    protected LdapConnection _ldapConnection;
    
    protected readonly string _objectClass;

    #region Constants
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public const string EXISTS                                         = "Exists";
    public const string NOT_FOUND                                      = "NotFound";
    public const string SEARCH_FILTER_ALL_COMPUTERS                    = "(objectCategory=computer)";
    public const string SEARCH_FILTER_ALL_DISABLED_USERS               = "(&(objectCategory=person)(objectClass=user)(userAccountControl:1.2.840.113556.1.4.803:=2))";
    public const string SEARCH_FILTER_ALL_GROUPS                       = "(objectCategory=group)";
    public const string SEARCH_FILTER_ALL_OU                           = "(objectCategory=organizationalUnit)";
    public const string SEARCH_FILTER_ALL_USERS                        = "(sAMAccountType=805306368)";
    public const string SEARCH_FILTER_ALL_USERS_PASSWORD_NEVER_EXPIREa = "(&(objectCategory=person)(objectClass=user)(userAccountControl:1.2.840.113556.1.4.803:=65536))";
#pragma warning restore

    #endregion

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="objectClass"></param>
    public ADpBaseProcessor(string objectClass, string objectEnglishName, LdapConnection ldapConnection)
    {
        _objectClass = objectClass;
        ObjectEnglishName = objectEnglishName;
        _ldapConnection = ldapConnection;
    }

    
    /// <summary>
    /// This is the type of object that this class represents.
    /// For example, "user", "group", "organizationalUnit", etc.
    /// </summary>
    protected string ObjectEnglishName { get; private set; }

    
    
    /// <summary>
    /// Deletes the requested object from Active Directory.  The distinguishedName must be provided.
    /// </summary>
    /// <param name="distinguishedName"></param>
    /// <returns></returns>
    public Result<DeleteResponse> Delete (string distinguishedName)
    {
        if (_ldapConnection == null)
            return Result.Fail<DeleteResponse>("The LDAP Connection has not been set.  Cannot delete object.");

        try
        {
            DeleteRequest  deleteRequest  = new(distinguishedName);
            DeleteResponse deleteResponse = (DeleteResponse)_ldapConnection.SendRequest(deleteRequest);
            if (deleteResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok(deleteResponse);
            }

            return Result.Fail($"Failed to delete {ObjectEnglishName}: {deleteResponse.ErrorMessage} [ {deleteResponse.ResultCode} ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError($"Failed to delete {ObjectEnglishName}: {distinguishedName} Error: {e.Message}", e));
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
    public Result<object> ObjectGetByAttribute(string searchContainerDn,
                                                      string attributeName,
                                                      string attributeValue,
                                                      SearchScope searchScope = SearchScope.Subtree,
                                                      List<string> attributesToReturn = null)
    {
        /*
        try
        {
            // If no attributes are provided we will add the base attributes.
            if (attributesToReturn == null)
            {
                attributesToReturn = new();
                ADpUserFromAD_RO.AddAllAttributes(attributesToReturn);
            }


            string searchFilter = $"(&(objectClass=Person)({attributeName}={attributeValue}))";
            Result<ADpUserFromAD_RO> result = FindSingleObject(searchContainerDn,
                                                                searchScope,
                                                                searchFilter,
                                                                attributesToReturn);
            return result;
        }
        catch (Exception e)
        {
            return Result.Fail($"Failed to retrieve user with attribute [ {attributeName} ]  containing value [{attributeValue} | Error:  {e.Message}");
        }
        */
        return Result.Fail("");
    }


    /// <summary>
    /// Retrieves a single object based upon the provided distinguished name.
    /// </summary>
    /// <param name="dn">The distinguished name of the object to retrieve.</param>
    /// <param name="searchScope">The scope of the search.</param>
    /// <param name="attributesToReturn">The attributes to return for the object.</param>
    /// <returns>A Result containing the search result entry collection or an error.</returns>
    protected Result<SearchResultEntryCollection> GetSingle (string dn)
    {
        SearchScope searchScope = SearchScope.Base;
        string searchFilter = $"(objectClass=*)";
        
        
        //        Result<SearchResultEntryCollection> result = Find(dn, searchScope, $"(objectClass={_objectClass})", attributesToReturn);
        Result<List<SearchResponse>> result = 
            SearchDirectoryRaw(dn,searchFilter, searchScope);
        
        if (result.IsFailed)
            return Result.Fail(result.Errors);
        
        if (result.Value.Count == 0)
            return Result.Fail(NOT_FOUND);
        
        if (result.Value[0].Entries.Count == 0)
            return Result.Fail(NOT_FOUND);
        
        return Result.Ok(result.Value[0].Entries);
    }

    /// <summary>
    /// Finds all objects that match the search scope and filter.
    /// Converts them to the appropriate object type and returns them in a list.
    /// </summary>
    /// <param name="searchContainerDn"></param>
    /// <param name="searchScope"></param>
    /// <param name="searchFilter"></param>
    /// <param name="attributesToReturn"></param>
    /// <returns></returns>
    protected Result<SearchResultEntryCollection> Find(string searchContainerDn,
                                                  SearchScope searchScope,
                                                  string searchFilter)
    {
        try
        {
            Result<List<SearchResponse>> resultResponse = SearchDirectoryRaw(searchContainerDn,
                                                                          searchFilter,
                                                                          searchScope);
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

            return Result.Ok(resultResponse.Value[0].Entries);
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Unexpected exception occured", e));
        }
    }



    /// <summary>
    ///   Performs the Raw Active Directory search and returns the results.
    /// This is a low level function that does not convert the results into
    /// any specific object type.
    /// </summary>
    /// <param name="searchContainerDn">Distinguished name from which to start the search from</param>
    /// <param name="searchFilter">LDAP Syntax search filter</param>
    /// <param name="searchScope">LDAP Syntax search scope</param>
    /// <param name="attributeList">List of attributes to bring back</param>
    /// <returns>Result Success or Failure (along with error message)</returns>
    public Result<List<SearchResponse>> SearchDirectoryRaw(string searchContainerDn,
                                                        string searchFilter,
                                                        SearchScope searchScope)
    {
        List<SearchResponse> result = new();
        SearchResponse? response = null;
        int maxResultsToRequest = 200;

        try
        {
            PageResultRequestControl pageRequestControl = new(maxResultsToRequest);

            // used to retrieve the cookie to send for the subsequent request
            PageResultResponseControl pageResponseControl;
            SearchRequest searchRequest = new(searchContainerDn,
                                              searchFilter,
                                              searchScope,
                                              AttributeRetrieverMgr.Attributes);
            searchRequest.Controls.Add(pageRequestControl);

            while (true)
            {
                response = (SearchResponse)_ldapConnection.SendRequest(searchRequest);
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
            return Result.Fail(new ExceptionalError($"SearchDirectory: [ {searchContainerDn} ]  Had Error. {e.Message}", e));
        }

        return Result.Ok(result);
    }


    /// <summary>
    /// Provides access to the AttributeRetrieverMgr which is used to set the attributes that
    /// should be returned from Active Directory when retrieving objects.  This allows for customization of the attributes that are returned for different object types.
    /// </summary>
    public AttributeRetrieverMgr AttributeRetrieverMgr { get; private set; } = new AttributeRetrieverMgr();

}

