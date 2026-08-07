using AD.Protocols;
using AD.Protocols.ADObjects;
using Microsoft.Identity.Client;
using SlugEnt.FluentResults;

namespace Test.ADProtocols;

[TestFixture]
public class Test_ADSPath
{
    [TestCase("cn=slug,OU=animals,DC=some,dc=local", "CN=slug,OU=animals,DC=some,DC=local")]
    [TestCase("CN=slug,OU=animals,DC=some,DC=local", "CN=slug,OU=animals,DC=some,DC=local")]
    [Test]
    public void Create_From_string_path(string parent,
                                        string expected)
    {
        ADSPath adsPath = new(parent);
        Assert.AreEqual(expected, adsPath.Path, $"[V_100]  Path is not expected value.");
    }


    /// <summary>
    /// Confirms the ADSPath constructor 
    /// </summary>
    [Test]
    public void CreateFromAnotherADSPathObj()
    {
        // A --> Setup
        string  origPath = "CN=slug,OU=animals,DC=some,DC=local";
        ADSPath adsSlug  = new(origPath);

        // B --> Act
        ADSPath adsCopy = new(adsSlug);
        
        // V --> Verify
        Assert.That(adsCopy,Is.EqualTo(adsSlug),"[V_100]  ADSPath copy is not equal to the original.");
    }


    [Test]
    public void CreateFromStringAssignment()
    {
        string original = "CN=slug,OU=animals,DC=some,DC=local";
        ADSPath adsPath = original;
        
        // V --> Verifty
        Assert.That(adsPath.Path, Is.EqualTo(original),"[V_100]");
    }
    

    /// <summary>
    /// Tests the creation of an ADSPath from two string paths, ensuring that the resulting path matches the expected value.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="child"></param>
    /// <param name="expected"></param>
    [TestCase("cn=slug,OU=animals,DC=some,dc=local", "cN=poisonous Slug", "CN=poisonous Slug,CN=slug,OU=animals,DC=some,DC=local")]
    [TestCase("CN=slug,OU=animals,DC=some,DC=local", "CN=poisonous Slug", "CN=poisonous Slug,CN=slug,OU=animals,DC=some,DC=local")]
    [Test]
    public void Create_From_2_string_paths(string parent,
                                           string child,
                                           string expected)
    {
        ADSPath adsPath = new(parent, child);
        Assert.AreEqual(expected, adsPath.Path, $"[V_100]  Path is not expected value.");
    }


    /// <summary>
    /// Tests the creation of an ADSPath from a list of components, ensuring that the resulting path matches the expected value.
    /// </summary>
    /// <param name="expected"></param>
    /// <param name="components"></param>
    [TestCase("CN=poisonous Slug,CN=slug,OU=animals,DC=some,DC=local",
                 new string[]
                 {
                     "CN=poisonous Slug", "CN=slug", "OU=animals", "DC=some", "DC=local"
                 })]
    [Test]
    public void Create_From_ComponentList(string expected,
                                          string[] components)
    {
        List<KeyValuePair<string, string>> rdnComponents = new List<KeyValuePair<string, string>>();
        foreach (string component in components)
        {
            string[] keyValue = component.Split(new char[]
                                                {
                                                    '='
                                                },
                                                2);
            rdnComponents.Add(new KeyValuePair<string, string>(keyValue[0].ToUpper(), keyValue[1]));
        }

        ADSPath adsPath = new(rdnComponents);
        Assert.AreEqual(expected, adsPath.Path, $"[V_100]  Path is not expected value.");
    }


    [TestCase("dc=abc,dc=local", "ou=first", "OU=first,DC=abc,DC=local")]
    [TestCase("ou=firstOu,dc=abc,dc=local", "ou=second", "OU=second,OU=firstOu,DC=abc,DC=local")]
    [Test]
    public void GetChildADSPath(string path,
                                string child,
                                string expected)
    {
        ADSPath adsPath      = new(path);
        ADSPath childAdsPath = adsPath.CreateChild(child);
        Assert.AreEqual(expected, childAdsPath.Path, "[V_100]");
    }


    [TestCase("CN=slug,OU=animals,DC=some,DC=local", "OU=animals,DC=some,DC=local")]
    [TestCase("cn=scott,ou=people,dc=some,dc=local", "OU=people,DC=some,DC=local")]
    [TestCase("cn=mary,ou=people,ou=us,ou=California,ou=San Diego,dc=some,dc=local", "OU=people,OU=us,OU=California,OU=San Diego,DC=some,DC=local")]
    [Test]
    public void GetParentADSPath(string path,
                                 string expected)
    {
        ADSPath         adsPath    = new(path);
        Result<ADSPath> testResult = adsPath.GetParent();

        ADSPath parent = testResult.IsSuccess ? testResult.Value : null;

        Assert.AreEqual(expected, parent.Path, "[V_100]");
    }



