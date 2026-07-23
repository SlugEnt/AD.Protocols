// This should normally be defined.  This allows unit tests to run without colliding into each other as everything happens in 
// a transaction that is never committed.  
//  - Undef it for specialized cases where you need to see what happened with a particular unit test case by looking at the 
//    database.
#define ENABLE_TRANSACTIONS

//using AutoMapper;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ILogger = Serilog.ILogger;

namespace UT;

/// <summary>
///     This sets up a Test Data class of common items that are needed for each test scenario
///     * A Mock File System
///     * A Database Connection to a Unit Test Database
///     * The DocumentServerEngine
///     * Some Support Methods that can be used during testing
/// </summary>
public partial class SupportMethods
{
    private static          Faker?                      _faker = new();
    private static          ILogger?                    serilog;
    private static readonly object                      _lock = new();
    
    private static          bool                        _oneTimeSetupCompleted = false;


    /// <summary>
    ///     The Preferred Constructor...
    /// </summary>
    /// <param name="smConfiguration"></param>
    public SupportMethods()
    {
    }



    /// <summary>
    ///     Returns the faker instance
    /// </summary>
    public static Faker Faker => _faker!;




    /// <summary>
    ///     This is the Initialize Task.  Must be called to finalize setup of key objects
    /// </summary>
    public Task? Initialize { get; private set; }



    /// <summary>
    ///     Returns True if all initialization is completed.
    /// </summary>
    public static bool IsInitialized { get; private set; }


    /// <summary>
    ///     Returns the Actual Serilog logger
    /// </summary>
    public ILogger Logger => serilog!;



    /// <summary>
    ///     Provides an IServiceCollection
    /// </summary>
    public static IServiceCollection? Services { get; private set; }



    public string PrintArg(string name,
                           object value)
    {
        return " " + name + " [ " + value + " ] ";
    }



    /// <summary>
    ///     Setsup the Services Collection if it has not been done so already
    /// </summary>
    private void SetupServices()
    {
        if (Services == null)
        {
            Services = new ServiceCollection();
        }
    }


    /// <summary>
    /// Converts the Active Directory Configuration Domain value which is in domain.com format to LDAP style (dc=domain,dc=com)
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string DomainInLDAPStyle()
    {
        string domain = SupportMethods.ActiveDirectoryConfiguration?.Domain ?? "";
        if (domain == "")
            throw new ArgumentNullException("Domain", "Domain is not set in the Active Directory Configuration");

        string[] domainParts = domain.Split('.');
        string   ldapStyle   = "";
        foreach (string domainPart in domainParts)
        {
            if (ldapStyle != "")
                ldapStyle += ",";
            ldapStyle += "dc=" + domainPart;
        }

        return ldapStyle;
    }


}
#pragma warning restore CS8600
