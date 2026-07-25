
using System.DirectoryServices.Protocols;
using SlugEnt.AD.Protocols;
using SlugEnt.AD.Protocols.Attributes;
using SlugEnt.IS;

namespace AD.Protocols.ADObjects;

/// <summary>
/// THis is the base class for all Active Directory objects.  It provides the basic functionality for
/// creating, reading, updating, and deleting Active Directory objects.
/// </summary>
public abstract class ADpBaseObject
{
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
    public bool IsNew { get; protected set; }

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
    protected Dictionary<string, AttributeBase> AttributesToUpdate = new();


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
        
    /// <summary>
    /// Method that derived classes must implement to set default attributes to be retrieved from AD if none are specifically specified.
    /// </summary>
    /// <param name="attributeRetrieverMgr"></param>
    //internal abstract void AddDefaultRetrievalAttributes(AttributeRetrieverMgr attributeRetrieverMgr);


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
        get;
        internal set
        {
            if (field != null)
                throw new InvalidOperationException("Distinguished Name cannot be changed once it has been set.");

            field = value;

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

