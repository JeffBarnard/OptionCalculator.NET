using System;
using System.Threading;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Reflection;
using System.Collections.Specialized;
using System.Runtime.Remoting.Messaging;
using PinPoint.Unity.Interfaces;
using PinPoint.Common;
using System.IO;
using System.Xml;
using System.Configuration;

namespace OptionCalculator.Services.EventScheduler
{
    // delegate for OnTrigger() event
    public delegate void Invoke(string scheduleName);

    // enumeration for schedule types
    public enum ScheduleType { ONETIME, INTERVAL, DAILY, WEEKLY, MONTHLY };

    // base class for all schedules
    [Serializable]
    [XmlInclude(typeof(DailySchedule)), XmlInclude(typeof(WeeklySchedule)), XmlInclude(typeof(MonthlySchedule)), XmlInclude(typeof(IntervalSchedule)), XmlInclude(typeof(OneTimeSchedule))]
    abstract public class Schedule : IComparable, IScheduler
    {
        [XmlIgnore]
        public ILogInfo LogInfo { get; set; }
        [XmlIgnore]
        public IExceptionPolicy ExceptionPolicy { get; set; }
        public event Invoke OnTrigger;
        protected string _name;		// name of the schedule
        protected ScheduleType _type;	// type of schedule
        protected bool _active;		// is schedule active ?
        private bool _isRunning = false;

        public bool IsRunning
        {
            get { return _isRunning; }
        }
        protected DateTime _startTime;	// time the schedule starts
        protected DateTime _stopTime;	// ending time for the schedule
        protected DateTime _nextTime = System.DateTime.Now;	// time when this schedule is invoked next, used by scheduler
        protected List<int> _daysToRun = new List<int>();
        // _fromTime and _toTime are used to defined a time range during the day
        // between which the schedule can run.
        // This is useful to define a range of working hours during which a schedule can run
        protected TimeSpan _fromTime = new TimeSpan(0, 0, 0);
        private string _fromTimeString;

        public Schedule()
        {
            DaysToRun = new int[7] { 1, 2, 3, 4, 5, 6, 7 };
        }

        // Constructor
        public Schedule(string name, DateTime startTime, ScheduleType type)
            : this()
        {
            StartTime = startTime;
            _nextTime = startTime;
            _type = type;
            _name = name;
        }

        /// <summary>
        /// Should be of format hh:mm in 24 hour time
        /// </summary>
        [XmlAttribute]
        public string FromTime
        {
            get { return string.Format("{0:d2}:{1:d2}", _fromTime.Hours, _fromTime.Minutes); }
            set
            {
                _fromTimeString = value;
                string[] _timeparts = _fromTimeString.Split(new char[] { ':' });
                if (_timeparts.Length != 2)
                {
                    throw new Exception("The time must be in 24 hour format hh:mm");
                }
                _fromTime = new TimeSpan(int.Parse(_timeparts[0]), int.Parse(_timeparts[1]), 0);
            }
        }
        protected TimeSpan _toTime = new TimeSpan(23, 58, 59);
        private string _toTimeString;

        /// <summary>
        /// Should be of format hh:mm in 24 hour time
        /// </summary>
        [XmlAttribute]
        public string ToTime
        {
            get { return string.Format("{0:d2}:{1:d2}", _toTime.Hours, _toTime.Minutes); }
            set
            {
                _toTimeString = value;
                string[] _timeparts = _toTimeString.Split(new char[] { ':' });
                if (_timeparts.Length != 2)
                {
                    throw new Exception("The time must be in 24 hour format hh:mm");
                }
                _toTime = new TimeSpan(int.Parse(_timeparts[0]), int.Parse(_timeparts[1]), 0);
            }
        }

        // Array containing the 7 weekdays and their status
        // Using DayOfWeek enumeration for index of this array
        // By default Sunday and Saturday are non-working days


        // time interval in seconds used by schedules like IntervalSchedule
        long _interval = 0;

        string _providerType = "";

        [XmlAttribute]
        public string ProviderType
        {
            get { return _providerType; }
            set
            {
                _providerType = value;
                string[] type = _providerType.Split(new char[] { ',' });
                if (type.Length != 2)
                {
                    throw new SchedulerException("The ProviderType should be:  Assembly,Type");
                }
                else
                {
                    _assembly = type[0].ToLower();
                    if (!_assembly.EndsWith(".dll"))
                    {
                        _assembly = _assembly + ".dll";
                    }
                    if (_assembly.IndexOf("\\") == -1)
                    {
                        string loc = Assembly.GetExecutingAssembly().Location;

                        _assembly = loc.Substring(0, loc.LastIndexOf("\\")) + "\\" + _assembly;

                    }
                    _class = type[1];

                }
            }
        }

        private string _settingsPath = "";
        [XmlAttribute]
        public string SettingsPath
        {
            get { return _settingsPath; }
            set { _settingsPath = value; }
        }

        private string _assembly = "";
        private string _class = "";

        //[Conditional("DEBUG")]
        //public void foo()
        //{
        //    this.InitializationData.Properties.Add("value1", "this is the config value1");
        //    this.InitializationData.Properties.Add("value2", "this is the config value2");
        //}

