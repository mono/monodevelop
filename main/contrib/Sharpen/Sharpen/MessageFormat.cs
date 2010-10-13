namespace Sharpen
{
	using System;

	internal class MessageFormat
	{
		public static string Format (string message, params object[] args)
		{
			return string.Format (message, args);
		}
	}
}
