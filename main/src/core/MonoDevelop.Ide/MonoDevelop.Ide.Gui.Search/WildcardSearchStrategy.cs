//  WildcardSearchStrategy.cs
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
using System.Collections;

using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Search
{
	/// <summary>
	/// Implements a wildcard search strategy.
	/// 
	/// Wildcard search has following pattern code :
	///      * = Zero or more of any character
	///      ? = Any single character
	///      # = Any single digit
	///  [...] = Any one character in the set
	/// [!...] = Any one character not in the set
	/// </summary>
	internal class WildcardSearchStrategy : ISearchStrategy
	{
		enum CommandType {
			Match,
			AnyZeroOrMore,
			AnySingle,
			AnyDigit,
			AnyInList,
			NoneInList
		}
		
		class Command {
			public CommandType CommandType = CommandType.Match;
			public char        SingleChar  = '\0';
			public string      CharList    = String.Empty;
		}
		
		ArrayList patternProgram = null;
		
		void CompilePattern(string pattern, bool ignoreCase)
		{
			patternProgram = new ArrayList();
			for (int i = 0; i < pattern.Length; ++i) {
				Command newCommand = new Command();
				switch (pattern[i]) {
					case '#':
						newCommand.CommandType = CommandType.AnyDigit;
						break;
					case '*':
						newCommand.CommandType = CommandType.AnyZeroOrMore;
						break;
					case '?':
						newCommand.CommandType = CommandType.AnySingle;
						break;
					case '[':
						int index = pattern.IndexOf(']', i);
						if (index > 0) {
							newCommand.CommandType = CommandType.AnyInList;
							string list = pattern.Substring(i + 1, index - i - 1);
							if (list[0] == '!') {
								newCommand.CommandType = CommandType.NoneInList;
								list = list.Substring(1);
							}
							newCommand.CharList = ignoreCase ? list.ToUpper() : list;
							i = index;
						} else {
							goto default;
						}
						break;
					default:
						newCommand.CommandType = CommandType.Match;
						newCommand.SingleChar  = ignoreCase ? Char.ToUpper(pattern[i]) : pattern[i];
						break;
				}
				patternProgram.Add(newCommand);
			}
		}
		
		int Match (ITextIterator textIterator, bool ignoreCase, int  programStart)
		{
			int matchCharCount = 0;
			bool moreChars = true;
			for (int pc = programStart; pc < patternProgram.Count; ++pc) 
			{
				if (!moreChars) return -1;
				
				char    ch  = ignoreCase ? Char.ToUpper(textIterator.Current) : textIterator.Current;
				Command cmd = (Command)patternProgram[pc];
				
				switch (cmd.CommandType) {
					case CommandType.Match:
						if (ch != cmd.SingleChar) {
							return -1;
						}
						break;
					case CommandType.AnyZeroOrMore:
						int p = textIterator.Position;
						int subMatch = Match (textIterator, ignoreCase, pc + 1);
						if (subMatch != -1) return matchCharCount + subMatch;
						textIterator.Position = p;
						if (!textIterator.MoveAhead (1)) return -1;
						subMatch = Match (textIterator, ignoreCase, pc);
						if (subMatch != -1) return matchCharCount + subMatch;
						else return -1;
					case CommandType.AnySingle:
						break;
					case CommandType.AnyDigit:
						if (!Char.IsDigit(ch)) {
							return -1;
						}
						break;
					case CommandType.AnyInList:
						if (cmd.CharList.IndexOf(ch) < 0) {
							return -1;
						}
						break;
					case CommandType.NoneInList:
						if (cmd.CharList.IndexOf(ch) >= 0) {
							return -1;
						}
						break;
				}
				matchCharCount++;
				moreChars = textIterator.MoveAhead (1);
			}
			return matchCharCount;
		}
		
		int InternalFindNext(ITextIterator textIterator, SearchOptions options)
		{
			while (textIterator.MoveAhead(1)) 
			{
				int pos = textIterator.Position;
				int charCount = Match (textIterator, options.IgnoreCase, 0);
				textIterator.Position = pos;
				if (charCount != -1) {
					if (!options.SearchWholeWordOnly || SearchReplaceUtilities.IsWholeWordAt (textIterator, charCount))
						return charCount;
				}
			}
			return -1;
		}
		
		public void CompilePattern(SearchOptions options)
		{
			CompilePattern(options.SearchPattern, options.IgnoreCase);
		}
		
		public SearchResult FindNext(ITextIterator textIterator, SearchOptions options, bool reverseSearch)
		{
			if (reverseSearch)
				throw new NotSupportedException ();
				
			int charCount = InternalFindNext(textIterator, options);
			return charCount != -1 ? new SearchResult (textIterator, charCount) : null;
		}
		
		public bool SupportsReverseSearch (ITextIterator textIterator, SearchOptions options)
		{
			return false;
		}
	}
}
