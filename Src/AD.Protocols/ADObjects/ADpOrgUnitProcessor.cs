using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
using AD.Protocols.ADObjects;
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



}

