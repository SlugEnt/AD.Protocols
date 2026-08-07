using System.Text.RegularExpressions;

namespace AD.Protocols;
public class ADSItemContainer
{
    public static HashSet<string> ValidIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CN", "OU", "DC", "O", "L", "ST", "C", "UID", "SN", "GIVENNAME", "MAIL"
    };
    public string Id
    {
        get;
        protected set
        {
            field = value;
        }
    }

    public string Value { get; protected set; }
    
    public ADSItemContainer (string id, string value)
    {
        Id = id;
        Value = value;
    }


    /// <summary>Returns a string that represents the current container in LDAP Format.</summary>
    public override string ToString() => $"{Id}={Value}";
}


public class DnValidator
{
    // Pattern to match a single valid RDN key-value pair
    private static readonly Regex RdnPattern = new Regex(
                                                         @"^(CN|OU|DC|O|L|ST|C|UID)=((?:[^,=\\#+""]|\\.)*)$",
                                                         RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex to split on unescaped commas only
    private static readonly Regex SplitPattern = new Regex(
                                                           @"(?<!\\),",
                                                           RegexOptions.Compiled);


    public static bool IsValidDn(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // Split the DN into individual RDN components
        string[] parts = SplitPattern.Split(input);

        if (parts.Length == 0)
            return false;

        foreach (string part in parts)
        {
            string trimmedPart = part.Trim();

            // Each individual component must be a valid RDN
            if (!RdnPattern.IsMatch(trimmedPart))
            {
                return false;
            }
        }

        return true;
    }
}

