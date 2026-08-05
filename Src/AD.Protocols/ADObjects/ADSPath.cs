using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using SlugEnt.FluentResults;


namespace AD.Protocols.ADObjects;


/// <summary>
///     Represents an Active Directory LDAP ADSPath object.  Provides the means to
/// </summary>
public class ADSPath
{
    /// <summary>
    /// Constructor for an empty ADSPath object.  This is used internally when creating a new ADSPath from a parent and child path.
    /// </summary>
    private ADSPath() { }


    /// <summary>
    /// Constructs an ADSPath object from a validated RDN path.  This constructor is for internal use only as it does not validate the components.
    /// </summary>
    /// <param name="rdnPath"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <remarks>Internal Use Only.  Because it uses the ADpValidatedRdnPath object directly it is faster to create as it does not perform validation, nor need to build the RDNCompoment list.</remarks>
    private ADSPath(ADpValidatedRdnPath rdnPath)
    {
        if (rdnPath == null || !rdnPath.IsValid)
            throw new ArgumentException("Invalid RDN path provided.", nameof(rdnPath));
        if (rdnPath.HasBeenAssigned)
            throw new ArgumentException("RDN path has already been assigned.  ADpValidateRdnPath objects can only be assigned once.", nameof(rdnPath));

        RdnComponents    = rdnPath.AssignRdnComponentList();
        SpeedOverStorage = false;
    }

    
    /// <summary>
    /// Constructs an ADSPath object from a parent path and a child path.  The child path must start with cn= or CN=.
    /// </summary>
    /// <param name="parentPath"></param>
    /// <param name="childPath"></param>
    public ADSPath(string parentPath,
                   string childPath,
                   bool speedOverStorage = false) : this(parentPath, speedOverStorage)
    {
        SpeedOverStorage = speedOverStorage;
        ADSPath childADSPath = new(childPath, speedOverStorage);
        RdnComponents.InsertRange(0, childADSPath.RdnComponents);
    }

    
    /// <summary>
    /// Constructs an ADSPath object by copying another ADSPath object.
    /// </summary>
    /// <param name="source"></param>
    public ADSPath (ADSPath source)
    {
        CopyRdnComponents(source);
        SpeedOverStorage = source.SpeedOverStorage;
    }

    
    /// <summary>
    /// Constructs an ADSPath object from a string path.  The path is validated to ensure it is a valid Distinguished Name (DN).
    /// </summary>
    /// <param name="path"></param>
    /// <param name="speedOverStorage"></param>
    /// <exception cref="ArgumentException"></exception>
    public ADSPath(string path,
                   bool speedOverStorage = false)
    {

        Result<ADpValidatedRdnPath> result = ADpValidatedRdnPath.IsValidDn(path, false);
        if (result.IsFailed)
            throw new ArgumentException("Invalid Distinguished Name.  Cannot create ADSPath object from this path.", nameof(path));

        SpeedOverStorage = speedOverStorage;
        RdnComponents    = result.Value.AssignRdnComponentList();
    }



    /// <summary>
    /// Constructs an ADSPath object from a list of RDN components.  This constructor is for internal use only as it does not validate the components.
    /// Use the public constructor that takes a string path for validation.
    /// </summary>
    /// <param name="rdnComponents"></param>
    internal ADSPath(List<KeyValuePair<string, string>> rdnComponents,
                     bool speedOverStorage = false)
    {
        RdnComponents    = rdnComponents;
        SpeedOverStorage = speedOverStorage;
        if (SpeedOverStorage)
            Path = string.Join(',', RdnComponents);
    }



