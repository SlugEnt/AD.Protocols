using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;

namespace SlugEnt.AD.Protocols;

/// <summary>
/// Represents an AD Organization Unit object.  This is a Read Only Object.
/// </summary>
public class ADpReadOnlyOrgUnit
    {
    /// <summary>
    /// Name of the orgUnit
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description of the orgUnits purpose
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display Name for the orgUnit
    /// </summary>
    public string? DisplayName { get; protected set; }

    /// <summary>
    /// The full DN of the orgUnit
    /// </summary>
    public string? DistinguishedName { get; protected set; }


    /// <summary>
    /// When orgUnit was last changed
    /// </summary>
    public DateTimeOffset WhenChanged { get; protected set; }

    /// <summary>
    /// When orgUnit was created
    /// </summary>
    public DateTimeOffset WhenCreated { get; protected set; }

    
    /// <summary>
    /// Adds the Base attributes - sAMAccountName, displayName, mail, cn, distinguishedName, orgUnitType
    /// </summary>
    /// <param name="attributeList"></param>
    public static void AddBaseAttributes(List<string> attributeList)
    {
        attributeList.Add("displayName");
        attributeList.Add("distinguishedName");
        attributeList.Add("name");
        attributeList.Add("whenCreated");
        attributeList.Add("whenChanged");
    }



    /// <summary>
    /// Createa new Read Only Org Unit object from the SearchResultAttributeCollection
    /// </summary>
    /// <param name="attributes"></param>
    /// <returns></returns>
    public static Result<ADpReadOnlyOrgUnit> CreateOrgUnitObj(SearchResultAttributeCollection attributes)
    {
        ADpReadOnlyOrgUnit orgUnit = new();
        bool distinguishedNameFound = false;

        foreach (DirectoryAttribute dirObj in attributes.Values)
        {
            switch (dirObj.Name)
            {
                case "description":
                    orgUnit.Description = dirObj[0].ToString();
                    break;
                case "distinguishedName":
                    orgUnit.DistinguishedName = dirObj[0].ToString();
                    distinguishedNameFound = true;
                    break;
                case "displayName":
                    orgUnit.DisplayName = dirObj[0].ToString();
                    break;
                case "whenChanged":
                    orgUnit.WhenChanged = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!);
                    break;
                case "whenCreated":
                    orgUnit.WhenCreated = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!);
                    break;
                case "name":
                    orgUnit.Name = dirObj[0].ToString();
                    break;
            }
        }
        if (!distinguishedNameFound)
        {
            return Result.Fail<ADpReadOnlyOrgUnit>("No Distinguished Name found in the orgUnit object.  It is a required attribute.");
        }

        return Result.Ok(orgUnit);
    }

}
