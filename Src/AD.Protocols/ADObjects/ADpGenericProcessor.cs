using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;

namespace AD.Protocols.ADObjects;

/// <summary>
/// This is a generic abstract base class for all AD Objects.
/// It provides the basic functionality to find objects in AD and
/// return them as a list of the specified type and the typical CRUD operations via
/// dedicated object types.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ADpGenericProcessor<T> : ADpBaseProcessor where T : ADpBaseObject
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="objectClass"></param>
    /// <param name="objectEnglishName"></param>
    /// <param name="ldapConnection"></param>
    public ADpGenericProcessor(string objectClass, string objectEnglishName, LdapConnection ldapConnection) : base(objectClass, objectEnglishName, ldapConnection) { }


    /// <summary>
    /// Defines the default set of attributes to be retrieved if none are defined at time of retrieval from AD
    /// </summary>
    internal virtual void AttrRetrieval_Default()
    {
        AttrRetrieval_AddCore();
    }
    
    
    /*
    public Result<T> Get(string distinguishedName,
                         ADSPath container,
                         SearchScope searchScope,
                         List<string> attributesToReturn)
    {
        
        Result<List<T>> result = Find(container.Path,
                                      searchScope,
                                      searchFilter,
                                      attributesToReturn,
                                      true);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        if (result.Value.Count == 0)
        {
            return Result.Fail($"No {ObjectEnglishName} found.");
        }

        return Result.Ok(result.Value[0]);
    }
    */

    /// <summary>
    /// Finds objects in AD based on the specified search parameters and returns them as
    /// a list of the specified type.
    /// </summary>
    /// <param name="searchContainerDn">The distinguished name of the container to search within.</param>
    /// <param name="searchScope">The scope of the search.</param>
    /// <param name="searchFilter">The LDAP search filter.</param>
    /// <param name="attributesToReturn">The attributes to return.</param>
    /// <param name="findOnlyOne">Whether to find only one object.</param>
    /// <returns>A result containing a list of the found objects or an error.</returns>
    public Result<List<T>> Find(string searchContainerDn,
                                SearchScope searchScope,
                                string searchFilter,
                                bool findOnlyOne = false)
    {
        //        Result<SearchResultEntryCollection> result = base.Find(searchContainerDn, searchScope, searchFilter);
        try
        {
            if (AttributeRetrieverMgr.Count  == 0)
            {
                // Add in some of the basic ones.
                // TODO: need to add some base attributes to the list of attributes to return.  This is because some of the base attributes are not returned by default.
                //ADpUserFromAD_RO.AddBaseAttributes(attributesToReturn);
            }


            Result<List<SearchResponse>> resultResponse = SearchDirectoryRaw(searchContainerDn,
                                                                             searchFilter,
                                                                             searchScope,
                                                                             AttributeRetrieverMgr.Attributes);
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
            
            
            // Now process the returned entries into Objects of the specified type.
            if (findOnlyOne && resultResponse.Value.Count > 1)
            {
                return Result.Fail($"More than one {ObjectEnglishName} found.  Request was for a single {ObjectEnglishName}");
            }

            List<T> adObjects = new List<T>();

            // TODO I do not like the fact that we are returning a list of errors here.
            // I think we should just return the first error and stop processing.
            // This is because if we have an error creating one object, it is likely that all objects will have the same error.
            foreach (SearchResultEntry entry in resultResponse.Value[0].Entries)
            {
                Result<T> objResult = CreateObjectFromAttributes(entry.Attributes);
                if (objResult.IsFailed)
                {
                    return Result.Fail(objResult.ToStringErrorOnly());
                }

                adObjects.Add(objResult.Value);
            }

            return Result.Ok(adObjects);

        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError("Unexpected exception occured", e));
        }


        ///////////////////////////////////////////
        /// 
        /// /////////////////////////////////////////

        
    }


    protected abstract Result<T> CreateObjectFromAttributes(SearchResultAttributeCollection attributes);

    
    /// <summary>
    /// Deletes the specified object from Active Directory.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Result<DeleteResponse> Delete(T obj) { return Delete(obj.DistinguishedName); }

    
    /// <summary>
    /// Performs the actual save of an object to Active Directory.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    internal Result ObjectSave(T obj) 
    {
        try
        {
            string dn;
            if (obj.IsNew)
                if (obj.DistinguishedName == null || obj.DistinguishedName == string.Empty)
                    obj.BuildDistinguishedName();
            
            // Retrieve the Directory Attributes to save to AD.
            DirectoryAttribute[] attributesToLoad = obj.GetDirectoryAttributesNew();
            AddRequest           addRequest       = new(obj.DistinguishedName, attributesToLoad);
            AddResponse addResponse = (AddResponse)_ldapConnection.SendRequest(addRequest);
            if (addResponse.ResultCode == ResultCode.Success)
            {
                return Result.Ok();
            }

            return Result.Fail($"Failed to add {obj.ObjectTypeDescription}: " + addResponse.ErrorMessage + " [ " + addResponse.ResultCode + " ]");
        }
        catch (Exception ex)
        {
            return Result.Fail(new ExceptionalError($"Failed to add {obj.ObjectTypeDescription}: " + ex.Message, ex));
        }

    }


    public Result AddNew(T obj)
    {
        return ObjectSave(obj);
    }


    /// <summary>
    /// Adds the AD attributes when Created and Changed to list of attributes to retrieve.
    /// </summary>
    public void AttrRetrieval_AddAuditing()
    {
        AttributeRetrieverMgr.AddAttribute("whenChanged");
        AttributeRetrieverMgr.AddAttribute("whenCreated");
    }


    /// <summary>
    /// Adds the AD attributes cn, distinguishedName to list of attributes to retrieve.
    /// </summary>
    public void AttrRetrieval_AddCore()
    {
        AttributeRetrieverMgr.AddAttribute("distinguishedName");
        AttributeRetrieverMgr.AddAttribute("cn");
    }
}