    [TestCase("DC=some,DC=local", "OU=animals", "OU=animals,DC=some,DC=local")]
    [Test]
    public void BuildNewChild(string parent,
                              string child,
                              string expected)
    {
        ADSPath adsPath   = new(parent);
        ADSPath childPath = adsPath.CreateChild(child);
        Assert.AreEqual(expected, childPath.Path, "[V_100]");
    }


    [SetUp]
    public void Setup() { }



    [TestCase("cn=scott,ou=people,dc=some,dc=local", "scott")]
    [TestCase("ou=people,ou=us,ou=North America,dc=some,dc=local", "people")]
    [Test]
    public void ShortName(string path,
                          string expected)
    {
        ADSPath adsPath = new(path);
        Assert.AreEqual(expected, adsPath.ShortName(), "[V_100]");
    }


    [TestCase("cn=scott,ou=people,dc=some,dc=local", "CN=scott")]
    [TestCase("ou=people,ou=us,ou=North America,dc=some,dc=local", "OU=people")]
    [Test]
    public void Name(string path,
                     string expected)
    {
        ADSPath adsPath = new(path);
        Assert.AreEqual(expected, adsPath.Name(), "[V_100]");
    }


    [Test]
    public void ToString()
    {
        string  path    = "OU=people,OU=US,DC=some,DC=local";
        ADSPath adsPath = new(path);

        Assert.AreEqual(path, adsPath.ToString(), "[V_100]");
    }


    [TestCase("cn=scott,ou=people,dc=some,dc=local", "cn=scott,ou=people,dc=some,dc=local", true)]
    [TestCase("CN=scott,ou=people,dc=some,dc=local", "cn=scott,ou=people,dc=some,dc=local", true)]
    [TestCase("cn=scott,ou=people,dc=some,dc=local", "CN=scott,ou=people,dc=some,dc=local", true)]
    [TestCase("cn=scott,ou=people,dc=some,dc=local", "cn=scott,OU=people,dc=some,dc=local", true)]
    [TestCase("cn=scott,ou=people,dc=some,dc=local", "cn=scott,dc=some,dc=local", false)]
    [Test]
    public void Equals_Success(string path1,
                               string path2,
                               bool expected)
    {
        ADSPath adsPath1 = new(path1);
        ADSPath adsPath2 = new(path2);

        Assert.That((adsPath1 == adsPath2), Is.EqualTo(expected), "[V_100] equivalency did not compute correctly.");
    }



    [TestCase("CN=slug,OU=animals,DC=some,DC=local")]
    [TestCase("cn=scott,ou=people,dc=some,dc=local")]
    [Test]
    public void DNValidator_Success(string dn) { Assert.IsTrue(DnValidator.IsValidDn(dn), "[V_100] DN validation failed."); }


    [TestCase("CN=slug,OU=animals,DC=some,DC=local")]
    [Test]
    public void ADpValidatedRdnPath_Assignment(string path)
    {
        Result<ADpValidatedRdnPath> x = ADpValidatedRdnPath.IsValidDn(path, false);

        // B --> Verify
        Assert.That(x.IsSuccess, Is.True, "[B_100] ADpValidatedRdnPath assignment failed.");
        Assert.That(x.Value, Is.Not.Null, "[B_110] ADpValidatedRdnPath assignment failed.");

        // C --> Act - Assign the list.
        ADpValidatedRdnPath rdnPath = x.Value;
        Assert.That(rdnPath.HasBeenAssigned, Is.False, "[C_100] ADpValidatedRdnPath should be in a state of unassigned.");

        List<KeyValuePair<string, string>> rdnComponents = rdnPath.AssignRdnComponentList();
        Assert.That(rdnPath.HasBeenAssigned, Is.True, "[C_110] ADpValidatedRdnPath should be in a state of assigned.");
    }


    [TestCase("DC=some,DC=local", "OU=animals", "OU=animals,DC=some,DC=local")]
    [Test]
    public void Merge(string parent,
                      string child,
                      string expected)
    {
        ADSPath parentPath = new(parent);
        ADSPath newPath    = parentPath.CreateChild(child);
        Assert.AreEqual(expected, newPath.Path, "[V_100]");
        Assert.AreEqual(parent, parentPath.Path, "[V_110]");

    }
}