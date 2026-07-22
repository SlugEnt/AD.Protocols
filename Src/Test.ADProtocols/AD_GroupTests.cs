using SlugEnt.FluentResults;
using SlugEnt.AD.Protocols;
using SlugEnt.IS;
using System.DirectoryServices.Protocols;

namespace UT.ActiveDirectory_Tests;


/// <summary>
/// Tests Active Directory Group Functions
/// </summary>
[TestFixture]
public class AD_GroupTests
{
#pragma warning disable IDE0079
#pragma warning disable NUnit2045
    [Test]
    public void FindGroup ()
    {
        throw new NotImplementedException();


        // A --> Setup
        Ad_SupportInitializer asi = Ad_SupportInitializer.GetInitializer(true);

        string searchFilter = ADLDAPEngine.SEARCH_FILTER_ALL_GROUPS;
        List<string> attributes = [];
        ADpReadOnlyGroup.AddBaseAttributes(attributes);
        ADpReadOnlyGroup.AddInfoAttributes(attributes);
        ADpReadOnlyGroup.AddStatisticAttributes(attributes);
        ADpReadOnlyGroup.AddMemberAttribute(attributes);
        
        ADSPath grp =asi.UnitTestParent;
        Result<ADpReadOnlyGroup> resultF = asi.AdEngine.GroupFindSingle("OU=Temp,OU=UT_Groups,DC=ycy4y,DC=local",
                                                                        SearchScope.OneLevel,
                                                                        searchFilter,
                                                                        attributes);
        Assert.That(resultF.IsSuccess, Is.True, "Z-100:  Failed to find Group --> AppError: " + resultF.ToStringWithLineFeeds());
        //ADpReadOnlyGroup group = resultF.Value;
    }


    /// <summary>
    /// Tests Creating a new group.
    /// </summary>
    [Test]
    [TestCase(EnumGroupType.Domain_Local_Security)]
    [TestCase(EnumGroupType.Domain_Local_Distribution)]
    [TestCase(EnumGroupType.Global_Security)]
    [TestCase(EnumGroupType.Global_Distribution)]
    [TestCase(EnumGroupType.Universal_Security)]
    [TestCase(EnumGroupType.Universal_Distribution)]
    public void GroupAdd(EnumGroupType groupType)
    {
        // A --> Setup
        Ad_SupportInitializer asi = Ad_SupportInitializer.GetInitializer(true);

        // Create a random group OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu(asi.UnitTestParent);
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());
        Assert.That(newOuResult.Value,Is.Not.Null,"A-110: The value returned was null.  Should have been an object.  ");
        HelperMethods.DisplayOu(newOuResult.Value.Path);


        // Create Test Group
        string          groupName = SupportMethods.Faker.Commerce.ProductName();
        ADpGroupUpdater testGroup = new(groupName,groupType);

        HelperMethods.DisplayGroup(testGroup);

        Result resultA =  asi.AdEngine.GroupAdd(newOuResult.Value.Path, testGroup);
        Assert.That(resultA.IsSuccess,Is.True,"A-120: Failed to add group.  Errors: " + resultA.ToStringWithLineFeeds());


        // B. --> Verify Group was created
        Result<ADpReadOnlyGroup> resultB = ReadAndVerifyGroup(asi.AdEngine,
                                                              newOuResult.Value.Path,
                                                              [],
                                                              ADLDAPEngine.SEARCH_FILTER_ALL_GROUPS);
        ADpReadOnlyGroup group = resultB.Value;


