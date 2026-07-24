using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;

namespace SlugEnt.AD.Protocols;

/// <summary>
///     This class represents a user read from Active Directory.  This object cannot be used to update or create a user in
///     Active Directory.
///     It is only used to process an AD user object for reporting or other purposes.
///     It may have a minimum, some or all of its properties filled in.  It depends on what the caller requested to be
///     read from AD and how they intend  to use the data.
/// </summary>
public class ADpUserFromAD_RO
{
    /// <summary>
    /// Constructor for the ADpUserFromAD_RO class
    /// </summary>
    internal ADpUserFromAD_RO() { }


    /// <summary>
    /// The CN (Common Name) of the user
    /// </summary>
    public string? AD_CommonName { get; protected set; }

    /// <summary>
    ///     This is the same as the SAM Account
    /// </summary>
    public string? ADAccount { get; protected set; }

    /// <summary>
    /// Number of bad passwords entered by the user.  This is reset when a valid password is entered.
    /// </summary>
    public int BadPasswordCount { get; protected set; }

    /// <summary>
    ///    Date and Time of the last bad password attempt by the user.
    /// </summary>
    public DateTimeOffset BadPasswordDateTime { get; protected set; }

    /// <summary>
    ///    Full name of the department the user belongs too.
    /// </summary>
    public string? DepartmentFullName { get; protected set; }

    /// <summary>
    ///    Short name (Abbreviation) of the department the user belongs too.
    /// </summary>
    public string? DepartmentShortCode { get; protected set; }

    /// <summary>
    ///    Description of the user
    /// </summary>
    public string? Description { get; protected set; }

    /// <summary>
    ///    Display name of the user
    /// </summary>
    public string? DisplayName { get; protected set; }

    /// <summary>
    ///   Distinguished Name of the user.  This is the Full DN path of the user in Active Directory.
    /// </summary>
    public string? DistinguishedName { get; protected set; }

    /// <summary>
    /// Primary email address of the user
    /// </summary>
    public string? Email { get; protected set; }

    /// <summary>
    ///   First name of the user
    /// </summary>
    public string? FirstName { get; protected set; }

    /// <summary>
    /// Returns the user full name, which is a computed value.
    /// </summary>
    public string? FullName
    {
        get
        {
            string fullName = FirstName + " " + LastName;
            return fullName;
        }
    }

    /// <summary>
    ///    True if the "user" is a computer account in Active Directory.
    /// </summary>
    public bool IsComputerAccount { get; protected set; }

    /// <summary>
    ///   True if the user account is disabled in Active Directory.
    /// </summary>
    public bool IsDisabled { get; protected set; }

    /// <summary>
    ///   True if the user account is locked out in Active Directory.
    /// </summary>
    public bool IsLockedOut { get; protected set; }

    /// <summary>
    ///   True if the user account is a normal user account in Active Directory.
    /// </summary>
    public bool IsNormalAccount { get; protected set; }

    /// <summary>
    ///   True if the user account has a password that is expired in Active Directory.
    /// </summary>
    public bool IsPasswordExpired { get; protected set; }

    /// <summary>
    /// True if the password is set to never expire in Active Directory.  This is a flag that can be set on the user account in AD.
    /// </summary>
    public bool IsPasswordSetToNeverExpire { get; protected set; } 

    /// <summary>
    ///   Date and Time of the last logon by the user.
    /// </summary>
    public DateTimeOffset LastLogon { get; protected set; }

    /// <summary>
    ///    Last name of the user
    /// </summary>
    public string? LastName { get; protected set; }

    /// <summary>
    ///   Date and Time the user account was locked out in Active Directory.
    /// </summary>
    public DateTimeOffset LockOutDateTime { get; protected set; }

    /// <summary>
    ///     Amount of time Account has been locked out in nano seconds.
    /// </summary>
    public long LockOutDurationNS { get; protected set; }

    /// <summary>
    ///   Manager of the user (String name)
    /// </summary>
    public string? Manager { get; protected set; }

    /// <summary>
    /// Date Password was last set 
    /// </summary>
    public DateTimeOffset PasswordLastSet { get; protected set; }

    /// <summary>
    /// Date and Time the password will expire.  This is computed based on the password policy in Active Directory.  
    /// </summary>
    public DateTimeOffset PasswordExpiryDateTime { get; protected set; }

    /// <summary>
    /// Internal Phone of the user
    /// </summary>
    public string? Phone { get; protected set; }

    /// <summary>
    /// User's title
    /// </summary>
    public string? Title { get; protected set; }

    /// <summary>
    /// User Principal Name of the user.  This is unique and used to identity the user in SSO.
    /// </summary>
    public string? UPN { get; protected set; }

