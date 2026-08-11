using System.Text.RegularExpressions;
using SlugEnt.FluentResults;

namespace AD.Protocols.ADObjects;

/// <summary>
/// Clas used to valid an LDAP "Path" or Distinguished Name (DN) and return the individual RDN components if valid.  This is used to validate and parse DN's for AD objects.
/// <remarks>The RDNComponents list can be assigned to another object.  But only once.</remarks>
/// </summary>
public class ADpValidatedRdnPath
{
    /// <summary>
    /// Returns true if the RDN path is valid.  If false, the RDN path is invalid and the RdnComponents list will be empty.
    /// </summary>
    public bool IsValid { get; private set; }


    /// <summary>
    /// Returns true if the RDN component list has been assigned to a caller.  Once assigned, the internal list is cleared and cannot be accessed again.
    /// </summary>
    public bool HasBeenAssigned { get; private set; } = false;

    
    /// <summary>
    /// Assigns the RDN component list to the caller.  It can only be assigned once.  Once assigned the internal list is cleared and cannot be accessed again.
    /// </summary>
    /// <returns>The list of RDN components.</returns>
    /// <exception cref="ApplicationException">Thrown if the RDN component list has already been assigned.</exception>
    public List<KeyValuePair<string, string>> AssignRdnComponentList ()
    {
        if (HasBeenAssigned)
            throw new ApplicationException($"The RDN component list can only be assigned once, this one has already been previously assigned.");

        HasBeenAssigned = true;
        List<KeyValuePair<string, string>> temp = RdnComponents;
        RdnComponents = null;
        
        return temp;
    }


    /// <summary>
    /// The list of RDN components that make up the validated DN.  This is a list of key-value pairs where the key is the RDN type (e.g., CN, OU, DC) and the value is the corresponding value for that RDN.
    /// </summary>
    private List<KeyValuePair<string,string>> RdnComponents { get; set; } = new List<KeyValuePair<string, string>>();

    
    /// <summary>
    /// Empty Constructor.
    /// </summary>
    private ADpValidatedRdnPath () {}

    
    /// <summary>
    /// Determines if the passed string is a valid Distinguished Name (DN) and returns the individual RDN components if valid.
    /// </summary>
    /// <param name="input">The distinguished name (DN) string to validate.</param>
    /// <param name="validateOnly">If true, only validates the DN without returning the components.</param>
    /// <returns>Result.Ok if the input is a valid DN.  Otherwise returns Result.Fail</returns>
    public static Result<ADpValidatedRdnPath> IsValidDn(string input, bool validateOnly = true)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result.Fail("Input is null or whitespace.");

        // Split the DN into individual RDN components
        string[] parts = RegexSplitPattern.Split(input);

        if (parts.Length == 0)
            return Result.Fail("No RDN components found.");

        ADpValidatedRdnPath? x = null;
        if (!validateOnly)
            x = new();
        

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

            string[] keyValue = trimmedPart.Split(new char[]
                                                  {
                                                      '='
                                                  },
                                                  2);

            // We need to extract the key and value from the RDN component.
            // The split is guaranteed to have at least two parts because RdnPattern ensures it.

            // Add the raw RDN component to our list for later use.
            x!.RdnComponents!.Add(new KeyValuePair<string, string>(keyValue[0].ToUpper(), keyValue[1]));
        }

        // If we made it here, it is valid.
        if (x == null)
            return Result.Ok();
        else
        {
            x.IsValid = true;
            return Result.Ok(x);
        }
    }


    // Pattern to match a single valid RDN key-value pair
    private static readonly Regex RegexRdnPattern = new Regex(
                                                              @"^(CN|OU|DC|O|L|ST|C|UID)=((?:[^,=\\#+""]|\\.)*)$",
                                                              RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex to split on unescaped commas only
    private static readonly Regex RegexSplitPattern = new Regex(
                                                                @"(?<!\\),",
                                                                RegexOptions.Compiled);

}

