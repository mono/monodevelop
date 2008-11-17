//  RegExSearchStrategy.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
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

using System;
using System.Text.RegularExpressions;

namespace MonoDevelop.Ide.Gui.Search
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
		
		public SearchResult FindNext(ITextIterator textIterator, SearchOptions options, bool reverseSearch)
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
					return new SearchResult (textIterator, m.Length);
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