        SerializableNameValueCollection _initializationData;
        public SerializableNameValueCollection InitializationData
        {
            get
            {
                if (_initializationData != null)
                    return _initializationData;

                _initializationData = new SerializableNameValueCollection(new NameValueCollection());

                if (!string.IsNullOrWhiteSpace(_settingsPath))
                {
                    if (!File.Exists(_settingsPath))
                    {
                        throw new FileNotFoundException("Could not find or open config file", _settingsPath);
                    }

                    XmlTextReader reader = null;
                    try
                    {
                        reader = new XmlTextReader(_settingsPath);
                        reader.WhitespaceHandling = WhitespaceHandling.None;

                        // moves the reader to the root element.
                        reader.MoveToContent();
                        // first element should be configuration
                        reader.ReadStartElement("configuration");
                        // TODO: add ability to have other sections other than appSettings
                        reader.ReadStartElement("appSettings");

                        while (reader.LocalName == "add")
                        {
                            // must have key to avoid situation where nulls create an ambiguous situation
                            string key = reader.GetAttribute("key");
                            if (key == null)
                                throw new ConfigurationErrorsException("'key' attribute value must be present");
                            string val = reader.GetAttribute("value");
                            if (val == null)
                                throw new ConfigurationErrorsException("'value' attribute value must be present");
                            _initializationData.Properties.Add(key, val);
                            reader.Read();
                        }
                    }
                    // get this if bad xml in config...throw what ConfigurationSetting would so we are consistent
                    catch (XmlException xmlEx)
                    {
                        throw new ConfigurationErrorsException(string.Concat("Error parsing file: ", _settingsPath, " on line #", reader.LineNumber, "."), xmlEx, _settingsPath, reader.LineNumber);
                    }
                    finally
                    {
                        // close the reader
                        if (reader != null)
                            reader.Close();
                    }
                }
                return _initializationData;
            }
        }

        // Accessor for type of schedule
        [XmlAttribute]
        public ScheduleType Type { get; set; }

        [XmlAttribute]
        public int[] DaysToRun
        {
            get
            {
                int[] ints = new int[_daysToRun.Count];
                _daysToRun.CopyTo(ints);
                return ints;
            }
            set
            {
                _daysToRun.Clear();
                _daysToRun.AddRange(value);
            }
        }


        // Accessor for name of schedule
        // Name is set in constructor only and cannot be changed
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public bool Active { get; set; }

        // check if no week days are active
        protected bool NoFreeWeekDay()
        {
            //bool check = false;
            //for (int index=0; index<7; check = check|_workingWeekDays[index], index++);   
            return _daysToRun.Count > 0;
        }

        // Setting the status of a week day
        public void SetWeekDay(DayOfWeek day, bool On)
        {
            int dayint = (int)day + 1;
            //_workingWeekDays[(int)day  ] = On;
            int intIndex = _daysToRun.LastIndexOf(dayint);
            if (!On && intIndex > -1)
            {
                _daysToRun.RemoveAt(intIndex);
            }
            else if (On && intIndex == -1)
            {
                _daysToRun.Add(dayint);
            }
            Active = NoFreeWeekDay();

            //// Make schedule inactive if all weekdays are inactive
            //// If a schedule is not using the weekdays the array should not be touched
            //if (NoFreeWeekDay())
            //    Active = false;
        }

        // Return if the week day is set active
        public bool WeekDayActive(DayOfWeek day)
        {
            //return _workingWeekDays[(int)day ];
            return _daysToRun.Contains((int)day + 1);
        }

        // Method which will return when the Schedule has to be invoked next
        // This method is used by Scheduler for sorting Schedule objects in the list
        [XmlAttribute]
        public DateTime NextInvokeTime
        {
            get { return _nextTime; }
            set
            {
                _nextTime = value;
                if (_nextTime.CompareTo(System.DateTime.Now) < 0)
                {
                    _nextTime = System.DateTime.Now;
                }
            }
        }

        // Accessor for _startTime
        [XmlAttribute]
        public virtual DateTime StartTime
        {
            get { return _startTime; }
            set
            {
                // start time can only be in future
                if (value.CompareTo(DateTime.Now) <= 0)
                {
                    _startTime = System.DateTime.Now; // throw new SchedulerException("Start Time should be in future");
                }
                else
                {
                    _startTime = value;
                }
            }
        }

        // Accessor for _interval in seconds
        // Put a lower limit on the interval to reduce burden on resources
        // I am using a lower limit of 30 seconds
        [XmlAttribute]
        public long Interval
        {
            get { return _interval; }
            set
            {
                // JB2 - Minimum interval changed from 30 to 5 (08/15/2011).
                if (value < 5)
                {
                    _interval = 5; // throw new SchedulerException("Interval cannot be less than 60 seconds");
                    if (LogInfo != null)
                        LogInfo.Log("JobScheduler", TraceEventType.Information, "The Interval for {0} cannot be less than 5 seconds, it has been changed to 5 second.", this.Name);
                }
                else
                {
                    _interval = value;
                }
            }
        }

