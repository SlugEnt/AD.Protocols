using AD.Protocols.ADObjects;

namespace Test.ADProtocols;

[TestFixture]
public class Test_ADSPath
{
    [TestCase("CN=slug,OU=animals,DC=some,DC=local")]
    [TestCase("cn=scott,ou=people,dc=some,dc=local")]
    [TestCase("LDAP:")]
    [TestCase("LDAP://cn=mary,ou=people,dc=some,dc=local")]
    [TestCase("LDAP://ou=people,dc=some,dc=local")]
    [TestCase("LDAP://server.some.local")]
    [TestCase("LDAP://server.some.local:65000")]
    [TestCase("LDAP://server.some.local:65000/OU=people,dc=some,dc=local")]
    [TestCase("LDAP://server.some.local:65000/cn=mary smith,OU=people,dc=some,dc=local")]
    [TestCase("LDAP://server.some.local:65000/o=Petes Pizza,OU=people,dc=some,dc=local")]
    [Test]
    public void ADSPath_BuildPath_Success(string path)
    {
        ADSPath adsPath = new(path);

        string rebuild = adsPath.BuildFullPath();
        Assert.AreEqual(path, rebuild, "A10");
    }



    [TestCase("CN=slug,OU=animals,DC=some,DC=local", "CN=slug,OU=animals")]
    [TestCase("cn=scott,ou=people,dc=some,dc=local", "cn=scott,ou=people")]
    [TestCase("LDAP:", "")]
    [TestCase("LDAP://cn=mary,ou=people,dc=some,dc=local", "cn=mary,ou=people")]
    [TestCase("LDAP://ou=people,dc=some,dc=local", "ou=people")]
    [TestCase("LDAP://server.some.local", "")]
    [TestCase("LDAP://server.some.local:65000", "")]
    [TestCase("LDAP://server.some.local:65000/OU=people,dc=some,dc=local", "OU=people")]
    [TestCase("LDAP://server.some.local:65000/cn=mary smith,OU=people,dc=some,dc=local", "cn=mary smith,OU=people")]
    [TestCase("LDAP://server.some.local:65000/o=Petes Pizza,OU=people,dc=some,dc=local", "o=Petes Pizza,OU=people")]
    [Test]
    public void ADSPath_DistinguishedName_Success(string path,
                                                  string expDN)
    {
        ADSPath adsPath = new(path);
        Assert.AreEqual(expDN, adsPath.DN, "A10");
    }


    [TestCase("CN=slug,OU=animals,DC=some,DC=local", "")]
    [TestCase("cn=scott,ou=people,dc=some,dc=local", "")]
    [TestCase("LDAP:", "LDAP:")]
    [TestCase("LDAP://cn=mary,ou=people,dc=some,dc=local", "LDAP:")]
    [TestCase("LDAP://ou=people,dc=some,dc=local", "LDAP:")]
    [TestCase("LDAP://server.some.local", "LDAP://server.some.local")]
    [TestCase("LDAP://server.some.local:65000", "LDAP://server.some.local:65000")]
    [TestCase("LDAP://server.some.local:65000/OU=people,dc=some,dc=local", "LDAP://server.some.local:65000")]
    [TestCase("LDAP://server.some.local:65000/cn=mary smith,OU=people,dc=some,dc=local", "LDAP://server.some.local:65000")]
    [TestCase("LDAP://server.some.local:65000/o=Petes Pizza,OU=people,dc=some,dc=local", "LDAP://server.some.local:65000")]
    [Test]
    public void ADSPath_Prefix_Success(string path,
                                       string expPrefix)
    {
        ADSPath adsPath = new(path);
        Assert.AreEqual(expPrefix, adsPath.Prefix, "A10");
    }


    [TestCase("CN=slug,OU=animals,DC=some,DC=local", "DC=some,DC=local")]
    [TestCase("cn=scott,ou=people,dc=some,dc=local", "dc=some,dc=local")]
    [TestCase("LDAP:", "")]
    [TestCase("LDAP://cn=mary,ou=people,dc=some,dc=local", "dc=some,dc=local")]
    [TestCase("LDAP://ou=people,dc=some,dc=local", "dc=some,dc=local")]
    [TestCase("LDAP:", "")]
    [TestCase("LDAP://server.some.local", "")]
    [TestCase("LDAP://server.some.local:65000", "")]
    [TestCase("LDAP://server.some.local:65000/OU=people,dc=some,dc=local", "dc=some,dc=local")]
    [TestCase("LDAP://server.some.local:65000/cn=mary smith,OU=people,dc=some,dc=local", "dc=some,dc=local")]
    [TestCase("LDAP://server.some.local:65000/o=Petes Pizza,OU=people,dc=some,dc=local", "dc=some,dc=local")]
    [Test]
    public void DistinguishedName_Suffix_Success(string original,
                                                 string expSuffix)
    {
        //string dn = "CN=scott,OU=people,dc=some,dc=local";
        //string expSuffix = "dc=some,dc=local";

        ADSPath adsPath = new(original);

        Assert.AreEqual(expSuffix, adsPath.Suffix, "A10: ");
    }


