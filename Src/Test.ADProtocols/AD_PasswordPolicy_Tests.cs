using FluentResults.Reasons;
using SlugEnt.FluentResults;

using SlugEnt.AD.Protocols;
using SlugEnt.IS;
using System.DirectoryServices.Protocols;
using UT.CustomSupportObjects;
using UT.SupportObjects;

namespace UT.ActiveDirectory_Tests;

/// <summary>
/// Tests Active Directory Password Policy related actions.
/// </summary>
///
[TestFixture]
public class AD_PasswordPolicy_Tests
{
#pragma warning disable IDE0079
#pragma warning disable NUnit2045
    public string _testPolicyPrefix = "__";


    private Ad_SupportInitializer asi;


    [SetUp]
    public void Setup()
    {
        asi = Ad_SupportInitializer.GetInitializer();
        asi.Initialize();
    }

    /// <summary>
    /// Called before and after the start of all tests and end of all tests, to remove any password policies created during unit tests.
    /// </summary>
    [OneTimeSetUp]
    [OneTimeTearDown]
    public void Delete_Cleanup()
    {
        // Remove any test Password Policies that may have been created during the tests.
        Ad_SupportInitializer tempAsi = Ad_SupportInitializer.GetInitializer();
        tempAsi.Initialize();
        
        // Get list of policies
        Result<List<ADpReadOnlyPasswordPolicy>> result = tempAsi.ADConnector.PasswordPolicyFindOneOrMore(_testPolicyPrefix);
        if (!result.IsSuccess)
            if (result.ReasonCode != EnumReasonCode.NotFound)
                Assert.That(result.IsSuccess,Is.True,$"OneTime: Unexpected error during OneTime Setup/Teardown.  {result.ToStringErrorOnly()} ");

        // None found.  Nothing to do.
        if (!result.IsSuccess)
            return;

        // Delete them.
        foreach (ADpReadOnlyPasswordPolicy aDpReadOnlyPasswordPolicy in result.Value)
        {
            Result deleteResult =  tempAsi.ADConnector.PasswordPolicyDelete(aDpReadOnlyPasswordPolicy.DistinguishedName);
            Assert.That(deleteResult.IsSuccess,Is.True,$"OneTime:  Failed to delete policy: {aDpReadOnlyPasswordPolicy.DistinguishedName}.  {deleteResult.ToStringForPrint()}");
        }
    }




    [Test]
    public void FindPasswordPolicy()
    {
        // A --> Setup
        
        ADSPath               policyOu = asi.ADConnector.GetPasswordPolicyOU();


        // B. Create some policies
        // B1. 
        string                   methodPrefix = _testPolicyPrefix + "find_";
        string policyName = methodPrefix + asi.Faker.Commerce.ProductName();
        ADpPasswordPolicyUpdater testPolicy   = CreateTestPasswordPolicy(policyName);
//        testPolicy.NameChg = namePrefix + testPolicy.NameChg;
        Result                   resultB    = asi.ADConnector.PasswordPolicyAdd(testPolicy);
        Assert.That(resultB.IsSuccess, Is.True, "B-120: Failed to add Password Policy.  Errors: " + resultB.ToStringErrorOnly());

        // B2. 
        testPolicy.NameChg = testPolicy.NameChg + "2";
        resultB = asi.ADConnector.PasswordPolicyAdd(testPolicy);
        Assert.That(resultB.IsSuccess, Is.True, "B-130: Failed to add Password Policy.  Errors: " + resultB.ToStringErrorOnly());

        // B3. 
        testPolicy.NameChg = testPolicy.NameChg + "3";
        resultB            = asi.ADConnector.PasswordPolicyAdd(testPolicy);
        Assert.That(resultB.IsSuccess, Is.True, "B-140: Failed to add Password Policy.  Errors: " + resultB.ToStringErrorOnly());
        

        string searchFilter = $"(objectClass=msDS-PasswordSettings)(cn={methodPrefix}*)"; 
        List<string> attributes   = [];
        ADpReadOnlyPasswordPolicy.AddBaseAttributes(attributes);

        Result<List<ADpReadOnlyPasswordPolicy>> resultF = asi.ADConnector.PasswordPolicyFindOneOrMore(policyName);
        Assert.That(resultF.IsSuccess, Is.True, "Z-100:  Failed to find Password Policy --> AppError: " + resultF.ToStringErrorOnly());
        Assert.That(resultF.Value.Count, Is.EqualTo(3),"Z-120");
    }



