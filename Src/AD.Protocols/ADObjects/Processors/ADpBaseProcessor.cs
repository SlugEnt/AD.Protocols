using SlugEnt.AD.Protocols;
using SlugEnt.AD.Protocols.Attributes;
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
    /// Which attribute is used to represent the name of the object.
    /// For example, "cn" for users and groups, "ou" for organizational units, etc.
    /// </summary>
    protected virtual string NameAttributeName { get; set; } = "cn";
    
    /// <summary>
    /// Deletes the requested object from Active Directory.  The distinguishedName must be provided.
    /// </summary>
    /// <param name="distinguishedName"></param>
    /// <returns></returns>
    public Result Delete (string distinguishedName, bool recursiveDeleteChildren = false)
    {
        if (_ldapConnection == null)
            return Result.Fail("The LDAP Connection has not been set.  Cannot delete object.");
        try
        {
            DeleteRequest  deleteRequest  = new(distinguishedName);

            if (recursiveDeleteChildren)
            {
                DirectoryControl directoryControl = new DirectoryControl(
                
                    "1.2.840.113556.1.4.805",
                    null,
                    true,
                    true
                    );
                deleteRequest.Controls.Add(directoryControl);
            }
            
            DeleteResponse deleteResponse = (DeleteResponse)_ldapConnection.SendRequest(deleteRequest);
            if (deleteResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }

            return Result.Fail($"Failed to delete {ObjectEnglishName}: {deleteResponse.ErrorMessage} [ {deleteResponse.ResultCode} ]");
        }
        catch (Exception e)
        {
            if (e.Message.Contains("object does not exist"))
                return Result.Ok();
            
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
    public Result<SearchResultEntryCollection> FindByAttribute(string searchContainerDn,
                                                      string attributeName,
                                                      string attributeValue,
                                                      SearchScope searchScope = SearchScope.Subtree)
    {
        
        try
        {
            string searchFilter = $"(&(objectClass={_objectClass})({attributeName}={attributeValue}))";
            
            Result<SearchResultEntryCollection> result = Find(searchContainerDn,
                                                                searchScope,
                                                                searchFilter);
            if (result.IsFailed)
                return result;
            if (result.Value.Count == 0)
                return Result.Fail(NOT_FOUND);

            return Result.Ok(result.Value);

        }
        catch (Exception e)
        {
            return Result.Fail($"Failed to retrieve user with attribute [ {attributeName} ]  containing value [{attributeValue} | Error:  {e.Message}");
        }
    }


    /// <summary>
    /// Retrieves a single object based upon the provided distinguished name.
    /// </summary>
    /// <param name="dn">The distinguished name of the object to retrieve.</param>
    /// <param name="overrideAttributes">Optional array of attributes to override the default attributes to return.  Only needed in rare cases</param>
    /// <returns>If object Found:  A Result containing the search result entry collection
    /// <para>>If not Found:  Result.Failed with Reason Code: NotFound</para></returns>
    protected Result<SearchResultEntryCollection> GetSingle (string dn, string[] overrideAttributes = null)
    {
        SearchScope searchScope = SearchScope.Base;
        string searchFilter = $"(objectClass=*)";
        
        
        //        Result<SearchResultEntryCollection> result = Find(dn, searchScope, $"(objectClass={_objectClass})", attributesToReturn);
        Result<List<SearchResponse>> result = 
            SearchForSingleEntryRaw(dn,searchFilter, searchScope,overrideAttributes);
        if (result.ReasonCode == EnumReasonCode.NotFound)
            return Result.Fail(NOT_FOUND,EnumReasonCode.NotFound);
        
        if (result.IsFailed)
            return Result.Fail(result.Errors);
        
        if (result.Value.Count == 0)
            return Result.Fail(NOT_FOUND,EnumReasonCode.NotFound);
        
        if (result.Value[0].Entries.Count == 0)
            return Result.Fail(NOT_FOUND,EnumReasonCode.NotFound);
        
        return Result.Ok(result.Value[0].Entries);
    }


    /// <summary>
    /// Returns whether an object with the specified distinguished name exists in Active Directory.
    /// </summary>
    /// <param name="dn">The distinguished name of the object to check.</param>
    /// <returns>A Result indicating whether the object exists or an error occurred.  Value is True if it exists, False if not.</returns>
    public Result<bool> Exists (string dn)
    {
        string[] overrideAttributes = new string[] { "cn" }; // Only need to retrieve the cn attribute to check existence
        
        Result<SearchResultEntryCollection> result = GetSingle(dn, overrideAttributes);
        
        if (result.IsSuccess) return Result.Ok(true);
        if(result.ReasonCode == EnumReasonCode.NotFound)
            return Result.Ok(false);
        return Result.Fail(result.Errors);
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
                return Result.Fail(NOT_FOUND,EnumReasonCode.NotFound);
            }

            if (resultResponse.Value[0].Entries.Count == 0)
            {
                return Result.Fail(NOT_FOUND,EnumReasonCode.NotFound);
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
    /// <param name="overrideAttributes">Only if you wish to bypass the current Attributes to Retrieve for this call only</param>    
    /// <returns>Result Success or Failure (along with error message)</returns>
    public Result<List<SearchResponse>> SearchDirectoryRaw(string searchContainerDn,
                                                        string searchFilter,
                                                        SearchScope searchScope,
                                                        string[] overrideAttributes = null)
    {
        List<SearchResponse> result = new();
        SearchResponse? response = null;
        int maxResultsToRequest = 200;

        try
        {
            PageResultRequestControl pageRequestControl = new(maxResultsToRequest);

            // used to retrieve the cookie to send for the subsequent request
            PageResultResponseControl pageResponseControl;
            if (overrideAttributes == null)
            {
                overrideAttributes = AttributeRetrieverMgr.Attributes;
            }
            
            SearchRequest searchRequest = new(searchContainerDn,
                                              searchFilter,
                                              searchScope,
                                              overrideAttributes);
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
    /// This method is an exact copy of SearchDirectoryRaw, but it is used when we have specified the searchContainerDn
    /// to be a single item, such as CN= It responds with NotFound if AD returns object does not exist.
    /// <remarks>In our case, because we are looking for a single specific item, the Object Does Not Exist  means the
    /// object is not found and not also possibly the parent does not exist.</remarks>
    /// </summary>
    /// <param name="searchContainerDn"></param>
    /// <param name="searchFilter"></param>
    /// <param name="searchScope"></param>
    /// <param name="overrideAttributes"></param>
    /// <returns></returns>
    public Result<List<SearchResponse>> SearchForSingleEntryRaw(string searchContainerDn,
                                                    string searchFilter,
                                                    SearchScope searchScope,
                                                    string[] overrideAttributes = null)
    {
        List<SearchResponse> result = new();
        SearchResponse? response = null;
        int maxResultsToRequest = 200;

        try
        {
            PageResultRequestControl pageRequestControl = new(maxResultsToRequest);

            // used to retrieve the cookie to send for the subsequent request
            PageResultResponseControl pageResponseControl;
            if (overrideAttributes == null)
            {
                overrideAttributes = AttributeRetrieverMgr.Attributes;
            }

            SearchRequest searchRequest = new(searchContainerDn,
                                              searchFilter,
                                              searchScope,
                                              overrideAttributes);
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
                return Result.Fail(new Error("NF", EnumReasonCode.NotFound));
            }
            return Result.Fail(new ExceptionalError($"SearchForSingleEntryRaw: [ {searchContainerDn} ]  Had Error. {e.Message}", e));
        }

        return Result.Ok(result);
    }


    /// <summary>
    /// Simple means of creating objects in Active Directory.  It does nothing more than create an
    /// object of the requested class with the requested name in the requested parent path.  It does not set any attributes on the object.
    /// </summary>
    /// <param name="name">Name to be given to the object</param>
    /// <param name="parentPath">Path where the object is to be created in</param>
    /// <param name="objectClass">Type of object, ie, person, group, user, etc</param>
    /// <param name="distinguishedNamePrefix">The prefix that identifies this object.  Is ObjectClass specific</param>
    /// <returns></returns>
    public Result CreateSimple(string name, ADSPath parentPath, string objectClass,string distinguishedNamePrefix = "cn" )
    {
        try
        {
            string dn = $"{distinguishedNamePrefix}={name},{parentPath.Path}";
            
            AddRequest  addRequest  = new(dn, objectClass);
            AddResponse addResponse = (AddResponse)_ldapConnection.SendRequest(addRequest);
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
    ///   Moves an AD object
    /// </summary>
    /// <param name="currentDn">The distinguished name of the object to be moved</param>
    /// <param name="destinationParentPath">The path of the destination parent container</param>
    /// <param name="newName">Should just be the new name, without the attribute prefix</param>
    /// <returns></returns>
    protected Result<string> Move(string currentDn, ADSPath destinationParentPath, string newName)
    {
        try
        {
            if (newName.Length < 3)
                newName = NameAttributeName + "=" + newName;

            if (newName[2] != '=')
                newName = NameAttributeName + "=" + newName;
            
            ModifyDNRequest  modifyDNRequest  = new(currentDn, destinationParentPath.Path, newName);
            ModifyDNResponse modifyDNResponse = (ModifyDNResponse)_ldapConnection.SendRequest(modifyDNRequest);
            if (modifyDNResponse.ResultCode == ResultCode.Success)
            {
                ADSPath path = new ADSPath(destinationParentPath.Path,newName);
                return Result.Ok(path.Path);
            }

            return Result.Fail("Failed to move " + ObjectEnglishName + ": " + modifyDNResponse.ErrorMessage + " [ " + modifyDNResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError($"Failed to move {ObjectEnglishName}: {currentDn}  Error: {e.Message}", e));
        }
    }


    /// <summary>
    /// Renames the object and returns the full new DN.
    /// </summary>
    /// <param name="currentDistinguishedName"></param>
    /// <param name="newCommonName"></param>
    /// <param name="parentPath"></param>
    /// <returns></returns>
    protected Result<string> Rename(string currentDistinguishedName, string newCommonName, ADSPath parentPath)
    {
        try
        {
            string           fullNewCN        = $"{NameAttributeName}={newCommonName}";
            ModifyDNRequest  modifyDNRequest  = new(currentDistinguishedName, parentPath.Path, fullNewCN);
            ModifyDNResponse modifyDNResponse = (ModifyDNResponse)_ldapConnection.SendRequest(modifyDNRequest);
            if (modifyDNResponse.ResultCode == ResultCode.Success)
            {
                ADSPath child = new(parentPath.Path, fullNewCN);
                return Result.Ok(child.Path);
            }

            return Result.Fail("Failed to move User: " + modifyDNResponse.ErrorMessage + " [ " + modifyDNResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Failed to move User: " + currentDistinguishedName + " Error: " + e.Message, e));
        }

    }

    /// <summary>
    /// Provides access to the AttributeRetrieverMgr which is used to set the attributes that
    /// should be returned from Active Directory when retrieving objects.  This allows for customization of the attributes that are returned for different object types.
    /// </summary>
    public AttributeRetrieverMgr AttributeRetrieverMgr { get; private set; } = new AttributeRetrieverMgr();


    /// <summary>
    ///    Converts the EnumAttributeOperation to the DirectoryAttributeOperation
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    protected static DirectoryAttributeOperation ToOperation(EnumAttributeOperation operation)
    {
        return operation switch
        {
            EnumAttributeOperation.Add    => DirectoryAttributeOperation.Add,
            EnumAttributeOperation.Delete => DirectoryAttributeOperation.Delete,
            EnumAttributeOperation.Modify => DirectoryAttributeOperation.Replace,
            _                             => DirectoryAttributeOperation.Add
        };
    }

}

