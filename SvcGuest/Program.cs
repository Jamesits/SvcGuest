using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.ServiceProcess;
using System.Threading;
using McMaster.Extensions.CommandLineUtils;
using SvcGuest.ProgramWrappers;
using SvcGuest.ServiceInterface;
using SvcGuest.Win32;

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

        // ReSharper disable once StringLiteralTypo
        [Option("--noelevate", CommandOptionType.NoValue, ShowInHelpText = false, Description = "Do not try to elevate automatically if not running as Administrator.")]
        public bool DoNotElevate { get; }

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

            // If this is a helper process 
            if (IsImpersonatedProcess)
            {
                Debug.WriteLine("Executing impersonation helper routine");
                LoadConfig();
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
                Debug.WriteLine($"IsElevated: {UACHelper.IsRunAsAdmin()}");
                Debug.WriteLine($"Cmdline: {Environment.CommandLine}");

                if (!UACHelper.IsRunAsAdmin())
                {
                    Console.Error.WriteLine("Warning: you may not have sufficient privilege to install services.");
                }
                //if (!DoNotElevate && !UACHelper.IsRunAsAdmin())
                //{
                //    TryElevate();
                //}

                if (Install && Uninstall)
                {
                    Console.WriteLine("Self-contradictory arguments?");
                    Environment.Exit(1);
                } else if (Install) {
                    LoadConfig();
                    InstallService();
                } else if (Uninstall) {
                    LoadConfig();
                    UninstallService();
                }
                else
                {
                    Console.WriteLine("Searching for unit files in program directory...");
                    // Let's install every service in this folder
                    try
                    {
                        var txtFiles = Directory.EnumerateFiles(Globals.ExecutableDirectory, "*.service", SearchOption.TopDirectoryOnly);

                        foreach (string currentFile in txtFiles)
                        {
                            var proceed = Prompt.GetYesNo($"Do you want to register {Path.GetFileName(currentFile)}?", defaultAnswer: true);
                            if (proceed) RegisterService(currentFile);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            else
            {
                LoadConfig();
                ServiceBase.Run(new SupervisorService());
            }
        }

        public void LoadConfig()
        {
            // read config
            var configPath = ConfigPath ?? "default.service";
            Globals.ConfigPath = configPath;
            var config = new Config(configPath);
            Globals.Config = config;
            Globals.ServiceArguments = $"--config {configPath}";

            Debug.WriteLine($"{IsImpersonatedProcess}");
        }

        private static void RegisterService(string filename)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = Globals.ExecutablePath,
                    Arguments = $"--install --config \"{filename}\"",
                }
            };

            try
            {
                process.Start();
                process.WaitForExit();
            }
            catch
            {
                Console.Error.WriteLine("Register failed: unable to execute self.");
                Environment.Exit(1);
            }
        }

        // TODO: make it work
        private static void TryElevate()
        {
            if (UACHelper.IsRunAsAdmin()) return;
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = Globals.ExecutablePath,
                    Arguments = Environment.CommandLine,
                    Verb = "runas",
                }
            };

            try
            {
                process.Start();
                process.WaitForExit();
                Environment.Exit(process.ExitCode);
            }
            catch
            {
                Console.Error.WriteLine("Elevation failed.");
                Environment.Exit(1);
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
        /// <param name="eventArgs"></param>
        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
        {
            var e = eventArgs.ExceptionObject as Exception;
            if (e?.InnerException is System.Security.SecurityException)
            {
                Console.Error.WriteLine("You have insufficient permission for this action. Consider run this program as Administrator.");
                return;
            }
            else Console.WriteLine(e?.InnerException);
        }
    }
}
