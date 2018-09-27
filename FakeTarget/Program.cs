using System;
using McMaster.Extensions.CommandLineUtils;

namespace FakeTarget
{

    [Command(Name = "FakeTarget.exe", Description = "Host any program as a Windows service.")]
    class Program
    {
        #region Command Line Arguments
        // ReSharper disable UnassignedGetOnlyAutoProperty
        [Option("--file", Description = "Echo to which file")]
        public string Filename { get; }

        [Option("--onquit", Description = "Echo on quit")]
        public string QuitTest { get; }

        // ReSharper restore UnassignedGetOnlyAutoProperty

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);
        #endregion


        // ReSharper disable once UnusedMember.Global
        public void OnExecute()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            // execute
        }

        /// <summary>
        /// The default global exception handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
        {
            var e = eventArgs.ExceptionObject as Exception;
            Console.WriteLine(e?.InnerException);
        }
    }
}
