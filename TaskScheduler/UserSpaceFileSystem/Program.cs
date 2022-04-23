// See https://aka.ms/new-console-template for more information

/*using DokanNet;
using System.Diagnostics;
using System.Drawing;
using TaskScheduler;

Dictionary<string, bool> filesProcessed = new();

TaskScheduler.TaskScheduler scheduler = new TaskScheduler.TaskScheduler(4, 10);
scheduler.Start();
//using var watcher = new FileSystemWatcher();
new Thread(() =>
{
    /*Thread.Sleep(1000);
    Bitmap originalImage = (Bitmap)Bitmap.FromFile("test.jpg");
    originalImage.Save("V:\\input\\test.jpg");
    //originalImage = (Bitmap)Bitmap.FromFile("V:\\output\\test2.jpg");
    scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 2000, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, new FolderResource("V:\\output\\"), new FileResource("V:\\input\\test.jpg")));
    */
/*while (true)
{
    Thread.Sleep(500);
    string folder = "V:\\input\\";
    var files = Directory.GetFiles(folder);
    foreach (var file in files)
    {
        if (!filesProcessed.ContainsKey(file))
        {
            Thread.Sleep(500);
            File.AppendAllLines("log.txt", new string[] { file });
            scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 2000, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, new FolderResource("V:\\output\\"), new FileResource(file)));
            filesProcessed.Add(file, true);
        }
    }
    /*watcher.Path = "V:\\input";
    watcher.NotifyFilter = NotifyFilters.LastWrite;
    watcher.Filter = "*.*";
    watcher.EnableRaisingEvents = true;
    watcher.Created += (s, e) =>
    {
        Thread.Sleep(500);
        File.WriteAllLines(".\\line.txt", new string[] { "asdads" });
        scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 2000, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, new FolderResource("V:\\output\\"), new FileResource(e.FullPath)));
        new Process() { StartInfo = new ProcessStartInfo() { FileName = "cmd.exe", UseShellExecute = true } }.Start();
    };*/
/*}
});//.Start();

new UserSpaceFileSystem.FileSystem().Mount("V:\\", DokanOptions.DebugMode | DokanOptions.StderrOutput);*/

using DokanNet;
using System.Security.AccessControl;
using TaskScheduler;

Dictionary<string, bool> filesProcessed = new();

TaskScheduler.TaskScheduler scheduler = new TaskScheduler.TaskScheduler(4, 10);
scheduler.Start();
using var watcher = new FileSystemWatcher();
new Thread(() =>
{
    /*Thread.Sleep(1000);
    Bitmap originalImage = (Bitmap)Bitmap.FromFile("test.jpg");
    originalImage.Save("V:\\input\\test.jpg");
    //originalImage = (Bitmap)Bitmap.FromFile("V:\\output\\test2.jpg");
    scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 2000, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, new FolderResource("V:\\output\\"), new FileResource("V:\\input\\test.jpg")));
    */
    while (true)
    {
        Thread.Sleep(1000);
        Directory.CreateDirectory("V:\\input");
        Directory.CreateDirectory("V:\\output");
        string folder = "V:\\input\\";
        var files = Directory.GetFiles(folder);
        foreach (var file in files)
        {
            if (!filesProcessed.ContainsKey(file))
            {
                Thread.Sleep(500);
                scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 2000, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, new FolderResource("V:\\output\\"), new FileResource(file)));
                filesProcessed.Add(file, true);
            }
        }
        /*watcher.Path = "V:\\input\\";
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Filter = "*.*";
        watcher.EnableRaisingEvents = true;
        watcher.Created += (s, e) =>
        {
            Thread.Sleep(500);
            //File.WriteAllLines(".\\line.txt", new string[] { "asdads" });
            scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 2000, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, new FolderResource("V:\\output\\"), new FileResource(e.FullPath)));
            //new Process() { StartInfo = new ProcessStartInfo() { FileName = "cmd.exe", UseShellExecute = true } }.Start();
        };*/
    }
}).Start();



new MyFs().Mount("V:\\", DokanOptions.DebugMode | DokanOptions.StderrOutput);

class MyFs : IDokanOperations
{
    private readonly Dictionary<string, File> files = new();
    private readonly HashSet<string> directories = new();

    private readonly static int CAPACITY = 512 * 1024 * 1024;
    private int totalNumberOfBytes = CAPACITY;
    private int totalNumberOfFreeBytes = CAPACITY;

