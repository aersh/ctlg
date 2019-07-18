﻿using System;
using Ctlg.Core;
using Ctlg.Service;
using Ctlg.Service.Commands;
using Ctlg.Service.Utils;
using Moq;
using NUnit.Framework;

namespace Ctlg.UnitTests
{
    [TestFixture]
    public class FindCommandTests
    {
        [Test]
        public void Execute_Always_CallsFindFiles()
        {

            var serviceMock = new Mock<ICtlgService>(MockBehavior.Strict);

            serviceMock.Setup(s => s.GetHashAlgorithm(It.IsAny<string>()))
                .Returns(new HashAlgorithm {HashAlgorithmId = 1, Name = "test"});

            byte[] hashParameter = null;
            serviceMock.Setup(s => s.FindFiles(
                It.IsAny<Hash>(),
                It.IsAny<long?>(),
                It.IsAny<string>())).Callback<Hash, long?, string>((h, s, n) => hashParameter = h.Value);

            var command = new FindCommand(serviceMock.Object)
            {
                HashFunctionName = "test",
                Hash = "01FF"
            };
            command.Execute();

            serviceMock.VerifyAll();
            Assert.That(FormatBytes.ToHexString(hashParameter), Is.EqualTo("01ff").IgnoreCase);
        }
    }
}
