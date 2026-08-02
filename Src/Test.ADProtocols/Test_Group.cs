using System.Data.Common;
using AD.Protocols.ADObjects;
using AD.Protocols.ADObjects.Objects;
using AD.Protocols.ADObjects.Processors;
using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
using System.Text.RegularExpressions;
using UT.CustomSupportObjects;

namespace Test.ADProtocols;

[TestFixture]
public class Test_Group
{
#region "Setup Teardown"

#pragma warning disable IDE0079
#pragma warning disable NUnit2045

    private Ad_SupportInitializer asi;
    private ADpGroupProcessor      _groupProcessor;


    [SetUp]
    public void Setup()
    {
        asi = Ad_SupportInitializer.GetInitializer();
        asi.Initialize();
        
        _groupProcessor = new ADpGroupProcessor(asi.ADConnector.LdapConnection);
    }

    #endregion


    [Test]
    public void CycleGroup_CRUD_Success()
    {
        // A  --> Setup
        string  name = asi.Faker.Person.FullName;
        ADpGroup group = new ADpGroup(name, asi.UnitTestParent);

        // B  --> Post Setup Confirmation

        // C  --> Action
        Result x = _groupProcessor.AddNew(group);


        // V  -- Verify
        Assert.That(x.IsSuccess, Is.True, "[V_100] Failed to add group");
        Assert.That(group.DistinguishedName, Is.Not.Null.And.Not.Empty, "[V_110] Group DistinguishedName is null or empty");

        // Confirm it's in AD
        Result<ADpGroup> result = _groupProcessor.Get(group.DistinguishedName).Value;
        Assert.That(result.IsSuccess, Is.True, "[V_200] Failed to retrieve group from AD after creation.");
        ADpGroup foundGroup = result.Value;
        Assert.That(foundGroup, Is.Not.Null, "[V_200] Failed to retrieve group from AD after creation.");
        Assert.That((group.ParentPath == foundGroup.ParentPath), Is.True, "[V_205] Retrieved group has incorrect ParentPath.");
        Assert.That(foundGroup.EqualSameObject(group), Is.True, "[V_220] Retrieved group has incorrect Name.");

        // Z -- Delete the group
        Result z = _groupProcessor.Delete(foundGroup);
        Assert.That(z.IsSuccess, Is.True, "[Z_100] Failed to delete group    from AD.");
    }



    [Test]
    public void MoveGroup()
    {
        // A --> Setup
        // Create 2 random OU;s
        ADpOrgUnit newOu    = asi.CreateRandomOuNew();
        ADpOrgUnit moveToOu = asi.CreateRandomOuNew();


        // Create Test Group Basic
        ADpGroup testGroup = asi.CreateRandomGroupNew(newOu.Path);
        Result  result    = _groupProcessor.AddNew(testGroup);
        Assert.That(result.IsSuccess, Is.True, "[A_100]  Failed to add test group.");


        // C. Move the Group
        string  priorDn   = testGroup.DistinguishedName;
        ADSPath priorPath = testGroup.ParentPath;

        Result moveResult = _groupProcessor.Move(testGroup, moveToOu.Path);
        Assert.That(moveResult.IsSuccess, Is.True, "C-100:  Group move failed - " + moveResult.ToStringWithLineFeeds());


        // D. Verify It is in new location 
        Result<bool> exitResult = _groupProcessor.Exists(testGroup);
        Assert.That(exitResult.IsSuccess, Is.True, "D-100:  Failed to check if group exists.");
        Assert.That(exitResult.Value, Is.True, "D-110:  Group does not exist in the expected location.");

        // Verify it is not in old location
        Result<bool> exitResultOld = _groupProcessor.Exists(priorDn);
        Assert.That(exitResultOld.IsSuccess, Is.True, "D-120:  Failed to check if group exists in old location.");
        Assert.That(exitResultOld.Value, Is.False, "D-130:  Group still exists in the old location.");
    }


