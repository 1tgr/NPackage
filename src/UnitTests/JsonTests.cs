using System;
using System.IO;
using NPackage.Core;
using NUnit.Framework;
using Rhino.Mocks;

namespace NPackage.UnitTests
{
    [TestFixture]
    public class JsonTests
    {
        [Test]
        public void Serialize()
        {
            DownloadWorkflow workflow = new DownloadWorkflow();
            workflow.Enqueue(
                new Uri("http://np.partario.com/rhino.mocks-3.6/rhino.mocks.np"),
                AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar,
                File.Delete);

            while (workflow.Step())
                ;
        }
    }
}