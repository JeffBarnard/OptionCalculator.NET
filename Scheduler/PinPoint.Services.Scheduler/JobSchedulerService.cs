using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Collections.Specialized;
using System.IO;
using OptionCalculator.Services.EventScheduler;
using PinPoint.Unity.Interfaces;
using PinPoint.EnterpriseLibrary.Extensions.Logging;
using PinPoint.EnterpriseLibrary.Extensions.ExceptionHandling;

namespace OptionCalculator.Services.JobScheduler
{
    public partial class JobSchedulerService : PinPoint.Services.Common.ServiceBase, IScheduler
    {
        public ILogInfo LogInfo {get; private set;}           
        public IExceptionPolicy ExceptionPolicy {get; private set;}

        public JobSchedulerService()
        {
            InitializeComponent();
            LogInfo = new LogInfo();
            ExceptionPolicy = new ExceptionPolicy();
        }

        protected override void OnStart(string[] args)
        {
            if (Properties.Settings.Default.ForceDebugger)
            {
                Debugger.Launch();
            }
            try
            {
                LogInfo.Log("JobScheduler", TraceEventType.Information, "Service Started at :{0}", System.DateTime.Now);
                LogInfo.Log("JobScheduler", TraceEventType.Information, "Attempting to Load Schedule");

                Scheduler.ServiceContext = this;

                bool rv = Scheduler.Load(Properties.Settings.Default.Schedule, LogInfo, ExceptionPolicy);

                if (!rv)
                {
                    LogInfo.Log("JobScheduler", TraceEventType.Critical, "Service was unable to start, it has been stopped", "");
                    this.Stop();
                }
            }
            catch (Exception ex)
            {
                bool throwEx = ExceptionPolicy.HandleException(ex, ExceptionPolicyNames.Default);
                if (throwEx)
                {
                    throw;
                }
            }
        }

        protected override void OnStop()
        {
            LogInfo.Log("JobScheduler",TraceEventType.Information, "Attempting to stop service at :{0}", System.DateTime.Now);
            Scheduler.Stop(Properties.Settings.Default.TimeToWaitForShutdown);
        }

        #region IScheduler Members

        public void Remove()
        {
          
        }

        public void SignalFailure(TimeSpan waitToScheduleAgain)
        {
           
        }

        public void DelayNext(TimeSpan waitToScheduleAgain)
        {
           
        }

        #endregion

    }
}
