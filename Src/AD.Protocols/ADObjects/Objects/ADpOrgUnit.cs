using SlugEnt.AD.Protocols;

using System.DirectoryServices.Protocols;
using SlugEnt.FluentResults;

namespace AD.Protocols.ADObjects;

/// <summary>
/// Represents an AD Organization Unit object.  This is a Read/Write Object.
/// </summary>
public class ADpOrgUnit : ADpBaseObject
{
    // These are characters that will help to identify if a string is a distinguished name or not.  If the string contains any of these characters it is likely a DN.
    private static char[] _commonChars = new char[] { ',', '='};
    
    /// <summary>
    /// Creates a new organizational unit with the given name. You can override CommonName, DisplayName by
    /// setting the respective properties.
    /// </summary>
    /// <param name="ouName"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ADpOrgUnit(string ouName,
                      ADSPath parentPath)

    {
        if (string.IsNullOrEmpty(ouName))
            throw new ArgumentNullException(nameof(ouName));

        InCreationMode = true;

        ParentPath    = parentPath;
        Name          = ouName;
        CommonName    = ouName;

        IsNew          = true;
        
        BuildDistinguishedName();
        InCreationMode = false;
    }


    /// <summary>
    ///  Creates an Org Unit from a set of Active Directory Attributes.
    /// </summary>
    /// <param name="attributes"></param>
    public ADpOrgUnit(SearchResultAttributeCollection attributes)
    {
        InCreationMode = true;

        foreach (DirectoryAttribute dirObj in attributes.Values)
        {
            switch (dirObj.Name)
            {
                case "description": Description = dirObj[0].ToString(); break;
                case "distinguishedName":
                    DistinguishedName = dirObj[0].ToString();
                    break;
                case "whenChanged": WhenChanged = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!); break;
                case "whenCreated": WhenCreated = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!); break;
                case "name":        Name        = dirObj[0].ToString(); break;
            }
        }

        if (DistinguishedName == null | DistinguishedName == string.Empty)
            throw new ArgumentException("No Distinguished Name found in the orgUnit object.  Anytime you retrieve an object from Active Directory you must retrieve this attribute.");

        InCreationMode = false;
    }


    /// <summary>
    /// Starts the process of creating a new OU. 
    /// </summary>
    /// <param name="ouName">Can either be just the name of the OU or a distinguished name (DN).</param>
    public ADpOrgUnit(string ouName) : base(ouName)
    {
        InCreationMode = true;
        bool likelyDN = ouName.IndexOfAny(_commonChars) >= 0;
        if (likelyDN)
        {
            Result<ADpValidatedRdnPath> result = ADpValidatedRdnPath.IsValidDn(ouName, false).Value;
            if (result.IsFailed)
                throw new ArgumentException($"the ouName provided contained Distinguished Name like characters, but was invalid: {ouName}", "ouName");

            // Create an ADSPath object from the validated DN and set the parent path and name accordingly.
            ADSPath         path       = new ADSPath(result.Value);
            Result<ADSPath> pathResult = path.GetParent();

            if (pathResult.IsSuccess)
                ParentPath = pathResult.Value;
            Name = path.Name();
            BuildDistinguishedName();
            return;
        }

        // Appears to just be a name, not a DN.  Set the name and let the user set the parent path.
        Name           = ouName;
        InCreationMode = false;
    }
    


    /// <summary>
    /// The object class of this Object.
    /// </summary>
    public override string ObjectClassName
    {
        get { return ADpCommon.OBJ_CLASS_ORGUNIT; }
    }

    /// <summary>
    /// Used in prompts and error messages to name the object type we are working with,
    /// I.E. "organizational unit", "user", "group", etc.
    /// </summary>
    public override string ObjectTypeDescription {get {return "organizational unit";}}
    
    /// <summary>
    /// Constructor for creating an ADpOrgUnit object from an existing AD object.  This
    /// will set the InCreationMode to true so that the attributes are not added to the
    /// modified list during initial setting.
    /// </summary>
    internal ADpOrgUnit() : base() { }


    /// <summary>
    /// Returns the ADSPath for this object.  This is a convenience property that allows you to get the ADSPath for this object without having to create a new ADSPath object.
    /// </summary>
    public ADSPath Path {get{return new ADSPath(DistinguishedName);}}
    
    /// <summary>
    /// Builds the prefix for the distinguished name of the object.  Some objects use a prefix other than cn.
    /// </summary>
    /// <returns></returns>
    internal override string BuildDistinguishedNamePrefix()
    {
        return $"OU={CommonName}";
    }

}

