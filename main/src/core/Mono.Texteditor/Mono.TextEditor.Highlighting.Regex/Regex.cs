
using System;

namespace Mono.TextEditor.Highlighting
{
	public class Regex
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
				bool match = true;
				int length = 0;
				for (int i = 0; i < pattern.Length; i++) {
					if (pattern[i] == '\u00AE' && i + 1 < pattern.Length) {
						i++;
						if (curOffset >= doc.Length) {
							match = true;
							break;
						}
						if (pattern[i] == doc [curOffset]) {
							match = false;
							break;
						}
					} else {
						if (curOffset >= doc.Length) {
							match = false;
							break;
						}
						if (doc [curOffset] != pattern [i]) {
							match = false;
							break;
						}
						length++;
					}
					curOffset++;
					
				}
				if (match) 
					return new RegexMatch (length);
			}
			return RegexMatch.NoMatch;
		}
	}
}
