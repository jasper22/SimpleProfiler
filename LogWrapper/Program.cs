using System;
using System.IO;

namespace LogWrapper
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("You must pass all parameters: ");
                Console.WriteLine("LogWrapper.exe <full\\abosolute\\path\\to\\artifacts\\folder> <file_name_that_should_be_wrapped_in_logs>");
                Console.WriteLine("");
                Console.WriteLine("For example: ");
                Console.WriteLine($"LogWrapper.exe {Environment.CurrentDirectory}bin\\Debug ShugiShugi.Test.dll");

                return -1;
            }

            var sourceFolder = args[0];
            var sourceFile = args[1];

            // ETW assembly path is
            var etwAssemblyPath = Path.Combine(sourceFolder, "ShugiShugi.Common.dll");
            var targetFile = Path.Combine(sourceFolder, sourceFile);

            var wrapper = new Wrapper(etwAssemblyPath, targetFile);

            try
            {
                wrapper.AddLogs();
                
                wrapper.SaveAll();

                return 0;
            }
            catch(Exception exp)
            {
                Console.WriteLine($"Exception occurred ! Additional information: {exp.ToString()}");

                return -18;
            }
        }
    }
}
