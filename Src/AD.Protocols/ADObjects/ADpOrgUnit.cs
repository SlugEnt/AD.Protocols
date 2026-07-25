
using SlugEnt.AD.Protocols;
using SlugEnt.AD.Protocols.Attributes;
using SlugEnt.IS;
using System.DirectoryServices.Protocols;

namespace AD.Protocols.ADObjects;

public class ADpOrgUnit : ADpBaseObject
{
    /// <summary>
    ///     This is the most preferred constructor if you have already read the user from AD as it almost 100% assuredly will
    ///     pull the OU without error.
    /// </summary>
    /// <param name="readOnlyUser"></param>
    public ADpOrgUnit(ADpReadOnlyOrgUnit orgUnit)
    {
        string? value = orgUnit.DistinguishedName;
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException(nameof(value), "Distinguished Name cannot be null or empty.");

        DistinguishedName = value;
        DescriptionChg    = orgUnit.Description;
        NameChg           = orgUnit.Name;
        CommonNameChg     = orgUnit.Name;
        ParentPath        = orgUnit.ParentPath;
        IsNew             = false;
        
    }


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
        NameChg       = ouName;
        CommonNameChg = ouName;

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
        return $"ou={CommonName}";
    }


    public DirectoryAttribute[] GetDirectoryAttributesNew()
    {
        DirectoryAttribute[]     attributes    = Array.Empty<DirectoryAttribute>();
        List<DirectoryAttribute> dirAttributes = new List<DirectoryAttribute>();
        dirAttributes.Add(new DirectoryAttribute("objectClass", ObjectClassName));

        foreach (KeyValuePair<string, AttributeBase> attributeBase in AttributesToUpdate)
        {
            dirAttributes.Add(attributeBase.Value.DA);
        }

        return dirAttributes.ToArray();
    }

    

    public string? CommonNameChg
    {
        get => CommonName;
        set => CommonName = value;
    }

    public string? DescriptionChg
    {
        get => Description;
        set => Description = value;
    }

    // TODO remove this
    public string? NameChg
    {
        get => Name;
        set => Name = value;

    }
}

