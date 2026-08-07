
using AD.Protocols.ADObjects;
using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;


public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Press the Letter L for Linux Connection.  W for Windows Connection");
        bool isLinux = false;

        ActiveDirConfig config = new ActiveDirConfig()
        {
            AdPassword            = "T#sting2026",
            AdUser                = $"utadmin@ycy4y.local",
            Domain                = "ycy4y.local",
            Server1Name           = "ycdc1",
            Server2Name           = "",
            Port                  = 636,
            RootPath              = "",
            UserOu                = "",
        };
        
        


        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.L)
            {
                Console.WriteLine("Linux Selected");
                isLinux = true;
                break;

            }
            else if (key.Key == ConsoleKey.W)
            {
                Console.WriteLine("Windows Selected");
                isLinux = false;
                break;
            }
            else
            {
                Console.WriteLine("Invalid Input. Please enter L or W.");
            }
        }

        config.IsConnectingFromLinux = isLinux;
        
        ActiveDirectoryConnector activeDirectoryConnector = new ActiveDirectoryConnector(null);
        Result result = activeDirectoryConnector.Initialize(config);
        if (result.IsFailed)
        {
            Console.WriteLine("Connection Failed.");
            Console.WriteLine(result.ToStringErrorOnly());
            return;
        }
        Console.WriteLine("connected to AD!");


        // List Groups
        ADSPath UnitTestRoot = activeDirectoryConnector.DomainRoot.CreateChild("ou=zUnitTesting");
        ADSPath UnitTestParent = UnitTestRoot.CreateChild("ou=UT");

        string searchFilter = ActiveDirectoryConnector.SEARCH_FILTER_ALL_GROUPS;
        List<string> attributes   = new List<string>();
        ADpReadOnlyGroup.AddBaseAttributes(attributes);
        ADpReadOnlyGroup.AddInfoAttributes(attributes);
        ADpReadOnlyGroup.AddStatisticAttributes(attributes);
        ADpReadOnlyGroup.AddMemberAttribute(attributes);

        
        ADSPath grp = UnitTestParent;
        /*        Result<List<ADpReadOnlyGroup>> resultF = activeDirectoryConnector.GroupFindOneOrMore("OU=UT_Groups,DC=ycy4y,DC=local",
                                                                                            SearchScope.OneLevel,
                                                                                            searchFilter,
                                                                                            attributes);
                if (resultF.IsSuccess)
                {
                    List<ADpReadOnlyGroup> groups = resultF.Value;
                    foreach (var group in groups)
                    {
                        Console.WriteLine(group.Name);
                    }
                }
                else 
                    Console.WriteLine(resultF.ToStringErrorOnly());
        */
    }

}
