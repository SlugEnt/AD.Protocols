using AD.Protocols.ADObjects;
using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;

using System.DirectoryServices.Protocols;
using UT.CustomSupportObjects;

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
    public void UpdateOuProperties()
    {
        // A --> Setup
        Result<ADSPath> newOuResult;
        ADSPath         parentOu = asi.UnitTestParent;
        string          ouName   = asi.Faker.Random.Word();

        // B Act.
        ADpOrgUnitProcessor ouProcessor = new ADpOrgUnitProcessor(asi.ADConnector.LdapConnection);

        ADpOrgUnit ou = new(ouName, parentOu);
        newOuResult = ouProcessor.AddNew(ou);
        Assert.That(newOuResult.IsSuccess, Is.True, "[B-100]  Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());

        string newDescription = "999999abc";
        ou.Description = newDescription;
        ouProcessor.Update(ou);

        // Read the object back from AD to verify the update
        ouProcessor.AttributeRetrieverMgr.AddAttribute("description");
        Result<ADpOrgUnit> updatedOuResult = ouProcessor.Get(ou.DistinguishedName);

        Assert.That(updatedOuResult.IsSuccess, Is.True, "[C-100] Failed to retrieve the updated OU from AD. Errors: " + updatedOuResult.ToStringWithLineFeeds());
        Assert.That(updatedOuResult.Value.Description, Is.EqualTo(newDescription), "[C-110] The OU description was not updated correctly.");

        // Cleanup
        ouProcessor.Delete(ou.DistinguishedName);
        
    }


    [Test]
    public void SimpleCreate()
    {
        // A --> Setup
        ADSPath parentOu                = asi.UnitTestParent;
        string  ouName                  = asi.Faker.Random.Word()+"5456";
        string  objectClass             = "user";
        string  distinguishedNamePrefix = "cn";
        
        ADpOrgUnitProcessor ouProcessor = new ADpOrgUnitProcessor(asi.ADConnector.LdapConnection);
        Result result =  ouProcessor.CreateSimple(ouName,
                                 parentOu,
                                 objectClass,
                                 distinguishedNamePrefix);

        Assert.That(result.IsSuccess, Is.True, "[B-100]  Unable to create the unique containing OU for this test.  Errors: " + result.ToStringWithLineFeeds());
    }
    
    //*****************************************************************
    // New Tests 
    
    [Test]
    public void NewTestSearch()
    {
        string searchFilter       = ActiveDirectoryConnector.SEARCH_FILTER_ALL_OU;
        List<string> attributesToReturn = new List<string>();
        ADpReadOnlyOrgUnit.AddBaseAttributes(attributesToReturn);
        
        ADpOrgUnitProcessor ouProcessor = new (asi.ADConnector.LdapConnection);
        
        Result<List<ADpOrgUnit>> result = ouProcessor.Find(asi.UnitTestRoot.Path, SearchScope.Subtree,
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
        
        Assert.That(attributes.Length, Is.EqualTo(4), "[V_100]");
        Assert.That(attributes, Does.Contain("ou"),"[V_110]");
        Assert.That(attributes, Does.Contain("cn"),"[V_120]");
        Assert.That(attributes, Does.Contain("distinguishedName"),"[V_130]");
        Assert.That(attributes, Does.Contain("name"),"[V_140]");
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

        Result<ADpOrgUnit> result       = ouProcessor.Get(ou.DistinguishedName);
        Assert.That(result.IsFailed,Is.True, "[V-100]  Retrieving the OU should have failed. Errors: " + result.ToStringWithLineFeeds());
    }


    /// <summary>
    /// Validates we can retrieve a single OU using Distinguished name
    /// </summary>
    [Test]
    public void GetSingle()
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
        Result<ADpOrgUnit> result = ouProcessor.Get(ou.DistinguishedName);
        Assert.That(result.IsSuccess,Is.True,"[V_100]");
        Assert.That(result.Value.CommonName, Is.EqualTo(ou.CommonName), "[V_110]");
    }


    [Test]
    public void GetByName()
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
        Result<ADpOrgUnit> result = ouProcessor.GetBy_Name(ou.Name,parentOu);
        Assert.That(result.IsSuccess, Is.True, "[V_100]");
        Assert.That(result.Value.CommonName, Is.EqualTo(ou.CommonName), "[V_110]");
        Assert.That(result.Value.WasReadFromActiveDirectory, Is.True, "[V_120]  WasReadFromActiveDirectory property should have been set to True");
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

