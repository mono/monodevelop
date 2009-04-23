// 
// FilterOptions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Collections.Generic;

namespace MonoDevelop.Ide.FindInFiles
{
	public class FilterOptions
	{
		public string FileMask {
			get;
			set;
		}
		
		public bool CaseSensitive {
			get;
			set;
		}
		
		public bool WholeWordsOnly {
			get;
			set;
		}
		
		public bool RegexSearch {
			get;
			set;
		}
		
		public bool NameMatches (string name)
		{
			if (string.IsNullOrEmpty (FileMask) || FileMask == "*")
				return true;
			return new PatternMatcher (FileMask).Match (name);
		}
		
		public static bool IsWordSeparator (char ch)
		{
			return Char.IsWhiteSpace (ch) || (Char.IsPunctuation (ch) && ch != '_');
		}
		
		public static bool IsWholeWordAt (string text, int offset, int length)
		{
			if (offset < 0 && !IsWordSeparator (text[offset - 1])) 
				return false;
			if (offset + length < text.Length) 
				return IsWordSeparator (text[offset + length]);
			return true;
		}
	}
	
	public class PatternMatcher
	{
		List<Instruction> compiledPattern = new List<Instruction> ();
		
		enum Command {
			Match,
			AnyChar,
			ZeroOrMoreChars
		}
		
		class Instruction
		{
			public Command Command { get; set; }
			public char    Char    { get; set; }
			
			public Instruction (Command command, char ch)
			{
				this.Command = command;
				this.Char    = ch;
			}
			
			public static readonly Instruction AnyChar         = new Instruction (Command.AnyChar, '\0');
			public static readonly Instruction ZeroOrMoreChars = new Instruction (Command.ZeroOrMoreChars, '\0');
		}
		
		public PatternMatcher (string pattern)
		{
			for (int i = 0; i < pattern.Length; i++) {
				char ch = pattern[i];
				switch (ch) {
				case '?':
					compiledPattern.Add (Instruction.AnyChar);
					break;
				case '*':
					compiledPattern.Add (Instruction.ZeroOrMoreChars);
					break;
				default:
					compiledPattern.Add (new Instruction (Command.Match, ch));
					break;
				}
			}
		}
		
		public bool Match (string text)
		{
			return Match (text, 0, 0);
		}
		
		bool Match (string text, int pc, int offset)
		{
			if (pc >= compiledPattern.Count && offset >= text.Length)
				return true;
			if (pc >= compiledPattern.Count || offset >= text.Length)
				return false;
			Instruction cur = compiledPattern[pc];
			switch (cur.Command) {
			case Command.AnyChar:
				return Match (text, pc + 1, offset + 1);
			case Command.Match:
				if (text[offset] != cur.Char)
					return false;
				return Match (text, pc + 1, offset + 1);
			case Command.ZeroOrMoreChars:
				return Match (text, pc + 1, offset) || Match (text, pc, offset + 1) ;
			default:
				throw new ApplicationException ("Unknown command: " + cur.Command);
			}
		}
	}
}