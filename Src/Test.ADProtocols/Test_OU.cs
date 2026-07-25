using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
using SlugEnt.IS;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Formats.Asn1;
using System.Text;
using AD.Protocols;
using AD.Protocols.ADObjects;
using UT;
using UT.CustomSupportObjects;
using UT.SupportObjects;

namespace Test.ADProtocols;


[TestFixture]
public class Test_OU
{
#pragma warning disable IDE0079
#pragma warning disable NUnit2045

    private Ad_SupportInitializer asi;


    [SetUp]
    public void Setup()
    {
        asi = Ad_SupportInitializer.GetInitializer();
        asi.Initialize();
    }

    
    /// <summary>
    /// Creates a Random OU under the specified parent OU.
    /// If no parent OU is specified, then the random OU will be created under the UnitTestParent OU.
    /// </summary>
    /// <param name="parentAdsPath"></param>
    /// <returns></returns>
    private ADSPath CreateRandomOu(ADSPath parentAdsPath = null)
    {
        Result<ADSPath> newOuResult;
        if (parentAdsPath == null)
        {
            newOuResult = asi.CreateRandomOu();
        }
        else
        {
            newOuResult = asi.CreateRandomOu(parentAdsPath);
        }
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());
        return newOuResult.Value;
    }


    [Test]
    public void CreateOrgUnitSimple()
    {
        // A --> Setup

        Result<ADSPath> newOuResult;
        ADSPath         parentOu = asi.UnitTestParent;
        string ouName = asi.Faker.Random.Word();

        newOuResult = asi.ADConnector.OuCreate(ouName, parentOu);
        
        Assert.That(newOuResult.IsSuccess, Is.True, "[V-100]  Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());
    }


    [Test]
    public void CreateOrgUnitFromOuObject()
    {
        // A. Setup
        string  ouName      = asi.Faker.Random.Word();
        string  description = asi.Faker.Random.Words(5);
        ADSPath parentOu    = asi.UnitTestParent;

        ADpOrgUnitUpdater orgUnit = new ADpOrgUnitUpdater(ouName, parentOu);
        orgUnit.DescriptionChg = description;

        // Verify the orgUnit object has the correct properties
        Assert.That(orgUnit.NameChg, Is.EqualTo(ouName), "[A_100] NameChg does not match the expected value.");
        Assert.That(orgUnit.ParentPath, Is.EqualTo(parentOu), "[A_110] ParentPath does not match the expected value.");
        Assert.That(orgUnit.DescriptionChg, Is.EqualTo(description), "[A_120] DescriptionChg does not match the expected value.");
        Assert.That(orgUnit.IsNew, Is.True, "[A_130] IsNew is not true as expected.");


        // B Act
        Result<ADpOrgUnitUpdater> orgUnitResult = asi.ADConnector.OuCreate(orgUnit);
        Assert.That(orgUnitResult.IsSuccess,
                    Is.True,
                    $"[B-100]  Failed to create OU with OuCreateUpdater. Errors: {orgUnitResult.ToStringErrorOnly()}");

        Assert.That(asi.ADConnector.OuExists(parentOu, ouName).Value, Is.True, "[B-110] OU does not exist after creation.");
    }


    [Test]
    public void UpdateOuProperties()
    {
        // A. Setup
        string  ouName      = asi.Faker.Random.Word();
        string  description = asi.Faker.Random.Words(5);
        ADSPath parentOu    = asi.UnitTestParent;

        ADpOrgUnitUpdater orgUnit = new ADpOrgUnitUpdater(ouName, parentOu);
        orgUnit.DescriptionChg = description;

        // Verify the orgUnit object has the correct properties
        Assert.That(orgUnit.IsNew, Is.True, "[A_130] IsNew is not true as expected.");


        // B Setup - Save the new OU to AD
        Result<ADpOrgUnitUpdater> orgUnitResult = asi.ADConnector.OuCreate(orgUnit);
        Assert.That(orgUnitResult.IsSuccess,
                    Is.True,
                    $"[B-100]  Failed to create OU with OuCreateUpdater. Errors: {orgUnitResult.ToStringErrorOnly()}");

        // C Verify the OU exists in AD
        Assert.That(asi.ADConnector.OuExists(parentOu, ouName).Value, Is.True, "[B-110] OU does not exist after creation.");

        // D Retrieve the object from AD to verify the properties
        Result<List<ADpReadOnlyOrgUnit>> resultFind = asi.ADConnector.OuFindOneOrMore(parentOu.Path, SearchScope.Base);
        Assert.That(resultFind.IsSuccess, Is.True, "[D-100] Failed to find the OU in AD. Errors: " + resultFind.ToStringWithLineFeeds());

    }

    
    //*****************************************************************
    // New Tests 
    
    [Test]
    public void NewTestSearch()
    {
        string searchFilter       = ActiveDirectoryConnector.SEARCH_FILTER_ALL_OU;
        List<string> attributesToReturn = new List<string>();
        ADpReadOnlyOrgUnit.AddBaseAttributes(attributesToReturn);
        
        ADpOrgUnitProcessor x            = new (asi.ADConnector.LdapConnection);
        
        Result<List<ADpOrgUnit>> result = x.Find(asi.UnitTestRoot.Path, SearchScope.Subtree,
               searchFilter);
        Assert.That(result.IsSuccess, Is.True, "[V-200]  Failed to find OUs. Errors: " + result.ToStringWithLineFeeds());
        Assert.That(result.Value.Count, Is.GreaterThan(0), "[V-210]  No OUs found.");
    }


    /// <summary>
    /// Tests simple adding of a new OU to AD using the ADpOrgUnitProcessor.
    /// </summary>
    [Test]
    public void AddNew_Success()
    {
        // A --> Setup
        Result<ADSPath> newOuResult;
        ADSPath         parentOu = asi.UnitTestParent;
        string          ouName   = asi.Faker.Random.Word();

        // B Act.
        ADpOrgUnitProcessor ouProcessor = new ADpOrgUnitProcessor(asi.ADConnector.LdapConnection);
        
        ADpOrgUnit ou = new (ouName, parentOu);
        newOuResult = ouProcessor.AddNew(ou);
        
        
        Assert.That(newOuResult.IsSuccess, Is.True, "[V-100]  Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());
    }


    /// <summary>
    /// Confirms that the default attributes for an OU are set correctly in the ADpOrgUnitProcessor
    /// if the user does nothing to specify any attributes to return.
    /// Also, confirms the proper attributes are returned when the default attributes are set.
    /// </summary>
    [Test]
    public void DefaultAttributes_Set()
    {
        // A --> Setup
        string searchFilter       = ActiveDirectoryConnector.SEARCH_FILTER_ALL_OU;
        List<string> attributesToReturn = new List<string>();

        ADpOrgUnitProcessor ouProcessor = new ADpOrgUnitProcessor(asi.ADConnector.LdapConnection);
        ouProcessor.AttrRetrieval_Default();
        string[] attributes = ouProcessor.AttributeRetrieverMgr.Attributes;
        
        Assert.That(attributes.Length, Is.EqualTo(3), "[V_100]");
        Assert.That(attributes, Does.Contain("ou"),"[V_110]");
        Assert.That(attributes, Does.Contain("cn"),"[V_120]");
        Assert.That(attributes, Does.Contain("distinguishedName"),"[V_130]");
    }


    /// <summary>
    /// Tests simple deletion of an OU from AD using the ADpOrgUnitProcessor.
    /// </summary>
    [Test]
    public void DeleteOU_Success()
    {
        // A --> Setup
        Result<ADSPath> newOuResult;
        ADSPath         parentOu = asi.UnitTestParent;
        string          ouName   = asi.Faker.Random.Word();

        ADpOrgUnitProcessor ouProcessor = new ADpOrgUnitProcessor(asi.ADConnector.LdapConnection);

        ADpOrgUnit ou = new(ouName, parentOu);
        newOuResult = ouProcessor.AddNew(ou);
        Assert.That(newOuResult.IsSuccess, Is.True, "[B-100]  Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());

        // C --> Act
        string dn = ou.DistinguishedName;
        Result<DeleteResponse>delResult = ouProcessor.Delete(dn);
        
        // V --> Verify
        Assert.That(delResult.IsSuccess, Is.True, "[C-100]  Failed to delete the OU.  Errors: " + delResult.ToStringWithLineFeeds());

        string       searchFilter       = ActiveDirectoryConnector.SEARCH_FILTER_ALL_OU;
        
        Result<List<ADpOrgUnit>> result = ouProcessor.Find(parentOu.Path, SearchScope.Subtree, searchFilter);
        Assert.That(result.IsSuccess,Is.True, "[V-100]  Failed to find the OU in AD. Errors: " + result.ToStringWithLineFeeds());
        Assert.That(result.Value.Count,Is.Zero,"[V-110]  OU still exists in AD after deletion.");
    }
    /*

    [Test]
public void FindOneOrMoreOus()
{
// A --> Setup
/*
// Create a random user OU
ADSPath parentAdsPath = CreateRandomOu();

// Create 3 children OUs under the new OU
ADSPath childOu1 = CreateRandomOu(parentAdsPath);
ADSPath childOu2 = CreateRandomOu(parentAdsPath);
ADSPath childOu3 = CreateRandomOu(parentAdsPath);

// Part 2
asi.ADConnector.OuExists(parentAdsPath);
//ADpUserFromAD_RO? userFromAD = null;
List<string> attributes = [];
ADpUserFromAD_RO.AddBaseAttributes(attributes);
ADpUserFromAD_RO.AddInfoAttributes(attributes);
ADpUserFromAD_RO.AddStatisticAttributes(attributes);
ADpUserFromAD_RO.AddPasswordAttributes(attributes);

string searchFilter = "(&(objectClass=user)(objectCategory=person))";
Result<List<ADpUserFromAD_RO>> resultUsers = asi.ADConnector.UserFindOneOrMore(newOuResult.Value.Path,
                                                               SearchScope.OneLevel,
                                                               searchFilter,
                                                               attributes);

Assert.That(resultUsers.IsSuccess, Is.True, "Z-100:  Failed to find the test users --> AppError: " + resultUsers.ToStringWithLineFeeds());
List<ADpUserFromAD_RO> users = resultUsers.Value;
Assert.That(users, Has.Count.EqualTo(3), "Z-200:  Found user count was expected to be 3.");
Assert.That(users[0].UPN, Is.Not.EqualTo(users[1].UPN), "Z-300:  Users in list are the same user!");

}
*/
}