    [Test]
    public void Exists_Success()
    {
        // A --> Setup
        // Create 2 random OU;s
        ADpOrgUnit newOu = asi.CreateRandomOuNew();

        ADpGroup groupA = new ADpGroup(asi.Faker.Person.FullName, newOu.Path);

        // See if group exists.
        Result<bool> existsResult = _groupProcessor.Exists(groupA);
        Assert.That(existsResult.IsSuccess, Is.False, "[V_100] Failed to check if group exists.");

        // Save Group
        Result x = _groupProcessor.AddNew(groupA);
        Assert.That(x.IsSuccess, Is.True, "[V_110] Failed to add group.");

        // See if group exists.
        Result<bool> existsResultAfterAdd = _groupProcessor.Exists(groupA);
        Assert.That(existsResultAfterAdd.IsSuccess, Is.True, "[V_200] Failed to check if group exists after add.");
        Assert.That(existsResultAfterAdd.Value, Is.True, "[V_210] Group was not found after add.");
    }

    [Test]
    public void Rename_Success()
    {
        // A --> Setup
        // Create random OU;s
        ADpOrgUnit newOu = asi.CreateRandomOuNew();

        ADpGroup groupA = new ADpGroup(asi.Faker.Person.FullName, newOu.Path);

        // Save Group
        Result x = _groupProcessor.AddNew(groupA);
        Assert.That(x.IsSuccess, Is.True, "[A_110] Failed to add group.");


        // Confirm group exists.
        Result<bool> existsResultAfterAdd = _groupProcessor.Exists(groupA);
        Assert.That(existsResultAfterAdd.IsSuccess, Is.True, "[A_200] Failed to check if group exists after add.");
        Assert.That(existsResultAfterAdd.Value, Is.True, "[A_210] Group was not found after add.");


        // C --> Act
        string newName = "john hamilton smith";

        // V --> Verify
        Result<string> y = _groupProcessor.Rename(groupA, newName);
        Assert.That(y.IsSuccess, Is.True, "[V_100] Failed to rename group.");
        Assert.That(groupA.CommonName, Is.EqualTo(newName), "[V_110] Group was not renamed correctly.");
    }

    
    [Test]
    public void GroupFieldsTest()
    {
        // A --> Setup
        // Create random OU;s and a random person
        ADpOrgUnit newOu = asi.CreateRandomOuNew();
        List<TestGroupAttr> testGroups = asi.GenerateRandomGroup();
        ADpGroup groupA = testGroups[0].CreateADpGroup(newOu.Path);
        string password = "2026abcdef*";


        // C. Act.
        // Save Group
        Result x = _groupProcessor.AddNew(groupA);
        Assert.That(x.IsSuccess, Is.True, $"[C_100] Failed to add group. {x.ToStringErrorOnly()}");


        // Make sure to retrieve the group attributes from AD
        _groupProcessor.AttrRetrieval_Default();

        // Need to re-get the group object from AD to validate fields updated successfully
        Result<ADpGroup> groupResult = _groupProcessor.Get(groupA.DistinguishedName);
        Assert.That(groupResult.IsSuccess, Is.True, "[V_100] Failed to retrieve group object to verify field updates");
        ADpGroup adGroup = groupResult.Value;


        // Verify
        Assert.That(adGroup.Name, Is.EqualTo(groupA.Name), "[V_200] Name does not match.");
        Assert.That(adGroup.Description, Is.EqualTo(groupA.Description), "[V_230] Description does not match.");
        Assert.That(adGroup.SAMAccount, Is.EqualTo(groupA.SAMAccount), "[V_270] SAMAccount does not match.");
        Assert.That(adGroup.GroupType, Is.EqualTo(groupA.GroupType), "[V_300] GroupType does not match.");
        
        // Delete the group so it doesn't persist in AD
        Result deleteResult = _groupProcessor.Delete(adGroup);
        Assert.That(deleteResult.IsSuccess, Is.True, "[Z_100] Failed to delete group from AD.");
    }


