using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Threading;
using PinPoint.Unity.Interfaces;

namespace OptionCalculator.Services.Provider
{
    public abstract class ProviderBase: ISchedulerProvider, IDisposable
    {
        /// <summary>
        /// Entry point for exectuting scheduled job
        /// </summary>
        abstract public void Execute();

        /// <summary>
        /// 
        /// </summary>
        public IScheduler ScheduleContext {get;set;}          
        /// <summary>
        /// 
        /// </summary>
        public NameValueCollection InitializationProperties {get;set;}
       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="scheduleContext"></param>
        public ProviderBase(NameValueCollection properties, IScheduler scheduleContext)
        {
            ScheduleContext = scheduleContext;
            InitializationProperties = properties;
        }

        #region IDisposable Members

        public virtual void Dispose()
        {
            
        }

        #endregion
    }
}
