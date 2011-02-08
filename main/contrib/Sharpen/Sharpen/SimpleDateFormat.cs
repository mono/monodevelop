namespace Sharpen
{
	using System;
	using System.Globalization;

	internal class SimpleDateFormat
	{
		string format;
		TimeZoneInfo timeZone;
		
		public SimpleDateFormat (): this ("g")
		{
		}

		public SimpleDateFormat (string format): this (format, CultureInfo.CurrentCulture)
		{
		}

		public SimpleDateFormat (string format, CultureInfo c)
		{
			this.format = format.Replace ("EEE", "ddd");
			this.format = this.format.Replace ("Z", "zzz");
			this.timeZone = TimeZoneInfo.Local;
		}

		public string Format (DateTime date)
		{
			date += timeZone.BaseUtcOffset;
			return date.ToString (format);
		}

		public string Format (long date)
		{
			return Extensions.MillisToDateTimeOffset (date, (int)timeZone.BaseUtcOffset.TotalMinutes).DateTime.ToString (format);
		}

		public void SetTimeZone (TimeZoneInfo timeZone)
		{
			this.timeZone = timeZone;
		}
	}
}