    [Test]
    public void UpdatePasswordPolicy()
    {
        // A --> Setup
        ADSPath               policyOu     = asi.ADConnector.GetPasswordPolicyOU();
        string                methodPrefix = _testPolicyPrefix + "update_";
        string                policyName   = methodPrefix + asi.Faker.Commerce.ProductName();


        // B.  More Setup
        ADpPasswordPolicyUpdater testPolicy = CreateTestPasswordPolicy(policyName);
        Result resultB = asi.ADConnector.PasswordPolicyAdd(testPolicy);
        Assert.That(resultB.IsSuccess, Is.True, "B-120: Failed to add Password Policy.  Errors: " + resultB.ToStringErrorOnly());


        // C. --> Verify Policy was created
        Result<ADpReadOnlyPasswordPolicy> resultC = ReadAndVerifyPasswordPolicy(asi.ADConnector,
                                                                                [],
                                                                                testPolicy.NameChg);
        Assert.That(resultC.IsSuccess, Is.True, "C-100:  Failed to find Password Policy --> AppError: " + resultC.ToStringErrorOnly());
        ADpReadOnlyPasswordPolicy policy = resultC.Value;



        // D. Create Password Policy Update Object
        ADpPasswordPolicyUpdater currentPolicy = new(policy);


        // E.  Update some fields
        currentPolicy.DisplayNameChg    = "Updated " + asi.Faker.Commerce.ProductName();
        currentPolicy.DescriptionChg    = "Updated " + asi.Faker.Lorem.Sentence(5);
        currentPolicy.HistoryCount = 46;

        currentPolicy.MaximumAge = new TimeSpan(116,
                                                0,
                                                50,
                                                0);
        currentPolicy.MinimumAge = new TimeSpan(5,
                                                0,
                                                10,
                                                0);
        currentPolicy.LockOutThreshold = 13;
        currentPolicy.MinimumLength    = 17;
        currentPolicy.AppliesTo.Add($"CN=Domain Users,CN=Users,{asi.DomainInLDAPStyle()}");
        currentPolicy.AppliesToSetFlag   = true; 
        currentPolicy.SettingsPrecedence = 19;
        currentPolicy.LockoutObservationWindow = new TimeSpan(0,
                                                              1,
                                                              0,
                                                              0);

        // M.  Update the Password Policy
        Result<ModifyResponse> resultM = asi.ADConnector.PasswordPolicyUpdate(currentPolicy);
        Assert.That(resultM.IsSuccess, Is.True, "M-100:  Failed to update Password Policy --> AppError: " + resultM.ToStringErrorOnly());


        // Z. Read it back and validate changes were made.
        Result<ADpReadOnlyPasswordPolicy> resultZ = ReadAndVerifyPasswordPolicy(asi.ADConnector,
                                                                                [],
                                                                                policyName);
        Assert.That(resultZ.IsSuccess, Is.True, "C-100:  Failed to find Password Policy --> AppError: " + resultZ.ToStringErrorOnly());
        ADpReadOnlyPasswordPolicy updatedPolicy = resultZ.Value;


        // Validate the changes were made.
        Assert.That(updatedPolicy.DisplayName, Is.EqualTo(currentPolicy.DisplayNameChg), "Z-200:  Display Name is not correct");
        Assert.That(updatedPolicy.Description, Is.EqualTo(currentPolicy.DescriptionChg), "Z-300:  Description is not correct");
        Assert.That(updatedPolicy.PasswordMinAge, Is.EqualTo(currentPolicy.MinimumAge), "Z-400:  Password Minimum Age is not correct");
        Assert.That(updatedPolicy.PasswordMaxAge, Is.EqualTo(currentPolicy.MaximumAge), "Z-500:  Password Maximum Age is not correct");
        Assert.That(updatedPolicy.PasswordMinLength, Is.EqualTo(currentPolicy.MinimumLength), "Z-600:  Password Minimum Length is not correct");
        Assert.That(updatedPolicy.PasswordHistoryLength, Is.EqualTo(currentPolicy.HistoryCount), "Z-700:  Password History Length is not correct");
        Assert.That(updatedPolicy.PasswordLockThreshold, Is.EqualTo(currentPolicy.LockOutThreshold), "Z-800:  Password Lockout Threshold is not correct");

        //Assert.That(policy.PasswordLockDuration, Is.EqualTo(testPolicy.LockoutDuration), "Z-900:  Password Lockout Duration is not correct");
        Assert.That(updatedPolicy.PasswordLockoutObservationWindow, Is.EqualTo(currentPolicy.LockoutObservationWindow), "Z-1000:  Password Lockout Observation Window is not correct");
        Assert.That(updatedPolicy.PasswordComplexityEnabled, Is.EqualTo(currentPolicy.ComplexityEnabled), "Z-1100:  Password Complexity Enabled is not correct");
        Assert.That(updatedPolicy.PasswordReversibleEncryptionEnabled, Is.EqualTo(currentPolicy.ReversibleEncryptionEnabled), "Z-1200:  Reversible Encryption Enabled is not correct");
        Assert.That(updatedPolicy.PasswordSettingsPrecedence, Is.EqualTo(currentPolicy.SettingsPrecedence), "Z-1300:  Password Settings Precedence is not correct");
        Assert.That(updatedPolicy.AppliesTo.Count, Is.EqualTo(3), "Z-1400:  Applies to did not get updated correctly.");

    }

    
    /// <summary>
    /// Creates a TestPolicy that can be sent to 
    /// </summary>
    /// <param name="policyName"></param>
    /// <returns></returns>
    private ADpPasswordPolicyUpdater CreateTestPasswordPolicy(string policyName)
    {
        // Create a paassword policy
        string policyDesc       = asi.Faker.Lorem.Sentence(5);
        int    minAge           = asi.Faker.Random.Int(1, 60);
        int    maxAge           = asi.Faker.Random.Int(minAge, 100);
        int    minLength        = asi.Faker.Random.Int(9, 13);
        int    lockoutThreshold = asi.Faker.Random.Int(3, 30);
        int    history          = asi.Faker.Random.Int(7, 30);
        bool   complexity       = asi.Faker.Random.Bool();
        bool   encrypted        = asi.Faker.Random.Bool();
        int    precedence       = asi.Faker.Random.Int(3, 22);
        int    lockoutDuration  = asi.Faker.Random.Int(20, 6000); // in minutes
        int    lockoutWindow    = asi.Faker.Random.Int(3, 600); // in minutes
        

        ADpPasswordPolicyUpdater testPolicy = new(policyName);
        testPolicy.MinimumAge = new TimeSpan(minAge,
                                             0,
                                             0,
                                             0);
        testPolicy.MaximumAge = new TimeSpan(maxAge,
                                             0,
                                             0,
                                             0);
        testPolicy.MinimumLength               = minLength;
        testPolicy.HistoryCount                = history;
        testPolicy.ComplexityEnabled           = complexity;
        testPolicy.ReversibleEncryptionEnabled = encrypted;
        testPolicy.LockOutThreshold            = lockoutThreshold;
        testPolicy.LockoutDuration = new TimeSpan(0,
                                                  0,
                                                  lockoutDuration,
                                                  0);
        testPolicy.LockoutObservationWindow = new TimeSpan(0,
                                                           0,
                                                           lockoutWindow,
                                                           0); 

        testPolicy.DescriptionChg = policyDesc;

        testPolicy.AppliesTo = new List<string>
        {
            $"CN=Guest,CN=Users,{asi.DomainInLDAPStyle()}",
            $"CN=Domain Guests,CN=Users,{asi.DomainInLDAPStyle()}",
        }; // This is a default value, can be changed later.
        return testPolicy;
    }



