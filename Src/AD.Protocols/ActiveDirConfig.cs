namespace SlugEnt.AD.Protocols;

/// <summary>
///     Reads Active Directory Connection info from Configuration
/// </summary>
public class ActiveDirConfig : IActiveDirConfig
{
    /// <summary>
    /// Password used to connect to Active Directory
    /// </summary>
    public string AdPassword { get; set; } = "";

    /// <summary>
    /// User name used to connect to Active Directory
    /// </summary>
    public string AdUser { get; set; } = "";

    /// <summary>
    /// Active Directory Domain to connect to.  This is usually the last 2 parts of the domain name.
    /// For example, if the domain is "mycompany.local", this would be "mycompany.local"
    /// </summary>
    public string Domain { get; set; } = "";

    /// <summary>
    /// First server in the Active Directory domain to connect to
    /// </summary>
    public string Server1Name { get; set; } = "";

    /// <summary>
    /// Second server in the Active Directory domain to connect to
    /// </summary>
    public string Server2Name { get; set; } = "";
    
    /// <summary>
    /// Port to connect to the Active Directory server
    /// </summary>
    public int Port { get; set; } = 389;
    
    /// <summary>
    /// Root Path name for the Active Directory connection.
    /// For production this will typically be blank.
    /// For others, it might be a name that all the other folders are under.
    /// This way multiple hierarchies can exist in same directory
    /// </summary>
    public string RootPath { get; set; } = "";

    public string UserOu { get; set; } = "";
}

public interface IActiveDirConfig
{
}