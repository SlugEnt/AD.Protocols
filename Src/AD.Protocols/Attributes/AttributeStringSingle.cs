namespace SlugEnt.AD.Protocols.Attributes;


/// <summary>
///     Provides for an attribute that is based upon a single string value
/// </summary>
public class AttributeStringSingle : AttributeBase
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="name"></param>
    public AttributeStringSingle(string name,
                                 EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(name, changeMode) { }


    /// <summary>
    ///     Returns the string value of the Attribute
    /// </summary>
    public override string Value => (string)DA[0];


    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() { return Value; }
}


