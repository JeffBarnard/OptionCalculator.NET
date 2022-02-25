using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Data.SqlClient;
using OptionCalculator.Services.Provider;
using PinPoint.Unity.Interfaces;
using System.Configuration;
using System.Linq;

namespace OptionCalculator
{
    public class YahooService : ProviderBase
    {
        public YahooService(System.Collections.Specialized.NameValueCollection properties, IScheduler scheduleContext)
            : base(properties, scheduleContext)
        {
            
        }

        public override void Execute()
        {
            this.ScheduleContext.LogInfo.Log("JobScheduler", TraceEventType.Information, "YahooService Executed Started. Waiting for 15 seconds.");
                        
            var yql = new YahooQuery();
            yql.Fetch(this.ScheduleContext.LogInfo);

            this.ScheduleContext.LogInfo.Log("JobScheduler", TraceEventType.Information, "YahooService finished.");
        }

        public override void Dispose()
        {
            this.ScheduleContext.LogInfo.Log("JobScheduler", TraceEventType.Information, "ASK Destorying TestProvider: {0}.{1}.{2:x}", this.GetHashCode(), System.Threading.Thread.CurrentThread.Name, System.Threading.Thread.CurrentThread.ManagedThreadId);
        }

        ~YahooService()
        {
            Debug.Print(":::::DONE->" + this.GetHashCode().ToString());
        }
    }
}
