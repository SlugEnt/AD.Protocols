using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.AD.Protocols.Attributes;

/// <summary>
///     An Integer Attribute.
/// </summary>
public class AttributeByteArray : AttributeBase
{
    /// <summary>
    ///    Constructor for an attribute that stores its data as a byte array
    /// </summary>
    /// <param name="name"></param>
    /// <param name="changeMode"></param>
    public AttributeByteArray(string name,
                              EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(name, changeMode) { }


    /// <summary>
    ///    Returns the byte array value of the Attribute
    /// </summary>
    public override byte[] Value => (byte[])DA[0];


    /// <summary>
    ///   Returns the string value of the Attribute
    /// </summary>
    /// <returns></returns>
    public override string ToString() { return Value.ToString(); }
}