    /// <summary>
    /// Tests Creating a new group.
    /// </summary>
    [Test]
    public void PasswordPolicyAdd()
    {
        // A --> Setup
        
        string  methodPrefix = _testPolicyPrefix + "update_";
        string  policyName   = methodPrefix + asi.Faker.Commerce.ProductName();


        // B --> Additional setup
        // Create a paassword policy
        ADpPasswordPolicyUpdater testPolicy = CreateTestPasswordPolicy(policyName);


        //HelperMethods.DisplayGroup(testGroup);

        Result resultA = asi.ADConnector.PasswordPolicyAdd(testPolicy);
        Assert.That(resultA.IsSuccess, Is.True, "A-120: Failed to add Password Policy.  Errors: " + resultA.ToStringErrorOnly());


        // C. --> Verify Policy was created
        Result<ADpReadOnlyPasswordPolicy> resultC = ReadAndVerifyPasswordPolicy(asi.ADConnector,
                                                                                [],
                                                                                policyName);
        Assert.That(resultC.IsSuccess, Is.True, "C-100:  Failed to find Password Policy --> AppError: " + resultC.ToStringErrorOnly());
        ADpReadOnlyPasswordPolicy policy = resultC.Value;


        // Z. Validate
        Assert.That(policy.DisplayName, Is.EqualTo(testPolicy.DisplayNameChg), "Z-200:  Display Name is not correct");
        Assert.That(policy.Description, Is.EqualTo(testPolicy.DescriptionChg), "Z-300:  Description is not correct");
        Assert.That(policy.PasswordMinAge, Is.EqualTo(testPolicy.MinimumAge), "Z-400:  Password Minimum Age is not correct");
        Assert.That(policy.PasswordMaxAge, Is.EqualTo(testPolicy.MaximumAge), "Z-500:  Password Maximum Age is not correct");
        Assert.That(policy.PasswordMinLength, Is.EqualTo(testPolicy.MinimumLength), "Z-600:  Password Minimum Length is not correct");
        Assert.That(policy.PasswordHistoryLength, Is.EqualTo(testPolicy.HistoryCount), "Z-700:  Password History Length is not correct");
        Assert.That(policy.PasswordLockThreshold, Is.EqualTo(testPolicy.LockOutThreshold), "Z-800:  Password Lockout Threshold is not correct");
        
        //Assert.That(policy.PasswordLockDuration, Is.EqualTo(testPolicy.LockoutDuration), "Z-900:  Password Lockout Duration is not correct");
       Assert.That(policy.PasswordLockoutObservationWindow, Is.EqualTo(testPolicy.LockoutObservationWindow), "Z-1000:  Password Lockout Observation Window is not correct");
        Assert.That(policy.PasswordComplexityEnabled, Is.EqualTo(testPolicy.ComplexityEnabled), "Z-1100:  Password Complexity Enabled is not correct");
        Assert.That(policy.PasswordReversibleEncryptionEnabled, Is.EqualTo(testPolicy.ReversibleEncryptionEnabled), "Z-1200:  Reversible Encryption Enabled is not correct");
        Assert.That(policy.PasswordSettingsPrecedence, Is.EqualTo(testPolicy.SettingsPrecedence), "Z-1300:  Password Settings Precedence is not correct");
        Assert.That(policy.AppliesTo.Count,Is.EqualTo(2), "Z-1400:  Applies to did not get updated correctly.");


        //Assert.That(policy.Email, Is.EqualTo(testGroup.EmailChg), "C-400:  Mail is not correct");
        //Assert.That(policy.SAMAccount, Is.EqualTo(testGroup.SAMAccountChg), "C-500:  SamAccountName is not correct");
    }


