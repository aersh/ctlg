﻿using System;
using Autofac.Features.Indexed;
using Ctlg.Core;
using Ctlg.Core.Interfaces;
using Ctlg.Service.Events;

namespace Ctlg.Service.Commands
{
    public class AddCommand : ICommand
    {
        public string HashFunctionName { get; set; }
        public string Path { get; set; }
        public string SearchPattern { get; set; }

        private IHashFunction HashFunction;

        private ITreeProvider TreeProvider { get; }
        private ICtlgService CtlgService { get; }
        private IDataService DataService { get; }
        private IFilesystemService FilesystemService { get; }
        private IArchiveService ArchiveService { get; }

        public AddCommand(ITreeProvider treeProvider, ICtlgService ctlgService,
            IDataService dataService, IFilesystemService filesystemService, IArchiveService archiveService)
        {
            DataService = dataService;
            FilesystemService = filesystemService;
            ArchiveService = archiveService;
            TreeProvider = treeProvider;
            CtlgService = ctlgService;
        }

        public void Execute()
        {
            HashFunction = CtlgService.GetHashFunction(HashFunctionName ?? "SHA-256");

            var root = TreeProvider.ReadTree(Path, SearchPattern);
            var treeWalker = new TreeWalker(root);
            treeWalker.Walk(ProcessFile);

            DataService.AddDirectory(root);

            DataService.SaveChanges();

            DomainEvents.Raise(new AddCommandFinished());
        }

        private void CalculateHashes(File file)
        {
            try
            {
                var hash = CtlgService.CalculateHashForFile(file, HashFunction);
                DomainEvents.Raise(new HashCalculated(file.RelativePath, hash.Value));
            }
            catch (Exception e)
            {
                DomainEvents.Raise(new ErrorEvent(e));
            }
        }

        private void ProcessArchive(File file)
        {
            try
            {
                using (var stream = FilesystemService.OpenFileForRead(file.FullPath))
                {
                    var archive = ArchiveService.OpenArchive(stream);

                    DomainEvents.Raise(new ArchiveFound(file.FullPath));

                    foreach (var entry in archive.EnumerateEntries())
                    {
                        file.Contents.Add(entry);

                        DomainEvents.Raise(new ArchiveEntryFound(entry.Name));
                    }
                }
            }
            catch (Exception e)
            {
                DomainEvents.Raise(new ErrorEvent(e));
            }
        }

        protected void ProcessFile(File file)
        {
            CalculateHashes(file);

            if (ArchiveService.IsArchiveExtension(file.FullPath))
            {
                ProcessArchive(file);
            }
        }
    }
}
