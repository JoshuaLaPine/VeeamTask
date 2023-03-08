# VeeamTask

Completed for the Internal Development in QA task. 

I made the assumption that the program wouldn't need its state to persist between runs. 
Therefore each time the program is started, a fresh copy of all of the files and directories in the source folder is copied to the replica folded,
and the state the of the program is generated anew. 

The program requires 4 arguments: source folder, replica folder, an integer representing the number of minutes for the sync interval, and a folder 
for the log file. 

The program consists of 3 source files: FolderSynchroniser.cs, Program.cs, and SynchronisationLogger.cs. 

FolderSynchroniser.cs contains the bulk of the code and all of the logic for the folder synchronisation. 

Program.cs contains the main method which does a simple check with the user to ensure they wish to proceed and then creates an instance of 
FolderSynchroniser, passing the command line agruments to its constructor. It then starts the sync and waits to read a line from standard input. When it
does the sync will be stopped and the program will terminate. 

The two most important fields of FolderSynchroniser are the Dictionary fileHashDict, which stores the hash for each file as a value, and uses the path
as the key, and the HashSet directorySet which stores the path of each subdirectory. 

FolderSynchroniser used a timer object which calls the Sync method according to the sync interval. 
Sync calls 3 methods in sequence: SyncFiles, DeletionPass, and RemoveNonSourceFiles. 

SyncFiles is the bulk of the synchronisation logic.
For every file in the directory it computes the hash and checks to see if the hash is stored in fileHashDict. If it isn't then the file is new, and it 
will be copied to replica.

If the file has a hash in the dictionary but it doesn't match, then the file has been changed. The new hash is stored and the file is copied to replica.
If the file exists and hasn't changed, then the replica directory is checked to ensure that the file hasn't been manually deleted. If so, it is copied over.

Then, for each directory, the directorySet HashSet is checked. If the directory doesn't exist in the set then it is added and a copy is created in the
replica folder. 

If it is present in the set then the replica folder is checked. Should the directory be missing then a new copy is created. 

Then SyncFiles is called for each directory. 

DeletionPass iterates over all the keys in fileHashDict and all of the elements in directorySet. If the corresponding file no longer exists in the source
folder then the entries are deleted from the corresponding structure and the correpsponding files and directories in replica are deleted also. 

RemoveNonSourceFiles iterates over each file and directory in the replica folder and checks to see if they are present in fileHashDict and directorySet.
If not then the files should not exist in the replica folder and they are removed. This method is then called on each subdirectory. 

Logging is performed via SynchronisationLogger.cs.
An instance is created by FolderSynchroniser in its constructor. The SynchronisationLogger constructor creates a log file at the given log file path.
Each of the methods takes one or more strings as aruments and uses them in a context specific log message that is then written to standard out and to
the log file. 

Each of SynchronisationLogger's methods is called from within one of the 3 methods called by Sync in FolderSynchroniser.
