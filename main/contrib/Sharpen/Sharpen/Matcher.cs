namespace Sharpen
{
	using System;
	using System.Text.RegularExpressions;

	internal class Matcher
	{
		private int current;
		private MatchCollection matches;
		private Regex regex;
		private string str;

		internal Matcher (Regex regex, string str)
		{
			this.regex = regex;
			this.str = str;
		}

		public int End ()
		{
			if ((matches == null) || (current >= matches.Count)) {
				throw new InvalidOperationException ();
			}
			return (matches[current].Index + matches[current].Length);
		}

		public bool Find ()
		{
			if (matches == null) {
				matches = regex.Matches (str);
				current = 0;
			}
			return (current < matches.Count);
		}

		public bool Find (int index)
		{
			matches = regex.Matches (str, index);
			current = 0;
			return (matches.Count > 0);
		}

		public string Group (int n)
		{
			if ((matches == null) || (current >= matches.Count)) {
				throw new InvalidOperationException ();
			}
			Group grp = matches[current].Groups[n];
			return grp.Success ? grp.Value : null;
		}

		public bool Matches ()
		{
			matches = null;
			return Find ();
		}

		public string ReplaceFirst (string txt)
		{
			return regex.Replace (str, txt, 1);
		}

		public Matcher Reset (CharSequence str)
		{
			return Reset (str.ToString ());
		}

		public Matcher Reset (string str)
		{
			matches = null;
			this.str = str;
			return this;
		}

		public int Start ()
		{
			if ((matches == null) || (current >= matches.Count)) {
				throw new InvalidOperationException ();
			}
			return matches[current].Index;
		}
	}
}
