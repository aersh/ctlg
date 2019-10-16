﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autofac.Extras.Moq;
using Ctlg.Core;
using Ctlg.Core.Interfaces;
using Ctlg.Service;
using Ctlg.Service.Commands;
using Moq;
using NUnit.Framework;

namespace Ctlg.UnitTests
{
    [TestFixture]
    public class SnapshotReaderTests
    {
        private AutoMock AutoMock;
        private Mock<ISnapshotService> SnapshotServiceMock;
        private Mock<ICtlgService> CtlgServiceMock;
        private IEnumerable<SnapshotRecord> SnapshotRecords
        {
            get
            {
                if (SnapshotRecord == null)
                {
                    yield break;
                }

                yield return SnapshotRecord;
            }
        }
        private string SnapshotPath;
        private SnapshotRecord SnapshotRecord;
        private File Root;
        private File File;

        [SetUp]
        public void Setup()
        {
            SnapshotPath = "snapshot_path";

            SnapshotRecord = new SnapshotRecord("ab",
                new DateTime(2019, 1, 1), 1024, "foo");

            Root = new File("root", true);

            File = new File("foo")
            {
                Size = 1234,
                FileModifiedDateTime = new DateTime(2019, 10, 15)
            };

            AutoMock = AutoMock.GetLoose();

            SnapshotServiceMock = AutoMock.Mock<ISnapshotService>();
            CtlgServiceMock = AutoMock.Mock<ICtlgService>();

            SnapshotServiceMock.Setup(s => s.FindSnapshotPath("snapshot", null))
                .Returns(() => SnapshotPath);

            SnapshotServiceMock.Setup(s => s.ReadSnapshotFile("snapshot_path"))
                .Returns(() => SnapshotRecords);

            CtlgServiceMock.Setup(s => s.GetInnerFile(
                It.IsAny<File>(), It.IsAny<string>()))
                .Returns(() => File);
        }

        [TearDown]
        public void TearDown()
        {
            AutoMock.Dispose();
        }

        [Test]
        public void ReadHashesFromLatestSnapshot_WhenSnapshotDoesNotExist()
        {
            SnapshotPath = null;

            ReadHashes();

            CtlgServiceMock.Verify(s => s.SortTree(It.IsAny<File>()), Times.Never);

            Assert.That(File.Hashes, Is.Empty);
        }

        [Test]
        public void ReadHashesFromLatestSnapshot_WhenFileNotFound()
        {
            File = null;

            ReadHashes();

            CtlgServiceMock.Verify(s => s.SortTree(Root), Times.Once);
            CtlgServiceMock.Verify(s => s.GetInnerFile(Root, "foo"), Times.Once);
        }

        [Test]
        public void ReadHashesFromLatestSnapshot_WhenDateAndSizeDontMatch()
        {
            ReadHashes();

            CtlgServiceMock.Verify(s => s.SortTree(Root), Times.Once);
            CtlgServiceMock.Verify(s => s.GetInnerFile(Root, "foo"), Times.Once);

            Assert.That(File.Hashes, Is.Empty);
        }

        [Test]
        public void ReadHashesFromLatestSnapshot_WhenDateDoesNotMatch()
        {
            File.Size = SnapshotRecord.Size;

            ReadHashes();

            CtlgServiceMock.Verify(s => s.SortTree(Root), Times.Once);
            CtlgServiceMock.Verify(s => s.GetInnerFile(Root, "foo"), Times.Once);

            Assert.That(File.Hashes, Is.Empty);
        }

        [Test]
        public void ReadHashesFromLatestSnapshot_WhenDateAndSizeMatch()
        {
            File.Size = SnapshotRecord.Size;
            File.FileModifiedDateTime = SnapshotRecord.Date;

            ReadHashes();

            Assert.That(File.Hashes.Count, Is.EqualTo(1));
            Assert.That(File.Hashes.First, Is.EqualTo(new Hash(
                HashAlgorithmId.SHA256, new byte[] { 0xab })));
        }

        private void ReadHashes()
        {
            var reader = AutoMock.Create<SnapshotReader>();
            reader.ReadHashesFromLatestSnapshot("snapshot", Root);
        }
    }
}