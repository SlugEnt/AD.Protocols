using SlugEnt.AD.Protocols.Attributes;
using SlugEnt.FluentResults;
using AD.Protocols.ADObjects;
using System.DirectoryServices.Protocols;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

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
        try
        {
            Result<List<SearchResponse>> resultResponse = SearchDirectoryRaw(searchContainerDn,
                                                                             searchFilter,
                                                                             searchScope);
            if (resultResponse.IsFailed)
                return Result.Fail(resultResponse.Errors);
            
            if (resultResponse.Value.Count == 0)
                return Result.Fail(NOT_FOUND);
            
            if (resultResponse.Value[0].Entries.Count == 0)
                return Result.Fail(NOT_FOUND);
            
            // Now process the returned entries into Objects of the specified type.
            if (findOnlyOne && resultResponse.Value.Count > 1)
                return Result.Fail($"More than one {ObjectEnglishName} found.  Request was for a single {ObjectEnglishName}");
            
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
   
    }



    protected abstract Result<T> CreateObjectFromAttributes(SearchResultAttributeCollection attributes);

    
    /// <summary>
    /// Deletes the specified object from Active Directory.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Result Delete(T obj) { return Delete(obj.DistinguishedName); }

    
    
    
    
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

            // Perform any final Pre-Save processing on the object before saving to AD.
            obj.SyncPreSave();

            // Retrieve the Directory Attributes to save to AD.
            DirectoryAttribute[] attributesToLoad = obj.GetDirectoryAttributesNew();
            AddRequest           addRequest       = new(obj.DistinguishedName, attributesToLoad);
            AddResponse addResponse = (AddResponse)_ldapConnection.SendRequest(addRequest);
            if (addResponse.ResultCode == ResultCode.Success)
            {
                obj.IsNew = false;
                // Clear the changed attributes since we just saved the object to AD.
                obj.AttributesToUpdate.Clear();
                
                return Result.Ok();
            }

            return Result.Fail($"Failed to add {obj.ObjectTypeDescription}: " + addResponse.ErrorMessage + " [ " + addResponse.ResultCode + " ]");
        }
        catch (Exception ex)
        {
            return Result.Fail(new ExceptionalError($"Failed to add {obj.ObjectTypeDescription}: " + ex.Message, ex));
        }
    }


    /// <summary>
    /// Adds a new object to active Directory based on the specified name and parent path, and performs any post-save processing.
    /// </summary>
    /// <param name="name">Name to be given to the object</param>
    /// <param name="parentPath">The parent path in Active Directory where the object will be created</param>
    /// <returns>Result.Ok and the name of the object that was created.  Or an error if the operation failed.</returns>
    public Result<string> AddNew(string name, ADSPath parentPath)
    {
        string x   = _objectClass;
        
        T      obj    = (T)Activator.CreateInstance(typeof(T), name, parentPath);
        Result result = AddNew(obj);
        if (result.IsSuccess)
            return Result.Ok(obj.DistinguishedName);

        return result;
    }


    /// <summary>
    /// Adds a new object to Active Directory and performs any post-save processing.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Result AddNew(T obj)
    {
        Result result = ObjectSave(obj);
        if (result.IsSuccess)
        {
            Result x = AfterSave(obj);
            if (x.IsSuccess)
                return Result.Ok();

            Error er = new Error("Failed in AfterSave.  Object was saved to AD successfully, but some events afterward failed.").CausedBy(x.Errors);
            return Result.Fail(er);
        }
        return result;
    }


    /// <summary>
    /// Retries a single object from Active Directory based on the specified distinguished name.
    /// </summary>
    /// <param name="distinguishedName"></param>
    /// <returns></returns>
    public Result<T> Get(string distinguishedName)
    { 
        Result<SearchResultEntryCollection> result = GetSingle(distinguishedName);

        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        try
        {
            T x = (T)Activator.CreateInstance(typeof(T), result.Value[0].Attributes);

            // Return the object.
            return Result.Ok(x);
        }
        catch(Exception  ex) {
            return Result.Fail(new ExceptionalError($"Failed to Create the new object - {ObjectEnglishName}: " + ex.Message, ex));
        }
    }


    /// <summary>
    /// Retries all objects from Active Directory based on the specified name within the specified OU and scope
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Result<T> GetBy_Name(string name, ADSPath startingSearchPath, SearchScope searchScope = SearchScope.Subtree)
    {
        Result<List<T>> result = FindByAttribute(startingSearchPath.Path,NameAttributeName,name, searchScope);
        if (result.IsFailed)
            return Result.Fail(result.Errors);

        if (result.Value.Count == 0)
            return Result.Fail($"No {ObjectEnglishName} found.");

        if (result.Value.Count > 1)
            return Result.Fail($"Expected to only find one match for the given attribute, but found multiple {ObjectEnglishName}s.");
        
        // Return the object.
        return Result.Ok(result.Value[0]);
    }


    /// <summary>
    /// Retrieves a list of objects from Active Directory based on the specified attribute name and value.
    /// </summary>
    /// <param name="searchContainerDn"></param>
    /// <param name="attributeName"></param>
    /// <param name="attributeValue"></param>
    /// <param name="searchScope"></param>
    /// <returns></returns>
    public Result<List<T>> FindByAttribute(string searchContainerDn,
                                           string attributeName,
                                           string attributeValue,
                                           SearchScope searchScope = SearchScope.Subtree)
    {
        Result<SearchResultEntryCollection> result = base.FindByAttribute(searchContainerDn, attributeName, attributeValue, searchScope);
        if (result.IsFailed)
            return Result.Fail(result.Errors);
        if (result.Value.Count == 0)
            return Result.Fail($"No {ObjectEnglishName} found.");

        List<T> adObjects = new List<T>();
        foreach (SearchResultEntry entry in result.Value)
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


    /// <summary>
    /// Updates the object in Active Directory.  Only updates changes attributes.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Result Update(T obj) 
    {
        try
        {
            // Perform any Object Pre-Save processing on the object before saving to AD.
            obj.SyncPreSave();
            
            DirectoryAttributeModification[] modifications = new DirectoryAttributeModification[obj.AttributesToUpdate.Count];
            int i = 0;

            foreach (KeyValuePair<string, AttributeBase> attributeBase in obj.AttributesToUpdate)
            {
                AttributeBase attribute = attributeBase.Value;    
            
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

            ModifyRequest modifyRequest = new(obj.DistinguishedName, modifications);
            PermissiveModifyControl permissiveModify = new();
            modifyRequest.Controls.Add(permissiveModify);

            ModifyResponse modifyResponse = (ModifyResponse)_ldapConnection.SendRequest(modifyRequest);
            if (modifyResponse.ResultCode == ResultCode.Success)
            {
                    Result x = AfterSave(obj);
                    if (x.IsSuccess)
                        return Result.Ok();

                    Error er = new Error("Failed in AfterSave.  Object was saved to AD successfully, but some events afterward failed.").CausedBy(x.Errors);
                    return Result.Fail(er);
            }
            return Result.Fail($"Failed to update {ObjectEnglishName}: " + modifyResponse.ErrorMessage + " [ " + modifyResponse.ResultCode + " ]");
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError($"Failed to update {ObjectEnglishName}: " + e.Message, e));
        }

    }

    
    /// <summary>
    /// Returns True if the specified object exists in Active Directory, otherwise returns False.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Result<bool> Exists (T obj)
    {
        if (obj.DistinguishedName == null)
            return Result.Fail("Distinguished Name field does not exist - cannot check this object yet.");
        
        return Exists(obj.DistinguishedName);
    }

    
    
    public Result Move(T obj,
                       ADSPath destinationPath)
    {
        Result<string> result = Move(obj.DistinguishedName, destinationPath, obj.Name);
        if (result.IsFailed)
            return Result.Fail(result.Errors);
        
        obj.ReplaceDistinguishedName(result.Value);
        return Result.Ok();
    }
    
    
    /// <summary>
    /// Renames the given object to a new name.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="newCommonName"></param>
    /// <returns></returns>
    public Result Rename (T obj, string newCommonName)
    {
        ADSPath  parentPath = new ADSPath(obj.DistinguishedName).GetParent();
        Result<string> result     = Rename(obj.DistinguishedName, newCommonName, parentPath);
        if (result.IsFailed)
            return Result.Fail(result.Errors);
        
        obj.RenameObject(result.Value, newCommonName);
        return Result.Ok();
    }
    
    
    /// <summary>
    /// Derived classes should override this if they need to do anything after saving an object to AD.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected virtual Result AfterSave (T obj) { return Result.Ok(); }
}