    [TestCase("LDAP://server.some.local:65000/cn=mary smith,OU=people,dc=some,dc=local", "mary smith")]
    [TestCase("cn=scott,ou=people,dc=some,dc=local", "scott")]
    [TestCase("cn=scott,ou=people", "scott")]
    [TestCase("cn=scott", "scott")]
    [TestCase("ou=people,ou=us,ou=North America,dc=some,dc=local", "")]
    [Test]
    public void FindCN(string path,
                       string expected)
    {
        string cn = ADSPath.FindCN(path);
        Assert.AreEqual(expected, cn, "A10: ");
    }


    [TestCase("abc.xyz", "dc=abc,dc=xyz")]
    [TestCase("abc.mno.xyz", "dc=abc,dc=mno,dc=xyz")]
    [Test]
    public void FromDomainName(string domainName,
                               string expected)
    {
        ADSPath adsPath = ADSPath.FromDomainName(domainName);
        Assert.That(expected, Is.EqualTo(adsPath.Path), "A10: ");
    }


    [TestCase("cn=scott,ou=people,dc=some,dc=local", "ou=hired", "ou=hired,ou=people,dc=some,dc=local")]

    // TODO - what to do...Not sure we can get a valid child		[TestCase("LDAP:", "LDAP:")]
    [TestCase("LDAP://cn=mary,ou=people,dc=some,dc=local", "OU=giveraise,OU=high performer", "LDAP://OU=giveraise,OU=high performer,ou=people,dc=some,dc=local")]
    [TestCase("LDAP://dc=some,dc=local", "ou=greatchild", "LDAP://ou=greatchild,dc=some,dc=local")]

    // TODO what to do... Not sure this is valide		[TestCase("LDAP://server.some.local", "ou=firstone","LDAP://server.some.local/ou=firstone")]
    [TestCase("LDAP://server.some.local:65000", "ou=first", "LDAP://server.some.local:65000/ou=first")]
    [TestCase("LDAP://server.some.local:65000/OU=people,dc=some,dc=local", "ou=second", "LDAP://server.some.local:65000/ou=second,OU=people,dc=some,dc=local")]
    [Test]
    public void GetChildADSPath(string path,
                                string child,
                                string expected)
    {
        ADSPath adsPath = new(path);
        ADSPath childAdsPath = adsPath.NewChildADSPath(child);
        Assert.AreEqual(expected, childAdsPath.Path, "A10: ");
    }


    [TestCase("CN=slug,OU=animals,DC=some,DC=local", "OU=animals,DC=some,DC=local")]
    [TestCase("cn=scott,ou=people,dc=some,dc=local", "ou=people,dc=some,dc=local")]
    [TestCase("LDAP:", "LDAP:")]
    [TestCase("LDAP://cn=mary,ou=people,dc=some,dc=local", "LDAP://ou=people,dc=some,dc=local")]
    [TestCase("LDAP://ou=people,dc=some,dc=local", "LDAP://dc=some,dc=local")]
    [TestCase("LDAP://server.some.local", "LDAP://server.some.local")]
    [TestCase("LDAP://server.some.local:65000", "LDAP://server.some.local:65000")]
    [TestCase("LDAP://server.some.local:65000/OU=people,dc=some,dc=local", "LDAP://server.some.local:65000/dc=some,dc=local")]
    [TestCase("LDAP://server.some.local:65000/cn=mary smith,OU=people,dc=some,dc=local", "LDAP://server.some.local:65000/OU=people,dc=some,dc=local")]
    [TestCase("LDAP://server.some.local:65000/o=Petes Pizza,OU=people,dc=some,dc=local", "LDAP://server.some.local:65000/OU=people,dc=some,dc=local")]
    [TestCase("cn=mary,ou=people,ou=us,ou=California,ou=San Diego,dc=some,dc=local", "ou=people,ou=us,ou=California,ou=San Diego,dc=some,dc=local")]
    [Test]
    public void GetParentADSPath(string path,
                                 string expected)
    {
        ADSPath adsPath = new(path);
        ADSPath parent = adsPath.GetParent();


        //Assert.AreEqual(path, adsPath.Path, "A10: ");
        Assert.AreEqual(expected, parent.Path, "A20: ");
    }



