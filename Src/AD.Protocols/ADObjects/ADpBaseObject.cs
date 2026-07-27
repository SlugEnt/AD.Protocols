
using System.DirectoryServices.Protocols;
using SlugEnt.AD.Protocols;
using SlugEnt.AD.Protocols.Attributes;


namespace AD.Protocols.ADObjects;

/// <summary>
/// THis is the base class for all Active Directory objects.  It provides the basic functionality for
/// creating, reading, updating, and deleting Active Directory objects.
/// </summary>
public abstract class ADpBaseObject
{
    
    // This must be defined this way to allow special overridding of the property in some cases.
    private string _distinguishedName = "";
    
    /// <summary>
    /// Internal Constructor used by the ADpOrgUnitProcessor to create an ADpOrgUnit object from an existing AD object.  This
    /// constructor sets the object in creation mode to prevent adding attributes to the AttributesToUpdate dictionary during initial creation.
    /// </summary>
    internal ADpBaseObject() { InCreationMode = true; }


    /// <summary>
    /// Basic constructor for creating a new AD Object, or providing enough to retrieve an object
    /// from Active Directory.
    /// </summary>
    /// <param name="name"></param>
    public ADpBaseObject(string name)
    {
        bool isNew = true;
        Name = name;
    }

    /// <summary>
    /// Returns the object class for this object.
    /// </summary>
    public abstract string ObjectClassName { get; }

    /// <summary>
    /// Used in prompts and error messages to name the object type we are working with,
    /// I.E. "organizational unit", "user", "group", etc.
    /// </summary>
    public abstract string ObjectTypeDescription { get; }

    /// <summary>
    /// If the object is a new object - not coming from Active Directory.
    /// </summary>
    public bool IsNew { get; internal set; }

    /// <summary>
    /// Used to prevent adding attributes to the AttributesToUpdate dictionary when initially creating the object
    /// </summary>
    internal bool InCreationMode { get; set; }


    /// <summary>
    /// The parent AD path of this object.  This is the path to the parent OU in Active Directory.
    /// If this is a new OU, then this must be set before the OU can be created.
    /// </summary>
    public ADSPath ParentPath { get; protected set; }


    /// <summary>
    ///     The list of attributes that have been updated since initial loading and thus
    /// if saved, will be updated in AD.  This is used to track changes to the object and only update the attributes that have changed.
    /// </summary>
    internal Dictionary<string, AttributeBase> AttributesToUpdate = new();


    /// <summary>
    /// For internal debug purposes only.
    /// </summary>
    internal Dictionary<string, AttributeBase> DebugAttributesToUpdate
    {
        get { return AttributesToUpdate; }
    }

    
    
    /// <summary>
    /// Builds the prefix for the distinguished name of the object.  Some objects use a prefix other than cn.
    /// </summary>
    /// <returns></returns>
    internal virtual string BuildDistinguishedNamePrefix()
    {
        return $"CN={CommonName}";
    }

    
    /// <summary>
    /// Builds the value for the Distinguished Name of the object.
    /// </summary>
    internal void BuildDistinguishedName()
    {
        DistinguishedName = string.Join(",", BuildDistinguishedNamePrefix(), ParentPath);
    }


    /// <summary>
    /// Replaces the Distinguished Name of the object.  This should be used in very specific use cases only and not for general purpose
    /// use as it makes assumptions about the state of the object that may not apply in other scenarios.
    /// </summary>
    /// <param name="newDistinguishedName"></param>
    internal void ReplaceDistinguishedName (string newDistinguishedName)
    {
        // We bypass the property setter here because we are replacing the distinguished name typically after an AD move operation or similar.
        _distinguishedName = newDistinguishedName;
    }

    
    /// <summary>
    /// Some objects have complicated values (for instance - user with UserAccountControl) that need to be synchronized
    /// or have other changes made to the core object before saving.  This method is called before saving the object to Active Directory
    /// to allow the derived object to perform any necessary pre-save operations.
    /// </summary>
    /// <remarks>Is Internal because Processors need access to this.</remarks>
    internal virtual void SyncPreSave() { }
    
    
    /// <summary>
    /// Returns the list of attributes to be updated.  This will be a list of DirectoryAttribute objects that can be used to 
    /// </summary>
    /// <returns></returns>
    internal DirectoryAttribute[] GetDirectoryAttributesNew()
    {
        DirectoryAttribute[]     attributes    = Array.Empty<DirectoryAttribute>();
        List<DirectoryAttribute> dirAttributes = new List<DirectoryAttribute>();
        dirAttributes.Add(new DirectoryAttribute("objectClass",ObjectClassName));

        foreach (KeyValuePair<string, AttributeBase> attributeBase in AttributesToUpdate)
        {
            dirAttributes.Add(attributeBase.Value.DA);
        }

        return dirAttributes.ToArray();
    }

    #region "Retrieval Attributes"
    

    #endregion

    #region "Attributes"

    /// <summary>
    /// When orgUnit was last changed
    /// </summary>
    public DateTimeOffset WhenChanged { get; internal set; }

    /// <summary>
    /// When orgUnit was created
    /// </summary>
    public DateTimeOffset WhenCreated { get; internal set; }


    /// <summary>
    ///     Distinquished Name.  This is the unique identifier of the OrgUnit in Active Directory.
    /// Note:  If you change the Name of the OrgUnit, then the Distinguished Name will change WHEN
    /// the OrgUnit is updated in Active Directory.
    /// <remarks>You cannot change this once it has been set.</remarks>
    /// </summary>
    public string DistinguishedName
    {
        get { return _distinguishedName;}
        internal set
        {
            if (field != null)
                throw new InvalidOperationException("Distinguished Name cannot be changed once it has been set.");

            _distinguishedName = value;
            

            // Do not add attribute to modification list if in initial creation mode ie from Active Directory.
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
    /// Description of the organizational unit.  This is a free form text field that can be used to describe the organizational unit.
    /// </summary>
    public string? Description
    {
        get;
        set
        {
            if (value == null)
                value = "";
            field = value;

            // Do not add attribute to modification list if in initial creation mode ie from Active Directory.
            if (InCreationMode)
                return;

            string          key       = ADpCommon.ATN_DESCRIPTION;
            AttrDescription attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }
        
    
    /// <summary>
    /// The name of the object.  This is the same as Common Name and changing one changes the other.
    /// </summary>
    public string Name
    {
        get => CommonName;
        set => CommonName = value;
    }


    /// <summary>
    /// The CN or common name of the Org Unit.  This is the name that will be used to create the Org Unit in AD.
    /// </summary>
    public string? CommonName
    {
        get;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value), "Common Name cannot be null or empty.");

            field = value;

            // Do not add attribute to modification list if in initial creation mode ie from Active Directory.
            if (InCreationMode)
                return;

            string         key       = ADpCommon.ATN_CN;
            AttrCommonName attrValue = new(value, EnumAttributeOperation.Modify);
            if (!AttributesToUpdate.TryAdd(key, attrValue))
            {
                AttributesToUpdate[key] = attrValue;
            }
        }
    }


    #endregion    
}