        // Sets the next time this Schedule is kicked off and kicks off events on
        // a seperate thread, freeing the Scheduler to continue
        public void TriggerEvents()
        {
            if (LogInfo != null)
            {
                LogInfo.Log("JobScheduler", TraceEventType.Verbose, "Executing Schedule : {0} @ {1:r}. Type:{2}", this.Name, System.DateTime.Now, this.ProviderType);
            }

            CalculateNextInvokeTime(); // first set next invoke time to continue with rescheduling

            if (_isRunning)
            {
                if (LogInfo != null)
                {
                    LogInfo.Log("JobScheduler", TraceEventType.Information, "Schedule: {0}, is currently running.  This schedule will be skipped at run again at: {1}", this.Name, this.NextInvokeTime);
                }
            }
            else
            {
                if (this.ProviderType.Length > 0)
                {
                    try
                    {
                        if (!File.Exists(this._assembly))
                            throw new FileNotFoundException(String.Format("Could not find the specified assembly for the Provider: {0}", this._assembly));
                        ObjectHandle objectHandle = System.Activator.CreateInstanceFrom(this._assembly, this._class, true, BindingFlags.CreateInstance, null, new object[] { this.InitializationData.Properties, (IScheduler)this }, Thread.CurrentThread.CurrentCulture, null); //, null);

                        if (objectHandle != null)
                        {
                            ISchedulerProvider provider = objectHandle.Unwrap() as ISchedulerProvider;
                            ThreadStart ts = new ThreadStart(provider.Execute);
                            if (LogInfo != null)
                            {
                                LogInfo.Log("JobScheduler", TraceEventType.Verbose, "----> Started: " + this.Name);
                            }
                            this._isRunning = true;
                            ts.BeginInvoke(ScheduleExecutedCallback, this);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (LogInfo != null)
                            LogInfo.Log("JobScheduler", TraceEventType.Error, "Exception was unhandled. This may terminate the service: " + ex.Message + "  " + ex.StackTrace);
                        if (ExceptionPolicy != null)
                        {
                            if (ExceptionPolicy.HandleException(ex, "Continue"))
                            {
                                throw;
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
        }

        public static void ScheduleExecutedCallback(IAsyncResult ar)
        {
            bool hasError = false;
            Schedule s = null;
            try
            {
                s = ((Schedule)ar.AsyncState);

                ((ThreadStart)((AsyncResult)ar).AsyncDelegate).EndInvoke(ar);
            }
            catch (Exception ex)
            {
                if (s != null && s.ExceptionPolicy != null)
                {
                    s.ExceptionPolicy.HandleException(ex, "Continue");
                }
                hasError = true;
            }
            finally
            {
                if (s != null)
                {
                    s._isRunning = false;
                    //s.Log("JobScheduler",TraceEventType.Error,"Finished Executing: {0}. {1}",s.Name,hasError?"WITH ERRORS":"");
                    if (hasError)
                    {
                        s.LogInfo.Log("JobScheduler", TraceEventType.Error, "Finished Executing: {0}. {1}", s.Name, "WITH ERRORS");
                    }
                    else
                    {
                        s.LogInfo.Log("JobScheduler", TraceEventType.Information, "Finished Executing: {0}.", s.Name);
                    }

                }
            }

        }


        // Implementation of ThreadStart delegate.
        // Used by Scheduler to kick off events on a seperate thread
        private void KickOffEvents()
        {
            if (OnTrigger != null)
                OnTrigger(Name);
        }

        // To be implemented by specific schedule objects when to invoke the schedule next
        internal abstract void CalculateNextInvokeTime();

        // check to see if the Schedule can be invoked on the week day it is next scheduled 
        protected bool CanInvokeOnNextWeekDay()
        {
            //return _workingWeekDays[(int)_nextTime.DayOfWeek ];
            return _daysToRun.Contains((int)_nextTime.DayOfWeek + 1);

        }

        //// Check to see if the next time calculated is within the time range
        //// given by _fromTime and _toTime
        //// The ranges can be during a day, for eg. 9 AM to 6 PM on same day
        //// or overlapping 2 different days like 10 PM to 5 AM (i.e over the night)
        //protected bool IsInvokeTimeInTimeRange()
        //{
        //    //if (_fromTime < _toTime) // eg. like 9 AM to 6 PM
        //    //    return (_nextTime.TimeOfDay > _fromTime && _nextTime.TimeOfDay < _toTime);
        //    //else // eg. like 10 PM to 5 AM
        //    //    return (_nextTime.TimeOfDay > _toTime && _nextTime.TimeOfDay < _fromTime);
        //    return (_nextTime.TimeOfDay > _fromTime && _nextTime.TimeOfDay < _toTime);
        //}

        // IComparable interface implementation is used to sort the array of Schedules
        // by the Scheduler
        public int CompareTo(object obj)
        {
            if (obj is Schedule)
            {
                return _nextTime.CompareTo(((Schedule)obj)._nextTime);
            }
            throw new Exception("Not a Schedule object");
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
