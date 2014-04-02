//
// WordFindStrategy.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.Ide.Editor
{
	public interface IWordFindStrategy
	{
		int FindNextWordOffset (IDocument doc, int offset);
		int FindPrevWordOffset (IDocument doc, int offset);
		int FindNextSubwordOffset (IDocument doc, int offset);
		int FindPrevSubwordOffset (IDocument doc, int offset);
		int FindCurrentWordStart (IDocument doc, int offset);
		int FindCurrentWordEnd (IDocument doc, int offset);
	}

	public abstract class WordFindStrategy : IWordFindStrategy
	{
		public enum CharacterClass {
			Unknown,
			Whitespace,
			IdentifierPart,
			UppercaseLetter,
			LowercaseLetter,
			Digit
		}

		public static CharacterClass GetCharacterClass (char ch)
		{
			return GetCharacterClass (ch, false, false);
		}

		public static CharacterClass GetCharacterClass (char ch, bool subword, bool treat_)
		{
			if (Char.IsWhiteSpace (ch))
				return CharacterClass.Whitespace;
			if (Char.IsDigit (ch))
				return subword? CharacterClass.Digit : CharacterClass.IdentifierPart;
			if (Char.IsLetter (ch)) {
				if (!subword)
					return CharacterClass.IdentifierPart;
				else if (Char.IsUpper (ch))
					return CharacterClass.UppercaseLetter;
				else
					return CharacterClass.LowercaseLetter;
			}
			if (!subword && treat_ && ch == '_')
				return CharacterClass.IdentifierPart;
			return CharacterClass.Unknown;
		}

		public abstract int FindNextWordOffset (IDocument doc, int offset);
		public abstract int FindPrevWordOffset (IDocument doc, int offset);

		public virtual int FindNextSubwordOffset (IDocument doc, int offset)
		{
			return FindNextWordOffset (doc, offset);
		}

		public virtual int FindPrevSubwordOffset (IDocument doc, int offset)
		{
			return FindPrevWordOffset (doc, offset);
		}

		public virtual int FindCurrentWordStart (IDocument doc, int offset)
		{
			return ScanWord (doc, offset, false);
		}

		public virtual int FindCurrentWordEnd (IDocument doc, int offset)
		{
			return ScanWord (doc, offset, true);
		}

		internal static int ScanWord (IDocument doc, int offset, bool forwardDirection)
		{
			if (offset < 0 || offset >= doc.TextLength)
				return offset;
			var line = doc.GetLineByOffset (offset);
			char first = doc.GetCharAt (offset);
			while (offset >= line.Offset && offset < line.Offset + line.Length) {
				char ch = doc.GetCharAt (offset);
				if (char.IsWhiteSpace (first) && !char.IsWhiteSpace (ch)
					|| IsNoIdentifierPart (first) && !IsNoIdentifierPart (ch)
					|| (char.IsLetterOrDigit (first) || first == '_') && !(char.IsLetterOrDigit (ch) || ch == '_'))
					break;

				offset = forwardDirection ? offset + 1 : offset - 1;
			}
			return System.Math.Min (line.Offset + line.Length,
				System.Math.Max (line.Offset, offset + (forwardDirection ? 0 : 1)));
		}

		internal static bool IsNoIdentifierPart (char ch)
		{
			return !char.IsWhiteSpace (ch) && !char.IsLetterOrDigit (ch) && ch != '_';
		}
	}
}

