using AD.Protocols.ADObjects;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;
using AD.Protocols.ADObjects.Objects;
using AD.Protocols.ADObjects.Processors;
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
        Assert.That(foundUser.EqualSameObject(user),Is.True, "[V_220] Retrieved user has incorrect Name.");

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
    public void Exists_UserExist_Success()
    {
        // A --> Setup
        // Create random OU;s
        ADpOrgUnit newOu    = asi.CreateRandomOuNew();

        ADpUser          userA         = new ADpUser(asi.Faker.Person.FullName, newOu.Path);
        Assert.That(userA.DistinguishedName,Is.Not.Empty,"[A_100] Distinguished Name should not be empty.");
        _userProcessor.AddNew(userA);
        
        // See if user exists.
        Result<bool> existsResult = _userProcessor.Exists(userA);
        Assert.That(existsResult.IsSuccess,Is.True, "[V_100] Failed to check if user exists.");
        Assert.That(existsResult.Value,Is.True,"[V_110] User exists should have returned true.");
    }




    [Test]
    public void Exists_UserNotExist_Success()
    {
        // A --> Setup
        // Create random OU;s
        ADpOrgUnit newOu = asi.CreateRandomOuNew();

        // Create a user that does NOT exist
        ADpUser userThatDoesNotExist = new ADpUser(asi.Faker.Person.FullName + "9865", newOu.Path);
        Assert.That(userThatDoesNotExist.DistinguishedName,Is.Not.Empty,"[A_100] Distinguished Name should not be empty.");
        
        // See if user exists.
        Result<bool> result = _userProcessor.Exists(userThatDoesNotExist);
        Assert.That(result.IsSuccess, Is.True, "[V_100] User Exists should always return Success unless true errors.");
        Assert.That(result.Value, Is.False, "[V_110] Result Value should have been false - due to user not existing.");
    }



    [Test]
    public void Exists_UserObjWithNoDN_Success()
    {
        // A --> Setup
        // Create random OU;s
        ADpOrgUnit newOu = asi.CreateRandomOuNew();

        // Create a user that does NOT exist
        ADpUser userThatDoesNotExist = new ADpUser(asi.Faker.Person.FullName + "9865");
        
        
        // V --> Verify See if user exists.
        Result<bool> result = _userProcessor.Exists(userThatDoesNotExist);
        Assert.That(result.IsFailed, Is.True, "[V_100] User Exists should be false.  User does not have Distinguished name");
        Assert.That(result.ErrorTitle.Contains("Distinguished Name field does not exist"),"[V_110] Error title does not contain expected text.");
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


        DateTimeOffset temp = DateTimeOffset.Now.UtcDateTime;
        DateTimeOffset last5Seconds =  temp.AddSeconds(-10);

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

        Console.WriteLine($"Current Time:   {DateTimeOffset.UtcNow}");
        Console.WriteLine($"Temp Time:      {temp}");
        Console.WriteLine($"Last 5 Seconds: {last5Seconds}");
        Console.WriteLine($"Last Logon:     {user.LastLogon}");
        
        Assert.That(user.LastLogon, Is.GreaterThanOrEqualTo(last5Seconds), "[V_110] PasswordLastSet is not within the last 5 seconds.");
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
    public void EnableDisableAccount_UseUAC_Methods()
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
        Assert.That(x3.IsSuccess, Is.True, "[D_110] Failed to retrieve user after adding.");
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

    /// <summary>
    /// Runs thru an entire cycle of creating a user, enabling the account, disabling the account, and re-enabling the account.  Verifies that the account is enabled/disabled at each step.
    /// </summary>
    [Test]
    public void EnableDisableAccount_UseUserConvenience_Methods()
    {
        // A --> Setup

        // Make sure to retrieve the user attributes from AD
        _userProcessor.AttrRetrieval_Office();
        _userProcessor.AttrRetrieval_Default();
        

        // Create random OU;s and a random person
        ADpOrgUnit newOu = asi.CreateRandomOuNew();
        List<TestUserAttr> testUsers = asi.GenerateRandomPerson();
        ADpUser userA = testUsers[0].CreateADpUser(newOu.Path);
        string password = "2026abcdef*";
        userA.Password = password;
        //userA.UserAccountControlSetter.EnableAccount(); // You must have a password set if enabling account.
        userA.EnableAccount();


        // B --> Act 1.
        // Save User
        Result x = _userProcessor.AddNew(userA);
        Assert.That(x.IsSuccess, Is.True, $"[B_100] Failed to add user. {x.ToStringErrorOnly()}");

        var x2 = _userProcessor.Get(userA.DistinguishedName);
        Assert.That(x2.IsSuccess, Is.True, "[B_110] Failed to retrieve user after adding.");
        ADpUser userB = x2.Value;
        Assert.That(userB.IsEnabled, Is.True, "[B_120] User account is not marked as enabled.");

        // C --> Act 2. Disable the account
        userB.DisableAccount();
        x = _userProcessor.Update(userB);
        Assert.That(x.IsSuccess, Is.True, "[C_100] Failed to update user after disabling account.");


        // D --> Retrieve User from AD
        var x3 = _userProcessor.Get(userA.DistinguishedName);
        Assert.That(x3.IsSuccess, Is.True, "[D_110] Failed to retrieve user after adding.");
        ADpUser adUser = x3.Value;
        Assert.That(adUser.IsDisabled, Is.True, "[D_120 ] User account is not marked as disabled.");
        Assert.That(adUser.IsEnabled, Is.False, "[D_130 ] User account is not marked as disabled.");

        // E --> Act 3. Re-enable the account
        adUser.EnableAccount();
        x = _userProcessor.Update(adUser);
        Assert.That(x.IsSuccess, Is.True, "[E_100] Failed to update user after Re-enabling account.");

        // F --> Verify 
        var x4 = _userProcessor.Get(userA.DistinguishedName);
        Assert.That(x4.IsSuccess, Is.True, "[F_100] Failed to retrieve user after adding.");
        ADpUser adUserB = x4.Value;
        Assert.That(adUserB.IsEnabled, Is.True, "[F_110] User account is not marked as enabled.");
        Assert.That(adUserB.IsDisabled, Is.False, "[F_120] User account is not marked as enabled.");
        
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
        for (int i = 0; i < 10; i++)
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

        // E  --> Act 3. Unlock the account
        userB.UnlockAccount();
        Result updateResult = _userProcessor.Update(userB);
        Assert.That(updateResult.IsSuccess, Is.True, "[E_100] Failed to update user after unlocking account.");

        // F --> Verify 
        var x4 = _userProcessor.Get(userA.DistinguishedName);
        Assert.That(x4.IsSuccess, Is.True, "[F_100] Failed to retrieve user.");
        ADpUser adUserB = x4.Value;
        Assert.That(adUserB.MsDsUserAccountControlGetter.IsLockedOut, Is.False, "[F_110] User account is not marked as unlocked.");

        // Delete the user so it doesn't persist in AD
        Result deleteResult = _userProcessor.Delete(userA);
        Assert.That(deleteResult.IsSuccess, Is.True, "[Z_100] Failed to delete user from AD.");
 
    }


    /// <summary>
    /// Tests the AddNew method of the ADpUserProcessor class by creating a new user with a random name and adding it to Active Directory. Verifies that the user was successfully added by retrieving it from AD.
    /// </summary>
    [Test]
    public void AddNew_SimpleMethod()
    {
        // A --> Setup
        // Create random OU;s
        ADpOrgUnit     newOu = asi.CreateRandomOuNew();
        Result<string> x     = _userProcessor.AddNew(asi.Faker.Person.FullName, newOu.Path);

        // Verify
        Result<ADpUser> updatedResult = _userProcessor.Get(x.Value);
        Assert.That(updatedResult.IsSuccess, Is.True, "[V_100] Failed to retrieve updated user.");
    }


    /// <summary>
    ///  Confirms can find User BY UPN - searching sub trees.
    /// </summary>
    [Test]
    public void GetUserBy_UPN()
    {
        // A --> Setup
        string attrValue = "someupn@abc.com";
        _userProcessor.AttrRetrieval_Default();
        
        // Create random OU;s
        ADpOrgUnit newOu = asi.CreateRandomOuNew();

        ADpUser userA = new ADpUser(asi.Faker.Person.FullName, newOu.Path);
        userA.UPN = attrValue;

        // B --> Act - Save User
        Result x = _userProcessor.AddNew(userA);
        Assert.That(x.IsSuccess, Is.True, "[B_100] Failed to add user.");

        // Confirm user exists.
        Result<bool> existsResultAfterAdd = _userProcessor.Exists(userA);
        Assert.That(existsResultAfterAdd.IsSuccess, Is.True, "[B_200] Failed to check if user exists after add.");
        Assert.That(existsResultAfterAdd.Value, Is.True, "[B_210] User was not found after add.");


        // C --> Act - Get User by UPN
        Result<ADpUser> getResult = _userProcessor.GetBy_UPN(attrValue, asi.UnitTestRoot);
        Assert.That(getResult.IsSuccess, Is.True, "[C_100] Failed to get user by UPN.");
        ADpUser userB = getResult.Value;

        // D --> Verify
        Assert.That(userB.UPN, Is.EqualTo(attrValue), "[D_100] User UPN does not match the expected value.");
        Assert.That(ADpUser.EqualSameObject(userA.DistinguishedName,userB.DistinguishedName), Is.True, "[D_110] User distinguished names do not match.");
    }

    
    [Test]
    public void GetUserBy_SAMAccount()
    {
        // A --> Setup
        string attrValue = "CoolUser24";
        _userProcessor.AttrRetrieval_Default();

        // Create random OU;s
        ADpOrgUnit newOu = asi.CreateRandomOuNew();

        ADpUser userA = new ADpUser(asi.Faker.Person.FullName, newOu.Path);
        userA.SAMAccount = attrValue;

        // B --> Act - Save User
        Result x = _userProcessor.AddNew(userA);
        Assert.That(x.IsSuccess, Is.True, "[B_100] Failed to add user.");

        // Confirm user exists.
        Result<bool> existsResultAfterAdd = _userProcessor.Exists(userA);
        Assert.That(existsResultAfterAdd.IsSuccess, Is.True, "[B_200] Failed to check if user exists after add.");
        Assert.That(existsResultAfterAdd.Value, Is.True, "[B_210] User was not found after add.");
        

        // C --> Act - Get User by SAM Account
        Result<ADpUser> getResult = _userProcessor.GetBy_SAMAccount(attrValue, asi.UnitTestRoot);
        Assert.That(getResult.IsSuccess, Is.True, "[C_100] Failed to get user by SAM Account.");
        ADpUser userB = getResult.Value;

        // D --> Verify
        Assert.That(userB.SAMAccount, Is.EqualTo(attrValue), "[D_100] User SAM Account does not match the expected value.");
        Assert.That(ADpUser.EqualSameObject(userA.DistinguishedName, userB.DistinguishedName), Is.True, "[D_110] User distinguished names do not match.");
    }


    [TestCase("givenName", "CoolUser2")]
    [TestCase("sn", "Edwards2")]
    [TestCase("title", "Engineer")]
    [Test]
    public void GetUserBy_Attribute(string attributeName, string attributeValue)
    {
        // A --> Setup
        
        _userProcessor.AttrRetrieval_Default();
        _userProcessor.AttrRetrieval_Office();

        // Create random OU;s
        ADpOrgUnit newOu = asi.CreateRandomOuNew();

        ADpUser userA = new ADpUser(asi.Faker.Person.FullName, newOu.Path);
        if (attributeName == "givenName")
            userA.FirstName = attributeValue;
        else if (attributeName == "sn")
            userA.LastName = attributeValue;
        else if (attributeName == "title")
            userA.Title = attributeValue;

        // B --> Act - Save User
        Result x = _userProcessor.AddNew(userA);
        Assert.That(x.IsSuccess, Is.True, "[B_100] Failed to add user.");

        // Confirm user exists.
        Result<bool> existsResultAfterAdd = _userProcessor.Exists(userA);
        Assert.That(existsResultAfterAdd.IsSuccess, Is.True, "[B_200] Failed to check if user exists after add.");
        Assert.That(existsResultAfterAdd.Value, Is.True, "[B_210] User was not found after add.");


        // C --> Act - Get User by Attribute
        Result<List<ADpUser>> getResult = _userProcessor.GetBy_Attribute(attributeName, attributeValue, asi.UnitTestRoot);
        Assert.That(getResult.IsSuccess, Is.True, "[C_100] Failed to get user by Attribute.");
        List<ADpUser> users = getResult.Value;
        

        // D --> Verify
        Assert.That(users.Count, Is.EqualTo(1), "[D_100] Expected exactly one user to be returned.");
        ADpUser userB = users[0];
        Assert.That(ADpUser.EqualSameObject(userA.DistinguishedName, userB.DistinguishedName), Is.True, "[D_120] User distinguished names do not match.");
    }


    /// <summary>
    /// Performs a Full test of memberOf functionality.
    /// 1 - Can Retrieve current members.
    /// 2 - Can remove user from group and verify memberOf is updated.
    /// 3 - Can add user to group and verify memberOf is updated.
    /// </summary>
    [Test]
    public void MemberOf()
    {
        // A --> Setup
        ADpGroupProcessor groupProcessor = asi.ADConnector.GetGroupProcessor();
        ADpUserProcessor  userProcessor  = asi.ADConnector.GetUserProcessor();
        userProcessor.GroupsRetrievedPerRequest = 3;
        
        
        // Create random OU;s., random person and some groups
        ADpOrgUnit          newOu      = asi.CreateRandomOuNew();
        ADpUser userA = asi.CreateRandomUserNew(newOu.Path);    
        List<TestGroupAttr> testGroups = asi.GenerateRandomGroup(7);
        ADpGroup            groupA     = testGroups[0].CreateADpGroup(newOu.Path);

        
        // B --> Act - Save User
        Result xResult = userProcessor.AddNew(userA);
        Assert.That(xResult.IsSuccess, Is.True, "[B_100] Failed to add user.");

        
        // C --> Act - Save Groups
        List<ADpGroup> groups = new List<ADpGroup>();
        foreach (TestGroupAttr testGroup in testGroups)
        {
            ADpGroup  group = testGroup.CreateADpGroup(newOu.Path);
            groups.Add(group);
            group.AddUserToGroup(userA.DistinguishedName);
            Result x = groupProcessor.AddNew(group);
            Assert.That(x.IsSuccess,Is.True,"[C_100] Failed to add group.");
        }

        // D --> Act - Retrieve the Member of for the user.
        Result getResult = userProcessor.GetMemberOfs(userA);
        Assert.That(getResult.IsSuccess,Is.True,"[D_100] Failed to get member ofs for user.");
        Assert.That(userA.MemberOfGroups.CurrentValues.Count, Is.EqualTo(testGroups.Count), "[D_110] User is not a member of the expected number of groups.");

        // E --> Remove the user from a group and verify the change is reflected in the MemberOf list.
        //ADpUser userB = userProcessor.Get(userA.DistinguishedName).Value;
        userA.RemoveUserFromGroup(groups[0].DistinguishedName);
        
        // Retrieve the group and ensure it has 1 member before removal
        ADpGroup groupB = groupProcessor.Get(groups[0].DistinguishedName).Value;
        groupProcessor.GetMembers(groupB);
        Assert.That(groupB.Members.CurrentValues.Count, Is.EqualTo(1), "[E_100] Group does not have exactly 1 member before removal.");

        // F --> Act - Update the user to reflect the removal from the group.
        Result updateResult = userProcessor.Update(userA);
        Assert.That(updateResult.IsSuccess, Is.True, "[F_100] Failed to update user after removing from group.");

        // G --> Act - Retrieve the Member of for the user again.
        ADpGroup groupC = groupProcessor.Get(groups[0].DistinguishedName).Value;
        groupProcessor.GetMembers(groupC);
        Assert.That(groupC.Members.CurrentValues.Count, Is.EqualTo(0), "[G_100] Group still has members after removing the user   .");

        // H --> Act - add user to group.
        ADpUser userD = userProcessor.Get(userA.DistinguishedName).Value;
        userD.AddUserToGroup(groupC.DistinguishedName);
        Result addResult     = userProcessor.Update(userD);
        Assert.That(addResult.IsSuccess, Is.True, "[H_100] Failed to add user to group.");

        // J --> Act - Retrieve the group and ensure it has 1 member again.
        ADpGroup groupD = groupProcessor.Get(groupC.DistinguishedName).Value;
        groupProcessor.GetMembers(groupD);
        Assert.That(groupD.Members.CurrentValues.Count, Is.EqualTo(1), "[J_100] Group does not have exactly 1 member after adding user back.");

        // K --> Verify the user is now a member of the group again.
        Assert.That( ADpBaseObject.EqualSameObject(groupD.Members.CurrentValues.First(),userA.DistinguishedName), Is.True, "[K_100] User is not a member of the group after being added back.");
    }


    /// <summary>
    /// Test that ambiguous name search returns the correct users when multiple users share a common attribute value.
    /// In this test, we create 6 users, 5 of which share a common name value in different attributes (CN, LastName, FirstName, DisplayName, SAMAccount),
    /// and 1 user who does not share that value. We then search for the common name and verify that only the 5 matching users are returned.
    /// </summary>
    [Test]
    public void AmbiguousNameSearch()
    {
        // A --> Setup
        string commonNameValue = "Smith";
        ADpOrgUnit newOu = asi.CreateRandomOuNew();

        // Create 6 users. 5 who share some common attribute value, and 1 who does not share that value.  Then search for the common value and verify that only the 5 users are returned.
        // User A - CN and name attribute contain the same value "Smith"
        string cnName = commonNameValue + " " + asi.Faker.Person.FirstName;
        ADpUser    userA  = new ADpUser(cnName, newOu);

        string  lastName = commonNameValue;
        ADpUser userB    = new ADpUser("Frank Adams", newOu);
        userB.LastName = lastName;
        
        string firstName = commonNameValue;
        ADpUser userC = new ADpUser("John Jefferson" , newOu);
        userC.FirstName = firstName;
        
        string displayName = commonNameValue;
        ADpUser userD = new ADpUser("Abigail Jones",newOu);
        userD.DisplayName = displayName;

        string samAccount = commonNameValue;
        ADpUser userE = new ADpUser("Albert Einstein",newOu);
        userE.SAMAccount = samAccount;  

        // Now create user who does not have that name anywhere.
        ADpUser userF = new ADpUser("Isaac Newton", newOu);
        userF.DisplayName = "Isaac Newton";
        userF.FirstName   = "Isaac";
        userF.LastName    = "Newton";
        userF.SAMAccount  = "IN20";

        // B --> Act - Add users to AD.
        _userProcessor.AddNew(userA);
        _userProcessor.AddNew(userB);
        _userProcessor.AddNew(userC);
        _userProcessor.AddNew(userD);
        _userProcessor.AddNew(userE);
        _userProcessor.AddNew(userF);

        // For debug purposes , print out the distinguished names of the users added.
        Console.WriteLine($"User A DN: {userA.DistinguishedName}");
        Console.WriteLine($"User B DN: {userB.DistinguishedName}");
        Console.WriteLine($"User C DN: {userC.DistinguishedName}");
        Console.WriteLine($"User D DN: {userD.DistinguishedName}");
        Console.WriteLine($"User E DN: {userE.DistinguishedName}");
        Console.WriteLine($"User F DN: {userF.DistinguishedName}");

        // Verify they all exist
        Result<bool> userAExists = _userProcessor.Exists(userA);
        Result<bool> userBExists = _userProcessor.Exists(userB);
        Result<bool> userCExists = _userProcessor.Exists(userC);
        Result<bool> userDExists = _userProcessor.Exists(userD);
        Result<bool> userEExists = _userProcessor.Exists(userE);
        Result<bool> userFExists = _userProcessor.Exists(userF);

        // B --> Act - Verify they all exist
        Assert.That(userAExists.IsSuccess && userAExists.Value, Is.True, "[B_200] User A did not get added.");
        Assert.That(userBExists.IsSuccess && userBExists.Value, Is.True, "[B_210] User B did not get added.");
        Assert.That(userCExists.IsSuccess && userCExists.Value, Is.True, "[B_220] User C did not get added.");
        Assert.That(userDExists.IsSuccess && userDExists.Value, Is.True, "[B_230] User D did not get added.");
        Assert.That(userEExists.IsSuccess && userEExists.Value, Is.True, "[B_240] User E did not get added.");
        Assert.That(userFExists.IsSuccess && userFExists.Value, Is.True, "[B_250] User F did not get added.");

        // C --> Act - Search for ambiguous name
        Result<List<ADpUser>> searchResult = _userProcessor.FindByAmbiguosNameResolution(commonNameValue, newOu);
        Assert.That(searchResult.IsSuccess, Is.True, "[C_100] Failed to search for ambiguous name.");

        // D --> Verify We have results
        Assert.That(searchResult.Value, Is.Not.Null, "[D_100] Search result should not be null.");
        Assert.That(searchResult.Value.Count, Is.EqualTo(5), "[D_110] Should have found 5 users matching the ambiguous name.");

        // E --> Ensure the non-matching user is not in the list
        bool foundUserF = searchResult.Value.Any(u => u.DistinguishedName == userF.DistinguishedName);
        Assert.That(foundUserF, Is.False, "[E_120] User F (Isaac Newton) should not be in the ambiguous search results.");

        // F --> Ensure the matching users are in the list
        bool foundUserA = searchResult.Value.Any(u => u.DistinguishedName == userA.DistinguishedName);
        Assert.That(foundUserA, Is.True, "[F_130] User A (Smith [FirstName]) should be in the ambiguous search results.");

        bool foundUserB = searchResult.Value.Any(u => u.DistinguishedName == userB.DistinguishedName);
        Assert.That(foundUserB, Is.True, "[F_140] User B (Frank Adams, LastName=Smith) should be in the ambiguous search results.");

        bool foundUserC = searchResult.Value.Any(u => u.DistinguishedName == userC.DistinguishedName);
        Assert.That(foundUserC, Is.True, "[F_150] User C (John Jefferson, FirstName=Smith) should be in the ambiguous search results.");

        bool foundUserD = searchResult.Value.Any(u => u.DistinguishedName == userD.DistinguishedName);
        Assert.That(foundUserD, Is.True, "[F_160] User D (Abigail Jones, DisplayName=Smith) should be in the ambiguous search results.");

        bool foundUserE = searchResult.Value.Any(u => u.DistinguishedName == userE.DistinguishedName);
        Assert.That(foundUserE, Is.True, "[F_170] User E (Albert Einstein, SAMAccount=Smith) should be in the ambiguous search results.");

        // Z --> Cleanup
        _userProcessor.Delete(userA);
        _userProcessor.Delete(userB);
        _userProcessor.Delete(userC);
        _userProcessor.Delete(userD);
        _userProcessor.Delete(userE);
        _userProcessor.Delete(userF);
    }

}

