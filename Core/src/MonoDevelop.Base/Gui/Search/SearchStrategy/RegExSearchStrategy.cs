// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Text.RegularExpressions;

namespace MonoDevelop.Gui.Search
{
	internal class RegExSearchStrategy : ISearchStrategy
	{
		Regex regex = null;
		
		public void CompilePattern(SearchOptions options)
		{
			RegexOptions regexOptions = RegexOptions.Compiled;
			if (options.IgnoreCase) {
				regexOptions |= RegexOptions.IgnoreCase;
			}
			regex = new Regex(options.SearchPattern, regexOptions);
		}
		
		public ISearchResult FindNext(ITextIterator textIterator, SearchOptions options, bool reverseSearch)
		{
			if (reverseSearch)
				throw new NotSupportedException ();
				
			if (!textIterator.MoveAhead(1)) return null;
			if (regex == null) return null;

			int pos = textIterator.Position;
			string document = textIterator.ReadToEnd ();
			textIterator.Position = pos;
			
			Match m = regex.Match (document, 0);
			if (m == null || m.Index <= 0 || m.Length <= 0) {
				return null;
			} else {
				if (textIterator.MoveAhead (m.Index)) {
					return new DefaultSearchResult (textIterator, m.Length);
				} else {
					return null;
				}
			}
		}
		
		public bool SupportsReverseSearch (ITextIterator textIterator, SearchOptions options)
		{
			return false;
		}
	}
}
