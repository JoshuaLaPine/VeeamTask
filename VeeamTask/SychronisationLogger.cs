using System;
using System.IO;

namespace VeeamTask
{
    public class SychronisationLogger
    {
        private string logPath;
        private string logFileName = "FolderSyncLog.txt";
        private string fullLogPath;

        public SychronisationLogger(string logPath)
        {
            this.logPath = logPath;
            fullLogPath = Path.Combine(logPath, logFileName);

            try
            {
                using (StreamWriter writer = new StreamWriter(fullLogPath, false))
                {
                    writer.WriteLine(logFileName);
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

        private void PrintOutput(string output)
        {

            Console.WriteLine(output);

            try
            {
                using (StreamWriter writer = new StreamWriter(fullLogPath, true))
                {
                    writer.WriteLine(output);
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

        public void SourceCreation(string path)
        {
            string output = "Created: " + path;
            PrintOutput(output);
        }

        public void InitialCopy(string sourcePath, string destPath)
        {
            string output = "Initial copy: " + sourcePath + " -> " + destPath;
            PrintOutput(output);
        }

        public void SourceRemoval(string sourcePath, string replicaPath)
        {
            string output = "Deleted at source: " + sourcePath + "\nDeleted for synchronisation: " + replicaPath;
            PrintOutput(output);
        }

        public void ChangeCopy(string sourcePath, string destPath)
        {
            string output = "Copied due to modification: " + sourcePath + " -> " + destPath;
            PrintOutput(output);
        }

        public void ManualRemovalCopy(string sourcePath, string destPath)
        {
            string output = "Copied due to manual deletion in replica directory: " + sourcePath + " -> " + destPath;
            PrintOutput(output);
        }

        public void ReplicaRemoval(string path)
        {
            string output = "File/Directory not in source. Deleted: " + path;
            PrintOutput(output);
        }
    }
}
