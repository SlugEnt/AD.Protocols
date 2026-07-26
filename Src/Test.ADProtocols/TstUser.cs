using Bogus;
using SlugEnt;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;
using AD.Protocols.ADObjects;
using SlugEnt.AD.Protocols;
using SlugEnt.AD.Protocols.Attributes;

namespace UT;

/// <summary>
///     Createa a basic Test User with Randomized Data that can be used for testing with
/// </summary>
public class TstUserBasic
{
    public ADpUserEditable User { get; protected set; }


    /// <summary>
    ///     Creates a new User, Assigns the UPN and SAM Account automatically.
    /// </summary>
    /// <param name="description"></param>
    public TstUserBasic(string description,
                        ADSPath parentOU,
                        Faker faker)
    {
        Person person     = new();
        string commonName = person.FullName;
        string userDNPath = "cn=" + commonName + "," + parentOU.Path;


        User = new(person.FullName)
        {
            SAMAccount            = "TstUser" + Guid.NewGuid().ToString().Substring(0, 8),
            UPN                   = person.Email,
            DescriptionChg        = description,
            DistinquishedName     = userDNPath,
            CommonNameChg         = commonName,
            FirstNameChg          = person.FirstName,
            LastNameChg           = person.LastName,
            DisplayNameChg        = person.FullName,
            EmailChg              = person.Email,
            TitleChg              = faker.Name.JobTitle(),
            DepartmentFullNameChg = faker.Commerce.Department(),
            PhoneChg              = faker.Phone.PhoneNumber(),
            PasswordChg           = faker.Random.AlphaNumeric(20) + "#$59assFG",
        };

        // Make array large enough to retrieve mosst of the attributes
        AttributesToRetrieve = new string[100];

        // Default Search Filter
        SearchFilter = "(sAMAccountName=" + User.SAMAccount + ")";
    }



    /// <summary>
    ///     The AttributesToRetreive Index
    /// </summary>
    protected int ATRIndex
    {
        get;
        set;
    }



    /// <summary>
    ///     The Attributes to retrieve from Active Directory.  If this is not set, then only the basic attributes are set.
    /// </summary>
    public string[] AttributesToRetrieve
    {
        get;
    }

    public AttrCommonName? CommonName { get; set; }
    public AttrDepartment? DepartmentFullName { get; set; }
    public AttrDescription? Description { get; set; }
    public AttrDisplayName? DisplayName { get; set; }


    public AttrDistinguishedName DistinguishedName { get; set; }
    public AttrEmail? Email { get; set; }
    public AttrFirstName? FirstName { get; set; }
    public AttrLastName? LastName { get; set; }
    public AttrObjectClass? ObjectClass { get; set; }
    public AttrUserPassword? Password { get; set; }
    public AttrSamAccount? SamAccount { get; set; }


    public string SearchFilter
    {
        get;
        set;
    }

    public AttrTitle? Title { get; set; }

    public AttrUserAccountControl? UAC { get; set; }
    public AttrUserPrincipalName? UPN { get; set; }
    public AttrWorkPhone? WorkPhone { get; set; }


    /// <summary>
    ///     Adds an Attribute to the AttributesToRetrieve Array
    /// </summary>
    /// <param name="attributeName"></param>
    public void AddAttributeToRetrieve(string attributeName)
    {
        AttributesToRetrieve[ATRIndex] = attributeName;
        ATRIndex++;
    }


    /// <summary>
    ///     Creates this user in Active Directory.
    /// </summary>
    /// <param name="adEngine"></param>
    /// <returns></returns>
    public Result CreateUser(ActiveDirectoryConnector adEngine)
    {
        Result result = adEngine.UserAddNew(User);
        Assert.That(result.IsSuccess,
                    Is.True,
                    "TstUser:CreateUser: CU-100:  Failed to create user in Active Directory - Tried to create DN: " + User.DistinquishedName + " AppError Msg: " +
                    result.ToStringErrorOnly());
        return result;

        return result;
    }



    public Result<SearchResponse> ExecuteSearch(ActiveDirectoryConnector adEngine,
                                                ADSPath pathToStartAt,
                                                SearchScope searchScope = SearchScope.OneLevel)
    {
        // If no attributes specified to retrieve then retrieve most common ones.
        if (AttributesToRetrieve.Length == 0)
        {
            ATRIndex = ADpUserFromAD_RO.SetBaseAttributes(AttributesToRetrieve, ATRIndex);
            ATRIndex = ADpUserFromAD_RO.SetInfoAttributes(AttributesToRetrieve, ++ATRIndex);
            ATRIndex = ADpUserFromAD_RO.SetPasswordAttributes(AttributesToRetrieve, ++ATRIndex);
            ATRIndex = ADpUserFromAD_RO.SetStatisticAttributes(AttributesToRetrieve, ++ATRIndex);
            ATRIndex = ADpUserFromAD_RO.SetAccountInfoAttributes(AttributesToRetrieve, ++ATRIndex);
        }

        Result<List<SearchResponse>> searchResult = adEngine.SearchDirectory(pathToStartAt.Path,
                                                                             SearchFilter,
                                                                             searchScope,
                                                                             AttributesToRetrieve);

        Assert.That(searchResult.IsSuccess, Is.True, "TstUser:ES-100:  Failed to successfully search for user.  AppError: " + searchResult.ToStringWithLineFeeds());
        Assert.That(searchResult.Value.Count, Is.EqualTo(1), "TstUser:ES-110:  Expected only a single search response from the search");
        Assert.That(searchResult.Value[0].Entries.Count, Is.EqualTo(1), "TstUser:ES-120:  Expected only a single match on the search criteria");
        return Result.Ok(searchResult.Value[0]);
    }



