using AD.Protocols.ADObjects;
using AD.Protocols.ADObjects.Objects;
using AD.Protocols.ADObjects.Processors;
using SlugEnt.AD.Protocols;
using UT.CustomSupportObjects;

namespace Test.ADProtocols;

[TestFixture]
public class Test_PasswordPolicy
{
#region "Setup Teardown"

#pragma warning disable IDE0079
#pragma warning disable NUnit2045

    private Ad_SupportInitializer asi;
    private ADpPasswordPolicyProcessor      _pwdProcessor;

    
    [SetUp]
    public void Setup()
    {
        asi = Ad_SupportInitializer.GetInitializer();
        asi.Initialize();
        _pwdProcessor = new ADpPasswordPolicyProcessor(asi.ADConnector.LdapConnection,asi.ADConnector.DomainRoot);
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


        ADpPasswordPolicy testPolicy = new(policyName);
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


    /*        testPolicy.AppliesTo = new List<string>
            {
                $"CN=Guest,CN=Users,{asi.DomainInLDAPStyle()}",
                $"CN=Domain Guests,CN=Users,{asi.DomainInLDAPStyle()}",
            }; // This is a default value, can be changed later.
            return testPolicy;
        }
    */
    #endregion


    [Test]
    public void Policy_AddNew()
    {
        // A --> Setup a new policy and add it to the AD
        ADpPasswordPolicy newPolicy = Policy_CreateTestPolicy("TestPolicy");
        Console.WriteLine($"New Policy --> {newPolicy}");
        
        _pwdProcessor.AddNew(newPolicy);

        // B --> Get the policy back from AD and compare it to the original
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
        
        
    }
}

