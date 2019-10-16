﻿using System;
using System.Collections.Generic;
using Ctlg.Core.Interfaces;

namespace Ctlg.Service
{
    public class IndexService: IIndexService
    {
        public IndexService(int hashLength)
        {
            HashLength = hashLength;
        }

        public void Add(byte[] hash)
        {
            if (hash.Length != HashLength)
            {
                throw new Exception($"Hash is {hash.Length} bytes. Expected hash to have lenght {HashLength} bytes.");
            }
            _set.Add(hash);
        }

        public IEnumerable<byte[]> GetAllHashes()
        {
            return _set;
        }

        private SortedSet<byte[]> _set = new SortedSet<byte[]>(new ByteArrayComparer());

        private readonly int HashLength;
    }
}