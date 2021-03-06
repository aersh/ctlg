﻿using System;
using Ctlg.Core;

namespace Ctlg.Service.Events
{
    public class BackupEntryCreated: IDomainEvent
    {
        public BackupEntryCreated(File file, byte[] hash, bool hashCalculated, bool isHashFoundInIndex, bool newFileAddedToStorage)
        {
            File = file;
            Hash = hash;
            HashCalculated = hashCalculated;
            IsHashFoundInIndex = isHashFoundInIndex;
            NewFileAddedToStorage = newFileAddedToStorage;
        }

        public File File { get; set; }
        public byte[] Hash { get; set; }
        public bool HashCalculated { get; set; }
        public bool IsHashFoundInIndex { get; set; }
        public bool NewFileAddedToStorage { get; set; }
    }
}
