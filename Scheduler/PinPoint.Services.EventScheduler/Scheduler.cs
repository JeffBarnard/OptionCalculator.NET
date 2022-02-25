using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Text;
using PinPoint.Unity.Interfaces;

namespace OptionCalculator.Services.EventScheduler
{
    // enumeration of Scheduler events used by the delegate
    public enum SchedulerEventType { CREATED, DELETED, INVOKED };

    // delegate for Scheduler events
    public delegate void SchedulerEventDelegate(SchedulerEventType type, string scheduleName);

    // This is the main class which will maintain the list of Schedules
    // and also manage them, like rescheduling, deleting schedules etc.
    public sealed class Scheduler
    {
        // Event raised when for any event inside the scheduler
        static public event SchedulerEventDelegate OnSchedulerEvent;

        // next event which needs to be kicked off,
        // this is set when a new Schedule is added or after invoking a Schedule
        static Schedule _nextSchedule = null;
        static List<Schedule> _scheduleList = new List<Schedule>();
        //static List<Thread> _threadList = new List<Thread>();

        static private IScheduler _serviceContext;

        public static IScheduler ServiceContext
        {
            get { return _serviceContext; }
            set { _serviceContext = value; }
        }

        public static List<Schedule> ScheduleList
        {
            get { return Scheduler._scheduleList; }
            set
            {
                lock (_scheduleList)
                {
                    Scheduler._scheduleList.Clear();
                    Scheduler._scheduleList.AddRange(value);
                    foreach (Schedule s in _scheduleList)
                    {
                        s.CalculateNextInvokeTime();
                    }

                    Scheduler._scheduleList.Sort();
                }
                SetNextEventTime();
            }
        }
        static Timer _timer = new Timer(new TimerCallback(DispatchEvents), null, Timeout.Infinite, Timeout.Infinite);

        // Get schedule at a particular index in the array list
        public static Schedule GetScheduleAt(int index)
        {

            if (index < 0 || index >= _scheduleList.Count)
                return null;
            return _scheduleList[index];
        }

        // Number of schedules in the list
        public static int Count()
        {
            return _scheduleList.Count;
        }

        // Indexer to access a Schedule object by name
        public static Schedule GetSchedule(string scheduleName)
        {
            for (int index = 0; index < _scheduleList.Count; index++)
            {
                if (_scheduleList[index].Name == scheduleName)
                {
                    return _scheduleList[index];
                }
            }
            return null;
        }

        // call back for the timer function
        static void DispatchEvents(object obj) // obj ignored
        {
            if (_nextSchedule != null)
            {
                _nextSchedule.TriggerEvents(); // make this happen on a thread to let this thread continue
                if (_nextSchedule.Type == ScheduleType.ONETIME)
                {
                    RemoveSchedule(_nextSchedule); // remove the schedule from the list
                }
                else
                {
                    if (OnSchedulerEvent != null)
                    {
                        OnSchedulerEvent(SchedulerEventType.INVOKED, _nextSchedule.Name);
                    }
                    _scheduleList.Sort();
                    SetNextEventTime();
                }
            }

            ServiceContext.LogInfo.Log("JobScheduler", TraceEventType.Verbose, "--------------------------------------");
            IEnumerable<Schedule> items = null;
            lock (ScheduleList)
            {
                items = ScheduleList.ToArray();
            }

            foreach (Schedule s in items)
            {
                ServiceContext.LogInfo.Log("JobScheduler", TraceEventType.Verbose, "Schedule: {0}. Next Run: {1}. Running: {3} Active:{2} ", s.Name, s.NextInvokeTime, s.Active, s.IsRunning);
            }

            ServiceContext.LogInfo.Log("JobScheduler", TraceEventType.Verbose, "--------------------------------------");
        }