    [Test]
    public void DeletePasswordPolicy()
    {
        // A --> Setup
        string                methodPrefix = _testPolicyPrefix + "update_";
        string                policyName   = methodPrefix + asi.Faker.Commerce.ProductName();

        string searchFilter = "(objectClass=msDS-PasswordSettings)";


        // B.  More Setup
        ADpPasswordPolicyUpdater testPolicy = CreateTestPasswordPolicy(policyName);
        Result                   resultB    = asi.ADConnector.PasswordPolicyAdd(testPolicy);
        Assert.That(resultB.IsSuccess, Is.True, "B-120: Failed to add Password Policy.  Errors: " + resultB.ToStringErrorOnly());


        // C. --> Verify Policy was created
        Result<ADpReadOnlyPasswordPolicy> resultC = ReadAndVerifyPasswordPolicy(asi.ADConnector,
                                                                                [],
                                                                                policyName);
        Assert.That(resultC.IsSuccess, Is.True, "C-100:  Failed to find Password Policy --> AppError: " + resultC.ToStringErrorOnly());
        ADpReadOnlyPasswordPolicy policy = resultC.Value;


        // D.  Delete the policy
        Result<DeleteResponse> deleteResult = asi.ADConnector.PasswordPolicyDelete(policy.DistinguishedName);
        Assert.That(deleteResult.IsSuccess, Is.True, "C-100:  PasswordPolicy delete failed - " + deleteResult.ToStringErrorOnly());


        // Z .  Verify the policy is deleted
        Result<ADpReadOnlyPasswordPolicy> resultAfter = ReadAndVerifyPasswordPolicy(asi.ADConnector,
                                                                                [],
                                                                                policyName,true);
        Assert.That(resultAfter.IsFailed, Is.True, "Z-100:  PasswordPolicy was not successfully deleted --> AppError: " + resultB.ToStringWithLineFeeds());
    }


