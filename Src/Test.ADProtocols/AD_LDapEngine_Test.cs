using Bogus.DataSets;
using NUnit.Framework.Interfaces;
using SlugEnt;
using SlugEnt.FluentResults;
using SlugEnt.HR.NextGen.Common;
using SlugEnt.AD.Protocols;
using SlugEnt.AD.Protocols.Attributes;
using SlugEnt.IS;
using System.DirectoryServices.Protocols;
using System.Formats.Asn1;
using System.Text;
using UT.CustomSupportObjects;
using UT.SupportObjects;

namespace UT.ActiveDirectory_Tests;

/// <summary>
/// Tests the AD_LDapEngine class
/// </summary>
[TestFixture]
[Order(10)]
public class AD_LDapEngine_Test
{
#pragma warning disable IDE0079
#pragma warning disable NUnit2045
    [SetUp]
    public void Setup()
    {
        Assume.That(SupportMethods.ContinueLdapTests, Is.True);
    }


    [TearDown]
    public void TearDown()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Passed)
        {
            if (_stopAllTestsOnFailure)
            {
                SupportMethods.ContinueLdapTests = false;
            }
        }
    }



    /// <summary>
    ///     Allows us to have initial tests that are run first and if any of them fail to prevent the rest of the tests from
    ///     running.
    /// </summary>
    private bool _stopAllTestsOnFailure = true;


    /// <summary>
    ///     Test to ensure that the RootDSE is set to the domain when the object is created
    ///     NOTE:  This test should be first one run as it validates stuff that all others will need.
    /// </summary>
    [Test]
    [Order(1)]
    public void RootOuSetAtConstruction()
    {
        // A.  Setup
        SupportMethods sm = new(false, false);


        // B.  Pre Setup Validation


        // C.  Execute
        ADLDAPEngine adEngine = new(sm.DB!, CustomMockLoggers.GetMockLogger_AdLdapEngine);


        // Z1. Validate
        ADSPath expected = ADSPath.FromDomainName(SupportMethods.ActiveDirectoryConfiguration!.Domain);
        Assert.That(adEngine.DomainRoot.ToString(), Is.EqualTo(expected.ToString()), "Z-100: Root was not set to domain");
    }



    /// <summary>
    ///     This test is part of the AD Setup process.  IT creates an OU called UT under the domain root.
    ///     If it exists, it deletes it.
    /// </summary>
    [Test]
    [Order(10)]
    [TestCase(HelperMethods.UTBASE)]
    public void UTAddOu(string baseOuToAdd)
    {
        Ad_SupportInitializer asi = Ad_SupportInitializer.GetInitializer();

        Result         addResult;
        try
        {
            addResult = asi.AdEngine.OuCreate(HelperMethods.UTBASE, asi.UnitTestRoot);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        if (addResult.IsFailed)
        {
            if (addResult.Errors[0].Message == ADLDAPEngine.EXISTS)
            {
                
                ADSPath deletePath   = asi.UnitTestRoot.NewChildADSPath("ou=" + HelperMethods.UTBASE);
                Result  deleteResult =asi.AdEngine.OuDeleteAll(deletePath);
                Assert.That(deleteResult.IsSuccess, Is.True, "Z-10:  Cleanup of prior version of UT OU failed.  Failed to delete it.");
                _ = asi.AdEngine.OuCreate(HelperMethods.UTBASE, asi.UnitTestRoot);
            }
            else
            {
                Assert.That(addResult.IsSuccess, "Z-20:  Creation of OU failed with error " + addResult.ToStringWithLineFeeds());
            }
        }

        // If here we just need to test that the OU was created.
        Result<SearchResultEntry> searchResult = asi.AdEngine.FindSingleOuAtPath(asi.UnitTestRoot, HelperMethods.UTBASE);
        Assert.That(searchResult.IsSuccess, "Z-100:  OU was not found after creation");
    }



    /// <summary>
    ///     Deletes the UT Testing OU.  While this does test the Delete OU functionality there is a separate test specifically
    ///     for that.
    ///     This is part of the pre-test tests, that ensure the AD Directory is ready to be tested.
    /// </summary>
    [Test]
    [Order(30)]
    [TestCase(HelperMethods.UTBASE)]
    public void UTDeleteOu(string baseOuToDelete)
    {
        Ad_SupportInitializer asi = Ad_SupportInitializer.GetInitializer();

        /*
        SupportMethods sm       = new(false, false);
        ADLDAPEngine   adEngine = new(sm.DB!, SupportMethods.ActiveDirectoryConfiguration!, sm.GetMockLogger_AdLdapEngine);
        Result<ADSPath> utRootResult = sm.Set_UnitTestRootOu(adEngine);
        Assert.That(utRootResult.IsSuccess, Is.True, "A-100:");
        ADSPath utRoot = utRootResult.Value;

        

        // C. Act
        Result deletionResult = asi.AdEngine.OuCreate(baseOuToDelete, asi.AdEngine.DomainRoot);

        // Z. Validate
        Assert.That(deletionResult.IsSuccess, "A-110:  Failed to create the OU for deletion.  AppError: " + deletionResult.ToStringWithLineFeeds());
        */
    }


    [Test]
    [Order(20)]
    [TestCase(HelperMethods.UTBASE)]
    public void FindSingleOuAtPath(string baseOuToFind)
    {
        Ad_SupportInitializer asi = Ad_SupportInitializer.GetInitializer();

        Result<SearchResultEntry> searchResult = asi.AdEngine.FindSingleOuAtPath(asi.UnitTestRoot, baseOuToFind);
        Assert.That(searchResult.IsSuccess, Is.True, "Z-100:  Unable to find the OU - " + baseOuToFind + " AppError" + searchResult.Reasons);
    }



    /// <summary>
    ///     Serves to reset the test on failure flag to false, all tests after this will run regardless of failure of any other
    ///     tests.
    /// </summary>
    [Test]
    [Order(9999)]
    public void ResetTestFailureCheck()
    {
        _stopAllTestsOnFailure = false;
    }






    [Test]
    public void CreateGroup()
    {

    }

#pragma warning restore NUnit2045
#pragma warning restore IDE0079
}

