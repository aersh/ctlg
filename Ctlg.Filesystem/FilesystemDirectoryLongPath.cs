﻿using System;
using System.Collections.Generic;
using Ctlg.Core.Interfaces;
using Pri.LongPath;
using File = Ctlg.Core.File;

namespace Ctlg.Filesystem
{
    public class FilesystemDirectoryLongPath: IFilesystemDirectory
    {
        public File Directory { get; set; }

        public FilesystemDirectoryLongPath(string path)
        {
            Initialize(new DirectoryInfo(path));
        }

        private FilesystemDirectoryLongPath(DirectoryInfo directoryInfo)
        {
            Initialize(directoryInfo);
        }

        private void Initialize(DirectoryInfo directoryInfo)
        {
            _directoryInfo = directoryInfo;
            Directory = new File
            {
                IsDirectory = true,
                Name = directoryInfo.Name,
                FullPath = directoryInfo.FullName,
                FileCreatedDateTime = directoryInfo.CreationTimeUtc,
                FileModifiedDateTime = directoryInfo.LastWriteTimeUtc,
                RecordUpdatedDateTime = DateTime.UtcNow
            };
        }

        public IEnumerable<IFilesystemDirectory> EnumerateDirectories()
        {
            foreach (var dir in _directoryInfo.GetDirectories())
            {
                yield return new FilesystemDirectoryLongPath(dir);
            }
        }

        public IEnumerable<File> EnumerateFiles(string searchPattern)
        {
            foreach (var file in _directoryInfo.GetFiles(searchPattern))
            {
                yield return CreateFilesystemEntry(file);
            }
        }

        private DirectoryInfo _directoryInfo;

        protected static File CreateFilesystemEntry(FileInfo fileInfo)
        {
            return new File
            {
                FullPath = fileInfo.FullName,
                IsDirectory = false,
                Name = fileInfo.Name,
                FileCreatedDateTime = fileInfo.CreationTimeUtc,
                FileModifiedDateTime = fileInfo.LastWriteTimeUtc,
                Size = fileInfo.Length,
                RecordUpdatedDateTime = DateTime.UtcNow
            };
        }
    }
}