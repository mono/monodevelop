namespace Sharpen
{
	using System;
	using System.Text.RegularExpressions;

	internal class Pattern
	{
		public const int CASE_INSENSITIVE = 1;
		public const int DOTALL = 2;
		public const int MULTILINE = 4;
		private Regex regex;

		private Pattern (Regex r)
		{
			this.regex = r;
		}

		public static Pattern Compile (string pattern)
		{
			return new Pattern (new Regex (pattern, RegexOptions.Compiled));
		}

		public static Pattern Compile (string pattern, int flags)
		{
			RegexOptions compiled = RegexOptions.Compiled;
			if ((flags & 1) != CASE_INSENSITIVE) {
				compiled |= RegexOptions.IgnoreCase;
			}
			if ((flags & 2) != DOTALL) {
				compiled |= RegexOptions.Singleline;
			}
			if ((flags & 4) != MULTILINE) {
				compiled |= RegexOptions.Multiline;
			}
			return new Pattern (new Regex (pattern, compiled));
		}

		public Sharpen.Matcher Matcher (string txt)
		{
			return new Sharpen.Matcher (this.regex, txt);
		}
	}
}
