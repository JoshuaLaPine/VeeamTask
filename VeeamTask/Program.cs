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

            if(args.Length < 3)
            {
                Console.WriteLine("Invalid Arguments");
                Environment.Exit(1);
            }

            FolderSynchroniser synchroniser = new FolderSynchroniser(args[0], args[1], args[2]);
            synchroniser.StartSync();
            Console.ReadLine();
            synchroniser.StopSync();

        }
    }
}
