using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Timers;
using System.Linq;


namespace VeeamTask
{
    public class FolderSynchroniser
    {
        private DirectoryInfo sourceInfo;
        private DirectoryInfo replicaInfo;
        private int syncPeriodMinutes;

        private Dictionary<string, byte[]> fileHashDict;
        private HashSet<string> directorySet;
        private SHA256 hasher;

        private Timer syncTimer;
        private SychronisationLogger logger;

        public FolderSynchroniser(string sourcePath, string replicaPath, string syncPeriodMinutes, string logPath)
        {
            if (!Directory.Exists(sourcePath))
            {
                Console.WriteLine("Source directory not found");
                Environment.Exit(3);
            }

            if (!Directory.Exists(replicaPath))
            {
                Console.WriteLine("Replica directory not found");
                Environment.Exit(3);
            }

            if (!Directory.Exists(logPath))
            {
                Console.WriteLine("log directory not found");
                Environment.Exit(3);
            }

            if (int.TryParse(syncPeriodMinutes, out int parsedInt))
            {
                this.syncPeriodMinutes = parsedInt;
            }
            else
            {
                Console.WriteLine("Sync interval value is invalid");
                Environment.Exit(1);
            }

            sourceInfo = new DirectoryInfo(sourcePath);
            replicaInfo = new DirectoryInfo(replicaPath);
            logger = new SychronisationLogger(logPath);


            fileHashDict = new Dictionary<string, byte[]>();
            directorySet = new HashSet<string>();
            hasher = SHA256.Create();

            //Initial Sync
            SyncFiles(sourceInfo);
            RemoveNonSourceFiles(replicaInfo);

            SetupTimer();

        }

        private void SetupTimer()
        {
            syncTimer = new Timer(syncPeriodMinutes * 60 * 200); //Converts minutes to milliseconds
            syncTimer.Elapsed += Sync;
            syncTimer.AutoReset = true;
            syncTimer.Enabled = true;
        }

        public void StartSync()
        {
            syncTimer.Start();
        }

        public void StopSync()
        {
            syncTimer.Stop();
            syncTimer.Dispose();
        }

        private void Sync(Object source, ElapsedEventArgs e)
        {
            SyncFiles(sourceInfo);
            DeletionPass();
            RemoveNonSourceFiles(replicaInfo);
            
        }

