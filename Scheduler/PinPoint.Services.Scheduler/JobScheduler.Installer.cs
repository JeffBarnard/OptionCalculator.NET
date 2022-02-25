using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Collections.Specialized;
using System.Diagnostics;
using PinPoint.Services.Common;
using PinPoint.Common;

namespace OptionCalculator.Services.JobScheduler
{
    [RunInstaller(true)]
    public partial class JobSchedulerInstaller : PinPoint.Services.Common.ServiceBaseInstaller
    {

        public JobSchedulerInstaller()
        {
            InitializeComponent();
            this.SetInstallParameters += new EventHandler<EventArgs<ServiceInstallerConfig>>(JobSchedulerInstaller_SetInstallParameters);
            
        }

        void JobSchedulerInstaller_SetInstallParameters(object sender, EventArgs<ServiceInstallerConfig> e)
        { 
            //We call this method, to set defaults in case the installutil was executed 
            //without any parameters.
            e.Data.SetDefaults("OptionCalculator.Services.JobScheduler", "PINpoint Job Scheduler Service", "PINpoint Job Scheduler Service");
        }   
    }
}