    [Test]
    public void Members_SmallList()
    {
        // A  --> Setup
        // Create random OU;s and a random person
        ADpOrgUnit newOu = asi.CreateRandomOuNew();

        string name  = asi.Faker.Commerce.ProductName();
        ADpGroup group = new ADpGroup(name, newOu.Path);

        // create user procesor
        ADpUserProcessor userProcessor = new ADpUserProcessor(asi.ADConnector.LdapConnection);
        
        // Create users to add to group.
        List<TestUserAttr> testUsers = asi.GenerateRandomPerson(6);

        List<string> userDistinguishedNames = new List<string>();

        
        // B  --> Post Setup Confirmation
        // Save 
        foreach (TestUserAttr testUserAttr in testUsers)
        {
            ADpUser user      = testUserAttr.CreateADpUser(newOu.Path);
            Result  userAdded = userProcessor.AddNew(user);
            Assert.That(userAdded.IsSuccess, Is.True, $"[B_110] Failed to add user {user.DistinguishedName} to Active Directory for later Testing.");
            userDistinguishedNames.Add(user.DistinguishedName);
        }

        
        // Add the users to the group
        foreach (string dn in userDistinguishedNames)
        {
            group.AddUserToGroup(dn);
        }


        // C  --> Action - Save the group
        Result x = _groupProcessor.AddNew(group);
        Assert.That(x.IsSuccess,Is.True,"[C_100] Failed to add group");

        
        // D  -- Verify
        Assert.That(x.IsSuccess, Is.True, "[D_100] Failed to add group");
        Assert.That(group.DistinguishedName, Is.Not.Null.And.Not.Empty, "[D_110] Group DistinguishedName is null or empty");

        
        // Confirm it's in AD
        Result<ADpGroup> result = _groupProcessor.Get(group.DistinguishedName).Value;
        Assert.That(result.IsSuccess, Is.True, "[D_200] Failed to retrieve group from AD after creation.");
        ADpGroup foundGroup = result.Value;
        Assert.That(foundGroup, Is.Not.Null, "[D_200] Failed to retrieve group from AD after creation.");


        // E. Retrieve members and verify they match what was added.
        Result getMembersResult = _groupProcessor.GetMembers(foundGroup);
        Assert.That(getMembersResult.IsSuccess, Is.True, "[E_100] Failed to retrieve group members from AD.");
        foreach (string foundGroupMember in foundGroup.Members)
        {
            bool found = false;
            foreach (string userDistinguishedName in userDistinguishedNames)
            {
                if (ADpBaseObject.EqualSameObject(foundGroupMember, userDistinguishedName))
                {
                    found = true;
                    break;
                }
            }
            Assert.That(found,Is.True,"Unable to find the user in the download Active Directory object's member list.");
        }

        // Z -- Delete the group
        Result z = _groupProcessor.Delete(foundGroup);
        Assert.That(z.IsSuccess, Is.True, "[Z_100] Failed to delete group    from AD.");
        
        
        // Delete the users.
        foreach (string user in userDistinguishedNames)
        {
            Result deleteUserResult = userProcessor.Delete(user);
            Assert.That(deleteUserResult.IsSuccess, Is.True, $"[Z_110] Failed to delete user {user} from AD.");
        }
        
        // Delete the OU.
        ADpOrgUnitProcessor ouProcessor    = asi.ADConnector.OrgUnitProcessor();
        Result              deleteOuResult = ouProcessor.Delete(newOu);
        Assert.That(deleteOuResult.IsSuccess, Is.True, "[Z_200] Failed to delete OU from AD.");
    }


