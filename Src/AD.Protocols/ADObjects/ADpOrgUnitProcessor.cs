using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Text;
using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;

namespace AD.Protocols.ADObjects;

public class ADpOrgUnitProcessor : ADpGenericProcessor<ADpOrgUnit>
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="ldapConnection"></param>
    public ADpOrgUnitProcessor(LdapConnection ldapConnection) : base(ADpCommon.OBJ_CLASS_ORGUNIT, "Organizational Unit", ldapConnection) { }

    protected override Result<ADpOrgUnit> CreateObjectFromAttributes(SearchResultAttributeCollection attributes)
    {
        ADpOrgUnit orgUnit                = new();
        bool               distinguishedNameFound = false;

        foreach (DirectoryAttribute dirObj in attributes.Values)
        {
            switch (dirObj.Name)
            {
                case "description": orgUnit.Description = dirObj[0].ToString(); break;
                case "distinguishedName":
                    orgUnit.DistinguishedName = dirObj[0].ToString();
                    distinguishedNameFound    = true;
                    break;
                case "whenChanged": orgUnit.WhenChanged = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!); break;
                case "whenCreated": orgUnit.WhenCreated = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!); break;
                case "name":        orgUnit.Name        = dirObj[0].ToString(); break;
            }
        }

        if (!distinguishedNameFound)
        {
            return Result.Fail<ADpOrgUnit>("No Distinguished Name found in the orgUnit object.  It is a required attribute.");
        }

        return Result.Ok(orgUnit);

    }

    

    /// <summary>
    /// Set Default Attributes to be retrieved if none are defined at time of retrieval from AD
    /// </summary>
    internal override void AttrRetrieval_Default()
    {
        base.AttrRetrieval_Default();
        AttributeRetrieverMgr.AddAttribute("ou");
    }



}

