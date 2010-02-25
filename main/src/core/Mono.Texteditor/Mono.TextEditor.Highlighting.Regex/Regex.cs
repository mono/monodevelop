
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
		
		public Regex (string pattern)
		{
			this.Pattern = pattern;
			this.patterns = pattern.Split ('|');
		}
		
		public RegexMatch TryMatch (Document doc, int offset)
		{
			foreach (string pattern in patterns) {
				int curOffset = offset;
				bool match = true;
				for (int i = 0; i < pattern.Length; i++) {
					if (curOffset >= doc.Length) {
						match = false;
						break;
					}
					
					if (doc.GetCharAt (curOffset) != pattern[i]) {
						match = false;
						break;
					}
					curOffset++;
					
				}
				if (match) 
					return new RegexMatch (pattern.Length);
			}
			return RegexMatch.NoMatch;
		}
	}
}
