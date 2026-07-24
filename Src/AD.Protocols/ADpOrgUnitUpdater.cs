using SlugEnt.AD.Protocols;
using SlugEnt.AD.Protocols.Attributes;
using System.DirectoryServices.Protocols;
using SlugEnt.IS;

namespace AD.Protocols;

public class ADpOrgUnitUpdater
{
    private string? _CommonName;
    private string? _Description;
    private string? _DisplayName;
    private string? _Name;
    
    /// <summary>
    ///     The list of attributes to update
    /// </summary>
    protected Dictionary<string, AttributeBase> AttributesToUpdate = new();


    /// <summary>
    ///     This is the most preferred constructor if you have already read the user from AD as it almost 100% assuredly will
    ///     pull the OU without error.
    /// </summary>
    /// <param name="readOnlyUser"></param>
    public ADpOrgUnitUpdater(ADpReadOnlyOrgUnit orgUnit)
    {
        string? value = orgUnit.DistinguishedName;
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException(nameof(value), "Distinguished Name cannot be null or empty.");
        DistinquishedName = value;
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
    public ADpOrgUnitUpdater(string ouName, ADSPath parentPath) 

    {
        if (string.IsNullOrEmpty(ouName)) throw new ArgumentNullException(nameof(ouName));

        InCreationMode = true;
        
        ParentPath     = parentPath;
        NameChg        = ouName;
        CommonNameChg  = ouName;

        IsNew          = true;
        InCreationMode = false;
    }
    
    
    
    /// <summary>
    /// If true, this will be a creation, not an update.
    /// </summary>
    internal bool IsNew { get; set; }

    
    /// <summary>
    /// The CN or common name of the Org Unit.  This is the name that will be used to create the Org Unit in AD.
    /// </summary>
    public string? CommonNameChg
    {
        get => _CommonName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value), "Common Name cannot be null or empty.");

            _CommonName = value;
            string key = ADpCommon.ATN_CN;
            AttrCommonName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }
    

    /// <summary>
    /// The Name of the organizational unit.  WHEN this object is saved to AD, the Distinguished Name will be updated
    /// to reflect the new name.  This is the name that will be used to create the Org Unit in AD.  In this object the
    /// Distinguished Name will continue to reflect the original name until the object is saved to AD.
    /// </summary>
    public string? NameChg
    {
        get => _Name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value), "Name cannot be null or empty.");
            _Name = value;
            string key = ADpCommon.ATN_NAME;
            AttrName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }



    /// <summary>
    /// Description of the organizational unit.  This is a free form text field that can be used to describe the organizational unit.
    /// </summary>
    public string? DescriptionChg
    {
        get => _Description;
        set
        {
            if (value == null) value = "";
            _Description = value;
            string key = ADpCommon.ATN_DESCRIPTION;
            AttrDescription attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }

    
    /// <summary>
    /// Used when creating the object from an existing AD object.  This prevents the attributes from being added to the modified list during initial setting.
    /// </summary>
    protected bool InCreationMode { get; set; }



    /// <summary>
    ///     Distinquished Name.  This is the unique identifier of the OrgUnit in Active Directory.
    /// Note:  If you change the Name of the OrgUnit, then the Distinguished Name will change WHEN
    /// the OrgUnit is updated in Active Directory.  
    /// </summary>

    public string DistinquishedName
    {
        get;
        set
        {
            field = value;

            // Do not add attribute to modification list if in initial creation mode.
            if (InCreationMode)
                return;

            string                key       = ADpCommon.ATN_DISTINGUISHED_NAME;
            AttrDistinguishedName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    /// <summary>
    /// The parent of this OU.  This is the path to the parent OU in Active Directory.
    /// If this is a new OU, then this must be set before the OU can be created.
    /// </summary>
    public ADSPath ParentPath { get; protected set; }


    /// <summary>
    ///     Performs all final settings to prepare the user's attributes to be updated.  For instance it will look at the
    ///     current disposition of AccountEnable to see how it should be set.
    /// </summary>
    /// <returns></returns>
    /*
    public AttributeBase[] GetAttributes()
    {
        // / If this is a new OU, we need to set the object class to organizationalUnit
        if (IsNew)
        {
            AttrObjectClass objectClass = new AttrObjectClass("organizationalUnit");
            if (!AttributesToUpdate.TryAdd(ADpCommon.ATN_OBJECT_CLASS, objectClass))
            {
                AttributesToUpdate[ADpCommon.ATN_OBJECT_CLASS] = objectClass;
            }
        }
        return AttributesToUpdate.Values.ToArray();
    }
    */

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

    
    /// <summary>
    /// Returns the object class for this object.
    /// </summary>
    public string ObjectClassName
    {
        get { return ADpCommon.OBJ_CLASS_ORGUNIT; }
    }
}


