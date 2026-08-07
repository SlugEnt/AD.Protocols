using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;

namespace AD.Protocols.ADObjects;

/// <summary>
/// Provides the ability to perform CRUD operations on Active Directory in regards to Organizational Units.
/// </summary>
public class ADpOrgUnitProcessor : ADpGenericProcessor<ADpOrgUnit>
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="ldapConnection"></param>
    public ADpOrgUnitProcessor(LdapConnection ldapConnection) : base(ADpCommon.OBJ_CLASS_ORGUNIT, "Organizational Unit", ldapConnection) { }

    protected override Result<ADpOrgUnit> CreateObjectFromAttributes(SearchResultAttributeCollection attributes)
    {
        ADpOrgUnit orgUnit                = new(attributes);
        return Result.Ok(orgUnit);
    }


    /// <inheritdoc cref="GetAllChildOrgUnits(ADSPath)"/>
    /// <param name="parentOu"></param>
    /// <returns></returns>
    public Result<List<ADpOrgUnit>> GetAllChildOrgUnits(ADpOrgUnit parentOu)
    {
        return GetAllChildOrgUnits(parentOu.Path);
    }


    /// <summary>
    /// Retrieves all Children OU's under the parent OU.  Does Not recurse.
    /// </summary>
    /// <param name="parentDn"></param>
    /// <returns></returns>
    public Result<List<ADpOrgUnit>> GetAllChildOrgUnits(ADSPath parentDn)
    {
        string searchFilter = $"(&(objectClass={ADpCommon.OBJ_CLASS_ORGUNIT}))";
        Result<List<ADpOrgUnit>> result = Find(parentDn.Path, SearchScope.OneLevel, searchFilter);
        return result;
    }

    

    /// <summary>
    /// OU's do not use CN as the name attribute, they use OU.
    /// This property overrides the base class to set the name attribute to OU.
    /// </summary>
    protected override string NameAttributeName { get; set; } = "ou";


    /// <summary>
    /// Set Default Attributes to be retrieved if none are defined at time of retrieval from AD
    /// </summary>
    internal override void AttrRetrieval_Default()
    {
        base.AttrRetrieval_Default();
        AttributeRetrieverMgr.AddAttribute("ou");
    }

    
    /// <summary>
    /// Provides a simple way to add a new OU to the directory.
    /// </summary>
    /// <param name="ouName">Name to be given to the OU</param>
    /// <param name="parentPath">Parent path under which the OU will be created</param>
    /// <returns>Result containing the newly created OU or errors if creation failed</returns>
    public Result<ADpOrgUnit> AddNew (string ouName, ADSPath parentPath)
    {
        ADpOrgUnit newOu = new ADpOrgUnit(ouName, parentPath);
        Result result = AddNew(newOu);
        if (result.IsFailed)
            return Result.Fail<ADpOrgUnit>(result.Errors);
        return Result.Ok(newOu);
    }


    /// <inheritdoc cref="AddNew(string, ADSPath)"/>
    /// <param name="ouName">Name to be given to the OU</param>
    /// <param name="parentOu">Parent OU under which the new OU will be created</param>
    public Result<ADpOrgUnit> AddNew(string ouName,
                                     ADpOrgUnit parentOu)
    {
        return AddNew(ouName, parentOu.Path);
    }
}