    [Test]
    public void UpdateGroupFields_Success()
    {
        // A --> Setup
        // Create random OU;s
        ADpOrgUnit newOu = asi.CreateRandomOuNew();

        ADpGroup groupA = new ADpGroup(asi.Faker.Person.FullName, newOu.Path);

        // Save Group
        Result x = _groupProcessor.AddNew(groupA);
        Assert.That(x.IsSuccess, Is.True, "[A_110] Failed to add group.");

        // B --> Verify It exists and has default values
        Result<ADpGroup> result = _groupProcessor.Get(groupA.DistinguishedName);
        Assert.That(result.IsSuccess, Is.True, "[B_100] Failed to retrieve group for verification.");
        ADpGroup retrievedGroup = result.Value;

        // C --> Act - Modify fields
        string        newDesc      = "This is a test description.";
        string        newDisplay   = "A New Display Name";
        string        newEmail     = "test@example.com";
        string        newSam       = "NewSAM123";
        EnumGroupType newGroupType = EnumGroupType.Universal_Security;

        retrievedGroup.Description = newDesc;
        retrievedGroup.DisplayName = newDisplay;
        retrievedGroup.Email       = newEmail;
        retrievedGroup.SAMAccount  = newSam;
        retrievedGroup.GroupType   = newGroupType;

        Result updateResult = _groupProcessor.Update(retrievedGroup);
        Assert.That(updateResult.IsSuccess, Is.True, "[C_100] Failed to update group fields.");

        // D --> Verify fields are updated
        Result<ADpGroup> updatedResult = _groupProcessor.Get(groupA.DistinguishedName);
        Assert.That(updatedResult.IsSuccess, Is.True, "[D_100] Failed to retrieve updated group.");
        ADpGroup finalGroup = updatedResult.Value;

        Assert.That(finalGroup.Description, Is.EqualTo(newDesc), "[D_110] Description field was not updated correctly.");
        Assert.That(finalGroup.DisplayName, Is.EqualTo(newDisplay), "[D_120] DisplayName field was not updated correctly.");
        Assert.That(finalGroup.Email, Is.EqualTo(newEmail), "[D_130] Email field was not updated correctly.");
        Assert.That(finalGroup.SAMAccount, Is.EqualTo(newSam), "[D_140] SAMAccount field was not updated correctly.");
        Assert.That(finalGroup.GroupType, Is.EqualTo(newGroupType), "[D_150] GroupType field was not updated correctly.");

        // Z -- Cleanup
        Result z = _groupProcessor.Delete(groupA);
        Assert.That(z.IsSuccess, Is.True, "[Z_100] Failed to delete group from AD.");
        Result deleteOuResult = asi.ADConnector.OrgUnitProcessor().Delete(newOu);
        Assert.That(deleteOuResult.IsSuccess, Is.True, "[Z_200] Failed to delete OU from AD.");
    }

    
    
    [Test]
    public void AddNew_SimpleMethod()
    {
        // A --> Setup
        // Create random OU;s
        ADpOrgUnit newOu = asi.CreateRandomOuNew();
        Result<string>     x     = _groupProcessor.AddNew(asi.Faker.Person.FullName, newOu.Path);

        // Verify
        Result<ADpGroup> updatedResult = _groupProcessor.Get(x.Value);
        Assert.That(updatedResult.IsSuccess, Is.True, "[V_100] Failed to retrieve updated group.");
    }


