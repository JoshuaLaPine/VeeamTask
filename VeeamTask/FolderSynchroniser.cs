using System;
using System.IO;
using System.Security.Cryptography;
using System.Timers;

namespace VeeamTask
{
    public class FolderSynchroniser
    {
        private DirectoryInfo sourceInfo;
        private DirectoryInfo replicaInfo;
        private int syncPeriodMinutes;

        private static Timer syncTimer;

        public FolderSynchroniser(string sourcePath, string replicaPath, string syncPeriodMinutes)
        {
            if (!Directory.Exists(sourcePath))
            {
                Console.WriteLine("Source folder directory not found");
                Environment.Exit(3);
            }

            if (!Directory.Exists(replicaPath))
            {
                Console.WriteLine("Replica folder directory not found");
                Environment.Exit(3);
            }

            sourceInfo = new DirectoryInfo(sourcePath);
            replicaInfo = new DirectoryInfo(replicaPath);
            this.syncPeriodMinutes = int.Parse(syncPeriodMinutes);

            //Directory.Delete(Path.Combine(sourceInfo.FullName, "fold"), true);

            SetupTimer();

        }

        private void SetupTimer()
        {
            syncTimer = new Timer(syncPeriodMinutes * 60 * 100); //Multiple of 6 seconds rather than minute
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
            foreach (FileInfo tempFileInfo in sourceInfo.GetFiles())
            {
                tempFileInfo.CopyTo(Path.Combine(replicaInfo.FullName, tempFileInfo.Name), true);
                //Console.WriteLine(Path.Combine(replicaInfo.FullName,tempFileInfo.Name));
            }
       
        }

        private void SyncFiles()
        {
            
        }
    }
}