    /// <summary>
    /// User Account Control value for the user.  This is a bitwise value that contains flags for the user account.
    /// </summary>
    public int UserAccountControl { get; internal set; }

    /// <summary>
    /// Date and Time the user account was last changed in Active Directory.
    /// </summary>
    public DateTimeOffset WhenChanged { get; protected set; }

    /// <summary>
    /// Date and Time the user account was created in Active Directory.
    /// </summary>
    public DateTimeOffset WhenCreated { get; protected set; }


    /// <summary>
    /// All the groups a user is a member of.
    /// </summary>
    public HashSet<string> MemberOf { get; protected set; } = [];


    /// <summary>
    ///     Adds core user attributes:  SAMAccount, UPN, first,last, display and common names.  Also mail,distinguished
    /// </summary>
    /// <param name="attributeList"></param>
    public static void AddBaseAttributes(List<string> attributeList)
    {
        attributeList.Add("sAMAccountName");
        attributeList.Add("userprincipalname");
        attributeList.Add("userAccountControl");
        attributeList.Add("givenName");
        attributeList.Add("sn");
        attributeList.Add("displayName");
        attributeList.Add("mail");
        attributeList.Add("cn");
        attributeList.Add("distinguishedName");
    }


    /// <summary>
    /// Adds groups the user is a member of to the list of attributes to retrieve.
    /// </summary>
    /// <param name="attributeList"></param>
    public static void AddGroupAttributes(List<string> attributeList)
    {
        attributeList.Add("memberOf");
    }


    /// <summary>
    ///     Adds the Informational Attributes to a List, includes:  Title, Manager, Department name and internal phone number
    /// </summary>
    /// <param name="attributeList"></param>
    public static void AddInfoAttributes(List<string> attributeList)
    {
        attributeList.Add("title");
        attributeList.Add("manager");
        attributeList.Add("department");
        attributeList.Add("telephoneNumber");
    }


    /// <summary>
    /// Adds Password related attributes to the list of attributes to retrieve: badPwdCount, badPasswordTime, lockoutTime, lockoutDuration, pwdLastSet
    /// </summary>
    /// <param name="attributeList"></param>
    public static void AddPasswordAttributes(List<string> attributeList)
    {
        attributeList.Add("badPwdCount");
        attributeList.Add("badPasswordTime");
        attributeList.Add("lockoutTime");
        attributeList.Add("lockoutDuration");
        attributeList.Add("pwdLastSet");
        attributeList.Add("msDS-UserPasswordExpiryTimeComputed");
    }


    /// <summary>
    ///    Adds the statistical attributes to retrieve - includes: pwdLastSet, lastLogon, badPasswordTime, whenChanged, whenCreated
    /// </summary>
    /// <param name="attributeList"></param>
    public static void AddStatisticAttributes(List<string> attributeList)
    {
        attributeList.Add("lastLogon");
        attributeList.Add("badPasswordTime");
        attributeList.Add("whenChanged");
        attributeList.Add("whenCreated");
    }


    /// <summary>
    ///     Adds all the attributes to retrieve for a user from Active Directory.  This includes base attributes, group attributes, info attributes, password attributes and statistic attributes.
    /// </summary>
    /// <param name="attributeList"></param>
    public static void AddAllAttributes (List<string> attributeList)
    {
        AddBaseAttributes(attributeList);
        AddGroupAttributes(attributeList);
        AddInfoAttributes(attributeList);
        AddPasswordAttributes(attributeList);
        AddStatisticAttributes(attributeList);
    }


    /// <summary>
    ///     Builds the User Account Control value from the individual flags set on the ADUser object.
    /// </summary>
    /// <returns></returns>
    public int BuildUserAccountControlValue()
    {
        int uac = 0;
        if (IsDisabled)
        {
            uac += 2;
        }

        if (IsLockedOut)
        {
            uac += 10;
        }

        if (IsNormalAccount)
        {
            uac += 512;
        }

        if (IsPasswordExpired)
        {
            uac += 0x800000;
        }

        if (IsComputerAccount)
        {
            uac += 0x1000;
        }

        return uac;
    }