    /// <summary>
    /// Tests whether we are able to successfully get the group members for a large list of members, that exceeds AD single retrieval limits.
    /// </summary>
    [Test]
    public void Members_LargeList()
    {
        // A  --> Setup
        int groupSegmentSize = 3;
        int groupSegments    = 4;
        
        // Create random OU;s and a random person
        ADpOrgUnit newOu = asi.CreateRandomOuNew();

        string name = asi.Faker.Commerce.ProductName();
        ADpGroup group = new ADpGroup(name, newOu.Path);

        // create user procesor
        ADpUserProcessor userProcessor = new ADpUserProcessor(asi.ADConnector.LdapConnection);

        // Create users to add to group.
        List<TestUserAttr> testUsers = asi.GenerateRandomPerson(groupSegmentSize*groupSegments);
        List<string> userDistinguishedNames = new List<string>();


        // B  --> Post Setup Confirmation
        // Save 
        foreach (TestUserAttr testUserAttr in testUsers)
        {
            ADpUser user = testUserAttr.CreateADpUser(newOu.Path);
            Result userAdded = userProcessor.AddNew(user);
            Assert.That(userAdded.IsSuccess, Is.True, $"[B_110] Failed to add user {user.DistinguishedName} to Active Directory for later Testing.");
            userDistinguishedNames.Add(user.DistinguishedName);
        }


        // Add the users to the group
        foreach (string dn in userDistinguishedNames)
        {
            group.AddUserToGroup(dn);
        }


        // C  --> Action - Save the group
        Result x = _groupProcessor.AddNew(group);
        Assert.That(x.IsSuccess, Is.True, "[C_100] Failed to add group");


        // D  -- Verify
        Assert.That(x.IsSuccess, Is.True, "[D_100] Failed to add group");
        Assert.That(group.DistinguishedName, Is.Not.Null.And.Not.Empty, "[D_110] Group DistinguishedName is null or empty");


        // Confirm it's in AD
        Result<ADpGroup> result = _groupProcessor.Get(group.DistinguishedName).Value;
        Assert.That(result.IsSuccess, Is.True, "[D_200] Failed to retrieve group from AD after creation.");
        ADpGroup foundGroup = result.Value;
        Assert.That(foundGroup, Is.Not.Null, "[D_200] Failed to retrieve group from AD after creation.");


        // E. Retrieve members and verify they match what was added.

        // Override the default segment size for member retrieval to a smaller number to force multiple retrievals.
        _groupProcessor.MembersRetrievedPerRequest = groupSegmentSize;
        
        Result getMembersResult = _groupProcessor.GetMembers(foundGroup);
        Assert.That(getMembersResult.IsSuccess, Is.True, "[E_100] Failed to retrieve group members from AD.");
        int count = 0;
        foreach (string foundGroupMember in foundGroup.Members)
        {
            bool found = false;
            foreach (string userDistinguishedName in userDistinguishedNames)
            {
                if (ADpBaseObject.EqualSameObject(foundGroupMember, userDistinguishedName))
                {
                    found = true;
                    break;
                }
            }
            Assert.That(found, Is.True, "Unable to find the user in the download Active Directory object's member list.");
            count++;
        }
        Assert.That(count, Is.EqualTo(groupSegmentSize*groupSegments), "[E_110] The number of retrieved group members does not match the number of added users.");

        // Z -- Delete the group
        Result z = _groupProcessor.Delete(foundGroup);
        Assert.That(z.IsSuccess, Is.True, "[Z_100] Failed to delete group    from AD.");


        // Delete the users.
        foreach (string user in userDistinguishedNames)
        {
            Result deleteUserResult = userProcessor.Delete(user);
            Assert.That(deleteUserResult.IsSuccess, Is.True, $"[Z_110] Failed to delete user {user} from AD.");
        }

        // Delete the OU.
        ADpOrgUnitProcessor ouProcessor = asi.ADConnector.OrgUnitProcessor();
        Result deleteOuResult = ouProcessor.Delete(newOu);
        Assert.That(deleteOuResult.IsSuccess, Is.True, "[Z_200] Failed to delete OU from AD.");
    }


