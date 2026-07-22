using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.AD.Protocols;



/// <summary>
/// This class contains common constants for Active Directory attribute names
/// </summary>
public class ADpCommon
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public const string ATN_SAM          = "sAMAccountName";
    public const string ATN_CN           = "cn";
    public const string ATN_DISTINGUISHED_NAME = "distinguishedName";
    public const string ATN_DEPARTMENT   = "department";
    public const string ATN_DESCRIPTION  = "description";
    public const string ATN_DISPLAYNAME  = "displayName";
    public const string ATN_DN           = "distinguishedName";
    public const string ATN_EMAIL        = "mail";
    public const string ATN_FIRSTNAME    = "givenName";
    public const string ATN_LASTNAME     = "sn";
    public const string ATN_MANAGER      = "manager";
    public const string ATN_PASSWORD     = "unicodePwd";
    public const string ATN_PASSWORD_LAST_SET = "pwdLastSet";
    public const string ATN_USER_ACCOUNT_CONTROL = "userAccountControl";
    public const string ATN_PHONE        = "telephoneNumber";
    public const string ATN_TITLE        = "title";
    public const string ATN_UAC          = "userAccountControl";
    public const string ATN_UPN          = "userPrincipalName";
    public const string ATN_GROUPTYPE    = "groupType";
    public const string ATN_OBJECT_CLASS = "objectClass";
    public const string ATN_NAME         = "name";
    public const string ATN_LOCKOUT_TIME = "lockoutTime";

    public const string ATN_PASSPOL_MINLENGTH = "msDS-MinimumPasswordLength";
    public const string ATN_PASSPOL_MINAGE = "msDS-MinimumPasswordAge";
    public const string ATN_PASSPOL_MAXAGE = "msDS-MaximumPasswordAge";
    public const string ATN_PASSPOL_HISTLEN = "msDS-PasswordHistoryLength";
    public const string ATN_PASSPOL_LOCKOUTTHRESHOLD = "msDS-LockoutThreshold";
    public const string ATN_PASSPOL_COMPLEXITYENABLED = "msDS-PasswordComplexityEnabled";
    public const string ATN_PASSPOL_REVERSEDENCRYPTIONENABLED = "msDS-PasswordReversibleEncryptionEnabled";
    public const string ATN_PASSPOL_PRECEDENCE = "msDS-PasswordSettingsPrecedence";
    public const string ATN_PASSPOL_APPLIESTO = "msDS-PSOAppliesTo";
    public const string ATN_PASSPOL_LOCKOUTOBSERVATIONWINDOW = "msDS-LockoutObservationWindow";
    public const string ATN_PASSPOL_LOCKOUTDURATION = "msDS-LockoutDuration";

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