        // C. Validate
        Assert.That(group.GroupType, Is.EqualTo(groupType), "C-100:  Group Type is not correct");
        Assert.That(group.DisplayName, Is.EqualTo(testGroup.DisplayNameChg), "C-200:  Display Name is not correct");
        Assert.That(group.Description, Is.EqualTo(testGroup.DescriptionChg), "C-300:  Description is not correct");
        Assert.That(group.Email, Is.EqualTo(testGroup.EmailChg), "C-400:  Mail is not correct");
        Assert.That(group.SAMAccount, Is.EqualTo(testGroup.SAMAccountChg), "C-500:  SamAccountName is not correct");
    }


    [Test]
    public void GroupUpdate()
    {
        // A --> Setup
        Ad_SupportInitializer asi = Ad_SupportInitializer.GetInitializer(true);


        // Create a random group OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu(asi.UnitTestParent);
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());
        HelperMethods.DisplayOu(newOuResult.Value.Path);


        // Create Test Group
        string          groupName = SupportMethods.Faker.Commerce.ProductName();
        int             i         = Random.Shared.Next(1, 6);
        int             j         = i * 5;
        ADpGroupUpdater testGroup = new(groupName, (EnumGroupType)j);

        HelperMethods.DisplayGroup(testGroup);

        Result resultA = asi.AdEngine.GroupAdd(newOuResult.Value.Path, testGroup);
        Assert.That(resultA.IsSuccess, Is.True, "A-100: Failed to add group.  Errors: " + resultA.ToStringWithLineFeeds());


        // B.  Validate the group was created
        Result<ADpReadOnlyGroup> resultB = ReadAndVerifyGroup(asi.AdEngine,
                                                              newOuResult.Value.Path,
                                                              [],
                                                              ADLDAPEngine.SEARCH_FILTER_ALL_GROUPS);
        ADpReadOnlyGroup group = resultB.Value;


        // C. Update the group.
        ADpGroupUpdater group2 = new(group)
        {
            DisplayNameChg = "Updated " + SupportMethods.Faker.Commerce.ProductName(),
            SAMAccountChg = "Upd" + group.SAMAccount,
            DescriptionChg = "Newly minted Description"
        };
        asi.AdEngine.GroupUpdate(group2);


        // D.  Re-read the group
        resultB = ReadAndVerifyGroup(asi.AdEngine,
                                     newOuResult.Value.Path,
                                     [],
                                     ADLDAPEngine.SEARCH_FILTER_ALL_GROUPS);
        Assert.That(resultB.IsSuccess, Is.True, "D-100:  Failed to find Group --> AppError: " + resultB.ToStringWithLineFeeds());
        group = resultB.Value;

        // E. Validate
        Assert.That(group.DisplayName, Is.EqualTo(group2.DisplayNameChg), "E-200:  Display Name is not correct");
        Assert.That(group.Description, Is.EqualTo(group2.DescriptionChg), "E-300:  Description is not correct");
        Assert.That(group.Email, Is.EqualTo(group2.EmailChg), "E-400:  Mail is not correct");
        Assert.That(group.SAMAccount, Is.EqualTo(group2.SAMAccountChg), "E-500:  SamAccountName is not correct");
    }


    [Test]
    public void DeleteGroup()
    {
        // A --> Setup
        Ad_SupportInitializer asi = Ad_SupportInitializer.GetInitializer(true);


        // Create a random user OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu(asi.UnitTestParent);
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());

        // C.  Act
        // Create Test Group
        string          groupName = SupportMethods.Faker.Commerce.ProductName();
        int             i         = Random.Shared.Next(1, 6)*5;
        ADpGroupUpdater testGroup = new(groupName, (EnumGroupType)i);

        HelperMethods.DisplayGroup(testGroup);

        Result resultA = asi.AdEngine.GroupAdd(newOuResult.Value.Path, testGroup);
        Assert.That(resultA.IsSuccess, Is.True, "C-200: Failed to create group.  Errors: " + resultA.ToStringWithLineFeeds());


        // B.  Validate the group was created
        Result<ADpReadOnlyGroup> resultB = ReadAndVerifyGroup(asi.AdEngine,
                                                              newOuResult.Value.Path,
                                                              [],
                                                              ADLDAPEngine.SEARCH_FILTER_ALL_GROUPS);
        ADpReadOnlyGroup group = resultB.Value;


        // C. Delete the group
        Result<DeleteResponse> deleteResult = asi.AdEngine.GroupDelete(group.DistinguishedName);
        Assert.That(deleteResult.IsSuccess, Is.True, "C-100:  Group delete failed - " + deleteResult.ToStringWithLineFeeds());


        // D .  Verify the user is deleted
        resultB = ReadAndVerifyGroup(asi.AdEngine,
                                     newOuResult.Value.Path,
                                     [],
                                     ADLDAPEngine.SEARCH_FILTER_ALL_GROUPS, true);

        Assert.That(resultB.IsFailed, Is.True, "Z-100:  Group was not successfully deleted --> AppError: " + resultB.ToStringWithLineFeeds());
    }





    [Test]
    public void MoveGroup()
    {
        // A --> Setup
        Ad_SupportInitializer asi = Ad_SupportInitializer.GetInitializer(true);

        // Create a random OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu();
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());

        // Create a second random OU we will move the group to
        Result<ADSPath> moveToOuResult = asi.CreateRandomOu();
        Assert.That(moveToOuResult.IsSuccess,
                    Is.True,
                    "A-100: Unable to create the unique containing OU for the move destination.  Errors: " + moveToOuResult.ToStringWithLineFeeds());


        // Create Test Group
        string          groupName = SupportMethods.Faker.Commerce.ProductName();
        int             i         = Random.Shared.Next(1, 6)*5;
        ADpGroupUpdater testGroup = new(groupName, (EnumGroupType)i);

        HelperMethods.DisplayGroup(testGroup);

        Result resultA = asi.AdEngine.GroupAdd(newOuResult.Value.Path, testGroup);
        Assert.That(resultA.IsSuccess, Is.True, "A-100: Failed to create group.  Errors: " + resultA.ToStringWithLineFeeds());

        // B.  Validate the group was created
        Result<ADpReadOnlyGroup> resultB = ReadAndVerifyGroup(asi.AdEngine,
                                                              newOuResult.Value.Path,
                                                              [],
                                                              ADLDAPEngine.SEARCH_FILTER_ALL_GROUPS);
        ADpReadOnlyGroup group = resultB.Value;


        // C. Move the Group
        Result<string> moveResult = asi.AdEngine.GroupMove(group, moveToOuResult.Value.Path);
        Assert.That(moveResult.IsSuccess, Is.True, "C-100:  Group move failed - " + moveResult.ToStringWithLineFeeds());
        Console.WriteLine("Moving group to : " + moveToOuResult.Value.Path);


        // D .  Verify the group was moved
        resultB = ReadAndVerifyGroup(asi.AdEngine,
                                     newOuResult.Value.Path,
                                     [],
                                     ADLDAPEngine.SEARCH_FILTER_ALL_GROUPS,true);


        Assert.That(resultB.IsFailed, Is.True, "D-100:  Group was still found in old OU. --> AppError: " + resultB.ToStringWithLineFeeds());


        // E.  Now confirm the group is in the new location
        resultB = ReadAndVerifyGroup(asi.AdEngine,
                                     moveToOuResult.Value.Path,
                                     [],
                                     ADLDAPEngine.SEARCH_FILTER_ALL_GROUPS);


        Assert.That(resultB.IsSuccess, Is.True, "E-100:  Group was still found in old OU. --> AppError: " + resultB.ToStringWithLineFeeds());
    }


    [Test]
    public void RenameGroup()
    {
        // A --> Setup
        Ad_SupportInitializer asi = Ad_SupportInitializer.GetInitializer(true);

        // Create a random OU
        Result<ADSPath> newOuResult = asi.CreateRandomOu();
        Assert.That(newOuResult.IsSuccess, Is.True, "A-100: Unable to create the unique containing OU for this test.  Errors: " + newOuResult.ToStringWithLineFeeds());


        // Create Test Group
        string          groupName = SupportMethods.Faker.Commerce.ProductName();
        int             i         = Random.Shared.Next(1, 6)*5;
        ADpGroupUpdater testGroup = new(groupName, (EnumGroupType)i);

        HelperMethods.DisplayGroup(testGroup);

        Result resultA = asi.AdEngine.GroupAdd(newOuResult.Value.Path, testGroup);
        Assert.That(resultA.IsSuccess, Is.True, "A-100: Failed to create group.  Errors: " + resultA.ToStringWithLineFeeds());


        // B.  Validate the group was created
        Result<ADpReadOnlyGroup> resultB = ReadAndVerifyGroup(asi.AdEngine, newOuResult.Value.Path, [], ADLDAPEngine.SEARCH_FILTER_ALL_GROUPS);
        ADpReadOnlyGroup group = resultB.Value;


        // C. Rename the Group
        string         newName      = group.AD_CommonName + "XY";
        Result<string> renameResult = asi.AdEngine.GroupRename(group, newName);
        Assert.That(renameResult.IsSuccess, Is.True, "C-100:  Group rename failed - " + renameResult.ToStringWithLineFeeds());
        Console.WriteLine("Renamed group to : " + renameResult.Value);


        // D .  Verify the group was renamed.  
        resultB = ReadAndVerifyGroup(asi.AdEngine,
                                     newOuResult.Value.Path,
                                     [],
                                     ADLDAPEngine.SEARCH_FILTER_ALL_GROUPS);


        Assert.That(resultB.IsSuccess, Is.True, "D-100:  User was not found under new name. --> AppError: " + resultB.ToStringWithLineFeeds());
        Assert.That(resultB.Value.AD_CommonName, Is.EqualTo(newName),"d-200: Users common name was not updated correctly.");
    }



    private Result<ADpReadOnlyGroup> ReadAndVerifyGroup(ADLDAPEngine engine,  string ouPath, List<string> attributesList, string searchFilter = "", bool dontAssert=false)
    {
        if (string.IsNullOrEmpty(searchFilter)) searchFilter = ADLDAPEngine.SEARCH_FILTER_ALL_GROUPS;
        if (attributesList.Count == 0)
        {
            ADpReadOnlyGroup.AddBaseAttributes(attributesList);
            ADpReadOnlyGroup.AddInfoAttributes(attributesList);
            ADpReadOnlyGroup.AddStatisticAttributes(attributesList);
        }

        Result<ADpReadOnlyGroup> resultF = engine.GroupFindSingle(ouPath,
                                                                    SearchScope.OneLevel,
                                                                    searchFilter,
                                                                    attributesList);
        if (!dontAssert)
        {
            Assert.That(resultF.IsSuccess, Is.True, "ReadAndVerifyGroup: VGC-100:  Failed to find Group --> AppError: " + resultF.ToStringWithLineFeeds());
        }
        else
        {
            Console.WriteLine("ReadAndVerifyGroup: VGC-100:  Failed to find Group, Was told not to Assert! --> AppError: " + resultF.ToStringWithLineFeeds());
            return Result.Fail("Group Not Found");
        }

        ADpReadOnlyGroup group = resultF.Value;
        return Result.Ok(group);

    }
#pragma warning restore NUnit2045
#pragma warning restore IDE0079
}
