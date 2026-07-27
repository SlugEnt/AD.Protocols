using AD.Protocols.ADObjects;
using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;
using UT;
using UT.CustomSupportObjects;
using UT.SupportObjects;

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
        /// Simple Create, Add, Get, Delete user Test.  Creates a user with just a name and saves it to AD.
        /// </summary>
    [Test]
    public void CycleUser_CRUD_Success()
    {
        // A  --> Setup
        string  name = asi.Faker.Person.FullName;
        ADpUser user = new ADpUser(name,asi.UnitTestParent);
        ADpUserProcessor userProcessor = new ADpUserProcessor(asi.ADConnector.LdapConnection);

        // B  --> Post Setup Confirmation

        // C  --> Action
        Result x = userProcessor.AddNew(user);
        

        // V  -- Verify
        Assert.That(x.IsSuccess,Is.True,"[V_100] Failed to add user");
        Assert.That(user.DistinguishedName, Is.Not.Null.And.Not.Empty, "[V_110] User DistinguishedName is null or empty");
        
        // Confirm it's in AD
        Result<ADpUser> result = userProcessor.Get(user.DistinguishedName).Value;
        Assert.That(result.IsSuccess,Is.True,"[V_200] Failed to retrieve user from AD after creation.");
        ADpUser foundUser = result.Value;
        Assert.That(foundUser, Is.Not.Null, "[V_200] Failed to retrieve user from AD after creation.");
        Assert.That((user.ParentPath == foundUser.ParentPath),Is.True,"[V_205] Retrieved user has incorrect ParentPath.");
        Assert.That(foundUser.EqualSameUser(user),Is.True, "[V_220] Retrieved user has incorrect Name.");

        // Z -- Delete the user
        Result z = userProcessor.Delete(foundUser);
        Assert.That(z.IsSuccess,Is.True,"[Z_100] Failed to delete user from AD.");
    }


    [Test]
    public void MoveUser()
    {
        // A --> Setup
        // Create 2 random OU;s
        ADpOrgUnit newOu = asi.CreateRandomOuNew();
        ADpOrgUnit moveToOu = asi.CreateRandomOuNew();


        // Create Test User Basic
        ADpUser          testUser      = asi.CreateRandomUserNew(newOu.Path);
        ADpUserProcessor userProcessor = new ADpUserProcessor(asi.ADConnector.LdapConnection);
        Result result = userProcessor.AddNew(testUser);
        Assert.That(result.IsSuccess, Is.True, "[A_100]  Failed to add test user.");


        // C. Move the User
        string  priorDn    = testUser.DistinguishedName;
        ADSPath priorPath  = testUser.ParentPath;
        
        Result  moveResult = userProcessor.Move(testUser, moveToOu.Path);
        Assert.That(moveResult.IsSuccess, Is.True, "C-100:  User move failed - " + moveResult.ToStringWithLineFeeds());


        // D. Verify It is in new location 
        Result<bool> exitResult = userProcessor.Exists(testUser);
        Assert.That(exitResult.IsSuccess, Is.True, "D-100:  Failed to check if user exists.");
        Assert.That(exitResult.Value, Is.True, "D-110:  User does not exist in the expected location.");

        // Verify it is not in old location
        Result<bool> exitResultOld = userProcessor.Exists(priorDn);
        Assert.That(exitResultOld.IsSuccess, Is.True, "D-120:  Failed to check if user exists in old location.");
        Assert.That(exitResultOld.Value, Is.False, "D-130:  User still exists in the old location.");
    }


    [Test]
    public void Exists_Success()
    {
        // A --> Setup
        // Create 2 random OU;s
        ADpOrgUnit newOu    = asi.CreateRandomOuNew();

        ADpUserProcessor userProcessor = asi.ADConnector.UserProcessor();
        ADpUser          userA         = new ADpUser(asi.Faker.Person.FullName, newOu.Path);

        // See if user exists.
        Result<bool> existsResult = userProcessor.Exists(userA);
        Assert.That(existsResult.IsSuccess,Is.False, "[V_100] Failed to check if user exists.");    

        // Save User
        Result x = userProcessor.AddNew(userA);
        Assert.That(x.IsSuccess,Is.True, "[V_110] Failed to add user.");

        // See if user exists.
        Result<bool> existsResultAfterAdd = userProcessor.Exists(userA);
        Assert.That(existsResultAfterAdd.IsSuccess, Is.True, "[V_200] Failed to check if user exists after add.");
        Assert.That(existsResultAfterAdd.Value, Is.True, "[V_210] User was not found after add.");
    }


    [Test]
    public void Rename_Success()
    {
        // A --> Setup
        // Create 2 random OU;s
        ADpOrgUnit newOu = asi.CreateRandomOuNew();

        ADpUserProcessor userProcessor = asi.ADConnector.UserProcessor();
        ADpUser          userA         = new ADpUser(asi.Faker.Person.FullName, newOu.Path);

        // Save User
        Result x = userProcessor.AddNew(userA);
        Assert.That(x.IsSuccess, Is.True, "[A_110] Failed to add user.");

        
        // Confirm user exists.
        Result<bool> existsResultAfterAdd = userProcessor.Exists(userA);
        Assert.That(existsResultAfterAdd.IsSuccess, Is.True, "[A_200] Failed to check if user exists after add.");
        Assert.That(existsResultAfterAdd.Value, Is.True, "[A_210] User was not found after add.");

        
        // C --> Act
        string newName = "john hamilton smith";

        // V --> Verify
        Result<string> y = userProcessor.Rename(userA,newName);
        Assert.That(y.IsSuccess, Is.True, "[V_100] Failed to rename user.");
        Assert.That(userA.CommonName, Is.EqualTo(newName), "[V_110] User was not renamed correctly.");
    }
}

