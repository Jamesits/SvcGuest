using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Text;

namespace SvcGuest
{
    /// <summary>
    /// Handles installutil.exe
    /// </summary>
    [RunInstaller(true)]
    // ReSharper disable once UnusedMember.Global
    public class GuestServiceInstaller : Installer
    {
        public GuestServiceInstaller()
        {
            var processInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();

            //set the privileges
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller.DisplayName = Globals.Config.Name;
            serviceInstaller.Description = "";
            if (Globals.Config.Description != null) serviceInstaller.Description += Globals.Config.Description;
            if (Globals.Config.Documentation != null) serviceInstaller.Description += "\n" + Globals.Config.Documentation;
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            //must be the same as what was set in Program's constructor
            serviceInstaller.ServiceName = Globals.ServiceName;

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }

        public override void Install(IDictionary stateSaver)
        {
            // inject commandline arguments
            if (Globals.ServiceArguments.Length > 0)
            {
                var path = new StringBuilder(Context.Parameters["assemblypath"]);
                if (path[0] != '"')
                {
                    path.Insert(0, '"');
                    path.Append('"');
                }
                path.Append(" " + Globals.ServiceArguments);
                Context.Parameters["assemblypath"] = path.ToString();
            }
            base.Install(stateSaver);
        }
    }
}