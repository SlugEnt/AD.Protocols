using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.AD.Protocols.Attributes;



/// <summary>
///     Provides for a TimeSpan Attribute.
/// </summary>
public class AttributeTimeSpan : AttributeBase
{
    /// <summary>
    /// Constructs an Integer Attribute
    /// </summary>
    /// <param name="name">Attribute name</param>
    /// <param name="changeMode">Operation to be performed</param>
    public AttributeTimeSpan(string name,
                             EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(name, changeMode) { }


    /// <summary>
    ///    Returns the string value of the Attribute
    /// </summary>
    public override string Value => (string)DA[0];


    /// <summary>
    ///    Returns the long value of the Attribute
    /// </summary>
    public long ValueAsLong
    {
        get
        {
            string v = (string)DA[0];
            return long.Parse(v);
        }
    }



    /// <summary>
    /// Returns the string value of the Attribute
    /// </summary>
    /// <returns></returns>
    public override string ToString() { return Value; }
}


