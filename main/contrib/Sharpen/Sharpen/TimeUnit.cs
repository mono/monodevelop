namespace Sharpen
{
	using System;

	public enum TimeUnit : long
	{
		MILLISECONDS = 1,
		SECONDS = 1000
	}

	internal static class TimeUnitExtensions
	{
		public static long Convert (this TimeUnit thisUnit, long duration, TimeUnit targetUnit)
		{
			return ((duration * (long)targetUnit) / (long)thisUnit);
		}
	}
}
