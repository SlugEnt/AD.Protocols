using AD.Protocols.ADObjects;
using UT.CustomSupportObjects;

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
    }
}