    /// <summary>
    ///     Creates a new ADpUserFromAD_RO object from the attributes provided by the AD Search
    ///     Note:  This method will only create the object if the SAM Account and UPN are found in the attributes.
    /// </summary>
    /// <param name="attributes"></param>
    /// <returns>Result Success or Fail.  </returns>
    public static Result<ADpUserFromAD_RO> CreateUserObj(SearchResultAttributeCollection attributes)
    {
        ADpUserFromAD_RO userFromAdRo = new();

        bool samAccountFound = false;
        bool upnFound        = false;

        foreach (DirectoryAttribute dirObj in attributes.Values)
        {
            int ival;

            switch (dirObj.Name)
            {
                case "sAMAccountName":
                    samAccountFound = true;
                    userFromAdRo.ADAccount  = dirObj[0].ToString();
                    break;
                case "distinguishedName":
                    userFromAdRo.DistinguishedName = dirObj[0].ToString();
                    break;
                case "cn":
                    userFromAdRo.AD_CommonName = dirObj[0].ToString();
                    break;
                case "displayName":
                    userFromAdRo.DisplayName = dirObj[0].ToString();
                    break;
                case "givenName":
                    userFromAdRo.FirstName = dirObj[0].ToString();
                    break;
                case "sn":
                    userFromAdRo.LastName = dirObj[0].ToString();
                    break;
                case "title":
                    userFromAdRo.Title = dirObj[0].ToString();
                    break;
                case "userPrincipalName":
                    upnFound = true;
                    userFromAdRo.UPN = dirObj[0].ToString();
                    break;
                case "department":
                    userFromAdRo.DepartmentFullName = dirObj[0].ToString();
                    break;
                case "mail":
                    userFromAdRo.Email = dirObj[0].ToString();
                    break;
                case "manager":
                    userFromAdRo.Manager = dirObj[0].ToString();
                    break;
                case "telephoneNumber":
                    userFromAdRo.Phone = dirObj[0].ToString();
                    break;
                case "description":
                    userFromAdRo.Description = dirObj[0].ToString();
                    break;
                case "memberOf":
                    for (int i = 0; i < dirObj.Count; i++)
                    {
                        userFromAdRo.MemberOf.Add(dirObj[i].ToString());
                    }
                    break;
                case "userAccountControl":
                    if (!int.TryParse(dirObj[0].ToString(), out ival))
                    {
                        throw new ArgumentException("DA is not a int value - key [" + dirObj.Name + "] value: [" + dirObj[0]! + "]");
                    }

                    userFromAdRo.UserAccountControl = ival;

                    int flags = ival;
                    userFromAdRo.IsDisabled                 =  Convert.ToBoolean(flags & 0x0002);
                    userFromAdRo.IsLockedOut                =  Convert.ToBoolean(flags & 0x0010);
                    userFromAdRo.IsNormalAccount            =  Convert.ToBoolean(flags & 0x0200);
                    userFromAdRo.IsPasswordExpired          =  Convert.ToBoolean(flags & 0x800000);
                    userFromAdRo.IsComputerAccount          =  Convert.ToBoolean(flags & 0x1000);
                    userFromAdRo.IsPasswordSetToNeverExpire =  Convert.ToBoolean(flags & 0x10000);
                    break;

                case "lastLogon":
                    userFromAdRo.LastLogon = ADFunctions.GetDateTime_FromLDAPPropertyLong(dirObj[0].ToString()!);
                    break;
                case "whenChanged":
                    userFromAdRo.WhenChanged = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!);
                    break;
                case "whenCreated":
                    userFromAdRo.WhenCreated = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!);
                    break;
                case "badPwdCount":
                    if (!int.TryParse(dirObj[0].ToString(), out ival))
                    {
                        throw new ArgumentException("DA is not an integer value - key [" + dirObj.Name + "] value: [" + dirObj[0]! + "]");
                    }

                    userFromAdRo.BadPasswordCount = ival;
                    break;
                case "badPasswordTime":
                    userFromAdRo.BadPasswordDateTime = ADFunctions.GetDateTime_FromLDAPPropertyLong(dirObj[0].ToString());
                    break;
                case "lockoutTime":
                    userFromAdRo.LockOutDateTime = ADFunctions.GetDateTime_FromLDAPPropertyLong(dirObj[0].ToString());
                    break;
                case "lockoutDuration":
                    if (!long.TryParse(dirObj[0].ToString(), out long lval))
                    {
                        throw new ArgumentException("DA is not a long value - key [" + dirObj.Name + "] value: [" + dirObj[0] + "]");
                    }

                    userFromAdRo.LockOutDurationNS = lval;
                    break;
                case "pwdLastSet":
                    userFromAdRo.PasswordLastSet = ADFunctions.GetDateTime_FromLDAPPropertyLong(dirObj[0].ToString());
                    break;
                case "msDS-UserPasswordExpiryTimeComputed":
                    // This is a special case for the msDS-UserPasswordExpiryTimeComputed attribute
                    // which is stored as a long value representing ticks.
                    
                    if (!long.TryParse(dirObj[0].ToString(), out long ticks))
                    {
                        throw new ArgumentException("DA is not a long value - key [" + dirObj.Name + "] value: [" + dirObj[0] + "]");
                    }

                    // This is equivalent to Hex: 0x7fffffffffffffff which means it is set to never expire.  We have to set it to something
                    // so we set to maximum date value...
                    if (ticks == 9223372036854775807) userFromAdRo.PasswordExpiryDateTime = DateTimeOffset.MaxValue;
                    else
                        userFromAdRo.PasswordExpiryDateTime = ADFunctions.GetDateTime_FromLDAPPropertyLong(dirObj[0].ToString());
                    //userFromAdRo.PasswordExpiryDateTime = new DateTime(ticks);
                    if (userFromAdRo.PasswordExpiryDateTime < DateTime.Now)
                        userFromAdRo.IsPasswordExpired = true;
                    break;
            }
        }

        if (samAccountFound && upnFound)
        {
            return Result.Ok(userFromAdRo);
        }

        return Result.Fail("SAM Account and/or UPN not found in AD User Object provided attributes.  Both must be returned at a minimum!");
    }


    /// <summary>
    ///    Adds the userAccountControl attribute to the collection of attributes to retrieve.
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="startIndex"></param>
    /// <returns></returns>
    public static int SetAccountInfoAttributes(string[] collection,
                                               int startIndex)
    {
        collection[startIndex++] = "userAccountControl";
        return --startIndex;
    }


    /// <summary>
    ///     The Basic User Attributes we should always retrieve.  SAMAccount, UPN, First and last names, Display name,
    /// internal email address, Common Name and Distinguished name
    /// </summary>
    /// <param name="collection">String Array to populate into</param>
    /// <param name="startIndex">Index at which to start placing new values</param>
    /// <remarks>int: the position of the last entry</remarks>
    public static int SetBaseAttributes(string[] collection,
                                        int startIndex)
    {
        collection[startIndex++] = "sAMAccountName";
        collection[startIndex++] = "userprincipalname";
        collection[startIndex++] = "userAccountControl";
        collection[startIndex++] = "givenName";
        collection[startIndex++] = "sn";
        collection[startIndex++] = "displayName";
        collection[startIndex++] = "mail";
        collection[startIndex++] = "cn";
        collection[startIndex++] = "distinguishedName";

        return --startIndex;
    }

    /// <summary>
    ///    Adds the thumbnailPhoto attribute to the collection of attributes to retrieve.
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="startIndex"></param>
    public static void SetExtraAttributes(string[] collection,
                                          int startIndex)
    {
        collection[startIndex++] = "thumbnailPhoto";
    }


    /// <summary>
    ///     Adds additional informational attributes to be retrieved: Title, manager, department, telephoneNumber
    /// </summary>
    /// <param name="collection">String Array to populate into</param>
    /// <param name="startIndex">Index at which to start placing new values</param>
    /// <remarks>int: the position of the last entry</remarks>
    public static int SetInfoAttributes(string[] collection,
                                        int startIndex)
    {
        collection[startIndex++] = "title";
        collection[startIndex++] = "manager";
        collection[startIndex++] = "department";
        collection[startIndex++] = "telephoneNumber";
        return --startIndex;
    }



    /// <summary>
    ///     Adds additional password related attributes to be retrieved:
    /// </summary>
    /// <param name="collection">String Array to populate into</param>
    /// <param name="startIndex">Index at which to start placing new values</param>
    /// <remarks>int: the position of the last entry</remarks>
    public static int SetPasswordAttributes(string[] collection,
                                            int startIndex)
    {
        collection[startIndex++] = "badPwdCount";
        collection[startIndex++] = "badPasswordTime";
        collection[startIndex++] = "lockoutTime";
        collection[startIndex++] = "lockoutDuration";
        collection[startIndex++] = "pwdLastSet";
        return --startIndex;
    }


    /// <summary>
    ///     Adds additional statistical and time/date attributes to be retrieved:
    /// </summary>
    /// <param name="collection">String Array to populate into</param>
    /// <param name="startIndex">Index at which to start placing new values</param>
    /// <remarks>int: the position of the last entry</remarks>
    public static int SetStatisticAttributes(string[] collection,
                                             int startIndex)
    {
        collection[startIndex++] = "pwdLastSet";
        collection[startIndex++] = "lastLogon";
        collection[startIndex++] = "badPasswordTime";
        collection[startIndex++] = "whenChanged";
        collection[startIndex++] = "whenCreated";
        return --startIndex;
    }


    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        if (string.IsNullOrEmpty(DisplayName))
        {
            return ADAccount + " ( " + UPN + " )";
        }

        return DisplayName;
    }
}