using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SvcGuestTest
{
    [TestClass]
    public class OverallTest
    {
        private string ProgramDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static int RunAndWaitForOutput(string programArgs, out List<string> stdout, out List<string> stderr)
        {
            var _stdout = new List<string>();
            var _stderr = new List<string>();

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "SvcGuest.exe",
                    Arguments = programArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
                EnableRaisingEvents = true,
            };
            p.OutputDataReceived += (sender, args) => _stdout.Add(args.Data);
            p.ErrorDataReceived += (sender, args) => _stderr.Add(args.Data);
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();

            stdout = _stdout;
            stderr = _stderr;
            return p.ExitCode;
        }

        private int GetIndex(List<string> source, string target)
        {
            for (var i = 0; i < source.Count; ++i)
            {
                if (string.Equals(source[i], target)) return i;
            }

            return -1;
        }

        private bool AssertStringSequence(List<string> source, List<string> expectation)
        {
            for (var i = 0; i < expectation.Count - 1; ++i)
            {
                if (GetIndex(source, expectation[i]) > GetIndex(source, expectation[i + 1])) return false;
            }

            return true;
        }

        private bool AssertStringNotInclude(List<string> source, List<string> blockedList)
        {
            foreach (var s in blockedList)
                if (!source.Any(x => string.Equals(x, s, StringComparison.Ordinal)))
                    return false;
            return true;
        }

        // test the assembly with some config file
        [TestMethod]
        public void BasicFunctionalityTest()
        {
            Assert.AreEqual(
                0, 
                RunAndWaitForOutput(@"-D --config TestConfigs\BasicFunctionalityTest.service", out var stdout, out var stderr),
                $"stdout: \n{string.Join("\n", stdout)}\nstderr: \n{string.Join("\n", stderr)}"
                );
            var ret = false;
            foreach (var line in stdout)
            {
                if (string.Equals("ExecStart", line)) ret = true;
            }
            Assert.IsTrue(ret);
        }

        [TestMethod]
        public void LaunchSequenceTest()
        {
            Assert.AreEqual(0, 
                RunAndWaitForOutput(@"-D --config TestConfigs\LaunchSequenceTest.service", out var stdout, out var stderr),
                $"stdout: \n{string.Join("\n", stdout)}\nstderr: \n{string.Join("\n", stderr)}"
                );
            Assert.IsTrue(AssertStringSequence(stdout, new List<string>()
            {
                "ExecStartPre1",
                "ExecStartPre2",
                "ExecStartPre3",
                "ExecStartPost1",
                "ExecStart1",
                "ExecStop1",
                "ExecStop2",
                "ExecStopPost1",
                "ExecStopPost2",
            }));
        }

        [TestMethod]
        public void LaunchSequenceTestWithError()
        {
            Assert.AreEqual(0, 
                RunAndWaitForOutput(@"-D --config TestConfigs\LaunchSequenceTestWithError.service", out var stdout, out var stderr),
                $"stdout: \n{string.Join("\n", stdout)}\nstderr: \n{string.Join("\n", stderr)}"
                );
            Assert.IsTrue(AssertStringSequence(stdout, new List<string>()
            {
                "ExecStartPre1",
                "ExecStartPre2",
                "ExecStartPre3",
                "ExecStartPost1",
                "ExecStart1",
                "ExecStopPost1",
                "ExecStopPost2",
            }));
            Assert.IsTrue(AssertStringNotInclude(stdout, new List<string>()
            {
                "ExecStop1",
                "ExecStop2",
            }));
        }
    }
}
