using AD.Protocols.ADObjects.Objects;
using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;

namespace AD.Protocols.ADObjects.Processors;

/// <summary>
/// Processor for Password Settings Customization.
/// </summary>
public class ADpPasswordPolicyProcessor : ADpGenericProcessor<ADpPasswordPolicy>
{
    public const string PASS_POLICY_OU = "CN=Password Settings Container,CN=System";
    
    public ADpPasswordPolicyProcessor(LdapConnection ldapConnection, ADSPath domainRoot) : base(ADpCommon.OBJ_CLASS_PASSWORD_POLICY, "Password Policy", ldapConnection)
    { 
        // Password Policy always has the same attributes. 
        AttrRetrieval_Default();

        ParentPath = domainRoot.BuildChildADSPath(PASS_POLICY_OU);
    }

    
    /// <summary>
    /// Constructor for building PasswordPolicy object from Active Directory Attributes.  
    /// </summary>
    /// <param name="attributes"></param>
    /// <returns></returns>
    protected override Result<ADpPasswordPolicy> CreateObjectFromAttributes(SearchResultAttributeCollection attributes)
    {
        ADpPasswordPolicy policy = new(attributes);
        ParentPath = new ADSPath(policy.DistinguishedName);
        
        return Result.Ok(policy);
    }

    
    /// <summary>
    /// Sets the Parent Path to be used for all saves of Password Policies.  They are always stored in the same folder off the root domain.
    /// </summary>
    public ADSPath ParentPath { get; private set; }
    

    /// <summary>
    /// Set Default Attributes to be retrieved if none are defined at time of retrieval from AD
    /// DisplayName, sAMAccountName, Description, CN and DistinguishedName
    /// </summary>
    internal override void AttrRetrieval_Default()
    {
        AttributeRetrieverMgr.AddAttribute("displayName");
        AttributeRetrieverMgr.AddAttribute("sAMAccountName");
        AttributeRetrieverMgr.AddAttribute("description");
        AttributeRetrieverMgr.AddAttribute("whenCreated");
        AttributeRetrieverMgr.AddAttribute("whenChanged");
        AttributeRetrieverMgr.AddAttribute(ADpCommon.ATN_PASSPOL_MINLENGTH);
        AttributeRetrieverMgr.AddAttribute(ADpCommon.ATN_PASSPOL_MINAGE);
        AttributeRetrieverMgr.AddAttribute(ADpCommon.ATN_PASSPOL_MAXAGE);
        AttributeRetrieverMgr.AddAttribute(ADpCommon.ATN_PASSPOL_HISTLEN);
        AttributeRetrieverMgr.AddAttribute(ADpCommon.ATN_PASSPOL_COMPLEXITYENABLED);
        AttributeRetrieverMgr.AddAttribute(ADpCommon.ATN_PASSPOL_LOCKOUTTHRESHOLD);
        AttributeRetrieverMgr.AddAttribute(ADpCommon.ATN_PASSPOL_REVERSEDENCRYPTIONENABLED);
        AttributeRetrieverMgr.AddAttribute(ADpCommon.ATN_PASSPOL_PRECEDENCE);
        AttributeRetrieverMgr.AddAttribute(ADpCommon.ATN_PASSPOL_APPLIESTO);
        AttributeRetrieverMgr.AddAttribute(ADpCommon.ATN_PASSPOL_LOCKOUTOBSERVATIONWINDOW);
        AttributeRetrieverMgr.AddAttribute(ADpCommon.ATN_PASSPOL_LOCKOUTDURATION);
        AttributeRetrieverMgr.AddAttribute("msDS-PasswordSettingsPrecedence");
    }


    /// <summary>
    /// Some objects need information from the Processor before they can be successfully saved....
    /// </summary>
    /// <param name="obj">Object being saved</param>
    /// <returns></returns>
    protected override Result ProcessorPreSave(ADpPasswordPolicy obj)
    {
        // Password Policy is always at the same ParentPath location.   However, the ADpPasswordPolciy object does not have access to get the Domain
        // Level root.  The pre-processor does.  So the pre-processor needs to make sure on AddNew's that the ParentPath is set and the DN is set.
        if (obj.IsNew)
        {
            // The default constructor for ADpPasswordPolicy does not set a Distinguished Name.
            // It assumes the password policy is always at the same location.
            // However, the object has to have a DN.
            // If it is not set, set it.
            if (obj.ParentPath == null)
            {
                obj.ParentPath = ParentPath;
            }
                
            if (string.IsNullOrEmpty(obj.DistinguishedName))
            {
                obj.BuildDistinguishedName();
            }
        }
        return base.ProcessorPreSave(obj);
    }
}

