using Microsoft.VisualStudio.TestTools.UnitTesting;
using SvcGuest;

namespace SvcGuestTest
{
    [TestClass]
    public class ExecConfigTest
    {
        [TestMethod]
        public void NormalCase()
        {
            var c = new ExecConfig(@"C:\Windows\System32\cmd.exe");
            Assert.AreEqual(@"C:\Windows\System32\cmd.exe", c.ProgramPath);
            Assert.AreEqual(@"", c.Arguments);
            Assert.AreEqual(ExecLaunchPrivilegeLevel.Normal, c.ExecLaunchPrivilegeLevel);
            Assert.AreEqual(false, c.IgnoreFirstArg);
            Assert.AreEqual(false, c.IgnoreFailure);
        }

        [TestMethod]
        public void Arguments()
        {
            var c = new ExecConfig(@"C:\Windows\System32\cmd.exe --arg1 -arg2 /arg3 arg4");
            Assert.AreEqual(@"C:\Windows\System32\cmd.exe", c.ProgramPath);
            Assert.AreEqual(@" --arg1 -arg2 /arg3 arg4", c.Arguments);
        }

        [TestMethod]
        public void SpecialCharacters()
        {
            var c = new ExecConfig("\"C:\\Program Files (x86)\\Windows Media Player\\wmplayer.exe\" --arg1 -arg2 /arg3 arg4");
            Assert.AreEqual("\"C:\\Program Files (x86)\\Windows Media Player\\wmplayer.exe\"", c.ProgramPath);
            Assert.AreEqual(" --arg1 -arg2 /arg3 arg4", c.Arguments);
        }

        [TestMethod]
        public void Modifiers()
        {
            const string defPath = @"C:\Windows\System32\cmd.exe";
            ExecConfig c;

            // @ only
            c = new ExecConfig("@" + defPath);
            Assert.AreEqual(true, c.IgnoreFirstArg);
            Assert.AreEqual(false, c.IgnoreFailure);
            Assert.AreEqual(ExecLaunchPrivilegeLevel.Normal, c.ExecLaunchPrivilegeLevel);

            // - only
            c = new ExecConfig("-" + defPath);
            Assert.AreEqual(false, c.IgnoreFirstArg);
            Assert.AreEqual(true, c.IgnoreFailure);
            Assert.AreEqual(ExecLaunchPrivilegeLevel.Normal, c.ExecLaunchPrivilegeLevel);

            // + only
            c = new ExecConfig("+" + defPath);
            Assert.AreEqual(false, c.IgnoreFirstArg);
            Assert.AreEqual(false, c.IgnoreFailure);
            Assert.AreEqual(ExecLaunchPrivilegeLevel.Full, c.ExecLaunchPrivilegeLevel);

            // ! only
            c = new ExecConfig("!" + defPath);
            Assert.AreEqual(false, c.IgnoreFirstArg);
            Assert.AreEqual(false, c.IgnoreFailure);
            Assert.AreEqual(ExecLaunchPrivilegeLevel.IgnoreUser, c.ExecLaunchPrivilegeLevel);

            // !! only
            c = new ExecConfig("!!" + defPath);
            Assert.AreEqual(false, c.IgnoreFirstArg);
            Assert.AreEqual(false, c.IgnoreFailure);
            Assert.AreEqual(ExecLaunchPrivilegeLevel.Least, c.ExecLaunchPrivilegeLevel);

            // mix order
            c = new ExecConfig("!!@" + defPath);
            Assert.AreEqual(true, c.IgnoreFirstArg);
            Assert.AreEqual(false, c.IgnoreFailure);
            Assert.AreEqual(ExecLaunchPrivilegeLevel.Least, c.ExecLaunchPrivilegeLevel);

            c = new ExecConfig("-+" + defPath);
            Assert.AreEqual(false, c.IgnoreFirstArg);
            Assert.AreEqual(true, c.IgnoreFailure);
            Assert.AreEqual(ExecLaunchPrivilegeLevel.Full, c.ExecLaunchPrivilegeLevel);

            c = new ExecConfig("@-!!" + defPath);
            Assert.AreEqual(true, c.IgnoreFirstArg);
            Assert.AreEqual(true, c.IgnoreFailure);
            Assert.AreEqual(ExecLaunchPrivilegeLevel.Least, c.ExecLaunchPrivilegeLevel);
        }
    }
}
