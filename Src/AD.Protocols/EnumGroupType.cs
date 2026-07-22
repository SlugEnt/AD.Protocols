using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.AD.Protocols;

/// <summary>
/// Type of AD Group
/// </summary>
public enum EnumGroupType
{
    /// <summary>Indicates the value has not been set yet.  This is not a valid value</summary>
    NotSpecified = 0,

    /// <summary>Global Distribution Group</summary>
    Global_Distribution = 5,

    /// <summary>Domain Local Distribution Group</summary>
    Domain_Local_Distribution = 10,

    /// <summary>Universal Distribution Group</summary>
    Universal_Distribution = 15,

    /// <summary>Global Security Group</summary>
    Global_Security = 20,

    /// <summary>Domain Local Security Group</summary>
    Domain_Local_Security = 25,

    /// <summary>Universal Security Group</summary>
    Universal_Security = 30         
}
