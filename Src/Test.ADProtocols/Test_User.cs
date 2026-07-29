using AD.Protocols.ADObjects;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;
using UT.CustomSupportObjects;

namespace Test.ADProtocols;

[TestFixture]
public class Test_User
{
    #region "Setup Teardown"
        
#pragma warning disable IDE0079
#pragma warning disable NUnit2045

    private Ad_SupportInitializer asi;
    private ADpUserProcessor _userProcessor;

    [SetUp]
    public void Setup()
    {
        asi = Ad_SupportInitializer.GetInitializer();
        asi.Initialize();
        _userProcessor = new ADpUserProcessor(asi.ADConnector.LdapConnection);
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

        // B  --> Post Setup Confirmation

        // C  --> Action
        Result x = _userProcessor.AddNew(user);
        

        // V  -- Verify
        Assert.That(x.IsSuccess,Is.True,"[V_100] Failed to add user");
        Assert.That(user.DistinguishedName, Is.Not.Null.And.Not.Empty, "[V_110] User DistinguishedName is null or empty");
        
        // Confirm it's in AD
        Result<ADpUser> result = _userProcessor.Get(user.DistinguishedName).Value;
        Assert.That(result.IsSuccess,Is.True,"[V_200] Failed to retrieve user from AD after creation.");
        ADpUser foundUser = result.Value;
        Assert.That(foundUser, Is.Not.Null, "[V_200] Failed to retrieve user from AD after creation.");
        Assert.That((user.ParentPath == foundUser.ParentPath),Is.True,"[V_205] Retrieved user has incorrect ParentPath.");
        Assert.That(foundUser.EqualSameUser(user),Is.True, "[V_220] Retrieved user has incorrect Name.");

        // Z -- Delete the user
        Result z = _userProcessor.Delete(foundUser);
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
        Result result = _userProcessor.AddNew(testUser);
        Assert.That(result.IsSuccess, Is.True, "[A_100]  Failed to add test user.");


        // C. Move the User
        string  priorDn    = testUser.DistinguishedName;
        ADSPath priorPath  = testUser.ParentPath;
        
        Result  moveResult = _userProcessor.Move(testUser, moveToOu.Path);
        Assert.That(moveResult.IsSuccess, Is.True, "C-100:  User move failed - " + moveResult.ToStringWithLineFeeds());


        // D. Verify It is in new location 
        Result<bool> exitResult = _userProcessor.Exists(testUser);
        Assert.That(exitResult.IsSuccess, Is.True, "D-100:  Failed to check if user exists.");
        Assert.That(exitResult.Value, Is.True, "D-110:  User does not exist in the expected location.");

        // Verify it is not in old location
        Result<bool> exitResultOld = _userProcessor.Exists(priorDn);
        Assert.That(exitResultOld.IsSuccess, Is.True, "D-120:  Failed to check if user exists in old location.");
        Assert.That(exitResultOld.Value, Is.False, "D-130:  User still exists in the old location.");
    }


    [Test]
    public void Exists_Success()
    {
        // A --> Setup
        // Create 2 random OU;s
        ADpOrgUnit newOu    = asi.CreateRandomOuNew();

        ADpUser          userA         = new ADpUser(asi.Faker.Person.FullName, newOu.Path);

        // See if user exists.
        Result<bool> existsResult = _userProcessor.Exists(userA);
        Assert.That(existsResult.IsSuccess,Is.False, "[V_100] Failed to check if user exists.");    

        // Save User
        Result x = _userProcessor.AddNew(userA);
        Assert.That(x.IsSuccess,Is.True, "[V_110] Failed to add user.");

        // See if user exists.
        Result<bool> existsResultAfterAdd = _userProcessor.Exists(userA);
        Assert.That(existsResultAfterAdd.IsSuccess, Is.True, "[V_200] Failed to check if user exists after add.");
        Assert.That(existsResultAfterAdd.Value, Is.True, "[V_210] User was not found after add.");
    }


    [Test]
    public void Rename_Success()
    {
        // A --> Setup
        // Create random OU;s
        ADpOrgUnit newOu = asi.CreateRandomOuNew();

        ADpUser          userA         = new ADpUser(asi.Faker.Person.FullName, newOu.Path);

        // Save User
        Result x = _userProcessor.AddNew(userA);
        Assert.That(x.IsSuccess, Is.True, "[A_110] Failed to add user.");

        
        // Confirm user exists.
        Result<bool> existsResultAfterAdd = _userProcessor.Exists(userA);
        Assert.That(existsResultAfterAdd.IsSuccess, Is.True, "[A_200] Failed to check if user exists after add.");
        Assert.That(existsResultAfterAdd.Value, Is.True, "[A_210] User was not found after add.");

        
        // C --> Act
        string newName = "john hamilton smith";

        // V --> Verify
        Result<string> y = _userProcessor.Rename(userA,newName);
        Assert.That(y.IsSuccess, Is.True, "[V_100] Failed to rename user.");
        Assert.That(userA.CommonName, Is.EqualTo(newName), "[V_110] User was not renamed correctly.");
    }


