using System.ServiceProcess;

namespace SvcGuest
{
    public class SupervisorService : ServiceBase
    {
        public SupervisorService()
        {
            this.ServiceName = Globals.ServiceName;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            base.OnStop();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}