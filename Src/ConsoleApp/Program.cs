
using System.Xml.XPath;
using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;


public class Program
{
    public static void Main(string[] args)
    {
        ActiveDirConfig config = new ActiveDirConfig()
        {
            AdPassword = "T#sting2026",
            AdUser     = "Test3",
            Domain     = "ycy4y.local",
            Server1Name    = "ycdc1",
            Server2Name    = "",
            Port       = 636,
            RootPath   = "",
            UserOu     = ""
        };

        ActiveDirectoryConnector activeDirectoryConnector = new ActiveDirectoryConnector(null);
        Result result = activeDirectoryConnector.Initialize(config);
        if (result.IsFailed)
        {
            Console.WriteLine(result.ToStringErrorOnly());
            return;
        }
        Console.WriteLine("connected to AD!");
        
    }
}
