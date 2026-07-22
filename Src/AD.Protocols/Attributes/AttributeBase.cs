using System.DirectoryServices.Protocols;
using System.Text;

namespace SlugEnt.AD.Protocols.Attributes;

/// <summary>
///     Base class for manipulating Active Directory attributes 
/// </summary>
public abstract class AttributeBase
{
    /// <summary>
    /// Constructs the Attribute Base
    /// </summary>
    /// <param name="name">Attribute Name</param>
    /// <param name="changeMode">Operation to be performed on the attribute</param>
    public AttributeBase(string name,
                         EnumAttributeOperation changeMode = EnumAttributeOperation.Add)
    {
        DirectoryAttribute = new DirectoryAttribute(name);
        OperationMode      = changeMode;
    }


    /// <summary>
    ///    Returns the DirectoryAttribute object that represents the attribute in Active Directory
    /// </summary>
    public DirectoryAttribute DA => DirectoryAttribute;


    protected DirectoryAttribute DirectoryAttribute { get; set; }


    /// <summary>
    ///     The offical name of the attribute in Active Directory
    /// </summary>
    public string Name
    {
        get => DirectoryAttribute.Name;
        protected set => DirectoryAttribute.Name = value;
    }


    /// <summary>
    ///     The mode of operation to be performed on this attribute.  Only comes into play when its an update or delete
    /// </summary>
    public EnumAttributeOperation OperationMode { get; set; } = EnumAttributeOperation.Read;


    /// <summary>
    ///     Returns the internally stored value of the object.  This may be a single value or an array of values.
    /// </summary>
    public virtual object? Value { get; } = null;


    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        if (Value != null)
            return Value.ToString();
        return "null";
    }
}









#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member



/// <summary>
///     Describes the object
/// </summary>
public class AttrUPN : AttributeStringSingle
{
    public AttrUPN(string value,
                           EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_UPN, changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


/// <summary>
///     The First Name of the User as an Attribute
/// </summary>
public class AttrFirstName : AttributeStringSingle
{
    public AttrFirstName(string value,
                         EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_FIRSTNAME, changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


/// <summary>
///     The First Name of the User as an Attribute
/// </summary>
public class AttrLastName : AttributeStringSingle
{
    public AttrLastName(string value,
                        EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_LASTNAME, changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


/// <summary>
///     The Common Name for the User (this is usually some form of the Full Name)
/// </summary>
public class AttrCommonName : AttributeStringSingle
{
    public AttrCommonName(string value,
                          EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base("cn", changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


public class AttrSamAccount : AttributeStringSingle
{
    public AttrSamAccount(string value,
                          EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_SAM, changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


/// <summary>
///     Object Class Attribute
/// </summary>
public class AttrObjectClass : AttributeStringSingle
{
    public AttrObjectClass(string value,
                           EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base("objectClass", changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


/// <summary>
///     The User Principal Name of the object
/// </summary>
public class AttrUserPrincipalName : AttributeStringSingle
{
    public AttrUserPrincipalName(string value,
                                 EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base("userPrincipalName", changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


/// <summary>
///     The Display Name of the object
/// </summary>
public class AttrDisplayName : AttributeStringSingle
{
    public AttrDisplayName(string value,
                           EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base("displayName", changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


public class AttrEmail : AttributeStringSingle
{
    public AttrEmail(string value,
                     EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base("mail", changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


/// <summary>
///     The User's Title
/// </summary>
public class AttrTitle : AttributeStringSingle
{
    public AttrTitle(string value,
                     EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base("title", changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


/// <summary>
///     Describes the object
/// </summary>
public class AttrDescription : AttributeStringSingle
{
    public AttrDescription(string value,
                           EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base("description", changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


/// <summary>
///     The User's manger in full AD Distinguished Name form
/// </summary>
public class AttrManager : AttributeStringSingle
{
    public AttrManager(string value,
                       EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base("manager", changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


/// <summary>
///     The Department the User is in
/// </summary>
public class AttrDepartment : AttributeStringSingle
{
    public AttrDepartment(string value,
                          EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base("department", changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


/// <summary>
///     The Users Primary Work Number
/// </summary>
public class AttrWorkPhone : AttributeStringSingle
{
    public AttrWorkPhone(string value,
                         EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base("telephoneNumber", changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}



/// <summary>
///     The Users Primary Work Number
/// </summary>
public class AttrPasswordLastSet : AttributeDateTimeOffset
{
    public AttrPasswordLastSet(DateTimeOffset value,
                         EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_PASSWORD_LAST_SET, changeMode)
    {
        DirectoryAttribute.Add(value.ToString());
    }
}


/// <summary>
///     The Distinguished Name of the object.
/// </summary>
public class AttrDistinguishedName : AttributeStringSingle
{
    public AttrDistinguishedName(string value,
                                 EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(ADpCommon.ATN_DISTINGUISHED_NAME, changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


/// <summary>
///     user Account Control which is actually an integer with each bit a separete indicator.
/// </summary>
public class AttrUserAccountControl : AttributeInt
{
    public AttrUserAccountControl(int value,
                                  EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base("userAccountControl", changeMode)
    {
        DirectoryAttribute.Add(value.ToString());
    }
}


/// <summary>
/// The password Attribute
/// </summary>
public class AttrUserPassword : AttributeByteArray
{
    public AttrUserPassword(byte[] value,
                            EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base("unicodePwd", changeMode)
    {

        byte[] ss = Encoding.Unicode.GetBytes("\"" + value + "\"");

        //AttrUserPassword password = new(ss, EnumAttributeOperation.Modify);
        DirectoryAttribute.Add(ss);
    }
}




/// <summary>
///   The Group Type of a Group.
/// </summary>
public class AttrGroupType : AttributeStringSingle
{
    public AttrGroupType(string value,
                            EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base("groupType", changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}


/// <summary>
/// The Name attribute
/// </summary>
public class AttrName : AttributeStringSingle
{
    public AttrName(string value,
                            EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base("name", changeMode)
    {
        DirectoryAttribute.Add(value);
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member    
