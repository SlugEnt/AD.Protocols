using Bogus.DataSets;
using SlugEnt.AD.Protocols.Attributes;
using SlugEnt.AD.Protocols;
using System.DirectoryServices.Protocols;
using System.Text;
using AD.Protocols.ADObjects;
using UT.CustomSupportObjects;
using UT.SupportObjects;
using SlugEnt.FluentResults;

namespace UT.ActiveDirectory_Tests;

[TestFixture]
public class AD_User_Tests
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
    /// Confirms we can change a user's password.
    /// </summary>
    [Test]
    public void Password_CanChange()
    {
        // A --> Setup        
        // Create a random userFromAdRo OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu();
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());

        // Create Test User Basic
        TstUserBasic testUser = new("TestBasicUser Creation", newOuResult.Value, asi.Faker);
        testUser.CreateUser(asi.ADConnector);

        // C.  Retrieve the User
        // Read the user back to ensure it was created
        ADpUserFromAD_RO?        userFromAD;
        Result<ADpUserFromAD_RO> foundUser = asi.ADConnector.UserGetByDn(asi.UnitTestParent.Path, testUser.User.DistinquishedName, SearchScope.Subtree);

        Assert.That(foundUser.IsSuccess, Is.True, "C-100:  Failed to find TestUser --> AppError: " + foundUser.ToStringWithLineFeeds());
        userFromAD = foundUser.Value;
        HelperMethods.DisplayUser(userFromAD);

        UserAccountControlManager uacFirst = new(userFromAD.UserAccountControl);

        // Change Password
        string pass = "Password1!";
        byte[] ss = Encoding.Unicode.GetBytes("\"" + pass + "\"");

        AttrUserPassword password = new(ss, EnumAttributeOperation.Modify);
        List<AttributeBase> attributes2 =
        [
            password
        ];


        Result<ModifyResponse> changePasswordResult = asi.ADConnector.UserUpdate(userFromAD.DistinguishedName, [.. attributes2]);
        Assert.That(changePasswordResult.IsSuccess, Is.True, "Z-400:  FAILED: " + changePasswordResult.ToStringWithLineFeeds());

        uacFirst.RemoveFlag(EnumUserAccountControlFlags.PASSWD_NOTREQD);
    }


    /// <summary>
    ///     Uses the simplified UserUpdate method to update a user's properties.  Tests the majority of the user properties as
    ///     well
    ///     as password.
    /// </summary>
    [Test]
    public void UserChangeFullTest()
    {
        // A --> Setup

        // B. More Setup
        // Create a random userFromAdRo OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu();
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());

        // Create Test User Basic
        TstUserBasic testUser = new("TestBasicUser Creation", newOuResult.Value, asi.Faker);
        testUser.CreateUser(asi.ADConnector);


        // C.  Retrieve the User
        // Read the user back to ensure it was created
        ADpUserFromAD_RO?        userFromAD;
        Result<ADpUserFromAD_RO> foundUser = asi.ADConnector.UserGetByDn(asi.UnitTestParent.Path, testUser.User.DistinquishedName, SearchScope.Subtree);

        Assert.That(foundUser.IsSuccess, Is.True, "C-100:  Failed to find TestUser --> AppError: " + foundUser.ToStringWithLineFeeds());
        userFromAD = foundUser.Value;
        HelperMethods.DisplayUser(userFromAD);


        // D. Change all of the common userFromAdRo properties.
        Name newRandomName = asi.Faker.Name;
        ADpUserEditable userEditable = new(userFromAD)
        {
            PasswordChg = "Password23!",
            TitleChg = newRandomName.JobTitle(),
            PhoneChg = asi.Faker.Phone.PhoneNumber(),
            FirstNameChg = newRandomName.FirstName(),
            LastNameChg = newRandomName.LastName(),
            DisplayNameChg = newRandomName.FullName(),
            IsDisabled = false,
            DepartmentFullNameChg = newRandomName.JobArea(),
        };
        userEditable.EmailChg = asi.Faker.Internet.Email(userEditable.FirstNameChg, userEditable.LastNameChg);
        Result<ModifyResponse> ruu = asi.ADConnector.UserUpdate(userEditable);

        Assert.That(ruu.IsSuccess, Is.True, "C-100:  User update failed - " + ruu.ToStringWithLineFeeds());


        // E.  Re-read userFromAdRo to confirm Title change
        Result<ADpUserFromAD_RO> updatedUser = asi.ADConnector.UserGetByDn(asi.UnitTestParent.Path, testUser.User.DistinquishedName, SearchScope.Subtree);
        Assert.That(updatedUser.IsSuccess, Is.True, "E-100:  Failed to find TestUser --> AppError: " + foundUser.ToStringWithLineFeeds());

        // Z. Validate the changes
        Assert.That(foundUser.IsSuccess, Is.True, "Z-100:  Failed to find TestUser --> AppError: " + foundUser.ToStringWithLineFeeds());
        userFromAD = updatedUser.Value;
        Assert.That(userFromAD.Title, Is.EqualTo(userEditable.TitleChg), "Z-200:  Title was not updated to correct value");
        Assert.That(userFromAD.Phone, Is.EqualTo(userEditable.PhoneChg), "Z-210:  Phone was not updated to correct value");
        Assert.That(userFromAD.Email, Is.EqualTo(userEditable.EmailChg), "Z-220:  Email was not updated to correct value");
        Assert.That(userFromAD.FirstName, Is.EqualTo(userEditable.FirstNameChg), "Z-230:  First Name was not updated to correct value");
        Assert.That(userFromAD.LastName, Is.EqualTo(userEditable.LastNameChg), "Z-240:  Last Name was not updated to correct value");
        Assert.That(userFromAD.DepartmentFullName, Is.EqualTo(userEditable.DepartmentFullNameChg), "Z-250:  Department was not updated to correct value");
        Assert.That(userFromAD.DisplayName, Is.EqualTo(userEditable.DisplayNameChg), "Z-260:  Display Name was not updated to correct value");
        Assert.That(userFromAD.IsDisabled, Is.False, "Z-270: ");
    }


    [Test]
    public void DeleteUser()
    {
        // A --> Setup

        // Create a random userFromAdRo OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu();
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());

        // Create Test User Basic
        TstUserBasic testUser = new("TestBasicUser Creation", newOuResult.Value, asi.Faker);
        testUser.CreateUser(asi.ADConnector);

        // C.  Retrieve the User

        // Read the user back to ensure it was created
        ADpUserFromAD_RO?        userFromAD;
        Result<ADpUserFromAD_RO> foundUser = asi.ADConnector.UserGetByDn(asi.UnitTestParent.Path, testUser.User.DistinquishedName, SearchScope.Subtree);

        Assert.That(foundUser.IsSuccess, Is.True, "C-100:  Failed to find TestUser --> AppError: " + foundUser.ToStringWithLineFeeds());
        userFromAD = foundUser.Value;
        HelperMethods.DisplayUser(userFromAD);

        
        // D. Delete the User
        Result<DeleteResponse> deleteResult = asi.ADConnector.UserDelete(userFromAD.DistinguishedName);
        Assert.That(deleteResult.IsSuccess, Is.True, "D-100:  User delete failed - " + deleteResult.ToStringWithLineFeeds());


        // Z.  Validate
        ADpUserFromAD_RO?        notFoundUser;
        Result<ADpUserFromAD_RO> foundResult = asi.ADConnector.UserGetByDn(asi.UnitTestParent.Path, testUser.User.DistinquishedName, SearchScope.Subtree);

        Assert.That(foundResult.IsSuccess, Is.False, "Z-100:  Found user after Deletion.  Deletion failed" );
    }






    [Test]
    public void RenameUser()
    {
        // A --> Setup

        // Create a random userFromAdRo OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu();
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());


        // Create Test User Basic
        TstUserBasic testUser = new("TestBasicUser Creation", newOuResult.Value, asi.Faker);
        testUser.CreateUser(asi.ADConnector);


        // B. Read the userFromAdRo back to verify successful creation
        // Part B - Retrieve the User
        string searchFilter = ActiveDirectoryConnector.SEARCH_FILTER_ALL_USERS;
        List<string> attributes = [];
        ADpUserFromAD_RO.AddInfoAttributes(attributes);
        ADpUserFromAD_RO.AddBaseAttributes(attributes);
        Result<ADpUserFromAD_RO> foundUser = asi.ADConnector.UserFindSingleUser(newOuResult.Value.Path,
                                                                        SearchScope.OneLevel,
                                                                        searchFilter,
                                                                        attributes);

        Assert.That(foundUser.IsSuccess, Is.True, "Z-100:  Failed to find TestUser --> AppError: " + foundUser.ToStringWithLineFeeds());
        ADpUserFromAD_RO userFromAdRoFromAd = foundUser.Value;
        HelperMethods.DisplayUser(userFromAdRoFromAd);


        // C. Rename the User
        Result<string> renameResult = asi.ADConnector.UserRename(userFromAdRoFromAd, "xyz User");
        Assert.That(renameResult.IsSuccess, Is.True, "C-100:  User rename failed - " + renameResult.ToStringWithLineFeeds());
        Console.WriteLine("Renamed userFromAdRo to : " + renameResult.Value);


        // D .  Verify the userFromAdRo is Moved.  First confirm not in old location
        foundUser = asi.ADConnector.UserFindSingleUser(newOuResult.Value.Path,
                                                SearchScope.OneLevel,
                                                searchFilter,
                                                attributes);

        Assert.That(foundUser.IsSuccess, Is.True, "D-100:  User was not found under new name. --> AppError: " + foundUser.ToStringWithLineFeeds());
    }



    /// <summary>
    ///     Tests the Find Single User method
    /// </summary>
    [Test]
    public void FindSingleUser()
    {
        // A --> Setup

        // Create a random user OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu();
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());

        // Create Test User Basic
        TstUserBasic testUser = new("TestBasicUser Creation", newOuResult.Value, asi.Faker);
        testUser.CreateUser(asi.ADConnector);


        // Part 2
        ADpUserFromAD_RO? userFromAD;
        List<string> attributes = [];
        ADpUserFromAD_RO.AddBaseAttributes(attributes);
        ADpUserFromAD_RO.AddInfoAttributes(attributes);
        ADpUserFromAD_RO.AddStatisticAttributes(attributes);
        ADpUserFromAD_RO.AddPasswordAttributes(attributes);

        string searchFilter = "(&(objectClass=user)(objectCategory=person))";
        Result<ADpUserFromAD_RO> foundUser = asi.ADConnector.UserFindSingleUser(newOuResult.Value.Path,
                                                                        SearchScope.OneLevel,
                                                                        searchFilter,
                                                                        attributes);

        Assert.That(foundUser.IsSuccess, Is.True, "Z-100:  Failed to find TestUser --> AppError: " + foundUser.ToStringWithLineFeeds());
        userFromAD = foundUser.Value;
        HelperMethods.DisplayUser(userFromAD);

        Assert.That(testUser.ValidateUserDefault(userFromAD).IsSuccess, Is.True, "Z-300:  Test User failed to validate successfully.");
    }


    /// <summary>
    ///     Tests the Find Single User method
    /// </summary>
    [Test]
    public void UserGetByCommonName()
    {
        // A --> Setup

        // Create Test User Basic
        TstUserBasic testUser = new("TestBasicUser Creation", asi.UnitTestParent, asi.Faker);
        testUser.CreateUser(asi.ADConnector);


        // Part Z
        ADpUserFromAD_RO? userFromAD;
        Result<ADpUserFromAD_RO> foundUser = asi.ADConnector.UserGetByCn(asi.UnitTestParent.Path, testUser.User.CommonNameChg, SearchScope.OneLevel);

        Assert.That(foundUser.IsSuccess, Is.True, "Z-100:  Failed to find TestUser --> AppError: " + foundUser.ToStringWithLineFeeds());
        userFromAD = foundUser.Value;
        HelperMethods.DisplayUser(userFromAD);

        Assert.That(testUser.ValidateUserDefault(userFromAD, true).IsSuccess, Is.True, "Z-300:  Test User failed to validate successfully.");
    }



    /// <summary>
    ///     Tests the Find Single User method
    /// </summary>
    [Test]
    [TestCase("UPN")]
    [TestCase("SAM")]
    [TestCase("CN")]
    [TestCase("DN")]
    public void UserGetByAttribute(string attributeToRetrieveBy)
    {
        // A --> Setup

        // Create Test User Basic
        TstUserBasic testUser = new("TestBasicUser Creation", asi.UnitTestParent, asi.Faker);
        testUser.CreateUser(asi.ADConnector);


        // Part Z
        ADpUserFromAD_RO? userFromAD;
        Result<ADpUserFromAD_RO> foundUser = null;

        string attributeValue = "";
        if (attributeToRetrieveBy == "UPN")
        {
            attributeValue = testUser.User.UPN;
            foundUser = asi.ADConnector.UserGetByUPN(asi.UnitTestParent.Path, attributeValue, SearchScope.OneLevel);
        }
        else if (attributeToRetrieveBy == "CN")
        {
            attributeValue = testUser.User.CommonNameChg;
            foundUser = asi.ADConnector.UserGetByCn(asi.UnitTestParent.Path, attributeValue, SearchScope.OneLevel);
        }
        else if (attributeToRetrieveBy == "DN")
        {
            attributeValue = testUser.User.DistinquishedName;
            foundUser = asi.ADConnector.UserGetByDn(asi.UnitTestParent.Path, attributeValue, SearchScope.OneLevel);
        }
        else if (attributeToRetrieveBy == "SAM")
        {
            attributeValue = testUser.User.SAMAccount;
            foundUser = asi.ADConnector.UserGetBySAMAccount(asi.UnitTestParent.Path, attributeValue, SearchScope.OneLevel);
        }



        Assert.That(foundUser.IsSuccess, Is.True, $"Z-100:  Failed to find TestUser using Attribute [ {attributeToRetrieveBy} User: {testUser.DistinguishedName} --> AppError: " + foundUser.ToStringErrorOnly());
        userFromAD = foundUser.Value;
        HelperMethods.DisplayUser(userFromAD);

        Assert.That(testUser.ValidateUserDefault(userFromAD, true).IsSuccess, Is.True, "Z-300:  Test User failed to validate successfully.");
    }



    /// <summary>
    ///     Tests the Find Single User method
    /// </summary>
    [Test]
    public void FindMultipleUsers()
    {
        // A --> Setup
 
        // Create a random user OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu();
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());

        // Create Test User Basic
        TstUserBasic testUser = new("TestMultiple User 1", newOuResult.Value, asi.Faker);
        testUser.CreateUser(asi.ADConnector);

        TstUserBasic testUser2 = new("TestMultiple User 2", newOuResult.Value, asi.Faker);
        testUser2.CreateUser(asi.ADConnector);

        TstUserBasic testUser3 = new("TestMultiple User 3", newOuResult.Value, asi.Faker);
        testUser3.CreateUser(asi.ADConnector);


        // Part 2
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




    /// <summary>
    ///     Makes sure we can add and read the basic info from the user.
    /// </summary>
    [Test]
    public void UserAddWithAttributes2()
    {
        // A --> Setup
  
        // B.  More Setup
        // Create a random user OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu(asi.UnitTestParent);
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringErrorOnly());

        // Create Test User Basic
        TstUserBasic testUser = new("TestBasicUser Creation", newOuResult.Value, asi.Faker);
        testUser.CreateUser(asi.ADConnector);


        // C.  Retrieve the User

        // Read the user back to ensure it was created
        ADpUserFromAD_RO?        userFromAD;
        Result<ADpUserFromAD_RO> foundUser = asi.ADConnector.UserGetByDn(asi.UnitTestParent.Path, testUser.User.DistinquishedName, SearchScope.Subtree);

        Assert.That(foundUser.IsSuccess, Is.True, "C-100:  Failed to find TestUser --> AppError: " + foundUser.ToStringWithLineFeeds());
        userFromAD = foundUser.Value;
        HelperMethods.DisplayUser(userFromAD);
        
        Assert.That(testUser.ValidateUserDefault(userFromAD).IsSuccess, Is.True, "Z-300:  Test User failed to validate successfully.");
    }



    /// <summary>
    ///     Tests that we can enable or disable user when new or during update
    /// </summary>
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SetUserEnablement(bool enabledStatus)
    {
        // A --> Setup
  

        // Create a random user OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu(asi.UnitTestParent);
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringErrorOnly());


        // B.  Create Test User Basic
        TstUserBasic testUser = new("TestBasicUser Creation", newOuResult.Value, asi.Faker);
        testUser.User.IsDisabled = enabledStatus;
        testUser.CreateUser(asi.ADConnector);

        List<string> attributesToRetrieve = new();
        ADpUserFromAD_RO.AddAllAttributes(attributesToRetrieve);


        // C.  Retrieve created user
        Result<ADpUserFromAD_RO> getUserResult = asi.ADConnector.UserGetByDn(newOuResult.Value.Path, testUser.User.DistinquishedName, searchScope: SearchScope.Subtree, attributesToRetrieve);
        Assert.That(getUserResult.IsSuccess, Is.True, "C-100:");
        Assert.That(getUserResult.Value, Is.Not.Null, "C-200:");
        ADpUserFromAD_RO userFromAD = getUserResult.Value;

        HelperMethods.DisplayUser(userFromAD);
        Assert.That(userFromAD.IsDisabled, Is.EqualTo(testUser.User.IsDisabled), "Z-300:  IsDisabled status not set to correct value");
        Assert.That(testUser.ValidateUserDefault(userFromAD, true).IsSuccess, Is.True, "Z-300:  Test User failed to validate successfully.");
    }



    /// <summary>
    ///     Tests that we can set password to never expires
    /// </summary>
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SetUserPasswordNeverExpires(bool enabledStatus)
    {
        // A --> Setup


        // Create a random user OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu(asi.UnitTestParent);
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringErrorOnly());


        // B.  Create Test User Basic
        TstUserBasic testUser = new("TestBasicUser Creation", newOuResult.Value, asi.Faker);
        testUser.User.IsPasswordSetToNeverExpires = enabledStatus;
        testUser.CreateUser(asi.ADConnector);

        List<string> attributesToRetrieve = new();
        ADpUserFromAD_RO.AddAllAttributes(attributesToRetrieve);


        // C.  Retrieve created user
        Result<ADpUserFromAD_RO> getUserResult = asi.ADConnector.UserGetByDn(newOuResult.Value.Path,
                                                                          testUser.User.DistinquishedName,
                                                                          searchScope: SearchScope.Subtree,
                                                                          attributesToRetrieve);
        Assert.That(getUserResult.IsSuccess, Is.True, $"C-100:  Error: {getUserResult.ToStringErrorOnly()}");
        Assert.That(getUserResult.Value, Is.Not.Null, "C-200:  Value was null, should have been set to something.");
        ADpUserFromAD_RO userFromAD = getUserResult.Value;

        HelperMethods.DisplayUser(userFromAD);
        Assert.That(userFromAD.IsPasswordSetToNeverExpire, Is.EqualTo(testUser.User.IsPasswordSetToNeverExpires), "Z-300:  IsPasswordSetToNeverExpires not set to correct value");
        Assert.That(testUser.ValidateUserDefault(userFromAD, true).IsSuccess, Is.True, "Z-300:  Test User failed to validate successfully.");
    }



    /// <summary>
    ///     Validates the ability to update a user
    /// </summary>
    [Test]
    public void UserUpdate()
    {
        // A --> Setup
 
        // B.  More setup
        // Create a random user OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu();
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());

        // C.  Create Test User Basic
        TstUserBasic testUser = new("Update User Test", newOuResult.Value, asi.Faker);
        testUser.CreateUser(asi.ADConnector);

        // Read the user back to ensure it was created
        ADpUserFromAD_RO?        userFromAD;
        Result<ADpUserFromAD_RO> foundUser = asi.ADConnector.UserGetByDn(asi.UnitTestParent.Path, testUser.User.DistinquishedName, SearchScope.Subtree);

        Assert.That(foundUser.IsSuccess, Is.True, "C-100:  Failed to find TestUser --> AppError: " + foundUser.ToStringWithLineFeeds());
        userFromAD = foundUser.Value;
        HelperMethods.DisplayUser(userFromAD);


        // D. - Update the User
        //AttributeBase[] attributesToUpdate = new AttributeBase[20];

        // Change their Email, Title and work phone. Generate a new person to get a unique email
        AttrEmail chgEmail = new(asi.Faker.Internet.Email(), EnumAttributeOperation.Modify);
        AttrTitle chgTitle = new(asi.Faker.Name.JobTitle(), EnumAttributeOperation.Modify);
        AttrWorkPhone chgWorkPhone = new(asi.Faker.Phone.PhoneNumber(), EnumAttributeOperation.Modify);

        Result<ModifyResponse> modifyResult = asi.ADConnector.UserUpdate(userFromAD!.DistinguishedName,
                                                                  [
                                                                      chgEmail, chgTitle, chgWorkPhone
                                                                  ]);
        Assert.That(modifyResult.IsSuccess, Is.True, "D-100:  Failed to update the user. " + modifyResult.ToStringWithLineFeeds());


        // Z.  Validate the changes.
        ADpUserFromAD_RO? changedUserFromAD = null;
        Result<SearchResponse> changeResponse = testUser.ExecuteSearch(asi.ADConnector, newOuResult.Value);

        SearchResponse srChange = changeResponse.Value;
        Console.WriteLine("Found {0} users matching criteria", srChange.Entries.Count);

        foreach (SearchResultEntry searchResultEntry in srChange.Entries)
        {
            Result<ADpUserFromAD_RO> userResult = ADpUserFromAD_RO.CreateUserObj(searchResultEntry.Attributes);
            Assert.That(userResult.IsSuccess, Is.True, "Z-500:  Failed to retrieve user object.  AppError: " + userResult.ToStringWithLineFeeds());
            changedUserFromAD = userResult.Value;
        }

        Assert.That(changedUserFromAD, Is.Not.Null, "Z-600:  Variable changedUserFromAD is null");
        Assert.That(changedUserFromAD.Email, Is.EqualTo(chgEmail.Value), "z-610:  email was not updated");
        Assert.That(changedUserFromAD.Title, Is.EqualTo(chgTitle.Value), "z-620:  Title was not updated");
        Assert.That(changedUserFromAD.Phone, Is.EqualTo(chgWorkPhone.Value), "z-650:  Work Phone was not updated");

        Assert.That(changedUserFromAD.Email, Is.Not.EqualTo(userFromAD.Email), "Z-660:  Email was not changed.");
    }





#pragma warning restore NUnit2045
#pragma warning restore IDE0079
}