    // Pattern to match a single valid RDN key-value pair
    private static readonly Regex RegexRdnPattern = new Regex(
                                                         @"^(CN|OU|DC|O|L|ST|C|UID)=((?:[^,=\\#+""]|\\.)*)$",
                                                         RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex to split on unescaped commas only
    private static readonly Regex RegexSplitPattern = new Regex(
                                                           @"(?<!\\),",
                                                           RegexOptions.Compiled);

    internal List<KeyValuePair<string,string>> RdnComponents { get; } = new List<KeyValuePair<string,string>>();


    
    /// <summary>
    /// Creates a new ADSPath by appending the child path to the front of the current objects Path.
    /// </summary>
    /// <param name="childPath"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public ADSPath CreateChild(string childPath)
    {
        Result<ADpValidatedRdnPath> isValidResult = ADpValidatedRdnPath.IsValidDn(childPath, false);
        
        //Result<List<KeyValuePair<string,string>>> result = IsValidDn(additionalPath,false);
        if (!isValidResult.IsSuccess)
            throw new ArgumentException("Path to append is not a valid RDN.",childPath);

        ADSPath newParent = new ADSPath();
        newParent.CopyRdnComponents(this);
        ADSPath newPath   = new ADSPath(isValidResult.Value);
        
        newParent.Merge(newPath);
        return newParent;
    }


    /// <summary>
    ///  Merges the child ADSPath into the current ADSPath as a child object (ie, at the front of the RDNComponents list).  This is used when creating a new ADSPath from a parent and child path.
    /// </summary>
    /// <param name="child"></param>
    private void Merge(ADSPath child)
    {
        int index = 0;
        foreach (KeyValuePair<string, string> childKV in child.RdnComponents)
        {
            RdnComponents.Insert(index, new KeyValuePair<string, string>(childKV.Key, childKV.Value));
            index++;
        }
    }


    /// <summary>
    /// Copies the passed in source ADSPath object to the current ADSPath object.  This should only be used internally in certain situations
    /// </summary>
    /// <param name="source"></param>
    private void CopyRdnComponents (ADSPath source)
    {
        RdnComponents.Clear();
        foreach (KeyValuePair<string, string> srcKV in source.RdnComponents)
        {
            RdnComponents.Add(new KeyValuePair<string, string>(srcKV.Key, srcKV.Value));
        }
    }   
    
    /// <summary>
    /// If false, the Path Property will be built from the RDN components each time it is requested.  This is slower, but ensures that the Path is always correct.
    /// If False, the Path will be stored and returned as is.  This is faster, but if the RDN components are modified, the Path will not reflect those changes.
    /// </summary>
    protected bool SpeedOverStorage { get; set; } = false;
    
    


    /// <summary>
    ///     The full ADSPath.
    /// </summary>
    /// <remarks>Note:  The Get actually can also set the value for faster access if the SpeedOverStorage is set to true.</remarks>
    public string? Path {
        get
        {
            if (field != null)
                return field;

            StringBuilder sb        = new(500);
            bool          first     = true;
            foreach (KeyValuePair<string, string> rdnComponent in RdnComponents)
            {
                if (first)
                {
                    sb.Append($"{rdnComponent.Key}={rdnComponent.Value}");
                    first = false;
                }
                else 
                    sb.Append($",{rdnComponent.Key}={rdnComponent.Value}");
            }

            if (SpeedOverStorage)
                field = sb.ToString();
            else
                return sb.ToString();
            return field;
        } 
    }

    /// <summary>
    /// Creates a new ADSPath object from a domain name.  The domain name is converted to a distinguished name format.  For example, "some.local" becomes "DC=some,DC=local".
    /// </summary>
    /// <param name="domainName"></param>
    /// <returns></returns>
    public static ADSPath FromDomainName (string domainName)
    {
        // Convert the domain name to a distinguished name format.  For example, "some.local" becomes "DC=some,DC=local".
        string[] parts = domainName.Split('.');
        List<KeyValuePair<string, string>> rdnComponents = new();
        foreach (string part in parts)
        {
            rdnComponents.Add(new KeyValuePair<string, string>("DC", part));
        }
        return new ADSPath(rdnComponents);
    }
    

    /// <summary>
    ///     Returns the full ADSPath of the parent of this Path
    /// </summary>
    /// <returns></returns>
    public Result<ADSPath> GetParent()
    {
        // The parent is the RDNComponents list minus the first RDN component.  So we can just create a new ADSPath object with the remaining components.
        if (RdnComponents.Count <= 1)
            return Result.Fail("This ADSPath has no parent.");
        
        ADSPath parent = new(RdnComponents.GetRange(1, RdnComponents.Count - 1), SpeedOverStorage);
        return Result.Ok(parent);
    }

    
    /// <summary>
    ///     Returns the name portion only of the left most RDN. So in OU=Tampa,OU=Florida,dc=some,dc=local, it would return
    ///     Tampa.
    /// </summary>
    /// <returns></returns>
    public string ShortName()
    {
        if (RdnComponents.Count > 1)
            return RdnComponents[0].Value;
        
        return string.Empty;
    }

    
    /// <summary>
    /// Returns the ADSPath name which is the left most RDN in the path.  So in OU=Tampa,OU=Florida,dc=some,dc=local, it would return OU=Tampa.
    /// </summary>
    /// <returns></returns>
    public string Name()
    {
        if (RdnComponents.Count > 1)
            return $"{RdnComponents[0].Key}={RdnComponents[0].Value}";

        return string.Empty;
    }


    /// <summary>
    ///     Pretty Print!
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Path;
    

    /// <summary>
    /// Tests for equality of 2 ADSPath objects.  They are equal if their Path properties are equal, ignoring case.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(ADSPath left,
                                   ADSPath right)
    {
        if (left is null && right is null)
            return true;
        if (left is null || right is null)
            return false;

        return left.Path.Equals(right.Path, StringComparison.OrdinalIgnoreCase);
    }


    /// <summary>
    /// Tests for inequality of 2 ADSPath objects.  They are not equal if their Path properties are not equal, ignoring case.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(ADSPath left,
                                   ADSPath right)
    {
        return !(left == right);
    }


    /// <summary>
    /// Takes a string and creates and ADSPath from it.
    /// </summary>
    /// <param name="path"></param>
    public static implicit operator ADSPath(string path) => new ADSPath(path);

    /// <summary>
    /// Tests for equality of 2 ADSPath objects.  They are equal if their Path properties are equal, ignoring case.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        if (obj is ADSPath other)
            return string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);

        return false;
    }


    public override int GetHashCode() { return Path != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Path) : 0; }
}