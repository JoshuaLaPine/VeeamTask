# VeeamTask

Completed for the Internal Development in QA task. 

I made the assumption that the program wouldn't need its state to persist between runs. 
Therefore each time the program is started, a fresh copy of all of the files and directories in the source folder is copied to the replica folded,
and the state the of the program is generated anew. 

The program requires 4 arguments: source folder, replica folder, an integer representing the number of minutes for the sync interval, 
and a folder for the log file. 

The program consists of 3 source files: FolderSynchroniser.cs, Program.cs, and SynchronisationLogger.cs. 

FolderSynchroniser.cs contains the bulk of the code and all of the logic for the folder synchronisation. 

Program.cs contains the main method which does a simple check with the user to ensure they wish to proceed and then creates an instance of 
FolderSynchroniser, passing the command line agruments to its constructor. It then starts the sync and waits to read a line from standard input.
When it does the sync will be stopped and the program will terminate. 

The two most important fields of FolderSynchroniser are the Dictionary fileHashDict, which stores the hash for each file as a value, and used the path
as the key, and the HashSet directorySet which stores the path of each subdirectory. 

FolderSynchroniser used a timer object which calls the Sync method according to the sync interval. 
Sync calls 3 methods in sequence: SyncFiles, DeletionPass, and RemoveNonSourceFiles. 

Sync files is the bulk of the synchronisation logic.
For every file in the directory it computes the hash and checks to see if the hash is stored in fileHashDict. If it isn't then the file is new, then it 
will be copied to replica and the operations will be logged. 

