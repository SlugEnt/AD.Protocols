namespace SlugEnt.AD.Protocols.Attributes;


/// <summary>
///     Provides for an Integer Attribute.
/// </summary>
public class AttributeInt : AttributeBase
{
    /// <summary>
    /// Construts an Integer Attribute
    /// </summary>
    /// <param name="name">Attribute name</param>
    /// <param name="changeMode">Operation to be performed</param>
    public AttributeInt(string name,
                        EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(name, changeMode) { }


    /// <summary>
    ///    Returns the string value of the Attribute
    /// </summary>
    public override string Value => (string)DA[0];


    /// <summary>
    ///    Returns the integer value of the Attribute
    /// </summary>
    public int ValueAsInteger
    {
        get
        {
            string v = (string)DA[0];
            return int.Parse(v);
        }
    }


    /// <summary>
    /// Returns the string value of the Attribute
    /// </summary>
    /// <returns></returns>
    public override string ToString() { return Value; }
}
