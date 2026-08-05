using AD.Protocols.ADObjects;
using AD.Protocols.ADObjects.Objects;
using AD.Protocols.ADObjects.Processors;
using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
using UT.CustomSupportObjects;

namespace Test.ADProtocols;

[TestFixture]
public class Test_PasswordPolicy
{
#region "Setup Teardown"

#pragma warning disable IDE0079
#pragma warning disable NUnit2045

    private Ad_SupportInitializer      asi;
    private ADpPasswordPolicyProcessor _pwdProcessor;
    private ADSPath                    PolicyRoot;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        asi = Ad_SupportInitializer.GetInitializer();
        asi.Initialize();
        PolicyRoot = asi.ADConnector.DomainRoot.CreateChild(ADpPasswordPolicyProcessor.PASS_POLICY_OU);
    }
    
    
    [SetUp]
    public void Setup()
    {
        //asi = Ad_SupportInitializer.GetInitializer();
        //asi.Initialize();
        _pwdProcessor = new ADpPasswordPolicyProcessor(asi.ADConnector.LdapConnection,PolicyRoot);
    }


    /// <summary>
    /// Creates a TestPolicy that can be sent to 
    /// </summary>
    /// <param name="policyName"></param>
    /// <returns></returns>
    private ADpPasswordPolicy Policy_CreateTestPolicy(string policyName)
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
        int    lockoutWindow    = asi.Faker.Random.Int(3, 600);   // in minutes


        ADpPasswordPolicy testPolicy = new(policyName,PolicyRoot);
        testPolicy.Precedence = precedence;
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
        testPolicy.LockoutThreshold            = lockoutThreshold;
        testPolicy.LockoutDuration = new TimeSpan(0,
                                                  0,
                                                  lockoutDuration,
                                                  0);
        testPolicy.LockoutObservationWindow = new TimeSpan(0,
                                                           0,
                                                           lockoutWindow,
                                                           0);
        testPolicy.Description = policyDesc;
        return testPolicy;
    }

    #endregion


    [Test]
    public void Policy_AddNew_Delete_Success()
    {
        // A --> Setup a new policy and add it to the AD
        ADpPasswordPolicy newPolicy = Policy_CreateTestPolicy("TestPolicy");
        Console.WriteLine($"New Policy --> {newPolicy}");
        _pwdProcessor.Delete(newPolicy);
        
        // B --> Get the policy back from AD and compare it to the original
        Result addResult = _pwdProcessor.AddNew(newPolicy);
        Assert.That(addResult.IsSuccess, Is.True, $"[B_010] Failed to add new policy.  {addResult.ToStringErrorOnly()}");
        
        ADpPasswordPolicy? retrievedPolicy = _pwdProcessor.Get(newPolicy.DistinguishedName).Value;
        Assert.That(retrievedPolicy, Is.Not.Null);
        Assert.That(retrievedPolicy.Name, Is.EqualTo(newPolicy.Name),"[B_100]  Name is not equal");
        Assert.That(retrievedPolicy.Description, Is.EqualTo(newPolicy.Description), "[B_110]  Description is not equal");
        Assert.That(retrievedPolicy.MinimumAge, Is.EqualTo(newPolicy.MinimumAge), "[B_120]  Minimum Age is incorrect.");
        Assert.That(retrievedPolicy.MaximumAge, Is.EqualTo(newPolicy.MaximumAge), "[B_130]  Maximum Age is incorrect.");
        Assert.That(retrievedPolicy.MinimumLength, Is.EqualTo(newPolicy.MinimumLength));
        Assert.That(retrievedPolicy.HistoryCount, Is.EqualTo(newPolicy.HistoryCount));
        Assert.That(retrievedPolicy.ComplexityEnabled, Is.EqualTo(newPolicy.ComplexityEnabled));
        Assert.That(retrievedPolicy.ReversibleEncryptionEnabled, Is.EqualTo(newPolicy.ReversibleEncryptionEnabled));
        Assert.That(retrievedPolicy.LockoutThreshold, Is.EqualTo(newPolicy.LockoutThreshold));
        Assert.That(retrievedPolicy.LockoutDuration, Is.EqualTo(newPolicy.LockoutDuration));
        Assert.That(retrievedPolicy.LockoutObservationWindow, Is.EqualTo(newPolicy.LockoutObservationWindow));
        Assert.That(retrievedPolicy.Precedence, Is.EqualTo(newPolicy.Precedence));
        
        // Now Delete Policy
        _pwdProcessor.Delete(newPolicy);

        // C --> Verify the policy exists
        Result<bool> existsResult = _pwdProcessor.Exists(newPolicy.DistinguishedName);
        Assert.That(existsResult.IsSuccess, Is.True, "[C_110] Exists check failed.");
        Assert.That(existsResult.Value, Is.False, "[C_120] Policy was not deleted");
    }


    [Test]
    public void Policy_Update_Success()
    {
        // A --> Setup a new policy and add it to the AD
        ADpPasswordPolicy newPolicy = Policy_CreateTestPolicy("TestPolicyUpdate");
        Console.WriteLine($"New Policy --> {newPolicy}");
        _pwdProcessor.Delete(newPolicy);

        // B --> Get the policy back from AD and compare it to the original
        Result addResult = _pwdProcessor.AddNew(newPolicy);
        Assert.That(addResult.IsSuccess, Is.True, $"[B_100] Failed to add new policy.  {addResult.ToStringErrorOnly()}");

        ADpPasswordPolicy? retrievedPolicy = _pwdProcessor.Get(newPolicy.DistinguishedName).Value;
        Assert.That(retrievedPolicy, Is.Not.Null);
        Assert.That(retrievedPolicy.Name, Is.EqualTo(newPolicy.Name), "[B_100]  Name is not equal");
        Assert.That(retrievedPolicy.Description, Is.EqualTo(newPolicy.Description), "[B_110]  Description is not equal");
        Assert.That(retrievedPolicy.MinimumAge, Is.EqualTo(newPolicy.MinimumAge), "[B_120]  Minimum Age is incorrect.");
        Assert.That(retrievedPolicy.MaximumAge, Is.EqualTo(newPolicy.MaximumAge), "[B_130]  Maximum Age is incorrect.");
        Assert.That(retrievedPolicy.MinimumLength, Is.EqualTo(newPolicy.MinimumLength));
        Assert.That(retrievedPolicy.HistoryCount, Is.EqualTo(newPolicy.HistoryCount));
        Assert.That(retrievedPolicy.ComplexityEnabled, Is.EqualTo(newPolicy.ComplexityEnabled));
        Assert.That(retrievedPolicy.ReversibleEncryptionEnabled, Is.EqualTo(newPolicy.ReversibleEncryptionEnabled));
        Assert.That(retrievedPolicy.LockoutThreshold, Is.EqualTo(newPolicy.LockoutThreshold));
        Assert.That(retrievedPolicy.LockoutDuration, Is.EqualTo(newPolicy.LockoutDuration));
        Assert.That(retrievedPolicy.LockoutObservationWindow, Is.EqualTo(newPolicy.LockoutObservationWindow));
        Assert.That(retrievedPolicy.Precedence, Is.EqualTo(newPolicy.Precedence));

        
        // C --> Act - Make changes
        string newDesc             = "Something differernt";
        int    newMinLength        = asi.Faker.Random.Int(5, 12);
        int    newLockoutThreshold = asi.Faker.Random.Int(4, 20);
        int    newHistory          = asi.Faker.Random.Int(6, 9);
        bool?   newComplexity       = !retrievedPolicy.ComplexityEnabled; // Invert
//        bool?   newEncrypted        = !retrievedPolicy.ReversibleEncryptionEnabled;  // Invert
        int    newPrecedence       = asi.Faker.Random.Int(4, 8);
        int    newLockoutDuration  = asi.Faker.Random.Int(3001, 5600); // in minutes
        int    newLockoutWindow    = asi.Faker.Random.Int(201, 406);   // in minutes
        TimeSpan newMinAge = new TimeSpan(asi.Faker.Random.Int(61, 120),
                                          0,
                                          0,
                                          0);
        TimeSpan newMaxAge = new TimeSpan(asi.Faker.Random.Int(101, 200),
                                          0,
                                          0,
                                          0);

        retrievedPolicy.Description                 = newDesc;
        retrievedPolicy.MinimumLength               = newMinLength;
        retrievedPolicy.LockoutThreshold            = newLockoutThreshold;
        retrievedPolicy.HistoryCount                = newHistory;
        retrievedPolicy.ComplexityEnabled           = newComplexity;
  //      retrievedPolicy.ReversibleEncryptionEnabled = newEncrypted;
        retrievedPolicy.Precedence                  = newPrecedence;
        retrievedPolicy.LockoutDuration = new TimeSpan(0,
                                                       0,
                                                       newLockoutDuration,
                                                       0);
        retrievedPolicy.LockoutObservationWindow = new TimeSpan(0,
                                                                0,
                                                                newLockoutWindow,
                                                                0);
        retrievedPolicy.MinimumAge = newMinAge;
        retrievedPolicy.MaximumAge = newMaxAge;
  
        Console.WriteLine($"Updated Policy --> {retrievedPolicy}");

        // C --> Update the policy in AD
        Result updateResult = _pwdProcessor.Update(retrievedPolicy);
        Assert.That(updateResult.IsSuccess, Is.True, $"[C_100] Failed to update policy. {updateResult.ToStringErrorOnly()}");

        // D --> Get the policy back from AD and compare it to the updated values
        ADpPasswordPolicy updatedRetrievedPolicy = _pwdProcessor.Get(newPolicy.DistinguishedName).Value;
        Assert.That(updatedRetrievedPolicy, Is.Not.Null);
        Assert.That(updatedRetrievedPolicy.Name, Is.EqualTo(newPolicy.Name), "[D_100]  Name is not equal");
        Assert.That(updatedRetrievedPolicy.Description, Is.EqualTo(newDesc), "[D_110]  Description is not equal");
        Assert.That(updatedRetrievedPolicy.MinimumLength, Is.EqualTo(newMinLength));
        Assert.That(updatedRetrievedPolicy.HistoryCount, Is.EqualTo(newHistory));
        Assert.That(updatedRetrievedPolicy.ComplexityEnabled, Is.EqualTo(newComplexity));
//        Assert.That(updatedRetrievedPolicy.ReversibleEncryptionEnabled, Is.EqualTo(newEncrypted));
        Assert.That(updatedRetrievedPolicy.LockoutThreshold, Is.EqualTo(newLockoutThreshold));
        Assert.That(updatedRetrievedPolicy.LockoutDuration,
                    Is.EqualTo(new TimeSpan(0,
                                            0,
                                            newLockoutDuration,
                                            0)));
        Assert.That(updatedRetrievedPolicy.LockoutObservationWindow,
                    Is.EqualTo(new TimeSpan(0,
                                            0,
                                            newLockoutWindow,
                                            0)));
        Assert.That(updatedRetrievedPolicy.MinimumAge, Is.EqualTo(newMinAge), "[D_120]  Minimum Age is incorrect.");
   Assert.That(updatedRetrievedPolicy.MaximumAge, Is.EqualTo(newMaxAge), "[D_130]  Maximum Age is incorrect.");

        Assert.That(updatedRetrievedPolicy.Precedence, Is.EqualTo(newPrecedence));
        
        
        // Now Delete Policy
        _pwdProcessor.Delete(updatedRetrievedPolicy);
    }


    [Test]
    public void UserAssignment()
    {
        // A --> Setup a new policy and add it to the AD
        ADpPasswordPolicy newPolicy     = Policy_CreateTestPolicy("UserAssign");
        ADpUserProcessor  userProcessor = asi.ADConnector.UserProcessor();
        
        Console.WriteLine($"New Policy --> {newPolicy}");
        _pwdProcessor.Delete(newPolicy);

        // B --> Create random OU;s and a random person
        ADpOrgUnit newOu = asi.CreateRandomOuNew();

        // Create a random group to assign the users to
        string name  = asi.Faker.Commerce.ProductName();
        ADpGroup group = new ADpGroup(name, newOu.Path);

        // Create users to add to group.
        List<TestUserAttr> testUsers              = asi.GenerateRandomPerson(4);
        List<string>       userDistinguishedNames = new List<string>();


        // C  --> Add Group users to AD and then to Group.
        // Save 
        int counter = 1;
        foreach (TestUserAttr testUserAttr in testUsers)
        {
            if (counter == 4)
                break;
            ADpUser user      = testUserAttr.CreateADpUser(newOu.Path);
            Result  userAdded = userProcessor.AddNew(user);
            Assert.That(userAdded.IsSuccess, Is.True, $"[C_100] Failed to add user {user.DistinguishedName} to Active Directory for later Testing.");
            userDistinguishedNames.Add(user.DistinguishedName);
            group.AddUserToGroup(user.DistinguishedName);
            counter++;
        }
        
        // Save Group to AD
        ADpGroupProcessor groupProcessor = asi.ADConnector.GroupProcessor();
        Result groupAddResult = groupProcessor.AddNew(group);
        Assert.That(groupAddResult.IsSuccess, Is.True, $"[C_110] Failed to add group {group.DistinguishedName} to Active Directory.");
        

        // Add one final user to AD to be assigned to the policy directly, not through a group.
        ADpUser userDirectAssign = testUsers[3].CreateADpUser(newOu.Path);
        Result  userDAdded        = userProcessor.AddNew(userDirectAssign);
        Assert.That(userDAdded.IsSuccess, Is.True, $"[C_120] Failed to add user {userDirectAssign.DistinguishedName} to Active Directory for direct assignment.");
        Console.WriteLine($"Direct Assign User --> {userDirectAssign.DistinguishedName}");
        userDistinguishedNames.Add(userDirectAssign.DistinguishedName);

        
        // Add users and group to the Password Policy
        newPolicy.AppliesTo.AddMember(group.DistinguishedName);
        newPolicy.AppliesTo.AddMember(userDirectAssign.DistinguishedName);
        
        // D --> Add the policy to AD
        Result addPolicyResult = _pwdProcessor.AddNew(newPolicy);
        Assert.That(addPolicyResult.IsSuccess, Is.True, $"[D_100] Failed to add new policy.  {addPolicyResult.ToStringErrorOnly()}");

        // E --> Retrieve the Policy back from AD and verify the group and user are in the AppliesTo list.
        Result<ADpPasswordPolicy> rx = _pwdProcessor.Get(newPolicy.DistinguishedName);
        Assert.That(rx.IsSuccess,Is.True,$"[E_100] Failed to retrieve policy {newPolicy.DistinguishedName} from Active Directory that we saved.");
        ADpPasswordPolicy retrievedPolicy = rx.Value;
        
        Result appliesToResult = _pwdProcessor.GetAppliesTo(retrievedPolicy);
        Assert.That(appliesToResult.IsSuccess, Is.True, $"[E_110] Failed to retrieve AppliesTo list for policy {retrievedPolicy.DistinguishedName}.");
        
        Assert.That(retrievedPolicy.AppliesTo.CurrentValues.Contains(group.DistinguishedName), "[E_120] The group was not found in the AppliesTo list.");
        Assert.That(retrievedPolicy.AppliesTo.CurrentValues.Contains(userDirectAssign.DistinguishedName), "[E_130] The user was not found in the AppliesTo list.");

        // F --> Cleanup
        Result deleteResult = _pwdProcessor.Delete(newPolicy);
        Assert.That(deleteResult.IsSuccess, "[F_100] Failed to delete the test policy.");
        Result deleteUserResult = userProcessor.Delete(userDirectAssign);
        Assert.That(deleteUserResult.IsSuccess, "[F_110] Failed to delete the test user.");
        Result deleteGroupResult = groupProcessor.Delete(group);
        Assert.That(deleteGroupResult.IsSuccess, "[F_120] Failed to delete the test group.");

        ADpOrgUnitProcessor orgProcessor   = asi.ADConnector.OrgUnitProcessor();
        Result              deleteOUResult = orgProcessor.Delete(newOu.DistinguishedName,true);
        Assert.That(deleteOUResult.IsSuccess, "[F_130] Failed to delete the test OU.");

        /*foreach (string userDistinguishedName in userDistinguishedNames)
        {
            userProcessor.Delete(userDistinguishedName);
        }
        */
        
    }
}

