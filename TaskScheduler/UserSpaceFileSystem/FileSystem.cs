using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using DokanNet;
using TaskScheduler;

namespace UserSpaceFileSystem
{
    public class FileSystem : IDokanOperations
    {

        public class File
        {
            private byte[] _data;
            private string _name;
            private DateTime _created;

            public File(string name, DateTime created, byte[] data)
            {
                _data = data;
                _name = name;
                _created = created;
            }

            public byte[] Data { get { return _data; } set { _data = value; } }

            public string Name { get { return _name; } set { _name = value; } }

            public DateTime Created { get { return _created; } set { _created = value; } }
        }

        private readonly Dictionary<string, File> inputFiles = new();
        private readonly Dictionary<string, File> outputFiles = new();

        //private readonly Dictionary<string, bool> filesToBeProcessed = new();

        //private readonly object _lock = new();

        //private readonly TaskScheduler.TaskScheduler _taskScheduler;

        private readonly static int CAPACITY = 512 * 1024 * 1024;
        private int totalNumberOfBytes = CAPACITY;
        private int totalNumberOfFreeBytes = CAPACITY;

        /*public FileSystem(TaskScheduler.TaskScheduler scheduler)
        {
            _taskScheduler = scheduler;
            new Thread(() =>
            {
                while (true)
                {
                    lock (_lock)
                    {
                        Thread.Sleep(40000);
                        foreach (string file in filesToBeProcessed.Keys)
                            if (filesToBeProcessed[file])
                            {
                                scheduler.AddTask(new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 2000, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, new FolderResource("V:\\output\\"), new FileResource("V:" + file)));
                                filesToBeProcessed[file] = false;
                            }
                    }
                }
            });//.Start();
        }*/

        public void Cleanup(string fileName, IDokanFileInfo info) { }

        public void CloseFile(string fileName, IDokanFileInfo info) { }

        public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, IDokanFileInfo info)
        {
            if (mode == FileMode.CreateNew)
            {
                if (fileName.StartsWith(Path.DirectorySeparatorChar + "input" + Path.DirectorySeparatorChar) && !inputFiles.ContainsKey(fileName))
                    inputFiles.Add(fileName, new File(fileName, DateTime.Now, Array.Empty<byte>()));
                else if (fileName.StartsWith(Path.DirectorySeparatorChar + "output" + Path.DirectorySeparatorChar) && !outputFiles.ContainsKey(fileName))
                    outputFiles.Add(fileName, new File(fileName, DateTime.Now, Array.Empty<byte>()));
            }
            return NtStatus.Success;
        }

