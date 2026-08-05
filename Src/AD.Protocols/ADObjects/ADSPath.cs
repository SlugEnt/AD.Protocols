using System.Text;
using System.Text.RegularExpressions;
using SlugEnt.FluentResults;


namespace AD.Protocols.ADObjects;


/// <summary>
///     Represents an Active Directory LDAP ADSPath object.  Provides the means to
/// </summary>
public class ADSPath
{
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
    /// Determines if the passed string is a valid Distinguished Name (DN) and returns the individual RDN components if valid.
    /// </summary>
    /// <param name="input">The distinguished name (DN) string to validate.</param>
    /// <param name="validateOnly">If true, only validates the DN without returning the components.</param>
    /// <returns>Result.Ok if the input is a valid DN.  Otherwise returns Result.Fail</returns>
    public static Result<List<KeyValuePair<string,string>>> IsValidDn(string input, bool validateOnly = true)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result.Fail("Input is null or whitespace.");

        // Split the DN into individual RDN components
        string[] parts = RegexSplitPattern.Split(input);

        if (parts.Length == 0)
            return Result.Fail("No RDN components found.");

        List<KeyValuePair<string, string>>? components = null;
        if (!validateOnly)
            components = new List<KeyValuePair<string,string>>();
        
        
        foreach (string part in parts)
        {
            string trimmedPart = part.Trim();

            // Each individual component must be a valid RDN
            if (!RegexRdnPattern.IsMatch(trimmedPart))
            {
                return Result.Fail($"Invalid RDN component: {trimmedPart}");
            }

            if (validateOnly)
                continue;
            
            string[] keyValue = trimmedPart.Split(new char[] { '=' }, 2);
            
            // We need to extract the key and value from the RDN component.
            // The split is guaranteed to have at least two parts because RdnPattern ensures it.

            // Add the raw RDN component to our list for later use.
            components!.Add(new KeyValuePair<string, string>(keyValue[0].ToUpper(), keyValue[1]));
        }

        return Result.Ok(components);
    }


    /// <summary>
    /// Joins 2 lists of RDN components into a new ADSPath object.  The child components are prepended to the parent components.
    /// </summary>
    /// <param name="child"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    private ADSPath JoinPaths(List<KeyValuePair<string, string>> child,
                               List<KeyValuePair<string, string>> parent)
    {
        ADSPath x = new ADSPath(parent);
        x.AppendChild(child);
        return x;
    }


    /// <summary>
    /// Appends the child path to the front of the current path.  This is used when creating a new ADSPath from a parent and child path.
    /// This should only be used internally.
    /// </summary>
    /// <param name="child"></param>
    internal void AppendChild(List<KeyValuePair<string, string>> child)
    {
        RdnComponents.InsertRange(0,child);
    }
    
    
    /// <summary>
    /// Creates a new ADSPath by appending the additional path to the current objects path.
    /// </summary>
    /// <param name="additionalPath"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public ADSPath AppendPaths(string additionalPath)
    {
        Result<List<KeyValuePair<string,string>>> result = IsValidDn(additionalPath,false);
        if (!result.IsSuccess)
            throw new ArgumentException("Path to append is not a valid RDN.",additionalPath);

        return JoinPaths(result.Value, RdnComponents);

    }

    /// <summary>
    /// If false, the Path Property will be built from the RDN components each time it is requested.  This is slower, but ensures that the Path is always correct.
    /// If False, the Path will be stored and returned as is.  This is faster, but if the RDN components are modified, the Path will not reflect those changes.
    /// </summary>
    protected bool SpeedOverStorage { get; set; } = false;
    
    
    /// <summary>
    /// Constructs an ADSPath object from a list of RDN components.  This constructor is for internal use only as it does not validate the components.
    /// Use the public constructor that takes a string path for validation.
    /// </summary>
    /// <param name="rdnComponents"></param>
    internal ADSPath(List<KeyValuePair<string,string>> rdnComponents, bool speedOverStorage = false)
    {
        RdnComponents = rdnComponents;
        SpeedOverStorage = speedOverStorage;
        if (SpeedOverStorage) 
            Path = string.Join(',', RdnComponents);
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
        Result<List<KeyValuePair<string,string>>> result = IsValidDn(path,false);
        if (result.IsFailed)
            throw new ArgumentException("Invalid Distinguished Name.  Cannot create ADSPath object from this path.", nameof(path));

        SpeedOverStorage = speedOverStorage;
        RdnComponents    = result.Value;
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
        AppendChild(childADSPath.RdnComponents);
    }

    /// <summary>
    ///     The distinguished name part of this ADSPath
    /// </summary>
    public string DN { get; private set; }


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
    ///     Builds a new ADSPath child container that has a parent of the current container.  The CN will be dropped of the
    ///     current path name.
    ///     <para>
    ///         Example:  Current Path = LDAP://ou=office,dc=some,dc=local   New Child LDAP://ou=New
    ///         Jersey,ou=office,dc=some,dc=local
    ///     </para>
    /// </summary>
    /// <param name="childPart">The child container of the current object.  In format:  OU=child or OU=grandchild,OU=child.
    /// <para>>It can also be in CN=child,CN=parent format, but the IsOuPath must be false.</para>
    /// </param>
    /// <param name="isOuPath">Indicates whether the path is an OU path.</param>
    /// <returns></returns>
    public ADSPath BuildChildADSPath(string childPart)
    {
        return AppendPaths(childPart);  
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