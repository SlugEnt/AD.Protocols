using System.ComponentModel;
using Bogus;
using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
using SlugEnt.IS;
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



    public ADSPath UnitTestParent { get; private set; }

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
                  
                addResult =  ADConnector.OuCreate(newOuName, parentPath);
                if (addResult.IsSuccess)
                {
                    return Result.Ok(parentPath.NewChildADSPath("ou=" + newOuName));
                }

                if (addResult.IsFailed)
                {
                    if (addResult.Errors[0].Message == ActiveDirectoryConnector.EXISTS)
                    {
                        // Try again...
                        continue;
                    }
                    else
                        return Result.Fail(new Error("Failed to create the random OU at path: " + parentPath.Path + " for reason " + addResult.ToStringWithLineFeeds()));
                }
            }
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError(e));
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
}