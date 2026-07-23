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
    public const string MASTER_AD_UNIT_TEST_ROOT = "zUnitTesting";

    public static Ad_SupportInitializer GetInitializer(bool isGroup = false)
    {
        Ad_SupportInitializer initializer = new();
        return initializer;
    }
    private Ad_SupportInitializer(bool isGroup = false)
    {
        SupportMethods = new();
        AdEngine       = new(null);
        UnitTestRoot   = AdEngine.DomainRoot.NewChildADSPath("ou=" + MASTER_AD_UNIT_TEST_ROOT);
        if (!isGroup) 
            UnitTestParent = UnitTestRoot.NewChildADSPath("ou=" + HelperMethods.UTBASE);
        else
            UnitTestParent = UnitTestRoot.NewChildADSPath("ou=" + HelperMethods.OU_UTGROUP);

    }

    public SupportMethods Sm { get => SupportMethods;}

    public SupportMethods SupportMethods { get; private set; }

    public ActiveDirectoryConnector  AdEngine { get; private set; }

    public ADSPath UnitTestParent { get; private set; }

    public ADSPath UnitTestRoot { get; private set; }


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
                string newOuName = SupportMethods.Faker.Random.Word();
                // Since word is really words, we need to remove bogus characters
                newOuName = newOuName.Replace("&", string.Empty);

                // Now  add OU to LDAP
                addResult =  AdEngine.OuCreate(newOuName, parentPath);
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
}