        public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
        {
            files = new List<FileInformation>();
            if (fileName == Path.DirectorySeparatorChar.ToString())
            {
                files.Add(new FileInformation()
                {
                    Attributes = FileAttributes.Directory,
                    FileName = "input"
                });
                files.Add(new FileInformation()
                {
                    Attributes = FileAttributes.Directory,
                    FileName = "output"
                });
            }
            else if (fileName.StartsWith(Path.DirectorySeparatorChar + "input"))
            {
                foreach (var file in inputFiles.Values)
                {
                    files.Add(new FileInformation()
                    {
                        FileName = Path.GetFileName(file.Name),
                        Length = file.Data.Length,
                        Attributes = FileAttributes.Normal,
                        CreationTime = file.Created
                    });
                }
            }
            else if (fileName.StartsWith(Path.DirectorySeparatorChar + "output"))
            {
                foreach (var file in outputFiles.Values)
                {
                    files.Add(new FileInformation()
                    {
                        FileName = Path.GetFileName(file.Name),
                        Length = file.Data.Length,
                        Attributes = FileAttributes.Normal,
                        CreationTime = file.Created
                    });
                }
            }
            return NtStatus.Success;
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
        {
            throw new NotImplementedException();
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
            if (fileName == Path.DirectorySeparatorChar.ToString())
            {
                fileInfo = new()
                {
                    FileName = fileName,
                    Attributes = FileAttributes.Directory
                };
            }
            else if (fileName == Path.DirectorySeparatorChar + "input")
            {
                fileInfo = new()
                {
                    FileName = "input",
                    Attributes = FileAttributes.Directory
                };
            }
            else if (fileName == Path.DirectorySeparatorChar + "output")
            {
                fileInfo = new()
                {
                    FileName = "output",
                    Attributes = FileAttributes.Directory
                };
            }
            else if (fileName.StartsWith(Path.DirectorySeparatorChar + "input") && inputFiles.ContainsKey(fileName))
            {
                fileInfo = new()
                {
                    FileName = fileName,
                    Length = inputFiles[fileName].Data.Length,
                    Attributes = FileAttributes.Normal,
                    CreationTime = inputFiles[fileName].Created
                };
            }
            else if (fileName.StartsWith(Path.DirectorySeparatorChar + "output") && outputFiles.ContainsKey(fileName))
            {
                fileInfo = new()
                {
                    FileName = fileName,
                    Length = outputFiles[fileName].Data.Length,
                    Attributes = FileAttributes.Normal,
                    CreationTime = outputFiles[fileName].Created
                };
            }
            else
            {
                fileInfo = default;
                return NtStatus.Error;
            }
            return NtStatus.Success;
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            security = null;
            return NtStatus.Success;
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
        {
            volumeLabel = "UserSpaceFSVolume";
            features = FileSystemFeatures.None;
            fileSystemName = "UserSpaceFS";
            maximumComponentLength = 255;
            return NtStatus.Success;
        }

        public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus Mounted(string mountPoint, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {
            File? file = null;   
            if (fileName.StartsWith(Path.DirectorySeparatorChar + "input"))
                file = inputFiles[fileName];
            else if (fileName.StartsWith(Path.DirectorySeparatorChar + "output"))
                file = outputFiles[fileName];
            file?.Data.Skip((int)offset).Take(buffer.Length).ToArray().CopyTo(buffer, 0);
            int diff = file.Data.Length - (int)offset;
            bytesRead = buffer.Length > diff ? diff : buffer.Length;
            return NtStatus.Success;
        }

        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
        {
            return NtStatus.Error;
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
        {
            return NtStatus.Success;
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            return NtStatus.Error;
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, IDokanFileInfo info)
        {
            return NtStatus.Error;
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus Unmounted(IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
        {
            File? file = null;
            if (fileName.StartsWith(Path.DirectorySeparatorChar + "input"))
            {
                //if (!inputFiles.ContainsKey(fileName))
                    //inputFiles.Add(fileName, new File(fileName, DateTime.Now, Array.Empty<byte>()));
                file = inputFiles[fileName];
            }
            else if (fileName.StartsWith(Path.DirectorySeparatorChar + "output"))
            {
                //if (!outputFiles.ContainsKey(fileName))
                    //outputFiles.Add(fileName, new File(fileName, DateTime.Now, Array.Empty<byte>()));
                file = outputFiles[fileName];
            }
            if (info.WriteToEndOfFile)
            {
                file.Data = file.Data.Concat(buffer).ToArray();
                bytesWritten = buffer.Length;
            }
            else
            {
                int difference = file.Data.Length - (int)offset;
                totalNumberOfFreeBytes += difference;
                file.Data = file.Data.Take((int)offset).Concat(buffer).ToArray();
                bytesWritten = buffer.Length;
            }
            totalNumberOfFreeBytes -= bytesWritten;

            //_task = new EdgeDetectionTask(new DateTime(2023, 2, 22, 0, 0, 0), 2000, 1, new ControlToken(), new ControlToken(), MyTask.TaskPriority.Normal, new FolderResource("V:\\output\\"), new FileResource("V:\\" + fileName));
            /*if (!filesToBeProcessed.ContainsKey(fileName))
            {
                filesToBeProcessed.Add(fileName, true);
                //lock (_lock)
                  //Monitor.PulseAll(_lock);
            }*/
            return NtStatus.Success;
        }

        public NtStatus Mounted(IDokanFileInfo info)
        {
            return NtStatus.Success;
        }
    }
}
