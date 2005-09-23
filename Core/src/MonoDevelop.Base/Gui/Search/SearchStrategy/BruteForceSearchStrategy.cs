// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Globalization;
using MonoDevelop.Core.Properties;
using MonoDevelop.Internal.Undo;

namespace MonoDevelop.Gui.Search
{
	/// <summary>
	///  Only for fallback purposes.
	/// </summary>
	internal class BruteForceSearchStrategy : ISearchStrategy
	{
		string searchPattern;
		
		int InternalFindNext(ITextIterator textIterator, SearchOptions options)
		{
			int[] compareIndex = new int [searchPattern.Length];
			int[] startPositions = new int [searchPattern.Length];
			int maxPoss = 0;
			bool ignoreCase = options.IgnoreCase;
			bool searchWord = options.SearchWholeWordOnly;
			CultureInfo cinfo = CultureInfo.InvariantCulture;
			int patternLength = searchPattern.Length;
			bool wasWordStart = true;
			
			char first = searchPattern[0];

			while (textIterator.MoveAhead(1))
			{
				char c = textIterator.Current;
				if (ignoreCase) c = Char.ToUpper (c, cinfo);
				
				int freePos = -1;
				for (int n=0; n<maxPoss; n++) 
				{
					int pos = compareIndex[n];
					if (pos != 0) {
						if (searchPattern[pos] == c) {
							pos++;
							if (pos == patternLength) {
								if (searchWord) {
									int curp = textIterator.Position;
									bool endw = !textIterator.MoveAhead (1);
									endw = endw || SearchReplaceUtilities.IsWordSeparator (textIterator.Current);
									textIterator.Position = curp;
									if (endw) return startPositions[n];
								}
								else
									return startPositions[n];
							}
							else {
								compareIndex[n] = pos;
								continue;
							}
						}
						compareIndex[n] = 0;
						if (n == maxPoss-1)
							maxPoss = n;
					}
					
					if (freePos == -1)
						freePos = pos;
				}
				
				if (c == first && (!searchWord || wasWordStart)) {
					if (patternLength == 1)
						return textIterator.Position;
						
					if (freePos == -1) {			
						freePos = maxPoss;
						maxPoss++;
					}

					compareIndex [freePos] = 1;
					startPositions [freePos] = textIterator.Position;
				}
				wasWordStart = SearchReplaceUtilities.IsWordSeparator (c);
			}
			
			return -1;
		}
		
		public void CompilePattern(SearchOptions options)
		{
			searchPattern = options.IgnoreCase ? options.SearchPattern.ToUpper() : options.SearchPattern;
		}
		
		public ISearchResult FindNext(ITextIterator textIterator, SearchOptions options, bool reverseSearch)
		{
			if (textIterator.SupportsSearch (options, reverseSearch)) {
				if (textIterator.SearchNext (searchPattern, options, reverseSearch)) {
					DefaultSearchResult sr = new DefaultSearchResult (textIterator, searchPattern.Length);
					if (!reverseSearch)
						textIterator.MoveAhead (searchPattern.Length);
					return sr;
				} else
					return null;
			}
			
			if (reverseSearch)
				throw new NotSupportedException ();
				
			int offset = InternalFindNext(textIterator, options);
			if (offset >= 0) {
				int pos = textIterator.Position;
				textIterator.Position = offset;
				DefaultSearchResult sr = new DefaultSearchResult (textIterator, searchPattern.Length);
				textIterator.Position = pos;
				return sr;
			} else
				return null;
		}
		
		public bool SupportsReverseSearch (ITextIterator textIterator, SearchOptions options)
		{
			return textIterator.SupportsSearch (options, true);
		}
	}
}
;
