using AD.Protocols.ADObjects;
using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
 

namespace UT.SupportObjects;


/// <summary>
/// Provides helper methods for testing.  These are not part of the main code base.
/// </summary>
public static class HelperMethods
{
    public const string UNIT_TEST_ROOT_OU = "ou=zUnitTesting";
    public const string UT_BASEOU_NAME    = "UT";
    public const string OU_UTBASE         = "ou=" + UT_BASEOU_NAME;
    public const string UTGROUP = "UT_Groups";
    public const string OU_UTGROUP = "ou=" + UTGROUP;


    /// <summary>
    /// Returns the UT Base Path used for most of testing.
    /// </summary>
    /// <param name="domainRootPath"></param>
    /// <returns></returns>
    public static ADSPath GetUT_BasePath(ADSPath domainRootPath)
    {
        ADSPath path = domainRootPath.BuildChildADSPath(UNIT_TEST_ROOT_OU);
        ADSPath ut = path.BuildChildADSPath(OU_UTBASE);
        return ut;
        //return domainRootPath.NewChildADSPath(OU_UTBASE);
    }


    /// <summary>
    /// Returns the UT Base Path used for most of testing.
    /// </summary>
    /// <param name="domainRootPath"></param>
    /// <returns></returns>
    public static ADSPath GetUT_GroupBasePath(ADSPath domainRootPath)
    {
        return domainRootPath.BuildChildADSPath(OU_UTGROUP);
    }



    /// <summary>
    /// Used to display the user's name in the Test output window to help with troubleshooting if issues
    /// </summary>
    /// <param name="user"></param>
    public static void DisplayUser(ADpUserFromAD_RO? user, bool assertIfNull = true)
    {
        if (user == null)
        {
            Console.WriteLine("Display User:  User is Null.  Nothing to Display");
            if (assertIfNull)
                Assert.That(user,Is.Not.Null,"DU-100:  DisplayUser found that the User object was null.  We were told to expect a value.");
        }
        else
            Console.WriteLine("User [ " + user.DistinguishedName + " ]  Full Name [ " + user.DisplayName + " ]");
    }


    /// <summary>
    /// Used to display the group's name in the Test output window to help with troubleshooting if issues
    /// </summary>
    /// <param name="group"></param>
    public static void DisplayGroup(ADpReadOnlyGroup group)
    {
        Console.WriteLine("Group [ " + group.DistinguishedName + " ]  Full Name [ " + group.DisplayName + " ]");
    }


    /// <summary>
    /// Used to display the group's name in the Test output window to help with troubleshooting if issues
    /// </summary>
    /// <param name="group"></param>
    public static void DisplayGroup(ADpGroupUpdater group)
    {
        Console.WriteLine("Group [ " + group.DistinquishedName + " ]  Full Name [ " + group.DisplayNameChg + " ]");
    }


    public static void DisplayOu(string ouPath)
    {
        Console.WriteLine("OU = " + ouPath);
    }

}
