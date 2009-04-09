
using System;

namespace Mono.TextEditor.Highlighting
{
	public class Regex
	{
		string[] patterns;
		
		public Regex (string pattern)
		{
			this.patterns = pattern.Split ('|');
		}
		
		public RegexMatch TryMatch (Document doc, int offset)
		{
			foreach (string pattern in patterns) {
				int curOffset = offset;
				bool match = true;
				for (int i = 0; i < pattern.Length; i++) {
					if (doc.GetCharAt (curOffset) != pattern[i]) {
						match = false;
						break;
					}
					curOffset++;
					if (curOffset >= doc.Length) {
						match = i + 1 == pattern.Length;
						break;
					}
				}
				if (match)
					return new RegexMatch (pattern.Length);
			}
			return RegexMatch.NoMatch;
		}
	}
}
