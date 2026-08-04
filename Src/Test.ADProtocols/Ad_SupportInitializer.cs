using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using AD.Protocols.ADObjects;
using AD.Protocols.ADObjects.Objects;
using Bogus;
using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
using UT.SupportObjects;

namespace UT.CustomSupportObjects;


/// <summary>
/// This is a class used at the very beginning of Active Directory testing to initialize the ADLDAPEngine and other objects needed for testing.
/// Also provides methods to create random OUs for testing.
/// </summary>
public class Ad_SupportInitializer
{
    private static Faker? _faker                   = new();
    public const   string MASTER_AD_UNIT_TEST_ROOT = "zUnitTesting";

    public static Ad_SupportInitializer GetInitializer(bool isGroup = false)
    {
        Ad_SupportInitializer initializer = new();
        return initializer;
    }


    private bool _isGroup = false;
    
    private Ad_SupportInitializer(bool isGroup = false)
    {
        ActiveDirConfig = new ActiveDirConfig()
        {
            AdPassword  = "T#sting2026",
            AdUser      = "UTAdmin",
            Domain      = "ycy4y.local",
            Server1Name = "ycdc1",
            Server2Name = "",
            Port        = 636,
            RootPath    = "",
            UserOu      = ""
        };

        _isGroup = isGroup;
    }

    /// <summary>
    /// Completes Setup of the AD Connector
    /// </summary>
    /// <returns></returns>
    public bool Initialize()
    {
        ADConnector       = new(null);
        ADConnector.Initialize(ActiveDirConfig);
        Assert.That(ADConnector.IsConnected, Is.True, "[Ad_SupportInitializer_010");

        UnitTestRoot   = ADConnector.DomainRoot.NewChildADSPath("ou=" + MASTER_AD_UNIT_TEST_ROOT);
        if (!_isGroup)
            UnitTestParent = UnitTestRoot.NewChildADSPath("ou=" + HelperMethods.UT_BASEOU_NAME);
        else
            UnitTestParent = UnitTestRoot.NewChildADSPath("ou=" + HelperMethods.OU_UTGROUP);

        return true;
    }


    //
    
    public ActiveDirConfig ActiveDirConfig { get; private set; }


    public ActiveDirectoryConnector  ADConnector { get; private set; }


    /// <summary>
    ///     Returns the faker instance
    /// </summary>
    public Faker Faker => _faker!;


    /// <summary>
    /// The actual folder that all unit tests are created under
    /// </summary>
    public ADSPath UnitTestParent { get; private set; }

    /// <summary>
    /// This is the root folder for all unit tests.  You should not be needing it for any tests.
    /// </summary>
    public ADSPath UnitTestRoot { get; private set; }

    /*
    public Result<ADpReadOnlyOrgUnit> CreateRandomOuReturnReadOnlyOu(ADpReadOnlyOrgUnit parentOrgUnit)
    {
        Result addResult;
        string parentPath = parentOrgUnit != null ? parentOrgUnit.
        if (parentOrgUnit == null)
    }  
    */

