using System;
using System.Security.Principal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SudoLib;
using SudoLib.Win32;

namespace SudoTest
{
    [TestClass]
    public class SudoTest
    {
        [TestMethod]
        public void TestLaunchProcessWithCurrentUserAccount()
        {
            Sudo s;

            s = new Sudo("FakeTarget.exe");
            s.Start();

            s = new Sudo("FakeTarget.exe", "-help");
            s.Start();

            s = new Sudo(new SudoConfig()
            {
                Program = "FakeTarget.exe",
                Arguments = "-help",
            });
            s.Start();
        }

        [TestMethod]
        public void TestHideWindow()
        {
            Sudo s;

            s = new Sudo(new SudoConfig()
            {
                Program = "FakeTarget.exe",
                Arguments = "-help",
                WindowMode = DeepDarkWin32Fantasy.ShowWindowCommands.SW_HIDE,
            });
            s.Start();
        }

        [TestMethod]
        public void TestLaunchProcessImpersonated()
        {
            Sudo s;

            // test with current user
            s = new Sudo(new SudoConfig()
            {
                Program = "FakeTarget.exe",
                Arguments = "-help",
                UserName = "jamesits@internal.swineson.me",
            });
            s.Start();
        }
    }
}
