

using SlugEnt.AD.Protocols;
using UT.CustomSupportObjects;

namespace Test.ADProtocols;
public class Test_LoginAuth
{
    private Ad_SupportInitializer asi;


    [SetUp]
    public void Setup()
    {
        asi = Ad_SupportInitializer.GetInitializer();
        asi.Initialize();
    }


    /// <summary>
    /// Confirms can login to domain with userId, Password and Domain specified in the config.  Domain is not specified in the Config.
    /// </summary>
    [Test]
    public void LoginSuccess_NoDomain()
    {
        ActiveDirConfig config = new ActiveDirConfig()
        {
            AdPassword  = "T#sting2026",
            AdUser      = "UTAdmin",
            Domain      = "ycy4y.local",
            Server1FQDN = "ycdc1.ycy4y.local",
            Server2FQDN = "",
            Port        = 636,
            RootPath    = "",
            UserOu      = ""
        };

        ActiveDirectoryConnector ADConnector = new(null);
        ADConnector.Initialize(config);
        Assert.That(ADConnector.IsConnected, Is.True, "[V_100");
    }



    /// <summary>
    /// Confirms can login to domain with userId, Password specified in the config.  Username is a full UPN.
    /// </summary>
    [Test]
    public void LoginSuccess_UPN_User()
    {
        ActiveDirConfig config = new ActiveDirConfig()
        {
            AdPassword  = "T#sting2026",
            AdUser      = "UTAdmin@ycy4y.local",
            Domain      = "ycy4y.local",
            Server1FQDN = "ycdc1.ycy4y.local",
            Server2FQDN = "",
            Port        = 636,
            RootPath    = "",
            UserOu      = ""
        };

        ActiveDirectoryConnector ADConnector = new(null);
        ADConnector.Initialize(config);
        Assert.That(ADConnector.IsConnected, Is.True, "[V_100");
    }


    /// <summary>
    /// Confirms can login to domain with userId, Password and Domain specified in the config.
    /// </summary>
    [Test]
    public void LoginSuccess_DomainSpecified()
    {
        ActiveDirConfig config = new ActiveDirConfig()
        {
            AdPassword  = "T#sting2026",
            AdUser      = "UTAdmin",
            AdLoginUserDomain = "ycy4y.local",
            Domain      = "ycy4y.local",
            Server1FQDN = "ycdc1.ycy4y.local",
            Server2FQDN = "",
            Port        = 636,
            RootPath    = "",
            UserOu      = ""
        };

        ActiveDirectoryConnector ADConnector = new(null);
        ADConnector.Initialize(config);
        Assert.That(ADConnector.IsConnected, Is.True, "[V_100");
    }

}