    [TestCase("CN=slug,OU=animals,DC=some,DC=local", "OU=animals")]
    [TestCase("cn=scott,ou=people,dc=some,dc=local", "ou=people")]
    [TestCase("LDAP:", "")]
    [TestCase("LDAP://cn=mary,ou=people,dc=some,dc=local", "ou=people")]
    [TestCase("LDAP://ou=people,dc=some,dc=local", "")]
    [TestCase("LDAP://server.some.local", "")]
    [TestCase("LDAP://server.some.local:65000", "")]
    [TestCase("LDAP://server.some.local:65000/OU=people,dc=some,dc=local", "")]
    [TestCase("LDAP://server.some.local:65000/cn=mary smith,OU=people,dc=some,dc=local", "OU=people")]
    [TestCase("LDAP://server.some.local:65000/o=Petes Pizza,OU=people,dc=some,dc=local", "OU=people")]
    [TestCase("cn=mary,ou=people,ou=us,ou=California,ou=San Diego,dc=some,dc=local", "ou=people,ou=us,ou=California,ou=San Diego")]
    [Test]
    public void GetParentDN(string path,
                            string expected)
    {
        ADSPath adsPath = new(path);
        string parent = adsPath.GetParentDN();

        Assert.AreEqual(path, adsPath.Path, "A10: ");
        Assert.AreEqual(expected, parent, "A20: ");
    }


    [TestCase("DC=some,DC=local", "OU=animals", "OU=animals,DC=some,DC=local")]
    [Test]
    public void NewChild(string parent,
                         string child,
                         string expected)
    {
        ADSPath adsPath = new(parent);
        ADSPath childPath = adsPath.NewChildADSPath(child);
        Assert.AreEqual(expected, childPath.Path, "A10: ");
    }


    [TestCase("DC=some,DC=local", "CN=animals", "CN=animals,DC=some,DC=local")]
    [TestCase("CN=WaterPets,DC=some,DC=local", "CN=animals", "CN=animals,CN=WaterPets,DC=some,DC=local")]
    [Test]
    public void NewChildCNStyle(string parent,
                         string child,
                         string expected)
    {
        ADSPath adsPath = new(parent);
        ADSPath childPath = adsPath.NewChildADSPath(child, false);
        Assert.AreEqual(expected, childPath.Path, "A10: ");
    }


    [SetUp]
    public void Setup() { }



    [TestCase("cn=scott,ou=people,dc=some,dc=local", "people")]
    [TestCase("ou=people,ou=us,ou=North America,dc=some,dc=local", "people")]

    //[TestCase()]
    [Test]
    public void ShortName(string path,
                          string expected)
    {
        ADSPath adsPath = new(path);
        Assert.AreEqual(expected, adsPath.ShortName(), "A10: ");
    }



    [Test]
    public void ToString()
    {
        string path = "LDAP://server.some.local:65000/OU=people/OU=US,DC=some,DC=local";
        ADSPath adsPath = new(path);

        Assert.AreEqual(path, adsPath.ToString(), "A10:");
    }


    [TestCase("cn=scott,ou=people,dc=some,dc=local", "cn=scott,ou=people,dc=some,dc=local",true)]
    [TestCase("CN=scott,ou=people,dc=some,dc=local", "cn=scott,ou=people,dc=some,dc=local", true)]
    [TestCase("cn=scott,ou=people,dc=some,dc=local", "CN=scott,ou=people,dc=some,dc=local", true)]
    [TestCase("cn=scott,ou=people,dc=some,dc=local", "cn=scott,OU=people,dc=some,dc=local", true)]
    [TestCase("cn=scott,ou=people,dc=some,dc=local", "cn=scott,dc=some,dc=local", false)]
    [Test]
    public void Equals_Success(string path1, string path2,bool expected)
    {
        ADSPath adsPath1 = new(path1);
        ADSPath adsPath2 = new(path2); 
        
        Assert.That((adsPath1 == adsPath2),Is.EqualTo(expected), "[V_100] equivalency did not compute correctly.");
    }


    [TestCase("ou=people,dc=some,dc=local", "cn=fudge cake", "cn=fudge cake,ou=people,dc=some,dc=local")]
    [Test]
    public void NewADSPath_FromParentChild (string parent, string child, string expected)
    {
        ADSPath combinedPath = new(parent,child);
        Assert.AreEqual(expected, combinedPath.Path, "A10: ");
    }
}