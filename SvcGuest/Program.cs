using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using McMaster.Extensions.CommandLineUtils;

namespace SvcGuest
{

    [Command(Name = "SvcGuest.exe", Description = "Host any program as a Windows service.")]
    class Program
    {
        #region Command Line Arguments
        // ReSharper disable UnassignedGetOnlyAutoProperty
        [Option("-i|--install", Description = "Install the service")]
        public bool Install { get; }

        [Option("-u|--uninstall", Description = "Uninstall the service")]
        public bool Uninstall { get; }

        [Option("-c|--config", CommandOptionType.SingleValue, Description = "Location for the config file. Default value is \"default.service\".")]
        public string ConfigPath { get; }

        [Option("--impersonated", CommandOptionType.NoValue, ShowInHelpText = false, Description = "Indicates this is a helper process used to spawn the object process after impersonation.")]
        public bool IsImpersonatedProcess { get; }

        [Option("--LaunchType", CommandOptionType.SingleValue, ShowInHelpText = false)]
        public string ExecConfigLaunchType { get; }

        [Option("--LaunchIndex", CommandOptionType.SingleValue, ShowInHelpText = false)]
        public int ExecConfigIndex { get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);
        #endregion

        public static ManagedProgramWrapper Wrapper;

        // ReSharper disable once UnusedMember.Global
        public void OnExecute()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (currentDirectory != null) Directory.SetCurrentDirectory(currentDirectory);

            // read config
            var configPath = ConfigPath ?? "default.service";
            Globals.ConfigPath = configPath;
            var config = new Config(configPath);
            Globals.Config = config;
            Globals.ServiceArguments = $"--config {configPath}";

            Debug.WriteLine($"{IsImpersonatedProcess}");

            // If this is a helper process 
            if (IsImpersonatedProcess)
            {
                Debug.WriteLine("Executing impersonation helper routine");
                
                ExecConfig execConfig;
                switch (ExecConfigLaunchType)
                {
                    case "ExecStart":
                        execConfig = Globals.Config.ExecStart[ExecConfigIndex];
                        break;
                    default:
                        throw new ArgumentException();
                }
                Wrapper = new ManagedProgramWrapper(execConfig.ProgramPath, execConfig.Arguments);
                var hasExited = false;
                Wrapper.ProgramExited += (sender, eventArgs) => { hasExited = true; };
                Wrapper.Start();
                
                while (!hasExited)
                {
                    Thread.Sleep(1000);
                }
                return;
            }

            if (Environment.UserInteractive)
            {
                if (Install && Uninstall)
                {
                    Console.WriteLine("Self-contradictory arguments?");
                    // TODO: correct return value
                } else if (Install) {
                    InstallService();
                } else if (Uninstall) {
                    UninstallService();
                }
            }
            else
            {
                ServiceBase.Run(new SupervisorService());
            }
        }

        /// <summary>
        /// Installs self as a Windows service.
        /// </summary>
        private static void InstallService()
        {
            ManagedInstallerClass.InstallHelper(new[]
            {
                Assembly.GetExecutingAssembly().Location
            });
        }

        /// <summary>
        /// Uninstalls self-installed service
        /// </summary>
        private static void UninstallService()
        {
            ManagedInstallerClass.InstallHelper(new [] { "/u", Assembly.GetExecutingAssembly().Location });
        }

        /// <summary>
        /// Triggered on receiving a termination request. Clean up self, terminate all child process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnProcessExit(object sender, EventArgs e)
        {
            Debug.WriteLine("Being killed, cleaning up...");
            Wrapper?.Stop();

            // shut down all child processes
            foreach (var pid in ProgramWrapper.GetChildProcessIds(ProgramWrapper.SelfProcessId))
            {
                ProgramWrapper.QuitProcess(Process.GetProcessById(pid));
            }
        }

        /// <summary>
        /// The default global exception handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e);
        }
    }
}