    /*


    [Test]
    public void RenameGroup()
    {
        // A --> Setup
        Ad_SupportInitializer asi = Ad_SupportInitializer.GetInitializer(true);

        // Create a random OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu();
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());


        // Create Test Group
        string groupName = asi.Faker.Commerce.ProductName();
        int i = Random.Shared.Next(1, 6) * 5;
        ADpGroupUpdater testGroup = new(groupName, (EnumGroupType)i);

        HelperMethods.DisplayGroup(testGroup);

        Result resultA = asi.AdEngine.GroupAdd(newOuResult.Value.Path, testGroup);
        Assert.That(resultA.IsSuccess, Is.True, "A-100: Failed to create group.  Errors: " + resultA.ToStringWithLineFeeds());


        // B.  Validate the group was created
        Result<ADpReadOnlyGroup> resultB = ReadAndVerifyPasswordPolicy(asi.AdEngine, newOuResult.Value.Path, [],  ADLDAPEngine.SEARCH_FILTER_ALL_GROUPS);
        ADpReadOnlyGroup group = resultB.Value;


        // C. Rename the Group
        string newName = group.AD_CommonName + "XY";
        Result<string> renameResult = asi.AdEngine.GroupRename(group, newName);
        Assert.That(renameResult.IsSuccess, Is.True, "C-100:  Group rename failed - " + renameResult.ToStringWithLineFeeds());
        Console.WriteLine("Renamed group to : " + renameResult.Value);


        // D .  Verify the group was renamed.
        resultB = ReadAndVerifyGroup(asi.AdEngine,
                                     newOuResult.Value.Path,
                                     [],
                                     ADLDAPEngine.SEARCH_FILTER_ALL_GROUPS);


        Assert.That(resultB.IsSuccess, Is.True, "D-100:  User was not found under new name. --> AppError: " + resultB.ToStringWithLineFeeds());
        Assert.That(resultB.Value.AD_CommonName, Is.EqualTo(newName), "d-200: Users common name was not updated correctly.");
    }
    */


    /// <summary>
    /// Reads the Password Policy by Common Name from Active Directory and verifies it exists.  Returns the policy read,
    /// </summary>
    /// <param name="engine"></param>
    /// <param name="attributesList"></param>
    /// <param name="commonName"></param>
    /// <param name="dontAssert"></param>
    /// <returns></returns>
    private Result<ADpReadOnlyPasswordPolicy> ReadAndVerifyPasswordPolicy(ActiveDirectoryConnector engine,
                                                                          List<string> attributesList,
                                                                          string commonName,
                                                                          bool dontAssert = false) //, string searchFilter = "")
    {
        ADSPath ouPath = engine.GetPasswordPolicyOU();

        if (attributesList.Count == 0)
            ADpReadOnlyPasswordPolicy.AddBaseAttributes(attributesList);

        Result<ADpReadOnlyPasswordPolicy> resultF = engine.PasswordPolicyGetByCn(commonName);

        if (!dontAssert)
        {
            Assert.That(resultF.IsSuccess, Is.True, "ReadAndVerifyPasswordPolicy: VPP-100:  Failed to find Password Policy --> AppError: " + resultF.ToStringErrorOnly());
        }
        else
        {
            Console.WriteLine("ReadAndVerifyPasswordPolicy: VPP-100:  Failed to find Password Policy, Was told not to Assert! --> AppError: " + resultF.ToStringErrorOnly());
            return Result.Fail("Password Policy Not Found");
        }

        ADpReadOnlyPasswordPolicy policy = resultF.Value;
        return Result.Ok(policy);
    }