    /// <summary>
    ///     Helper Function to create a random OU from the parent path
    /// </summary>
    /// <param name="parentPath">If Null it will use the UnitTestParent property for its value.</param>
    /// <param name="sm"></param>
    /// <returns></returns>
    [Obsolete]
    public Result<ADSPath> CreateRandomOu(ADSPath parentPath = null)
    {
        Result addResult;
        if (parentPath == null)
            parentPath = UnitTestParent;

        try
        {
            while (true)
            {
                string newOuName = Faker.Random.Word();
                
                // Since word is really words, we need to remove bogus characters
                newOuName = newOuName.Replace("&", string.Empty);

                // Now  add OU to LDAP
                ADpOrgUnitProcessor ouProcessor = new ADpOrgUnitProcessor(ADConnector.LdapConnection);
                Result<ADpOrgUnit> result = ouProcessor.AddNew(newOuName, parentPath);
                if (result.IsSuccess)
                {
                    return Result.Ok(parentPath.NewChildADSPath("ou=" + newOuName));
                }

                if (result.IsFailed)
                {
                    if (result.Errors[0].Message == ActiveDirectoryConnector.EXISTS)
                    {
                        // Try again...
                        continue;
                    }
                    else
                        return Result.Fail(new Error("Failed to create the random OU at path: " + parentPath.Path + " for reason " + result.ToStringWithLineFeeds()));
                }
            }
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError(e));
        }
    }


    /// <summary>
    ///     Helper Function to create a random OU from the parent path.
    /// </summary>
    /// <param name="parentPath">It throws if it errors
    /// <param name="sm"></param>
    /// <returns></returns>
    public ADpOrgUnit CreateRandomOuNew(ADSPath parentPath = null)
    {
        Result addResult;
        if (parentPath == null)
            parentPath = UnitTestParent;

        while (true)
        {
            try
            {
                string newOuName = Faker.Random.Word();

                // Since word is really words, we need to remove bogus characters
                newOuName = newOuName.Replace("&", string.Empty);

                // Now  add OU to LDAP
                ADpOrgUnitProcessor ouProcessor = new ADpOrgUnitProcessor(ADConnector.LdapConnection);
                Result<ADpOrgUnit>  result      = ouProcessor.AddNew(newOuName, parentPath);
                if (result.IsSuccess)
                    return result.Value;

                if (result.IsFailed)
                {
                    if (result.Errors[0].Message.Contains("The object exists"))
                    {
                        // Try again...
                        continue;
                    }
                    else
                        throw new Exception("[CreateRandomOuNew_100]  Failed to create the random OU at path: " + parentPath.Path + " for reason " +
                                            result.ToStringWithLineFeeds());
                }
            }

            catch (Exception e)
            {   // Every now and then the faker picks a bad name.
                if (!e.Message.Contains("(BAD_NAME)"))
                    throw new Exception("[CreateRandomOuNew_200]  Failed to create the random OU at path: " + parentPath.Path + " for reason " + e.Message, e);
            }
        }
    }



    /// <summary>
    /// Converts the Active Directory Configuration Domain value which is in domain.com format to LDAP style (dc=domain,dc=com)
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public string DomainInLDAPStyle()
    {
        string domain = ActiveDirConfig?.Domain ?? "";
        if (domain == "")
            throw new ArgumentNullException("Domain", "Domain is not set in the Active Directory Configuration");

        string[] domainParts = domain.Split('.');
        string   ldapStyle   = "";
        foreach (string domainPart in domainParts)
        {
            if (ldapStyle != "")
                ldapStyle += ",";
            ldapStyle += "dc=" + domainPart;
        }

        return ldapStyle;
    }

    
    public void Cleanup (){}

    
    /// <summary>
    /// Creates a new random user under the parent path.  If the parent path is null, it will use the UnitTestParent property.
    /// </summary>
    /// <param name="parentPath">The parent path under which to create the user. If null, the UnitTestParent is used.</param>
    /// <returns>A new ADpUser object.</returns>
    public ADpUser CreateRandomUserNew(ADSPath parentPath = null)
    {
        if (parentPath == null)
            parentPath = UnitTestParent;
        string name = Faker.Person.FullName;
        ADpUser user = new ADpUser(name, parentPath);
        return user;
    }

    
    private static Faker<TestUserAttr> PersonFaker;
    private static Faker<TestGroupAttr>              GroupFaker;
    
    
    /// <summary>
    /// Generates a list of random person objects
    /// </summary>
    /// <param name="numberOfPeople"></param>
    /// <returns></returns>
    public List<TestUserAttr> GenerateRandomPerson(int peopleToGenerate = 1)
    {
        if (PersonFaker == null)
        {
            PersonFaker = new Faker<TestUserAttr>()

                          // Pick realistic names
                          .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                          .RuleFor(p => p.LastName, f => f.Name.LastName())

                          // Generate context-aware email based on first and last name
                          .RuleFor(p => p.Email,
                                   (f,
                                    p) => f.Internet.Email(p.FirstName, p.LastName))

                          .RuleFor(p => p.Title, f => f.Name.JobTitle())
                          .RuleFor(p => p.Phone, f => f.Phone.PhoneNumber())
                          .RuleFor(p => p.Description, f => f.Lorem.Sentence())
                          .RuleFor(p => p.Department, f => f.Commerce.Department())
                          .RuleFor(p => p.Company, f => f.Company.CompanyName())
                          .RuleFor( p=>p.OfficeLocation, f => f.Address.City())

                          // Generate standardized phone numbers and full addresses
                          //.RuleFor(p => p.UserId, f => f.Person.Random.Word())
                          .RuleFor(p => p.FullName, f => f.Person.FullName);
        }

        List<TestUserAttr> people = PersonFaker.Generate(peopleToGenerate);
        return people;
    }



    /// <summary>
    /// Creates a new random user under the parent path.  If the parent path is null, it will use the UnitTestParent property.
    /// </summary>
    /// <param name="parentPath">The parent path under which to create the user. If null, the UnitTestParent is used.</param>
    /// <returns>A new ADpUser object.</returns>
    public ADpGroup CreateRandomGroupNew(ADSPath parentPath = null)
    {
        if (parentPath == null)
            parentPath = UnitTestParent;
        string  name = Faker.Commerce.ProductName();
        ADpGroup group = new ADpGroup(name, parentPath);
        return group;
    }

    
    public List<TestGroupAttr> GenerateRandomGroup(int groupsToGenerate = 1)
    {
        if (GroupFaker == null)
        {
            GroupFaker = new Faker<TestGroupAttr>()

                         // Pick realistic names
                         .RuleFor(p => p.Name, f => f.Commerce.ProductName())

                         // Generate context-aware email based on first and last name
                         .RuleFor(p => p.Email,
                                  (f,
                                   p) => f.Internet.Email(p.Name))
                         .RuleFor(p => p.GroupType, f => f.PickRandom<EnumGroupType>())
                         
                         .RuleFor(p => p.Description, f => f.Lorem.Sentence());
        }

        List<TestGroupAttr> groups = GroupFaker.Generate(groupsToGenerate);
        return groups;
    }

}


