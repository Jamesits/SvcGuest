using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SvcGuestTest
{
    [TestClass]
    public class OverallTest
    {
        private string ProgramDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        [TestMethod]
        public void IfRequiredFilesExist()
        {
            Assert.IsTrue(File.Exists(Path.Combine(ProgramDirectory, "SvcGuest.exe")));
            Assert.IsTrue(File.Exists(Path.Combine(ProgramDirectory, "FakeTarget.exe")));
        }

        // TODO: test the assembly with some config file
        // but it requires elevation and cannot be run under Azure DevOps hosted agent.
    }
}