    /// <summary>
    /// Tests several items related to Passwords:
    /// Can we set the password.  Can we login.  Can we get Login info.
    /// </summary>
    [Test]
    public void GoodPassword()
    {
        string password = "2026abcdef*";

        // A --> Setup
        // Create random OU;s and a random person
        ADpOrgUnit         newOu     = asi.CreateRandomOuNew();
        List<TestUserAttr> testUsers = asi.GenerateRandomPerson();
        ADpUser            userA     = testUsers[0].CreateADpUser(newOu.Path);

        userA.Password = password;
        userA.UserAccountControlSetter.EnableAccount();

        // Save User
        Result x = _userProcessor.AddNew(userA);
        Assert.That(x.IsSuccess, Is.True, "[A_110] Failed to add user.");


        DateTimeOffset last5Seconds = DateTimeOffset.Now.UtcDateTime;
        last5Seconds =  last5Seconds.AddSeconds(-5);

        // C  --> Action
        // Attempt to login as User
        Result<LdapConnection> loginResult = asi.ADConnector.ConnectAsUser(userA.SAMAccount, password);
        Assert.That(loginResult.IsSuccess, Is.True, "[C_100] Login should have succeeded with correct password.");

        // V  -- Verify
        _userProcessor.AttrRetrieval_PasswordLogonInfo();
        
        // Re-read User from AD and check Password fields
        Result<ADpUser> userResult = _userProcessor.Get(userA.DistinguishedName);
        Assert.That(userResult.IsSuccess, Is.True, "[V_100] Failed to retrieve user object after bad password attempts.");
        ADpUser user = userResult.Value;

        
        Assert.That(user.PasswordLastSet, Is.GreaterThan(last5Seconds), "[V_110] PasswordLastSet is not within the last 5 seconds.");
        Assert.That(user.PasswordLastSet, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow),"");
        Assert.That(user.BadPasswordCount, Is.Zero,"[V_120] BadPasswordCount is not zero after successful login.");
        //Assert.That(user.LastLogon,Is.InRange(last5Seconds, DateTimeOffset.UtcNow), "[V_130] LastLogon is not within the last 5 seconds.");
        