public class TestGroupAttr
{
    public string Name;

    public string? DisplayName
    {
        get { return $"{Name} - {Name}"; }
    }

    public string? GroupId;
    public string? Email;
    public string? Description { get; set; }
    public EnumGroupType GroupType { get; set; }


    /// <summary>
    /// Createa an ADpUser object from the TestUserAttr object.  If the parentPath is null, it will leave that field blank
    /// </summary>
    /// <param name="parentPath"></param>
    /// <returns></returns>
    public ADpGroup CreateADpGroup(ADSPath parentPath)
    {
        ADpGroup group;
        group = new ADpGroup(Name, parentPath);

        if (Name != null)
            group.Name = Name;
        if (Email != null)
            group.Email = Email;


        if (Description != null)
            group.Description = Description;

        // Set SAM Acct
        Random x     = new Random();
        int    value = x.Next(1, 99);
        group.SAMAccount = $"{Name}{value}";


        return group;
    }

}


/// <summary>
/// Used for generating random user attributes for testing purposes.  This is not a real user object, but rather a set of attributes that can be used to create a user in Active Directory.
/// </summary>
public class TestUserAttr
    {
        public string? FirstName;
        public string? LastName;

        public string? FullName
        {
            get { return $"{FirstName} {LastName}"; }
        }

        public string? DisplayName
        {
            get { return $"{LastName}, {FirstName}"; }
        }

        public string? UserId;
        public string? Email;
        public string? Phone;
        public string? Title;
        public string? ManagerDistinguishedName { get; set; }
        public string? Department { get; set; }
        public string? Company { get; set; }
        public string? OfficeLocation { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string? Description { get; set; }


        /// <summary>
        /// Createa an ADpUser object from the TestUserAttr object.  If the parentPath is null, it will leave that field blank
        /// </summary>
        /// <param name="parentPath"></param>
        /// <returns></returns>
        public ADpUser CreateADpUser(ADSPath parentPath = null)
        {
            ADpUser user;
            if (parentPath != null)
                user = new ADpUser(FullName, parentPath);
            else
                user = new ADpUser(FullName);

            if (FirstName != null)
                user.FirstName = FirstName;
            if (LastName != null)
                user.LastName = LastName;
            if (Email != null)
                user.Email = Email;
            if (Title != null)
                user.Title = Title;
            if (Department != null)
                user.DepartmentFullName = Department;
            if (OfficeLocation != null)
                user.Office = OfficeLocation;
            /*
            if (Street != null)
                user.StreetAddress = Street;
            if (City != null)
                user.City = City;
            if (State != null)
                user.State = State;
            if (ZipCode != null)
                user.ZipCode = ZipCode;
            if (Country != null)
                user.Country = Country;
            */

            if (Description != null)
                user.Description = Description;

            // Set SAM Acct
            Random x     = new Random();
            int    value = x.Next(1, 99);
            user.SAMAccount = $"{LastName}{value}";

            // Set UPN
            user.UPN = user.Email;

            return user;
        }
    }
