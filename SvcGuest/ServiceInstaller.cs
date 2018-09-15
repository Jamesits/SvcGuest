using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Text;

namespace SvcGuest
{
    [RunInstaller(true)]
    public class GuestServiceInstaller : Installer
    {
        public GuestServiceInstaller()
        {
            var processInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();

            //set the privileges
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller.DisplayName = Globals.DisplayName;
            serviceInstaller.Description = Globals.Description;
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            //must be the same as what was set in Program's constructor
            serviceInstaller.ServiceName = Globals.ServiceName;

            this.Installers.Add(processInstaller);
            this.Installers.Add(serviceInstaller);
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