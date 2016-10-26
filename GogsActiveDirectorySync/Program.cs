using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace GogsActiveDirectorySync
{
    class Program
    {
        static int Main(string[] args)
        {
            var exitCode = HostFactory.Run(configurator =>
            {
                configurator.Service<GroupSyncService>(service =>
                {
                    service.ConstructUsing(s => new GroupSyncService());
                    service.WhenStarted(start => start.Start());
                    service.WhenStopped(stop => stop.Stop());
                });
                configurator.SetDisplayName("Gogs Active Directory Sync");
                configurator.EnableServiceRecovery(action => action.RestartService(delayInMinutes: 60));
                configurator.UseNLog();
            });
            return (int)exitCode;
        }
    }
}