    [Test]
    public void SampleFindPasswordPolicy()
    {
        // A --> Setup
        
        Result<List<ADpReadOnlyPasswordPolicy>> resultF = asi.ADConnector.PasswordPolicyFindOneOrMore("");
        Assert.That(resultF.IsSuccess, Is.True, "Z-100:  Failed to find Password Policy --> AppError: " + resultF.ToStringErrorOnly());
    }



    [Test]
    public void UserPasswordExpiration()
    {
        // A. Setup
        
        string                methodPrefix = _testPolicyPrefix + "update_";
        string                policyName   = methodPrefix + asi.Faker.Commerce.ProductName();


        // B.  Create a userFromAdRo
        // Create a random userFromAdRo OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu();
        Assert.That(newOuResult.IsSuccess, Is.True, "B-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());

        // Create Test User Basic
        TstUserBasic testUser = new("TestBasicUser Creation", newOuResult.Value, asi.Faker);
        testUser.CreateUser(asi.ADConnector);


        // C.  Create a password policy
        ADpPasswordPolicyUpdater testPolicy = CreateTestPasswordPolicy(policyName);
        testPolicy.AppliesTo.Add(testUser.User.DistinquishedName);
        testPolicy.AppliesToSetFlag = true;

        Result resultPol = asi.ADConnector.PasswordPolicyAdd(testPolicy);
        Assert.That(resultPol.IsSuccess, Is.True, "A-120: Failed to add Password Policy.  Errors: " + resultPol.ToStringErrorOnly());


        // C. --> Verify Policy was created
        Result<ADpReadOnlyPasswordPolicy> resultPolCreate = ReadAndVerifyPasswordPolicy(asi.ADConnector,
                                                                                [],
                                                                                policyName);
        Assert.That(resultPolCreate.IsSuccess, Is.True, "C-100:  Failed to find Password Policy --> AppError: " + resultPolCreate.ToStringErrorOnly());
        ADpReadOnlyPasswordPolicy policy = resultPolCreate.Value;


        // D.  Retrieve the userFromAdRo object again, so we can set its last password set attribute
        List<string> attributes = new();
        ADpUserFromAD_RO.AddBaseAttributes(attributes);
        ADpUserFromAD_RO.AddPasswordAttributes(attributes);

        Result<ADpUserEditable> resultD = asi.ADConnector.GetUserFromADViaAttribute("cn",
                                                                                   testUser.User.CommonNameChg, asi.UnitTestParent.Path, SearchScope.Subtree,attributes);
        Assert.That(resultD.IsSuccess, Is.True, "D-100:");
        ADpUserEditable updateUser = resultD.Value;

        // E.  Set a new password 
        updateUser.PasswordChg = asi.Faker.Random.AlphaNumeric(20) + "$#855aD";
        //DateTimeOffset passwordLastSet = DateTimeOffset.MinValue;
        //updateUser.PasswordLastSet =passwordLastSet;

        // E2. Save userFromAdRo back to AD.
        Result<ModifyResponse> updateUserResult = asi.ADConnector.UserUpdate(updateUser);
        Assert.That(updateUserResult.IsSuccess, Is.True, "E-100:  Failed to update userFromAdRo with last password set date.  Errors: " + updateUserResult.ToStringErrorOnly());

        // F.  Read the userFromAdRo back and verify the password expiration date is set.
        Result<ADpUserFromAD_RO> userAfterUpdateResult = asi.ADConnector.UserGetByUPN(asi.UnitTestParent.Path, testUser.User.UPN);
        Assert.That(userAfterUpdateResult.IsSuccess, Is.True, "F-100:  Failed to read userFromAdRo back from AD after update.  Errors: " + userAfterUpdateResult.ToStringErrorOnly());
        ADpUserFromAD_RO userFromAdRoAfterUpdate = userAfterUpdateResult.Value;

        // G.  Verify the password expiration date is set.
        //DateTime dt = passwordLastSet.LocalDateTime;
        Assert.That(userFromAdRoAfterUpdate.PasswordExpiryDateTime, Is.GreaterThanOrEqualTo(DateTimeOffset.Now), "G-100:  Password Expiration Date was not set on userFromAdRo after update.");

    }
#pragma warning restore NUnit2045
#pragma warning restore IDE0079
}