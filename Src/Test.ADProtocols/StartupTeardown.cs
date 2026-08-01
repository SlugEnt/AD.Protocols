using AD.Protocols.ADObjects;
using AD.Protocols.ADObjects.Objects;
using AD.Protocols.ADObjects.Processors;
using UT.CustomSupportObjects;
using SlugEnt.FluentResults;

namespace Test.ADProtocols;

/// <summary>
/// This is one time setup and teardown for the entire test suite.  It is used to initialize the AD connection and other stuff that is needed for the tests.
/// </summary>
[SetUpFixture]
public class StartupTeardown
{
    [OneTimeSetUp]
    [OneTimeTearDown]
    public void TearDown()
    {
        Ad_SupportInitializer asi = Ad_SupportInitializer.GetInitializer();
        asi.Initialize();

        // Delete all OU's under the UT OU.
        ADpOrgUnitProcessor ouProcessor = new ADpOrgUnitProcessor(asi.ADConnector.LdapConnection );
        var ousResult         = ouProcessor.GetAllChildOrgUnits(asi.UnitTestParent);
        
        if (ousResult.IsFailed)
        {
            if (ousResult.ErrorTitle == "NotFound")
                return;
            Console.WriteLine("StartupTearDown:  Failed to retrieve child OUs.");
            return;
        }
        List<ADpOrgUnit> ous = ousResult.Value;
        foreach (ADpOrgUnit ou in ous)
        {
            ouProcessor.Delete(ou.DistinguishedName,true);
        }
        
        // Delete all Groups under the UT OU.
        ADpGroupProcessor groupProcessor = new ADpGroupProcessor(asi.ADConnector.LdapConnection);
        List<ADpGroup> groupsToDelete = groupProcessor.GetAllChildGroups(asi.UnitTestParent);
        foreach (ADpGroup group in groupsToDelete)
            {
            groupProcessor.Delete(group.DistinguishedName, true);
            }

        // Delete all Users under the UT OU.
        ADpUserProcessor userProcessor = new ADpUserProcessor(asi.ADConnector.LdapConnection);
        List<ADpUser> usersToDelete = userProcessor.GetAllChildUsers(asi.UnitTestParent);
        foreach (ADpUser user in usersToDelete)
            {
            userProcessor.Delete(user.DistinguishedName, true);
            }
    }
}

