using SlugEnt.AD.Protocols;
using SlugEnt.IS;

namespace AD.Protocols.ADObjects;

/// <summary>
/// Represents an AD Organization Unit object.  This is a Read/Write Object.
/// </summary>
public class ADpOrgUnit : ADpBaseObject
{
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
        InCreationMode = false;
    }

    
    /// <summary>
    /// Starts the process of creating a new OU.
    /// </summary>
    /// <param name="ouName"></param>
    public ADpOrgUnit (string ouName ) : base(ouName)
    {}

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
    /// Builds the prefix for the distinguished name of the object.  Some objects use a prefix other than cn.
    /// </summary>
    /// <returns></returns>
    internal override string BuildDistinguishedNamePrefix()
    {
        return $"OU={CommonName}";
    }
    
}