    [Test]
    public void AddNewMemberToGroup_RemoveMemberFromGroup_Success()
    {
        // A --> Setup
        // Create random OU;s and a random person
        ADpOrgUnit          newOu      = asi.CreateRandomOuNew();
        List<TestGroupAttr> testGroups = asi.GenerateRandomGroup();
        ADpGroup            groupA     = testGroups[0].CreateADpGroup(newOu.Path);
        string              password   = "2026abcdef*";


        // create user procesor
        ADpUserProcessor userProcessor = new ADpUserProcessor(asi.ADConnector.LdapConnection);

        // Create users to add to group.
        List<TestUserAttr> testUsers              = asi.GenerateRandomPerson(2);
        List<string>       userDistinguishedNames = new List<string>();


        // B  --> Post Setup Confirmation
        // Save 
        foreach (TestUserAttr testUserAttr in testUsers)
        {
            ADpUser user      = testUserAttr.CreateADpUser(newOu.Path);
            Result  userAdded = userProcessor.AddNew(user);
            Assert.That(userAdded.IsSuccess, Is.True, $"[B_100] Failed to add user {user.DistinguishedName} to Active Directory for later Testing.");
            userDistinguishedNames.Add(user.DistinguishedName);
        }


        // C --> Act.
        // Save Group
        Result x = _groupProcessor.AddNew(groupA);
        Assert.That(x.IsSuccess, Is.True, $"[C_100] Failed to add group. {x.ToStringErrorOnly()}");


        // Make sure to retrieve the group attributes from AD
        _groupProcessor.AttrRetrieval_Default();

        // D  --> Act 2 - Need to re-get the group object from AD to validate fields updated successfully
        Result<ADpGroup> groupResult = _groupProcessor.Get(groupA.DistinguishedName);
        Assert.That(groupResult.IsSuccess, Is.True, "[V_100] Failed to retrieve group object to verify field updates");
        ADpGroup adGroup = groupResult.Value;
        _groupProcessor.GetMembers(adGroup);

        // E  --> Act 3 - Add a new member to the group
        Assert.That(adGroup.Members.Count, Is.EqualTo(0),"[E_100] The group should have no members initially.");

        // F  --> Act 4 - Add a new member to the group
        adGroup.AddUserToGroup(userDistinguishedNames[0]);
        adGroup.AddUserToGroup(userDistinguishedNames[1]);

        // G  --> Act 5 - Update group in AD
        Result gResult =  _groupProcessor.Update(adGroup);
        Assert.That(gResult.IsSuccess, Is.True, "[G_100] Failed to update group in AD.");

        // H  --> Verify - Retrieve the group again and check members
        Result<ADpGroup> updatedGroupResult = _groupProcessor.Get(adGroup.DistinguishedName);
        Assert.That(updatedGroupResult.IsSuccess, Is.True, "[H_100] Failed to retrieve updated group from AD.");
        ADpGroup updatedGroup = updatedGroupResult.Value;
        _groupProcessor.GetMembers(updatedGroup);
        Assert.That(updatedGroup.Members.Count, Is.EqualTo(2), "[H_110] The group should have 2 members after the update.");


        // I  --> Act 6 - Remove a member from the group
        updatedGroup.RemoveUserFromGroup(userDistinguishedNames[1]);

        // J --> Act 7 - Update group in AD - this time to remove the member
        Result jResult = _groupProcessor.Update(updatedGroup);
        Assert.That(jResult.IsSuccess, Is.True, "[J_100] Failed to update group in AD after member removal.");

        // K --> Verify - Retrieve the group again and check members
        Result<ADpGroup> finalGroupResult = _groupProcessor.Get(updatedGroup.DistinguishedName);
        Assert.That(finalGroupResult.IsSuccess, Is.True, "[K_100] Failed to retrieve final group from AD.");
        ADpGroup finalGroup = finalGroupResult.Value;
        _groupProcessor.GetMembers(finalGroup);
        Assert.That(finalGroup.Members.Count, Is.EqualTo(1), "[K_110] The group should have 1 member after removal.");
        
        bool c = ADpUser.EqualSameObject(finalGroup.Members[0], userDistinguishedNames[0]);
        Assert.That(c, Is.True, "[K_120] The remaining member is incorrect.");
    }

    
    /// <summary>
    /// Confirm that when adding new members, we check to see if they are in the remove list as well.  If they are they cancel each other out.
    /// </summary>
    [Test]
    public void AddRemoveMembers_CheckOtherList_Success()
    {
        // A --> Setup
        ADpGroup group = new ADpGroup("test", asi.UnitTestRoot);
        group.AddUserToGroup("cn=abc");
        Assert.That(group.NewMembers.Count, Is.EqualTo(1), "[A_100] The group should have 1 new member.");
        
        // B --> Act
        group.RemoveUserFromGroup("cn=abc");
        Assert.That(group.NewMembers.Count, Is.EqualTo(0), "[B_100] The group should have 0 new members after removal.");
        Assert.That(group.RemovedMembers.Count, Is.EqualTo(0), "[B_110] The group should have 1 removed member.");
    }
}

