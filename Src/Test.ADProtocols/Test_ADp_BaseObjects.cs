
using AD.Protocols;
using AD.Protocols.ADObjects;
using SlugEnt.AD.Protocols.Attributes;

using UT.CustomSupportObjects;

namespace Test.ADProtocols;

[TestFixture]
public class Test_ADp_BaseObjects
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
    /// Confirm that when creating a new Base object that the list of changed attributes is empty and does not change after creation.
    /// </summary>
    [Test]
    public void BaseObject_InitialCreation_DoesNotChangeAttributesList()
    {
        // A Setup
        // Create an OU object
        ADpOrgUnit ou = new ADpOrgUnit();

        // B Act
        ou.CommonName = "test";

        Assert.That(ou.DebugAttributesToUpdate, Is.Empty, "[V_100]  Attributes changed should be empty after initial creation.");
    }


    /// <summary>
    /// Confirms that after initial creation, changing or updating attributes will add them to AttributesToUpdate list
    /// </summary>
    [Test]
    public void BaseObject_UpdateField_AddsToChangedAttributes()
    {
        // Setup - This constructor will create object and changes after construction will be tracked.
        ADpOrgUnit ou = new ADpOrgUnit("Test", new ADSPath("OU=Test,DC=example,DC=com"));


        // B Act
        string newName = "TestChange";
        ou.CommonName = newName;

        // Verify
        Assert.That(ou.CommonName, Is.EqualTo(newName), "[V_100]");
        Assert.That(ou.DebugAttributesToUpdate, Is.Not.Empty, "[V_110]  Attributes changed should not be empty after update.");
        Assert.That(ou.DebugAttributesToUpdate.First().Value.Name, Is.EqualTo("cn"), "[V_120]  AttributeBase should not be null.");
    }


    /// <summary>
    /// Puts the AttributeRetrieverMgr through a full cycle of adding attributes, retrieving them, and clearing them, verifying the state at each step.
    /// </summary>
    [Test]
    public void AttrRetrieveMgr_FullCycle_Success()
    {
        // A --> Setup
        AttributeRetrieverMgr attributeRetrieverMgr = new AttributeRetrieverMgr();
        Assert.That(attributeRetrieverMgr.Count, Is.EqualTo(2), "[A_100] Length of initial attributes array should be 2.");
        
        attributeRetrieverMgr.AddAttribute("cn");
        
        
        // C --> Act
        string[] attrFinal = attributeRetrieverMgr.Attributes;

        // V --> Verify the initial state of the attributes array
        Assert.That(attrFinal.Length, Is.EqualTo(3), "[V_100] Length of initial attributes array should be 2.");
        Assert.That(attrFinal.Contains("cn"), Is.True, "[V_110] Initial array should contain 'cn'.");
        Assert.That(attrFinal.Contains("distinguishedName"), Is.True, "[V_120] Initial array should contain 'distinguishedName'.");
        Assert.That(attrFinal.Contains("name"), Is.True, "[V_130] Initial array should contain 'name'.");
        
        // Is finalized should have been set when we retrieved the attributes.
        Assert.That(attributeRetrieverMgr.IsFinalized, Is.True, "[V_200] Attribute retriever should be finalized.");
        
        // Act - Verify that attributes are added
        Assert.That(attributeRetrieverMgr.Count, Is.EqualTo(3), "[V_210] Count should be 3.");

        // Act - Verify that adding a duplicate does not change the count
        attributeRetrieverMgr.AddAttribute("cn");
        Assert.That(attributeRetrieverMgr.Count, Is.EqualTo(3), "[V_210] Count should remain 3 after adding duplicate.");
        
        // Is finalized should not have changed since it was a duplicate.
        Assert.That(attributeRetrieverMgr.IsFinalized, Is.True, "[V_210] Attribute retriever should remain finalized after adding duplicate.");
        

        // Act - Clear the list and verify it is empty
        attributeRetrieverMgr.Clear();
        Assert.That(attributeRetrieverMgr.Count, Is.EqualTo(2), "[V_250] Count should be 2 (Default Required Attributes) after clear.");
        Assert.That(attributeRetrieverMgr.IsFinalized, Is.False, "[V_260] Attribute retriever should not be finalized after clear.");

        // Act - Verify that retrieving Attributes after clearing still works and is empty
        string[] attributes = attributeRetrieverMgr.Attributes;
        Assert.That(attributes.Length, Is.EqualTo(0), "[V_260] Length should be 0 after clear.");
    }


    [Test]
    public void UserAccountControl_HasChangedValue_ReturnsTrueWhenValueChanged()
    {
        // A --> Setup
        var uac = new UserAccountControl(null);

        // B --> Setup Verify
        Assert.That(uac.Value, Is.EqualTo(0), "[B_100] Initial value should be equal to the provided initial value.");

        // C --> Act
        uac.DisableAccount();

        // V --> Verify
        Assert.That(uac.Value, Is.EqualTo((int)UserAccountControl.UserAccountControlFlags.AccountDisabled), "[V_100] Value should be 0 after enabling account.");
        Assert.That(uac.HasChangedValue, Is.True, "[V_110] Should report changed when AccountEnabled is set.");
    }

    
    [TestCase(10000)]
    [TestCase(20)]
    [Test]
    public void UserAccountControl_WithInitializedValue_ReturnsTrueWhenValueChanged(int initialValue)
    {
        // A --> Setup
        UserAccountControlOnChangeTester uacTester = new();
        var                              uac       = new UserAccountControl(initialValue, uacTester.Changed);

        // B --> Setup Verify
        Assert.That(uac.Value, Is.EqualTo(initialValue), "[B_100] Initial value should be equal to the provided initial value.");
        Assert.That(uac.HasChangedValue,Is.False,"[B_110] Should not report changed when initialized value is set.");

        // C --> Act
        uac.DisableAccount();

        // V --> Verify
        Assert.That(uac.Value, Is.Not.EqualTo(initialValue), "[V_100] Value should be different after disabling account.");
        Assert.That(uac.HasChangedValue, Is.True, "[V_110] Should report changed when AccountEnabled is set.");
        Assert.That(uacTester.Value, Is.EqualTo(uac.Value), "[V_120] OnChange should have been called with the new value.");
    }

}


public class UserAccountControlOnChangeTester
{
    public int Value;
    
    public void Changed(int value)
    {
        Value = value;
    }
}