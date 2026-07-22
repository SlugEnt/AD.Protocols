namespace SlugEnt.AD.Protocols.Attributes;

/// <summary>
///     The operation to be performed on the attribute
/// </summary>
public enum EnumAttributeOperation
{
    /// <summary>Read the attribute</summary>
    Read,

    /// <summary>Add the attribute</summary>
    Add,

    /// <summary>Modify the attribute</summary>
    Modify,

    /// <summary>Delete the attribute</summary>
    Delete
}