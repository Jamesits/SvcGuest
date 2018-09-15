using System;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using McMaster.Extensions.CommandLineUtils;

namespace SvcGuest
{

    [Command(Name = "SvcGuest.exe", Description = "Host any program as a Windows service.")]
    class Program
    {
        [Option("-i|--install", Description = "Install the service")]
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public bool Install { get; }

        [Option("-u|--uninstall", Description = "Uninstall the service")]
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public bool Uninstall { get; }

        [Option("-c|--config", CommandOptionType.SingleValue, Description = "Location for the config file. Default value is \"default.service\".")]
        
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public string ConfigPath { get; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        // ReSharper disable once UnusedMember.Global
        public void OnExecute()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (currentDirectory != null) Directory.SetCurrentDirectory(currentDirectory);

            // read config
            var configPath = ConfigPath ?? "default.service";
            Globals.ConfigPath = configPath;
            var configRdr = new ConfigReader(configPath);
            Globals.Config = configRdr.Config;
            Globals.ServiceArguments = $"--config {configPath}";

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

        private static void InstallService()
        {
            ManagedInstallerClass.InstallHelper(new[]
            {
                Assembly.GetExecutingAssembly().Location
            });
        }

        private static void UninstallService()
        {
            ManagedInstallerClass.InstallHelper(new [] { "/u", Assembly.GetExecutingAssembly().Location });
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e);
        }
    }
}