        private void SyncFiles(DirectoryInfo directoryVar)
        {
            foreach (FileInfo tempFileInfo in directoryVar.GetFiles())
            {
                
                try
                {
                    FileStream fileStream = tempFileInfo.Open(FileMode.Open);
                    fileStream.Position = 0;
                    byte[] hashVal = hasher.ComputeHash(fileStream);
                    fileStream.Close();

                    //File is new, create an entry in the dictionary, copy it, log it.
                    if (!fileHashDict.ContainsKey(tempFileInfo.FullName))
                    {
                        fileHashDict.Add(tempFileInfo.FullName, hashVal);
                        string directorySuffix = directoryVar.FullName.Remove(0, sourceInfo.FullName.Length);
                        string fullReplicaPath = Path.Combine(replicaInfo.FullName, directorySuffix, tempFileInfo.Name);
                        tempFileInfo.CopyTo(fullReplicaPath, true);

                        logger.SourceCreation(tempFileInfo.FullName);
                        logger.InitialCopy(tempFileInfo.FullName, fullReplicaPath);
                    }
                    else
                    {
                        //File hash has changed, copy it, log it. 
                        if (!fileHashDict[tempFileInfo.FullName].SequenceEqual(hashVal))
                        {
                            fileHashDict[tempFileInfo.FullName] = hashVal;
                            string directorySuffix = directoryVar.FullName.Remove(0, sourceInfo.FullName.Length);
                            string fullReplicaPath = Path.Combine(replicaInfo.FullName, directorySuffix, tempFileInfo.Name);
                            tempFileInfo.CopyTo(fullReplicaPath, true);

                            logger.ChangeCopy(tempFileInfo.FullName, fullReplicaPath);

                        }
                        //File hash hasn't changed. Need to check that it hasn't been deleted manually and recopy it if so 
                        else
                        {
                            string directorySuffix = directoryVar.FullName.Remove(0, sourceInfo.FullName.Length);
                            string fullReplicaPath = Path.Combine(replicaInfo.FullName, directorySuffix, tempFileInfo.Name);
                            if (!File.Exists(fullReplicaPath))
                            {

                                tempFileInfo.CopyTo(fullReplicaPath, true);
                                logger.ManualRemovalCopy(tempFileInfo.FullName, fullReplicaPath);
                            }
                        }
                    }


                }
                catch (IOException e)
                {
                    Console.WriteLine($"I/O Exception: {e.Message}");
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine($"Access Exception: {e.Message}");
                }
            }

            //Check if each directory already existed, if not, add it to the set, copy it to replica, log it.
            foreach (DirectoryInfo tempDirInfo in directoryVar.GetDirectories())
            {
                try
                {
                    string directorySuffix = tempDirInfo.FullName.Remove(0, sourceInfo.FullName.Length);
                    string fullReplicaPath = Path.Combine(replicaInfo.FullName, directorySuffix);

                    if (!directorySet.Contains(tempDirInfo.FullName))
                    {
                        directorySet.Add(tempDirInfo.FullName);
 
                        Directory.CreateDirectory(fullReplicaPath);

                        logger.SourceCreation(tempDirInfo.FullName);
                        logger.InitialCopy(tempDirInfo.FullName, fullReplicaPath);
                    }
                    //Check to see if a directory has been removed manually, recreate it if so
                    else
                    {
                        if (!Directory.Exists(fullReplicaPath))
                        {
                            Directory.CreateDirectory(fullReplicaPath);
                            logger.ManualRemovalCopy(tempDirInfo.FullName, fullReplicaPath);
                        }
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine($"I/O Exception: {e.Message}");
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine($"Access Exception: {e.Message}");
                }
            }

            //Recursive call to ensure all subdirectories and contained files are processed
            foreach (DirectoryInfo tempDirInfo in directoryVar.GetDirectories())
            {
                SyncFiles(tempDirInfo);
            }
        }

        private void DeletionPass()
        {

            //Iterates over files and deletes any keys and copies if the source file no longer exists
            List<string> keysForDeletion = new List<string>();
            foreach(string key in fileHashDict.Keys)
            {
                if (!File.Exists(key))
                {
                    try
                    {
                        string fileSuffix = key.Remove(0, sourceInfo.FullName.Length);
                        string fullReplicaPath = Path.Combine(replicaInfo.FullName, fileSuffix);
                        File.Delete(fullReplicaPath);
                        keysForDeletion.Add(key);
                        logger.SourceRemoval(key, fullReplicaPath);
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine($"I/O Exception: {e.Message}");
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        Console.WriteLine($"Access Exception: {e.Message}");
                    }
                }
            }

            foreach(string key in keysForDeletion)
            {
                fileHashDict.Remove(key);
            }

            //Iterates over directories and deletes any elements and copies if the source folder no longer exists
            List<string> elemsForDeletion = new List<string>();
            foreach(string elem in directorySet)
            {
                if (!Directory.Exists(elem))
                {
                    try
                    {
                        string directorySuffix = elem.Remove(0, sourceInfo.FullName.Length);
                        string fullReplicaPath = Path.Combine(replicaInfo.FullName, directorySuffix);
                        if (Directory.Exists(fullReplicaPath)){
                            Directory.Delete(fullReplicaPath, true);
                        }
                        logger.SourceRemoval(elem, fullReplicaPath);
                        elemsForDeletion.Add(elem);
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine($"I/O Exception: {e.Message}");
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        Console.WriteLine($"Access Exception: {e.Message}");
                    }
                }
            }

            foreach(string elem in elemsForDeletion)
            {
                directorySet.Remove(elem);
            }
        }

        private void RemoveNonSourceFiles(DirectoryInfo directoryVar)
        {

            //Removes any files from replica that don't exist in source
            FileInfo[] files = directoryVar.GetFiles();
            foreach(FileInfo file in files)
            {
                try
                {
                    string directorySuffix = file.FullName.Remove(0, replicaInfo.FullName.Length);
                    if (!fileHashDict.ContainsKey(Path.Combine(sourceInfo.FullName, directorySuffix)))
                    {
                        File.Delete(file.FullName);
                        logger.ReplicaRemoval(file.FullName);
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine($"I/O Exception: {e.Message}");
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine($"Access Exception: {e.Message}");
                }
            }

            //Removes any folders from replica that don't exist in source
            DirectoryInfo[] directories = directoryVar.GetDirectories();
            foreach(DirectoryInfo directory in directories)
            {
                try
                {
                    string directorySuffix = directory.FullName.Remove(0, replicaInfo.FullName.Length);
                    if (!directorySet.Contains(Path.Combine(sourceInfo.FullName, directorySuffix)))
                    {
                        Directory.Delete(directory.FullName, true);
                        logger.ReplicaRemoval(directory.FullName);
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine($"I/O Exception: {e.Message}");
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine($"Access Exception: {e.Message}");
                }
            }

            foreach(DirectoryInfo directory in directoryVar.GetDirectories())
            {
                RemoveNonSourceFiles(directory);
            }
        }
    }
}
