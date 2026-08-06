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
    private ADpOrgUnitProcessor _ouProcessor;


    [SetUp]
    public void Setup()
    {
        asi = Ad_SupportInitializer.GetInitializer();
        asi.Initialize();

        _ouProcessor = new ADpOrgUnitProcessor(asi.ADConnector.LdapConnection);
    }

    
    [Test]
    public void CreateOrgUnitSimple()
    {
        // A --> Setup
        Result<ADpOrgUnit> newOuResult;
        ADSPath         parentOu = asi.UnitTestParent;
        string ouName = asi.Faker.Random.Word().Replace("&", string.Empty);

        newOuResult = _ouProcessor.AddNew(ouName, parentOu);
        
        Assert.That(newOuResult.IsSuccess, Is.True, "[V-100]  Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());
        Assert.That(newOuResult.Value,Is.Not.Null, "[V-110]  The created OU is null.");
    }
    

    [Test]
    public void UpdateOuProperties()
    {
        // A --> Setup
        ADpOrgUnit newOu = asi.CreateRandomOuNew(asi.UnitTestParent);

        // B Act.
        string newDescription = "999999abc";
        newOu.Description = newDescription;
        _ouProcessor.Update(newOu);

        // Read the object back from AD to verify the update
        _ouProcessor.AttributeRetrieverMgr.AddAttribute("description");
        Result<ADpOrgUnit> updatedOuResult = _ouProcessor.Get(newOu.DistinguishedName);

        Assert.That(updatedOuResult.IsSuccess, Is.True, "[C-100] Failed to retrieve the updated OU from AD. Errors: " + updatedOuResult.ToStringWithLineFeeds());
        Assert.That(updatedOuResult.Value.Description, Is.EqualTo(newDescription), "[C-110] The OU description was not updated correctly.");

        // Cleanup
        _ouProcessor.Delete(newOu.DistinguishedName);
        
    }


    [Test]
    public void SimpleCreate()
    {
        // A --> Setup
        ADSPath parentOu                = asi.UnitTestParent;
        string  ouName                  = asi.Faker.Random.Word()+"5456";
        ouName = ouName.Replace("&", string.Empty);
        
        string  objectClass             = "user";
        string  distinguishedNamePrefix = "cn";
        
        Result result =  _ouProcessor.CreateSimple(ouName,
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
        

        Result<List<ADpOrgUnit>> result = _ouProcessor.Find(asi.UnitTestRoot.Path, SearchScope.Subtree,
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
        ouName = ouName.Replace("&", string.Empty);

        // B Act.
        ADpOrgUnit ou = new (ouName, parentOu);
        newOuResult = _ouProcessor.AddNew(ou);
        
        
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

        _ouProcessor.AttrRetrieval_Default();
        string[] attributes = _ouProcessor.AttributeRetrieverMgr.Attributes;
        
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
        ADpOrgUnit      newOu = asi.CreateRandomOuNew(asi.UnitTestParent);

        // C --> Act
        string dn = newOu.DistinguishedName;
        Result<DeleteResponse>delResult = _ouProcessor.Delete(dn);
        
        // V --> Verify
        Assert.That(delResult.IsSuccess, Is.True, "[C-100]  Failed to delete the OU.  Errors: " + delResult.ToStringWithLineFeeds());

        Result<ADpOrgUnit> result       = _ouProcessor.Get(newOu.DistinguishedName);
        Assert.That(result.IsFailed,Is.True, "[V-100]  Retrieving the OU should have failed. Errors: " + result.ToStringWithLineFeeds());
    }


    /// <summary>
    /// Validates we can retrieve a single OU using Distinguished name
    /// </summary>
    [Test]
    public void GetSingle()
    {
        // A --> Setup
        ADpOrgUnit      newOu = asi.CreateRandomOuNew(asi.UnitTestParent);
        
        // C --> Act
        Result<ADpOrgUnit> result = _ouProcessor.Get(newOu.DistinguishedName);
        Assert.That(result.IsSuccess,Is.True,"[V_100]");
        Assert.That(result.Value.CommonName, Is.EqualTo(newOu.CommonName), "[V_110]");
    }


    [Test]
    public void GetByName()
    {
        // A --> Setup
        ADpOrgUnit newOu  = asi.CreateRandomOuNew(asi.UnitTestParent);

        // C --> Act
        Result<ADpOrgUnit> result = _ouProcessor.GetBy_Name(newOu.Name,asi.UnitTestParent);
        Assert.That(result.IsSuccess, Is.True, "[V_100]");
        Assert.That(result.Value.CommonName, Is.EqualTo(newOu.CommonName), "[V_110]");
        Assert.That(result.Value.WasReadFromActiveDirectory, Is.True, "[V_120]  WasReadFromActiveDirectory property should have been set to True");
    }

}