        // method to set the time when the timer should wake up to invoke the next schedule
        static void SetNextEventTime()
        {
            if (_scheduleList.Count == 0)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite); // this will put the timer to sleep
                ServiceContext.LogInfo.Log("JobScheduler", TraceEventType.Verbose, "Setting Timer to wait forever.");
            }
            else
            {
                _nextSchedule = (Schedule)_scheduleList[0];
                TimeSpan ts = _nextSchedule.NextInvokeTime.Subtract(DateTime.Now);
                if (ts < TimeSpan.Zero)
                {
                    ts = TimeSpan.Zero; // cannot be negative !
                }
                int tm = int.MaxValue;
                if (ts.TotalMilliseconds < int.MaxValue)
                {
                    tm = (int)ts.TotalMilliseconds;
                }

                _timer.Change(tm, Timeout.Infinite);
                 ServiceContext.LogInfo.Log("JobScheduler", TraceEventType.Verbose,"Setting Timer to wait for: {0} seconds. Next Item up: {1} @ {2:r}", ts.TotalMilliseconds / 1000, _nextSchedule.Name, _nextSchedule.NextInvokeTime);
            }

        }

        // add a new schedule
        public static void AddSchedule(Schedule s)
        {
            if (GetSchedule(s.Name) != null)
                throw new SchedulerException("Schedule with the same name already exists");
            lock (_scheduleList)
            {
                _scheduleList.Add(s);
                _scheduleList.Sort();
            }
            // adjust the next event time if schedule is added at the top of the list
            if (_scheduleList[0] == s)
                SetNextEventTime();
            if (OnSchedulerEvent != null)
                OnSchedulerEvent(SchedulerEventType.CREATED, s.Name);
        }

        // remove a schedule object from the list
        public static void RemoveSchedule(Schedule s)
        {
            lock (_scheduleList)
            {
                _scheduleList.Remove(s);
            }
            SetNextEventTime();
            if (OnSchedulerEvent != null)
                OnSchedulerEvent(SchedulerEventType.DELETED, s.Name);
        }
        public static void RemoveSchedule(string scheduleName)
        {
            RemoveSchedule(GetSchedule(scheduleName));
        }

        public static bool Load(System.Xml.XmlDocument xmlDocument, ILogInfo logInfo, IExceptionPolicy exceptionPolicy)
        {
            bool rv = true;
            try
            {
                XmlSerializer ss = new XmlSerializer(typeof(List<Schedule>));

                using (MemoryStream ms = new MemoryStream())
                {
                    xmlDocument.Save(ms);
                    ms.Position = 0;
                    List<Schedule> dd = ss.Deserialize(ms) as List<Schedule>;

                    foreach (Schedule s in dd)
                    {
                        s.LogInfo = logInfo;
                        s.ExceptionPolicy = exceptionPolicy;
                        _serviceContext.LogInfo.Log("JobScheduler", "Schedule Name:{0} Type: {1:g} Class To Run:{2}", s.Name, s.Type, s.ProviderType);
                    }
                    Scheduler.ScheduleList = dd;
                }
            }
            catch (Exception ex)
            {
                _serviceContext.ExceptionPolicy.HandleException(ex, "Default");
                rv = false;
            }
            return rv;
        }
        public static void Stop()
        {
            Stop(60000);
        }
        public static void Stop(int timeToWaitForShutdown)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            DateTime timeToStop = System.DateTime.Now.AddMilliseconds(timeToWaitForShutdown);
            while (IsEventRunning() && System.DateTime.Now.CompareTo(timeToStop) < 0)
            {
                _serviceContext.LogInfo.Log("JobScheduler", TraceEventType.Information, "Scheduler Shutdown: Schedules still active... sleeping for 5 seconds");
                Thread.Sleep(new TimeSpan(0, 0, 5));
            }
        }


        internal static bool IsEventRunning()
        {
            bool rv = false;
            lock (ScheduleList)
            {
                foreach (Schedule s in ScheduleList)
                {
                    if (s.IsRunning)
                    {
                        rv = true;
                        break;
                    }
                }
            }
            return rv;
        }
    }
}
