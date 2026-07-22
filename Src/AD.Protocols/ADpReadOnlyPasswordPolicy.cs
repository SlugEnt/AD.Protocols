
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography;

namespace SlugEnt.AD.Protocols;

public class ADpReadOnlyPasswordPolicy
    {
        public const string ROOT_OU_PATH = "CN=Password Settings Container,CN=System";

    /// <summary>
    /// Name of the orgUnit
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///  THe Active Directory CN name for this object.
    /// </summary>
    public string? CommonName { get; protected set; }

    /// <summary>
    /// Description of the orgUnits purpose
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display Name for the orgUnit
    /// </summary>
    public string? DisplayName { get; protected set; }

    /// <summary>
    /// The full DN of the orgUnit
    /// </summary>
    public string? DistinguishedName { get; protected set; }


    /// <summary>
    /// When orgUnit was last changed
    /// </summary>
    public DateTimeOffset WhenChanged { get; protected set; }

    /// <summary>
    /// When orgUnit was created
    /// </summary>
    public DateTimeOffset WhenCreated { get; protected set; }

    public int PasswordMinLength { get; protected set; }

    public TimeSpan PasswordMinAge { get; protected set; }

    public TimeSpan PasswordMaxAge { get; protected set; }

    public int PasswordHistoryLength { get; protected set; }

    public int PasswordLockThreshold { get; protected set; }

    public bool PasswordComplexityEnabled { get; protected set; }

    public bool PasswordReversibleEncryptionEnabled { get; protected set; }

    public int PasswordSettingsPrecedence { get; protected set; }

    public TimeSpan PasswordLockoutObservationWindow { get; protected set; }

    public TimeSpan PasswordLockoutDuration { get; protected set; }


    /// <summary>
    /// A list of Distinguished names of users or security groups who the policy applies to.
    /// </summary>
    public List<string> AppliesTo { get; protected set; }


    /// <summary>
    /// Adds the Base attributes required of a Password Policy Object
    /// </summary>
    /// <param name="attributeList"></param>
    public static void AddBaseAttributes(List<string> attributeList)
    {
        attributeList.Add("cn");
        attributeList.Add("name");
        attributeList.Add("description");
        attributeList.Add("displayName");
        attributeList.Add("distinguishedName");
        attributeList.Add("whenCreated");
        attributeList.Add("whenChanged");
        attributeList.Add(ADpCommon.ATN_PASSPOL_MINLENGTH);
        attributeList.Add(ADpCommon.ATN_PASSPOL_MINAGE);
        attributeList.Add(ADpCommon.ATN_PASSPOL_MAXAGE);
        attributeList.Add(ADpCommon.ATN_PASSPOL_HISTLEN);
        attributeList.Add(ADpCommon.ATN_PASSPOL_COMPLEXITYENABLED);
        attributeList.Add(ADpCommon.ATN_PASSPOL_LOCKOUTTHRESHOLD);
        attributeList.Add(ADpCommon.ATN_PASSPOL_REVERSEDENCRYPTIONENABLED);
        attributeList.Add(ADpCommon.ATN_PASSPOL_PRECEDENCE);
        attributeList.Add(ADpCommon.ATN_PASSPOL_APPLIESTO);
        attributeList.Add(ADpCommon.ATN_PASSPOL_LOCKOUTOBSERVATIONWINDOW);
        attributeList.Add(ADpCommon.ATN_PASSPOL_LOCKOUTDURATION);
        attributeList.Add("msDS-PasswordSettingsPrecedence");


    }



    /// <summary>
    /// Createa new Read Only Org Unit object from the SearchResultAttributeCollection
    /// </summary>
    /// <param name="attributes"></param>
    /// <returns></returns>
    public static Result<ADpReadOnlyPasswordPolicy> CreatePasswordPolicyObj(SearchResultAttributeCollection attributes)
    {
        ADpReadOnlyPasswordPolicy pwdPolicy              = new();
        bool                      distinguishedNameFound = false;
        Result                    result                 = new Result();

        foreach (DirectoryAttribute dirObj in attributes.Values)
        {
            try
            {
                switch (dirObj.Name)
                {
                    case "description": pwdPolicy.Description = dirObj[0].ToString(); break;
                    case "distinguishedName":
                        pwdPolicy.DistinguishedName = dirObj[0].ToString();
                        distinguishedNameFound      = true;
                        break;
                    case "displayName": pwdPolicy.DisplayName = dirObj[0].ToString(); break;
                    case "whenChanged": pwdPolicy.WhenChanged = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!); break;
                    case "whenCreated": pwdPolicy.WhenCreated = ADFunctions.GetDateTime_FromLDAPProperty(dirObj[0].ToString()!); break;
                    case "name":        pwdPolicy.Name        = dirObj[0].ToString(); break;
                    case "msDS-LockoutObservationWindow":
                        Result<TimeSpan> lockoutObsWindow = ADFunctions.GetTimeSpan_FromAD(dirObj[0].ToString()!);
                        pwdPolicy.PasswordLockoutObservationWindow = lockoutObsWindow.Value;

                        break;
                    case "msDS-MinimumPasswordLength": pwdPolicy.PasswordMinLength = int.Parse(dirObj[0].ToString()!); break;
                    case "msDS-MinimumPasswordAge":
                        Result<TimeSpan> minAgeResult = ADFunctions.GetTimeSpan_FromAD(dirObj[0].ToString()!);
                        if (minAgeResult.IsFailed)
                        {
                            return Result.Fail<ADpReadOnlyPasswordPolicy>($"Failed to parse msDS-MaximumPasswordAge: [ {dirObj[0].ToString()} ] into a TimeSpan");
                        }

                        pwdPolicy.PasswordMinAge = minAgeResult.Value;

                        break;

                    case "msDS-MaximumPasswordAge":
                        Result<TimeSpan> maxAgeResult = ADFunctions.GetTimeSpan_FromAD(dirObj[0].ToString()!);
                        if (maxAgeResult.IsFailed)
                        {
                            return Result.Fail<ADpReadOnlyPasswordPolicy>($"Failed to parse msDS-MaximumPasswordAge: [ {dirObj[0].ToString()} ] into a TimeSpan");
                        }

                        pwdPolicy.PasswordMaxAge = maxAgeResult.Value;

                        break;
                    case "msDS-PasswordHistoryLength": pwdPolicy.PasswordHistoryLength = int.Parse(dirObj[0].ToString()!); break;
                    case "msDS-LockoutThreshold":      pwdPolicy.PasswordLockThreshold = int.Parse(dirObj[0].ToString()!); break;
                    case "msDS-LockoutDuration":
                        Result<TimeSpan> lockoutDurationResult = ADFunctions.GetTimeSpan_FromAD(dirObj[0].ToString()!);
                        if (lockoutDurationResult.IsFailed)
                        {
                            return Result.Fail<ADpReadOnlyPasswordPolicy>($"Failed to parse msDS-LockoutDuration: [ {dirObj[0].ToString()} ] into a TimeSpan");
                        }

                        pwdPolicy.PasswordLockoutDuration = lockoutDurationResult.Value;
                        break;
                    case "msDS-PasswordComplexityEnabled":           pwdPolicy.PasswordComplexityEnabled           = bool.Parse(dirObj[0].ToString()!); break;
                    case "msDS-PasswordReversibleEncryptionEnabled": pwdPolicy.PasswordReversibleEncryptionEnabled = bool.Parse(dirObj[0].ToString()!); break;
                    case "msDS-PasswordSettingsPrecedence":          pwdPolicy.PasswordSettingsPrecedence          = int.Parse(dirObj[0].ToString()!); break;
                    case "msDS-PSOAppliesTo":
                        pwdPolicy.AppliesTo = new List<string>();
                        for (int i = 0; i < dirObj.Count; i++)
                        {
                            pwdPolicy.AppliesTo.Add(dirObj[i].ToString()!);
                        }

                        break;
                    case "cn": pwdPolicy.CommonName = dirObj[0].ToString(); break;

                    default:
                        string msg = "Unexpected data - " + dirObj.Name;
                        break;
                }
            }
            catch (Exception e)
            {
                result.AddError(new ExceptionalError($"Error trying to create ADpReadOnlyPasswordPolicy object from AD attributes.  Attribute name: [ {dirObj.Name} ]", e));
            }

        }

        // At end of process we clear the list of modified attributes, because we used the methods to update them, they appear as modified, when in fact they were not.
        

        if (!distinguishedNameFound)
        {
            result.AddError(new Error("No distinguished Name found in the policy object.  This is a mandatory field."));
        }

        if (result.Errors.Count > 0)
            return result;

        return Result.Ok(pwdPolicy);
    }

}

