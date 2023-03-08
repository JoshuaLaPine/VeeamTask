using System;
using System.IO;
using System.Security.Cryptography;
using System.Timers;

namespace VeeamTask
{
    class Program
    {
        static void Main(string[] args)
        {

            if(args.Length < 4)
            {
                Console.WriteLine("Invalid Arguments");
                Console.WriteLine("Arguments: Source folder path, Replica folder path, Sync interval(minutes as integer), Log file path");
                Environment.Exit(1);
            }

            Console.WriteLine("Everything in the replica directory will be removed or overwritten.\nLog file will be overwritten.\nType yes and press enter to proceed.");
            string confirmation = Console.ReadLine();

            if(confirmation.ToLower() != "yes")
            {
                Console.WriteLine("Program Terminated");
                Environment.Exit(1);
            }

            FolderSynchroniser synchroniser = new FolderSynchroniser(args[0], args[1], args[2], args[3]);
            synchroniser.StartSync();
            Console.ReadLine();
            synchroniser.StopSync();

        }
    }
}
