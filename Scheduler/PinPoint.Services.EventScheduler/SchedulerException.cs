using System;

namespace OptionCalculator.Services.EventScheduler
{
	/// <summary>
	/// Summary description for SchedulerException.
	/// </summary>
	public class SchedulerException : Exception
	{
		public SchedulerException(string msg) : base(msg)
		{
		}
	}
}
