using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.AD.Protocols.Attributes;

public class AttributeList : AttributeBase
{
    /// <summary>
    /// Construts a List Attribute
    /// </summary>
    /// <param name="name">Attribute name</param>
    /// <param name="changeMode">Operation to be performed</param>
    public AttributeList(string name,
                             EnumAttributeOperation changeMode = EnumAttributeOperation.Add) : base(name, changeMode) { }


    /// <summary>
    ///    Returns the string value of the Attribute
    /// </summary>
    public override string Value => (string)DA[0];


    public List<string> Values
    {
        get
        {
            var values = new List<string>();
            foreach (var item in DA)
            {
                if (item is string str)
                {
                    values.Add(str);
                }
            }
            return values;
        }
    }


    /// <summary>
    /// Returns the string value of the Attribute
    /// </summary>
    /// <returns></returns>
    public override string ToString() { return Value; }
}