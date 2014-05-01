// 
// ViWordFindStrategy.cs
//  
// Author:
//       Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com>
// 
// Copyright (c) 2009 Levi Bard
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

namespace Mono.TextEditor.Vi
{
	/// <summary>
	/// A word find strategy to mimic vi's
	/// </summary>
	public class ViWordFindStrategy: WordFindStrategy
	{

		#region IWordFindStrategy implementation
		
		/// <summary>
		/// Move to next non-whitespace change in character class.
		/// </summary>
		public override int FindNextSubwordOffset (TextDocument doc, int offset)
		{
			int myoffset = offset;
			if (0 > myoffset || doc.TextLength-1 <= myoffset){ return myoffset; }
			
			char c = doc.GetCharAt (myoffset);
			CharacterClass initialClass = GetCharacterClass (c);
			
			while (GetCharacterClass (c) == initialClass && 0 <= myoffset && doc.TextLength-1 > myoffset) {
				c = doc.GetCharAt (++myoffset);
			}
			for (c = doc.GetCharAt (myoffset);
			     char.IsWhiteSpace (c) && 0 <= myoffset && doc.TextLength-1 > myoffset;
			     c = doc.GetCharAt (++myoffset));
			     
			return (myoffset == offset)? myoffset+1: myoffset;
		}
		
		/// <summary>
		/// Move past next whitespace group.
		/// </summary>
		public override int FindNextWordOffset (TextDocument doc, int offset)
		{
			int myoffset = offset;
			if (0 > myoffset || doc.TextLength-1 <= myoffset){ return myoffset; }
			
			for (char c = doc.GetCharAt (myoffset);
			     !char.IsWhiteSpace (c) && 0 <= myoffset && doc.TextLength-1 > myoffset;
			     c = doc.GetCharAt (++myoffset));
			for (char c = doc.GetCharAt (myoffset);
			     char.IsWhiteSpace (c) && 0 <= myoffset && doc.TextLength-1 > myoffset;
			     c = doc.GetCharAt (++myoffset));
			     
			return (myoffset == offset)? myoffset+1: myoffset;
		}
		
		/// <summary>
		/// Move to previous non-whitespace change in character class.
		/// </summary>
		public override int FindPrevSubwordOffset (TextDocument doc, int offset)
		{
			int myoffset = offset-1;
			char c;
			if (0 > myoffset || doc.TextLength-1 <= myoffset){ return myoffset; }
			
			for (c = doc.GetCharAt (myoffset);
			     char.IsWhiteSpace (c) && 0 <= myoffset && doc.TextLength-1 > myoffset;
			     c = doc.GetCharAt (--myoffset));
			     
			CharacterClass initialClass = GetCharacterClass (c);
			
			for (; GetCharacterClass (c) == initialClass && 
			     0 <= myoffset && doc.TextLength-1 > myoffset;
			     c = doc.GetCharAt (--myoffset));
			     
			return (0 == myoffset)? myoffset: myoffset+1;
		}
		
		/// <summary>
		/// Move to end of previous whitespace group.
		/// </summary>
		public override int FindPrevWordOffset (TextDocument doc, int offset)
		{
			--offset;
			if (0 > offset || doc.TextLength-1 <= offset){ return offset; }
			
			for (char c = doc.GetCharAt (offset);
			     char.IsWhiteSpace (c) && 0 < offset && doc.TextLength > offset;
			     c = doc.GetCharAt (--offset));
			for (char c = doc.GetCharAt (offset);
			     !char.IsWhiteSpace (c) && 0 < offset && doc.TextLength > offset;
			     c = doc.GetCharAt (--offset));
			     
			return (0 == offset)? offset: offset+1;
		}
		
		#endregion

		private static bool OffsetIsWithinBounds (TextDocument doc, int offset)
		{
			return (offset >= 0 && offset <= doc.TextLength - 1);
		}

		public static int FindNextSubwordEndOffset (TextDocument doc, int offset)
		{
			int myoffset = offset + 1;

			if (!OffsetIsWithinBounds (doc, myoffset)) { 
				return myoffset; 
			}

			char c = doc.GetCharAt (myoffset);
			// skip whitespace
			while (char.IsWhiteSpace (c)) {
				if (OffsetIsWithinBounds (doc, ++myoffset)) { 
					c = doc.GetCharAt (myoffset);
				} else {
					return offset;
				}
			}
			var initialClass = ViWordFindStrategy.GetCharacterClass (c);
			while (ViWordFindStrategy.GetCharacterClass (c) == initialClass && 0 <= myoffset && doc.TextLength-1 > myoffset) {
				c = doc.GetCharAt (++myoffset);
			}

			return System.Math.Max (offset, myoffset - 1);
		}

		public static int FindNextWordEndOffset (TextDocument doc, int offset)
		{
			int myoffset = offset + 1;

			if (!OffsetIsWithinBounds (doc, myoffset)) { 
				return myoffset; 
			}

			char c = doc.GetCharAt (myoffset);
			// skip whitespace
			while (char.IsWhiteSpace (c)) {
				if (OffsetIsWithinBounds (doc, ++myoffset)) { 
					c = doc.GetCharAt (myoffset);
				} else {
					return offset;
				}
			}

			while (!char.IsWhiteSpace (c) && 0 <= myoffset && doc.TextLength-1 > myoffset) {
				c = doc.GetCharAt (++myoffset);
			}

			return System.Math.Max (offset, myoffset - 1);
		}	

		/// <summary>
		/// Gets the character class for a given character.
		/// </summary>
		new static CharacterClass GetCharacterClass (char c)
		{
			if (char.IsLetterOrDigit (c) || '_' == c) {
				return CharacterClass.AlphaNumeric;
			} else if (char.IsWhiteSpace (c)) {
				return CharacterClass.Whitespace;
			} else {
				return CharacterClass.Symbol;
			}
		}	
		
		new enum CharacterClass
		{
			AlphaNumeric, // Should be roughly equivalent to [\w\d]
			Whitespace,
			Symbol        // !(AlphaNumeric || Whitespace)
		}
	}
}
