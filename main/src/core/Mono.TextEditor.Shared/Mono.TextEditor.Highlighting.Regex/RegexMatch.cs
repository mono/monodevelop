
using System;

namespace Mono.TextEditor
{
	class RegexMatch
	{
		public static RegexMatch NoMatch = new RegexMatch (-1);
		
		public bool Success {
			get {
				return Length >= 0;
			}
		}
		
		public int Length {
			get;
			private set;
		}
		
		public RegexMatch (int length)
		{
			this.Length = length;
		}
		
	}
}