    /// <summary>
    ///     Compares this objects properties to the ADpUserFromAD_RO object passed in.  If they do not match then an Assert is
    ///     thrown.
    /// </summary>
    /// <param name="userFromAdRo"></param>
    /// <returns></returns>
    public Result ValidateUserDefault(ADpUserFromAD_RO userFromAdRo,
                                      bool onlyTheBasics = false)
    {
        string dn = userFromAdRo.DistinguishedName;
        Assert.That(userFromAdRo.AD_CommonName, Is.EqualTo(User.CommonNameChg), "TstUser:VU-100:  Common Name does not match | User: " + dn);
        Assert.That(userFromAdRo.DisplayName, Is.EqualTo(User.DisplayNameChg), "TstUser:VU-110:  Display Name does not match | User: " + dn);
        Assert.That(userFromAdRo.FirstName, Is.EqualTo(User.FirstNameChg), "TstUser:VU-120:  First Name does not match | User: " + dn);
        Assert.That(userFromAdRo.LastName, Is.EqualTo(User.LastNameChg), "TstUser:VU-130:  Last Name does not match | User: " + dn);
        Assert.That(userFromAdRo.Title, Is.EqualTo(User.TitleChg), "TstUser:VU-140:  Title does not match | User: " + dn);
        Assert.That(userFromAdRo.UPN, Is.EqualTo(User.UPN), "TstUser:VU-150:  UPN does not match | User: " + dn);
        Assert.That(userFromAdRo.DepartmentFullName, Is.EqualTo(User.DepartmentFullNameChg), "TstUser:VU-160:  Department does not match | User: " + dn);
        Assert.That(userFromAdRo.Email, Is.EqualTo(User.EmailChg), "TstUser:VU-170:  Email does not match | User: " + dn);
        Assert.That(userFromAdRo.DistinguishedName.ToLower(),
                    Is.EqualTo(User.DistinquishedName.ToLower()),
                    "TstUser:VU-180:  Distinguished name does not have a match | User: " + dn);
        Assert.That(userFromAdRo.IsNormalAccount, Is.True, "TstUser:VU-190:  Account should have been a normal account. | User: " + dn);

        if (onlyTheBasics)
        {
            return Result.Ok();
        }


        DateTimeOffset adStartDate = DateTimeOffset.Now;
        DateTimeOffset adlastLoginDate =  new(1601,
                                         1,
                                         1,
                                         0,
                                         0,
                                         0,
                                         new TimeSpan(0));
        
        DateTimeOffset adLockDate = new(1,
                                           1,
                                           1,
                                           0,
                                           0,
                                           0,
                                           new TimeSpan(0));
        

        Assert.That(userFromAdRo.WhenCreated,
                    Is.GreaterThan(DateTimeOffset.Now.AddMinutes(-5)),
                    "TstUser:VU-300  DateTime of when the AD object was created is outside the alllowed window. | User: " + dn);
        Assert.That(userFromAdRo.LastLogon,
                    Is.EqualTo(adlastLoginDate),
                    "TstUser:VU-310  DateTime of last login should have been minimum date.  It was not.. | User: " + dn);

        // By default new accounts are disabled.
        Assert.That(userFromAdRo.IsDisabled, Is.True, "TstUser:VU-320:  Account should have been disabled by default. | User: " + dn);
        Assert.That(userFromAdRo.IsLockedOut, Is.False, "TstUser:VU-340:  Account should not have been locked. | User: " + dn);
        Assert.That(userFromAdRo.IsPasswordExpired, Is.False, "TstUser:VU-350:  Password should not have been expired. | User: " + dn);
        Assert.That(userFromAdRo.PasswordLastSet, Is.GreaterThanOrEqualTo(adStartDate.AddHours(-1)), "TstUser:VU-360:  Password should have been greater than 1 hr ago.| User: " + dn);
        Assert.That(userFromAdRo.PasswordLastSet,
                    Is.LessThanOrEqualTo(adStartDate.AddHours(1)),
                    "TstUser:VU-360:  Password should have been less than current date time plus 1 hr.| User: " + dn);
        Assert.That(userFromAdRo.BadPasswordCount, Is.EqualTo(0), "TstUser:VU-370:  Bad Password count should have been set to 0. | User: " + dn);
        Assert.That(userFromAdRo.LockOutDateTime, Is.EqualTo(adLockDate), "TstUser:VU-380:  Password Lockout Date Time should have been set to AD Start Date | User: " + dn);
        return Result.Ok();
    }
}