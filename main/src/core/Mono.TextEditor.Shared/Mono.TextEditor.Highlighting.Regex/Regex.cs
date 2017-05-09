
using System;

namespace Mono.TextEditor.Highlighting
{
	class Regex
	{
		public string Pattern {
			get;
			private set;
		}
		string[] patterns;
		
		Regex ()
		{
		}
		
		public Regex (string pattern)
		{
			this.Pattern = pattern;
			this.patterns = pattern.Split ('|');
		}
		
		public Regex Clone ()
		{
			var newRegex = new Regex ();
			newRegex.Pattern = Pattern;
			newRegex.patterns = patterns;
			return newRegex;
		}
		
		public RegexMatch TryMatch (string doc, int offset)
		{
			foreach (string pattern in patterns) {
				int curOffset = offset;
				int length = 0;
				for (int i = 0; i < pattern.Length; i++) {
					if ((pattern[i] == '\u00AE' || pattern[i] == '‹')  && i + 1 < pattern.Length) {
						i++;
						if (curOffset >= doc.Length) {
							break;
						}
						if (pattern[i] == doc [curOffset]) {
							return RegexMatch.NoMatch;
						}
					} else {
						if (curOffset >= doc.Length || doc [curOffset] != pattern [i])
							return RegexMatch.NoMatch;
						length++;
					}
					curOffset++;
				}
				return new RegexMatch (length);
			}
			return RegexMatch.NoMatch;
		}
	}
}
