//  KMPSearchStrategy.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Andrea Paatz <andrea@icsharpcode.net>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
/*
using System;

namespace MonoDevelop.Ide.Gui.Search
{
	/// <summary>
	/// Implements the Knuth, Morris, Pratt searching algorithm.
	/// </summary>
	internal class KMPSearchStrategy : ISearchStrategy
	{
		string searchPattern;
		int[]  overlap;
		
		public void CompilePattern(SearchOptions options)
		{
			if (searchPattern != (options.IgnoreCase ? options.SearchPattern.ToUpper() : options.SearchPattern)) {
				searchPattern = options.IgnoreCase ? options.SearchPattern.ToUpper() : options.SearchPattern;
				overlap = new int[searchPattern.Length + 1];
				Preprocessing();
			}
		}
		
		void Preprocessing()
		{
			overlap[0] = -1;
			for (int i = 0, j = -1; i < searchPattern.Length;) {
				while (j >= 0 && searchPattern[i] != searchPattern[j]) {
					j = overlap[j];
				}
				++i;
				++j;
				overlap[i] = j;
			}
		}
		
		int InternalFindNext(ITextIterator textIterator, SearchOptions options)
		{
			int j = 0;
			if (!textIterator.MoveAhead(1)) {
				return -1;
			}
			while (true) { // until pattern found or Iterator finished
				while (j >= 0 && searchPattern[j] != (options.IgnoreCase ? Char.ToUpper(textIterator.GetCharRelative(j)) : textIterator.GetCharRelative(j))) {
					if (!textIterator.MoveAhead(j - overlap[j])) {
						return -1;
					}
					j = overlap[j];
				}
				if (++j >= searchPattern.Length) {
					if ((!options.SearchWholeWordOnly || SearchReplaceUtilities.IsWholeWordAt(textIterator, searchPattern.Length))) {
						return textIterator.Position;
					}
					if (!textIterator.MoveAhead(j - overlap[j])) {
						return -1;
					}
					j = overlap[j];
				}
			}			
		}
		
		public SearchResult FindNext(ITextIterator textIterator, SearchOptions options, bool reverseSearch)
		{
			if (reverseSearch)
				throw new NotSupportedException ();
				
			int pos = textIterator.Position;
			
			int offset = InternalFindNext(textIterator, options);
			if (offset == -1) return null;
			
			if (textIterator.GetCharRelative (searchPattern.Length) == char.MinValue) {
				if (pos != offset)
					return FindNext(textIterator, options, false);
				else
					return null;
			}
			
			return new SearchResult (textIterator, searchPattern.Length);
		}
		
		public bool SupportsReverseSearch (ITextIterator textIterator, SearchOptions options)
		{
			return false;
		}
	}
}
*/