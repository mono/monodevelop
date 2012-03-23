namespace Sharpen
{
	using System;
	using System.Globalization;

	public class SimpleDateFormat : DateFormat
	{
		string format;
		
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
			SetTimeZone (TimeZoneInfo.Local);
		}

		public override string Format (DateTime date)
		{
			date += GetTimeZone().BaseUtcOffset;
			return date.ToString (format);
		}
		
		public string Format (long date)
		{
			return Extensions.MillisToDateTimeOffset (date, (int)GetTimeZone ().BaseUtcOffset.TotalMinutes).DateTime.ToString (format);
		}
	}
}