        // Delete the user so it doesn't persist in AD
        Result deleteResult = _userProcessor.Delete(user);
        Assert.That(deleteResult.IsSuccess, Is.True, "[Z_100] Failed to delete user from AD.");

    }


    /// <summary>
    /// Tests several items related to Passwords:
    /// Can we set the password.  Can we retrieve Bad Password Count.
    /// </summary>
    [Test]
    public void BadPassword()
    {
        string        password = "2026abcdef*";
        
        // A --> Setup
        // Create random OU;s and a random person
        ADpOrgUnit newOu = asi.CreateRandomOuNew();
        List<TestUserAttr> testUsers = asi.GenerateRandomPerson();
        ADpUser    userA = testUsers[0].CreateADpUser(newOu.Path);
        
        userA.Password   = password;
        userA.UserAccountControlSetter.EnableAccount();
        
        // Save User
        Result x = _userProcessor.AddNew(userA);
        Assert.That(x.IsSuccess, Is.True, "[A_110] Failed to add user.");


        // C  --> Action
        // Attempt to login as User, but with bad password.
        Result<LdapConnection> loginResult = asi.ADConnector.ConnectAsUser(userA.SAMAccount, "badword");
        Assert.That(loginResult.IsSuccess,Is.False,"[C_100] Login should have failed with bad password.");
        loginResult = asi.ADConnector.ConnectAsUser(userA.SAMAccount, "badpassword");
        loginResult = asi.ADConnector.ConnectAsUser(userA.SAMAccount, "badpassword");


        // V  -- Verify
        // Should be 3 bad login attempts now.  Re-read User from AD and check BadPasswordCount
        // Make sure we add the retrieval of the BadPasswordCount attribute to the user processor
        _userProcessor.AttrRetrieval_PasswordLogonInfo();
        

        // Re-read User from AD and check BadPasswordCount
        // Make sure we add the retrieval of the BadPasswordCount attribute to the user processor

        // Need to re-get the user object from AD to get the updated bad password count
        // We pass the Distinguished Name as the User object may not be fully populated if it was just created
        Result<ADpUser> userResult = _userProcessor.Get(userA.DistinguishedName);
        Assert.That(userResult.IsSuccess, Is.True, "[V_100] Failed to retrieve user object after bad password attempts.");
        ADpUser userFromAD = userResult.Value;

        Assert.That(userFromAD.BadPasswordCount, Is.EqualTo(3), "[V_110] BadPasswordCount is not 3 after 3 failed attempts.");

        // Delete the user so it doesn't persist in AD
        Result deleteResult = _userProcessor.Delete(userFromAD);
        Assert.That(deleteResult.IsSuccess, Is.True, "[Z_100] Failed to delete user from AD.");
    }


    /// <summary>
    /// Make sure majority of user settable attributes are working on user.
    /// </summary>
    [Test]
    public void UserFieldsTest()
    {
        // A --> Setup
        // Create random OU;s and a random person
        ADpOrgUnit         newOu     = asi.CreateRandomOuNew();
        List<TestUserAttr> testUsers = asi.GenerateRandomPerson();
        ADpUser            userA     = testUsers[0].CreateADpUser(newOu.Path);
        string             password  = "2026abcdef*";
        userA.Password = password;
        userA.UserAccountControlSetter.EnableAccount();     // You must have a password set if enabling account.

        
        // C. Act.
        // Save User
        Result x = _userProcessor.AddNew(userA);
        Assert.That(x.IsSuccess, Is.True, $"[C_100] Failed to add user. {x.ToStringErrorOnly()}");


        // Make sure to retrieve the user attributes from AD
        _userProcessor.AttrRetrieval_Office();
        _userProcessor.AttrRetrieval_Default();

        // Need to re-get the user object from AD to get the updated bad password count
        Result<ADpUser> userResult = _userProcessor.Get(userA.DistinguishedName);
        Assert.That(userResult.IsSuccess, Is.True, "[V_100] Failed to retrieve user object after bad password attempts.");
        ADpUser adUser = userResult.Value;

        
        // Verify
        Assert.That(adUser.FirstName,Is.EqualTo(userA.FirstName),"[V_200] FirstName does not match.");
        Assert.That(adUser.LastName,Is.EqualTo(userA.LastName),"[V_210] LastName does not match.");
        Assert.That(adUser.Description, Is.EqualTo(userA.Description), "[V_230] Description does not match.");
        Assert.That(adUser.Office, Is.EqualTo(userA.Office), "[V_240] Office does not match.");
        Assert.That(adUser.Phone, Is.EqualTo(userA.Phone), "[V_250] Phone does not match.");
        Assert.That(adUser.Title, Is.EqualTo(userA.Title), "[V_260] Title does not match.");
        Assert.That(adUser.SAMAccount, Is.EqualTo(userA.SAMAccount), "[V_270] SAMAccount does not match.");
        Assert.That(adUser.UPN, Is.EqualTo(userA.UPN), "[V_280] UPN does not match.");
        Assert.That(adUser.FirstName, Is.EqualTo(userA.FirstName), "[V_290] FirstName does not match.");
        Assert.That(adUser.LastName, Is.EqualTo(userA.LastName), "[V_300] LastName does not match.");
        Assert.That(adUser.DepartmentFullName, Is.EqualTo(userA.DepartmentFullName), "[V_310] DepartmentFullName does not match.");
        Assert.That(adUser.UPN,Is.EqualTo(userA.UPN), "[V_320] UPN does not match.");
        // Delete the user so it doesn't persist in AD
        Result deleteResult = _userProcessor.Delete(adUser);
        Assert.That(deleteResult.IsSuccess, Is.True, "[Z_100] Failed to delete user from AD.");
    }


    /// <summary>
    /// Runs thru an entire cycle of creating a user, enabling the account, disabling the account, and re-enabling the account.  Verifies that the account is enabled/disabled at each step.
    /// </summary>
    [Test]
    public void EnableDisableAccount()
    {
        // A --> Setup

        // Make sure to retrieve the user attributes from AD
        _userProcessor.AttrRetrieval_Office();
        _userProcessor.AttrRetrieval_Default();

        // Create random OU;s and a random person
        ADpOrgUnit newOu     = asi.CreateRandomOuNew();
        List<TestUserAttr> testUsers = asi.GenerateRandomPerson();
        ADpUser            userA     = testUsers[0].CreateADpUser(newOu.Path);
        string             password  = "2026abcdef*";
        userA.Password = password;
        userA.UserAccountControlSetter.EnableAccount(); // You must have a password set if enabling account.


        // B --> Act 1.
        // Save User
        Result x = _userProcessor.AddNew(userA);
        Assert.That(x.IsSuccess, Is.True, $"[B_100] Failed to add user. {x.ToStringErrorOnly()}");
        
        var x2 = _userProcessor.Get(userA.DistinguishedName);
        Assert.That(x2.IsSuccess, Is.True, "[B_110] Failed to retrieve user after adding.");
        ADpUser userB = x2.Value;
        Assert.That(userB.UserAccountControlSetter.IsEnabled,Is.True, "[B_120] User account is not marked as enabled.");
        
        // C --> Act 2. Disable the account
        userB.UserAccountControlSetter.DisableAccount();
        x = _userProcessor.Update(userB);
        Assert.That(x.IsSuccess,Is.True,"[C_100] Failed to update user after disabling account.");


        // D --> Retrieve User from AD
        var x3 = _userProcessor.Get(userA.DistinguishedName);
        Assert.That(x3.IsSuccess, Is.True, "[B_110] Failed to retrieve user after adding.");
        ADpUser adUser = x3.Value;
        Assert.That(adUser.UserAccountControlSetter.IsDisabled, Is.True, "[D_120 ] User account is not marked as disabled.");

        // E --> Act 3. Re-enable the account
        adUser.UserAccountControlSetter.EnableAccount();
        x = _userProcessor.Update(adUser);
        Assert.That(x.IsSuccess, Is.True, "[E_100] Failed to update user after Re-enabling account.");

        // F --> Verify 
        var x4 = _userProcessor.Get(userA.DistinguishedName);
        Assert.That(x4.IsSuccess, Is.True, "[F_100] Failed to retrieve user after adding.");
        ADpUser adUserB = x4.Value;
        Assert.That(adUserB.UserAccountControlSetter.IsEnabled, Is.True, "[F_110] User account is not marked as enabled.");

        // Delete the user so it doesn't persist in AD
        Result deleteResult = _userProcessor.Delete(userA);
        Assert.That(deleteResult.IsSuccess, Is.True, "[Z_100] Failed to delete user from AD.");
    }


    [Test]
    public void AccountLockOut()
    {
        // A --> Setup

        // Make sure to retrieve the user attributes from AD
        _userProcessor.AttrRetrieval_Office();
        _userProcessor.AttrRetrieval_Default();
        _userProcessor.AttrRetrieval_PasswordLogonInfo();
        
        // Create random OU;s and a random person
        ADpOrgUnit         newOu     = asi.CreateRandomOuNew();
        List<TestUserAttr> testUsers = asi.GenerateRandomPerson();
        ADpUser            userA     = testUsers[0].CreateADpUser(newOu.Path);
        string             password  = "2026abcdef*";
        userA.Password = password;
        userA.UserAccountControlSetter.EnableAccount(); // You must have a password set if enabling account.


        // B --> Act 1.
        // Save User
        Result x = _userProcessor.AddNew(userA);
        Assert.That(x.IsSuccess, Is.True, $"[B_100] Failed to add user. {x.ToStringErrorOnly()}");

        // B --> Act 2. 
        // Login as user many times with bad password to lock the account.  The default lockout threshold is 10 bad attempts.
        for (int i = 0; i < 20; i++)
        {
            var loginResult = asi.ADConnector.ConnectAsUser(userA.SAMAccount, "badword");
            Assert.That(loginResult.IsFailed,Is.True,"[B_110] Login attempt should have failed.");
        }

        // C --> Verify locked.
        // Retrieve the user back.
        var x2 = _userProcessor.Get(userA.DistinguishedName);
        Assert.That(x2.IsSuccess, Is.True, "[C_100] Failed to retrieve user.");
        ADpUser userB = x2.Value;
        Assert.That(userB.MsDsUserAccountControlGetter.IsLockedOut, Is.True, "[C_110] User account is marked as locked out.");
        
        // No way to unlock via Ldap.
        
        /*

        // E --> Act 3. Unlock the account
//        userB.UserAccountControlSetter.UnlockAccount();
        x = _userProcessor.Update(userB);
        Assert.That(x.IsSuccess, Is.True, "[E_100] Failed to update user after unlocking account.");

        // F --> Verify 
        var x4 = _userProcessor.Get(userA.DistinguishedName);
        Assert.That(x4.IsSuccess, Is.True, "[F_100] Failed to retrieve user.");
        ADpUser adUserB = x4.Value;
        Assert.That(adUserB.UserAccountControlSetter.IsAccountLockedOut, Is.False, "[F_110] User account is not marked as unlocked.");

        // Delete the user so it doesn't persist in AD
        Result deleteResult = _userProcessor.Delete(userA);
        Assert.That(deleteResult.IsSuccess, Is.True, "[Z_100] Failed to delete user from AD.");
        */
    }

}