    public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
    {
        volumeLabel = "MyFSVolume";
        features = FileSystemFeatures.None;
        fileSystemName = "MyFS";
        maximumComponentLength = 255;
        return NtStatus.Success;
    }

    public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, IDokanFileInfo info)
    {
        totalNumberOfFreeBytes = this.totalNumberOfFreeBytes;
        totalNumberOfBytes = this.totalNumberOfBytes;
        freeBytesAvailable = this.totalNumberOfFreeBytes;
        return NtStatus.Success;
    }

    public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
    {
        if (directories.Contains(fileName))
        {
            fileInfo = new()
            {
                FileName = Path.GetFileName(fileName),
                Attributes = FileAttributes.Directory,
                CreationTime = null,
                LastWriteTime = null
            };
        }
        else if (files.ContainsKey(fileName))
        {
            fileInfo = new()
            {
                FileName = Path.GetFileName(fileName),
                Length = files[fileName].Bytes.Length,
                Attributes = FileAttributes.Normal,
                CreationTime = files[fileName].DateCreated,
                LastWriteTime = files[fileName].DateModified
            };
        }
        else
        {
            fileInfo = default;
            return NtStatus.Error;
        }

        return NtStatus.Success;
    }

    public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, IDokanFileInfo info)
    {
        if (mode == FileMode.CreateNew)
        {
            if (attributes == FileAttributes.Directory || info.IsDirectory)
            {
                directories.Add(fileName);
            }

            else if (!files.ContainsKey(fileName))
            {
                File file = new()
                {
                    DateCreated = DateTime.Now,
                    DateModified = DateTime.Now
                };
                files.Add(fileName, file);
            }
        }

        return NtStatus.Success;
    }

    public void Cleanup(string entry, IDokanFileInfo info)
    {
        if (info.DeleteOnClose == true)
        {
            if (directories.Contains(entry))
            {
                foreach (var key in FilesInDirectoryRecursive(entry))
                {
                    totalNumberOfFreeBytes += files[key].Bytes.Length;
                    files.Remove(key);
                }
                foreach (var dir in DirectoriesInDirectoryRecursive(entry))
                    directories.Remove(dir);
                directories.Remove(entry);
            }
            if (files.ContainsKey(entry))
            {
                totalNumberOfFreeBytes += files[entry].Bytes.Length;
                files.Remove(entry);
            }
        }
    }

    public NtStatus DeleteDirectory(string directory, IDokanFileInfo info)
    {
        if (!info.IsDirectory)
            return NtStatus.Error;
        // DeleteOnClose gets or sets a value indicating whether the folder has to be deleted during the IDokanOperations.Cleanup event. 
        info.DeleteOnClose = true;
        return NtStatus.Success;
    }

    public NtStatus DeleteFile(string file, IDokanFileInfo info)
    {
        if (info.IsDirectory)
            return NtStatus.Error;
        // DeleteOnClose gets or sets a value indicating whether the file has to be deleted during the IDokanOperations.Cleanup event. 
        info.DeleteOnClose = true;
        return NtStatus.Success;
    }

    public NtStatus MoveFile(string oldPath, string newPath, bool replace, IDokanFileInfo info)
    {
        if (replace)
            return NtStatus.NotImplemented;

        if (oldPath == newPath)
            return NtStatus.Success;

        if (directories.Contains(oldPath))
        {
            if (directories.Contains(newPath))
                return NtStatus.NotImplemented;

            directories.Add(newPath);

            List<string> stringList;

            stringList = FilesInDirectory(oldPath);
            foreach (var path in stringList)
            {
                files.Add(newPath + @"\" + Path.GetFileName(path), files[path]);
                files.Remove(path);
            }

            stringList = DirectoriesInDirectory(oldPath);
            foreach (var path in stringList)
            {
                directories.Add(newPath + @"\" + Path.GetFileName(path));
                directories.Remove(path);
            }

            directories.Remove(oldPath);
        }

        else if (files.ContainsKey(oldPath))
        {
            if (files.ContainsKey(newPath))
                return NtStatus.NotImplemented;

            files.Add(newPath, files[oldPath]);
            files.Remove(oldPath);
        }

        return NtStatus.Success;
    }

    public NtStatus FindFiles(string dirPathName, out IList<FileInformation> foundFiles, IDokanFileInfo info)
    {
        foundFiles = new List<FileInformation>();
        dirPathName = dirPathName == "\\" ? "" : dirPathName;
        int pathCount = dirPathName.Split('\\', StringSplitOptions.RemoveEmptyEntries).Length;
        foreach (var foundDirectory
            in directories.Where(directoryPath => directoryPath.StartsWith(dirPathName)
            && directoryPath.Length > dirPathName.Length
            && directoryPath.Split('\\', StringSplitOptions.RemoveEmptyEntries).Length == pathCount + 1))
        {
            FileInformation fileInfo = new()
            {
                Attributes = FileAttributes.Directory,
                FileName = Path.GetFileName(foundDirectory)
            };
            foundFiles.Add(fileInfo);
        }
        foreach (var foundFile
            in files.Where(filePath =>
            filePath.Key.StartsWith($"{dirPathName}\\")
            && filePath.Key.Length > dirPathName.Length + 1
            && filePath.Key.Split('\\', StringSplitOptions.RemoveEmptyEntries).Length == pathCount + 1))
        {
            FileInformation fileInfo = new()
            {
                FileName = Path.GetFileName(foundFile.Key),
                Length = foundFile.Value.Bytes.Length,
                CreationTime = foundFile.Value.DateCreated,
                LastWriteTime = foundFile.Value.DateModified
            };
            foundFiles.Add(fileInfo);
        }
        return NtStatus.Success;
    }
    public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
    {
        if (!files.ContainsKey(fileName))
            files.Add(fileName, new File() { DateCreated = DateTime.UtcNow });
        File file = files[fileName];

        if (info.WriteToEndOfFile)
        {
            file.Bytes = file.Bytes.Concat(buffer).ToArray();
            bytesWritten = buffer.Length;
        }
        else
        {
            int difference = file.Bytes.Length - (int)offset;
            totalNumberOfFreeBytes += difference;
            file.Bytes = file.Bytes.Take((int)offset).Concat(buffer).ToArray();
            bytesWritten = buffer.Length;
        }

        totalNumberOfFreeBytes -= bytesWritten;
        file.DateModified = DateTime.Now;

        return NtStatus.Success;
    }
    public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
    {
        File file = files[fileName];
        file.Bytes.Skip((int)offset).Take(buffer.Length).ToArray().CopyTo(buffer, 0);
        int diff = file.Bytes.Length - (int)offset;
        bytesRead = buffer.Length > diff ? diff : buffer.Length;
        return NtStatus.Success;
    }
    public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
    {
        files = Array.Empty<FileInformation>();
        return NtStatus.NotImplemented;
    }
    public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
    {
        streams = Array.Empty<FileInformation>();
        return NtStatus.NotImplemented;
    }
    public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info) => NtStatus.Success;
    public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity? security, AccessControlSections sections, IDokanFileInfo info)
    {
        security = null;
        return NtStatus.Success;
    }
    public void CloseFile(string fileName, IDokanFileInfo info) { }
    public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info) => NtStatus.Error;
    public NtStatus Mounted(IDokanFileInfo info) => NtStatus.Success;
    public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info) => NtStatus.Error;
    public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info) => NtStatus.Error;
    public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info) => NtStatus.Success;
    public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info) => NtStatus.Error;
    public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, IDokanFileInfo info) => NtStatus.Error;
    public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info) => NtStatus.Error;
    public NtStatus Unmounted(IDokanFileInfo info) => NtStatus.Success;

    public List<string> FilesInDirectory(string dirPath) => EntriesInDirectory(dirPath, files.Keys, false);
    public List<string> DirectoriesInDirectory(string dirPath) => EntriesInDirectory(dirPath, directories, false);
    public List<string> FilesInDirectoryRecursive(string dirPath) => EntriesInDirectory(dirPath, files.Keys, true);
    public List<string> DirectoriesInDirectoryRecursive(string dirPath) => EntriesInDirectory(dirPath, directories, true);
    static List<string> EntriesInDirectory(string dirPath, IEnumerable<string> paths, bool recursive)
    {
        List<string> result = new();
        int dirDepth = Depth(dirPath);
        foreach (var entry
            in paths.Where(e =>
                e.StartsWith($"{dirPath}\\") &&
                e.Length > dirPath.Length + 1 &&
                (recursive)
                || (Depth(e) == dirDepth + 1)))
            result.Add(entry);
        return result;
    }
    static int Depth(string dirPath) => dirPath == @"\" ? 0 : dirPath.Count(c => c == '\\');
}

class File
{
    public byte[] Bytes { get; set; } = Array.Empty<byte>();
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
}