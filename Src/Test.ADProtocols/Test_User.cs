using AD.Protocols.ADObjects;
using SlugEnt.FluentResults;
using UT.CustomSupportObjects;

namespace Test.ADProtocols;

[TestFixture]
public class Test_User
{
    #region "Setup Teardown"
        
#pragma warning disable IDE0079
#pragma warning disable NUnit2045

    private Ad_SupportInitializer asi;


    [SetUp]
    public void Setup()
    {
        asi = Ad_SupportInitializer.GetInitializer();
        asi.Initialize();
    }
    #endregion


        /// <summary>
        /// Simple Add user Test.  Creates a user with just a name and saves it to AD.
        /// </summary>
    [Test]
    public void AddUser()
    {
        // A  --> Setup
        string  name = asi.Faker.Person.FullName;
        ADpUser user = new ADpUser(name,asi.UnitTestParent);
        ADpUserProcessor userProcessor = new ADpUserProcessor(asi.ADConnector.LdapConnection);

        // B  --> Post Setup Confirmation

        // C  --> Action
        Result x =userProcessor.AddNew(user);
        

        // V  -- Verify
        Assert.That(x.IsSuccess,Is.True,"[V_100] Failed to add user");
        Assert.That(user.DistinguishedName, Is.Not.Null.And.Not.Empty, "[V_110] User DistinguishedName is null or empty");
        
        // Confirm it's in AD
        Result<ADpUser> result = userProcessor.Get(user.DistinguishedName).Value;
        Assert.That(result.IsSuccess,Is.True,"[V_200] Failed to retrieve user from AD after creation.");
        ADpUser foundUser = result.Value;
        Assert.That(foundUser, Is.Not.Null, "[V_200] Failed to retrieve user from AD after creation.");
        Assert.That(foundUser.DistinguishedName, Is.EqualTo(user.DistinguishedName), "[V_210] Retrieved user has incorrect DistinguishedName.");
        Assert.That(foundUser.Name, Is.EqualTo(name), "[V_220] Retrieved user has incorrect Name.");
    }

}

