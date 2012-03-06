using System;
using System.Globalization;

namespace Sharpen
{
	public abstract class DateFormat
	{
		public const int DEFAULT = 2;
		
		public static DateFormat GetDateTimeInstance (int dateStyle, int timeStyle)
		{
			return GetDateTimeInstance (dateStyle, timeStyle, CultureInfo.CurrentCulture);
		}
		
		public static DateFormat GetDateTimeInstance (int dateStyle, int timeStyle, CultureInfo aLocale)
		{
			return new SimpleDateFormat (aLocale.DateTimeFormat.FullDateTimePattern, aLocale);
		}
		
		TimeZoneInfo timeZone;
		
		public DateFormat ()
		{
		}
		
		public TimeZoneInfo GetTimeZone ()
		{
			return timeZone;
		}
		
		public void SetTimeZone (TimeZoneInfo timeZone)
		{
			this.timeZone = timeZone;
		}
	
		public abstract string Format (DateTime time);
	}
}

