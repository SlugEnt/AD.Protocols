using System.Text.RegularExpressions;
using SlugEnt.FluentResults;

namespace AD.Protocols.ADObjects;
public class ADpValidatedRdnPath
{
    public bool IsValid { get; private set; }

    public bool HasBeenAssigned { get; private set; } = false;

    public List<KeyValuePair<string, string>> AssignRdnComponentList ()
    {
        if (HasBeenAssigned)
            throw new ApplicationException($"The RDN component list can only be assigned once, this one has already been previously assigned.");

        HasBeenAssigned = true;
        List<KeyValuePair<string, string>> temp = RdnComponents;
        RdnComponents = null;
        
        return temp;
    }

    private List<KeyValuePair<string,string>> RdnComponents { get; set; } = new List<KeyValuePair<string, string>>();

    
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

