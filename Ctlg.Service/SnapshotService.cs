﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autofac.Features.Indexed;
using Ctlg.Core;
using Ctlg.Core.Interfaces;
using Ctlg.Service.Events;
using File = Ctlg.Core.File;

namespace Ctlg.Service
{
    public class SnapshotService: ISnapshotService
    {
        public SnapshotService(IFilesystemService filesystemService, ICtlgService ctlgService, IIndex<string, IHashFunction> hashFunctions)
        {
            FilesystemService = filesystemService;
            CtlgService = ctlgService;
            HashFunctions = hashFunctions;

            var currentDirectory = FilesystemService.GetCurrentDirectory();
            SnapshotsDirectory = FilesystemService.CombinePath(currentDirectory, "snapshots");
        }

        public string SnapshotsDirectory { get; private set; }

        public string GetSnapshotDirectory(string snapshotName)
        {
            return FilesystemService.CombinePath(SnapshotsDirectory, snapshotName);
        }

        public string GetLastSnapshotPath(string snapshotName)
        {
            var files = GetSnapshotFiles(snapshotName);

            var fileName = files.Max(f => f.Name);
            if (fileName == null) {
              return null;
            }

            return FilesystemService.CombinePath(SnapshotsDirectory, snapshotName, fileName);
        }

        public virtual IEnumerable<File> GetSnapshotFiles(string snapshotName)
        {
            var snapshotsPath = GetSnapshotDirectory(snapshotName);
            if (!FilesystemService.DirectoryExists(snapshotsPath))
            {
                return Enumerable.Empty<File>();
            }
            return FilesystemService.EnumerateFiles(snapshotsPath, "????-??-??_??-??-??");
        }

        public string FindSnapshotFile(string snapshotName, string snapshotDate = null)
        {
            var allSnapshots = GetSnapshotFiles(snapshotName).Select(d => d.Name).OrderBy(s => s);

            var fileName = SelectSnapshotByDate(allSnapshots, snapshotDate);

            return FilesystemService.CombinePath(SnapshotsDirectory, snapshotName, fileName);
        }

        public ISnapshotWriter CreateSnapshotWriter(string name)
        {
            var hashFunctionName = "SHA-256";
            if (!HashFunctions.TryGetValue(hashFunctionName, out IHashFunction hashFunction))
            {
                throw new Exception($"Unsupported hash function {hashFunctionName}");
            }

            var backupDirectory = GetSnapshotDirectory(name);
            FilesystemService.CreateDirectory(backupDirectory);

            var snapshotName = GenerateSnapshotFileName();
            var snapshotPath = FilesystemService.CombinePath(backupDirectory, snapshotName);

            var fileList = FilesystemService.CreateNewFileForWrite(snapshotPath);
            var fileListWriter = new StreamWriter(fileList);
            return new SnapshotWriter(fileListWriter, FilesystemService, CtlgService, hashFunction);
        }

        private string GenerateSnapshotFileName()
        {
            return FormatSnapshotName(DateTime.UtcNow);
        }

        private string FormatSnapshotName(DateTime date)
        {
            return date.ToString("yyyy-MM-dd_HH-mm-ss");
        }

        private string SelectSnapshotByDate(IOrderedEnumerable<string> snapshots, string snapshotDate)
        {
            if (string.IsNullOrEmpty(snapshotDate))
            {
                return snapshots.Last();
            }
            else
            {
                var date = DateTime.Parse(snapshotDate);
                var snapshotDateFormatted = FormatSnapshotName(date);
                return snapshots.Last(s => string.Compare(s, snapshotDateFormatted) <= 0);
            }
        }

        public IEnumerable<SnapshotRecord> ReadSnapshotFile(string path)
        {
            using (var stream = FilesystemService.OpenFileForRead(path))
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    var line = reader.ReadLine();
                    while (line != null)
                    {
                        SnapshotRecord snapshotRecord = null;
                        try
                        {
                            snapshotRecord = new SnapshotRecord(line);
                        }
                        catch (Exception ex)
                        {
                            DomainEvents.Raise(new ErrorEvent(ex));
                        }

                        if (snapshotRecord != null)
                        {
                            yield return snapshotRecord;
                        }

                        line = reader.ReadLine();
                    }
                }
            }
        }

        private IFilesystemService FilesystemService { get; }
        private ICtlgService CtlgService { get; }
        private IIndex<string, IHashFunction> HashFunctions { get; }
    }
}
