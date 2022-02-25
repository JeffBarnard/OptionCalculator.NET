using System;
using System.Xml.Serialization;
using PinPoint.Common;

namespace OptionCalculator.Services.EventScheduler
{
	// OneTimeSchedule is used to schedule an event to run only once
	// Used by specific tasks to check self status
    [Serializable]
	public class OneTimeSchedule : Schedule
	{
        public OneTimeSchedule()
        {
        }
		public OneTimeSchedule(string name, DateTime startTime) 
			: base(name, startTime, ScheduleType.ONETIME)
		{
		}
		internal override void CalculateNextInvokeTime()
		{
			// it does not matter, since this is a one time schedule
            _nextTime = this.StartTime;
            if (_nextTime < DateTime.Now)
            {
                _nextTime = DateTime.MaxValue; 
            }
		}
	}

	// IntervalSchedule is used to schedule an event to be invoked at regular intervals
	// the interval is specified in seconds. Useful mainly in checking status of threads
	// and connections. Use an interval of 60 hours for an hourly schedule
    [Serializable]
	public class IntervalSchedule : Schedule
	{
        public IntervalSchedule()
        { }
		public IntervalSchedule(string name, DateTime startTime, int secs,
					TimeSpan fromTime, TimeSpan toTime) // time range for the day
			: base(name, startTime, ScheduleType.INTERVAL)
		{
			_fromTime = fromTime;
			_toTime = toTime;
			Interval = secs;
		}
		internal override void CalculateNextInvokeTime()
		{
			// add the interval of _seconds
			_nextTime = _nextTime.AddSeconds(Interval);

			// if next invoke time is not within the time range, then set it to next start time
            if (!_nextTime.TimeOfDay.Between(_fromTime, _toTime)) 
            {
                //can we do it today?
                if (_nextTime.TimeOfDay > _fromTime)
                {
                    _nextTime = _nextTime.AddDays(1).Subtract(_nextTime.TimeOfDay - _fromTime);
                }
                else
                {
                    _nextTime = _nextTime.Add(_fromTime - _nextTime.TimeOfDay);
                }
            }

			// check to see if the next invoke time is on a working day
            while (!CanInvokeOnNextWeekDay())
            {
                _nextTime = _nextTime.AddDays(1); // start checking on the next day
            }
		}
	}

	// Daily schedule is used set off to the event every day
	// Mainly useful in maintanance, recovery, logging and report generation
	// Restictions can be imposed on the week days on which to run the schedule
    [Serializable]
	public class DailySchedule : Schedule
	{
        public DailySchedule()
        {
        }
		public DailySchedule(string name, DateTime startTime)
			: base(name, startTime, ScheduleType.DAILY)
		{
		}

		internal override void CalculateNextInvokeTime()
		{
			// add a day, and check for any weekday restrictions and keep adding a day
			_nextTime = _nextTime.AddDays(1);
			while (! CanInvokeOnNextWeekDay())
				_nextTime = _nextTime.AddDays(1);
		}
         // Accessor for _startTime
        [XmlAttribute]
        public override DateTime StartTime
        {
            get { return base.StartTime; }
            set
            {
                //Grab the time portion of the start time
                DateTime newtime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, value.Hour, value.Minute, value.Second);

                //if this is in the past, add one day to it.
                if (newtime.CompareTo(DateTime.Now) <= 0)
                {
                    newtime = newtime.AddDays(1);
                }
                base._startTime = newtime;
                _nextTime = _startTime.AddDays(-1);
            }
        }
        
	}

	// Weekly schedules, useful generally in lazy maintanance jobs and
	// restarting services and others major jobs
    [Serializable]
	public class WeeklySchedule : Schedule
	{
        public WeeklySchedule()
        { }
		public WeeklySchedule(string name, DateTime startTime)
			: base(name, startTime, ScheduleType.WEEKLY)
		{
		}        
       
		// add a week (or 7 days) to the date
		internal override void CalculateNextInvokeTime()
		{
			_nextTime = _nextTime.AddDays(7);
		}
	}

	// Monthly schedule - used to kick off an event every month on the same day as scheduled
	// and also at the same hour and minute as given in start time
    [Serializable]
	public class MonthlySchedule : Schedule
	{
        public MonthlySchedule()
        { }
		public MonthlySchedule(string name, DateTime startTime)
			: base(name, startTime, ScheduleType.MONTHLY)
		{
		}
		// add a month to the present time
		internal override void CalculateNextInvokeTime()
		{
			_nextTime = _nextTime.AddMonths(1);
		}
	}
}
