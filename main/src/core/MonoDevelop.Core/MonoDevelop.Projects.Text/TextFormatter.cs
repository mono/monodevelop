// 
// TextFormatter.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using System.Text;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Text
{
	public enum WrappingType
	{
		None,
		Char,
		Word,
		WordChar
	}
	
	public class TextFormatter
	{
		string indentString = "";
		string formattedIndentString;
		int indentColumnWidth;
		string paragFormattedIndentString;
		int paragIndentColumnWidth;
		int leftMargin;
		int paragraphStartMargin;
		WrappingType wrap;
		int tabWidth;
		bool tabsAsSpaces;
		
		public int MaxColumns { get; set; }
		
		StringBuilder builder = new StringBuilder ();
		StringBuilder currentWord = new StringBuilder ();
		int curCol;
		bool lineStart = true;
		bool lastWasSeparator;
		bool paragraphStart = true;
		int wordLevel;
		
		public TextFormatter ()
		{
			MaxColumns = 80;
			TabWidth = 4;
		}
		
		public int TabWidth {
			get { return tabWidth; }
			set {
				tabWidth = value;
				formattedIndentString = null;
			}
		}
		
		public string IndentString {
			get { return indentString; }
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				indentString = value;
				formattedIndentString = null;
			}
		}
		
		public int LeftMargin {
			get {
				return leftMargin;
			}
			set {
				leftMargin = value;
				formattedIndentString = null;
			}
		}
		
		public int ParagraphStartMargin {
			get { return paragraphStartMargin; }
			set {
				paragraphStartMargin = value;
				formattedIndentString = null;
			}
		}
		
		public WrappingType Wrap {
			get {
				return wrap;
			}
			set {
				if (wrap != value) {
					AppendCurrentWord ('x');
					wrap = value;
				}
			}
		}

		public bool KeepLines {
			get; set;
		}
		
		public bool TabsAsSpaces {
			get { return tabsAsSpaces; }
			set {
				tabsAsSpaces = value;
				formattedIndentString = null;
			}
		}
		
		string FormattedIndentString {
			get {
				if (formattedIndentString == null)
					CreateIndentString ();
				return formattedIndentString;
			}
		}
		
		int IndentColumnWidth {
			get {
				if (formattedIndentString == null)
					CreateIndentString ();
				return indentColumnWidth;
			}
		}
		
		string ParagFormattedIndentString {
			get {
				if (formattedIndentString == null)
					CreateIndentString ();
				return paragFormattedIndentString;
			}
		}
		
		int ParagIndentColumnWidth {
			get {
				if (formattedIndentString == null)
					CreateIndentString ();
				return paragIndentColumnWidth;
			}
		}
		
		public void Clear ()
		{
			builder = new StringBuilder ();
			currentWord = new StringBuilder ();
			curCol = 0;
			lineStart = true;
			paragraphStart = true;
			lastWasSeparator = false;
		}
		
		public void AppendWord (string text)
		{
			BeginWord ();
			Append (text);
			EndWord ();
		}
		
		public void Append (string text)
		{
			if (string.IsNullOrEmpty (text))
				return;
			
			if (builder.Length == 0) {
				curCol = IndentColumnWidth;
				lineStart = true;
				paragraphStart = true;
			}
			
			if (Wrap == WrappingType.None || Wrap == WrappingType.Char) {
				AppendChars (text, Wrap == WrappingType.Char);
				return;
			}
			
			int n = 0;
			
			while (n < text.Length)
			{
				int sn = n;
				bool foundSpace = false;
				while (n < text.Length && !foundSpace) {
					if ((char.IsWhiteSpace (text [n]) && wordLevel == 0) || text [n] == '\n')
						foundSpace = true;
					else
						n++;
				}
				
				if (n != sn)
					currentWord.Append (text, sn, n - sn);
				if (foundSpace) {
					AppendCurrentWord (text[n]);
					n++;
				}
			}
		}
		
		public void AppendLine ()
		{
			AppendCurrentWord ('x');
			AppendChar ('\n', false);
		}
		
		public void BeginWord ()
		{
			wordLevel++;
		}
		
		public void EndWord ()
		{
			if (wordLevel == 0)
				throw new InvalidOperationException ("Missing BeginWord call");
			wordLevel--;
			
			char lastChar = 'x';
			if (currentWord.Length > 0) {
				lastChar = currentWord [currentWord.Length - 1];
				if (char.IsWhiteSpace (lastChar))
					currentWord.Remove (currentWord.Length - 1, 1);
			}
			AppendCurrentWord (lastChar);
		}
		
		public void FlushWord ()
		{
			AppendCurrentWord ('x');
			if (curCol > MaxColumns)
				AppendSoftBreak ();
		}
		
		public override string ToString ()
		{
			if (currentWord.Length > 0)
				AppendCurrentWord ('x');
			return builder.ToString ();
		}
		
		void AppendChars (string s, bool wrapChars)
		{
			foreach (char c in s)
				AppendChar (c, wrapChars);
		}
		
		void AppendSoftBreak ()
		{
			AppendChar ('\n', true);
			paragraphStart = false;
			curCol = IndentColumnWidth;
		}
		
		void AppendChar (char c, bool wrapChars)
		{
			if (c == '\n') {
				lineStart = true;
				paragraphStart = true;
				builder.Append (c);
				curCol = ParagIndentColumnWidth;
				lastWasSeparator = false;
				return;
			}
			else if (lineStart) {
				if (paragraphStart)
					builder.Append (ParagFormattedIndentString);
				else
					builder.Append (FormattedIndentString);
				lineStart = false;
				paragraphStart = false;
				lastWasSeparator = false;
			}
			if (wrapChars && curCol >= MaxColumns) {
				AppendSoftBreak ();
				if (!char.IsWhiteSpace (c))
					AppendChar (c, false);
				return;
			}
			
			if (c == '\t') {
				int tw = GetTabWidth (curCol);
				if (TabsAsSpaces)
					builder.Append (' ', tw);
				else
					builder.Append (c);
				curCol += tw;
			}
			else {
				builder.Append (c);
				curCol++;
			}
		}
		
		void AppendCurrentWord (char separatorChar)
		{
			if (currentWord.Length == 0) {
				if (KeepLines && lineStart && separatorChar == '\n') {
					AppendChar (separatorChar, true);
				}
				return;
			}
			if (Wrap == WrappingType.Word || Wrap == WrappingType.WordChar) {
				if (curCol + currentWord.Length > MaxColumns) {
					// If the last char was a word separator, remove it
					if (lastWasSeparator)
						builder.Remove (builder.Length - 1, 1);
					if (!lineStart)
						AppendSoftBreak ();
				}
			}
			AppendChars (currentWord.ToString (), Wrap == WrappingType.WordChar);
			if (char.IsWhiteSpace (separatorChar) || (separatorChar == '\n' && !lineStart)) {
				lastWasSeparator = true;
				AppendChar (separatorChar, true);
			} else
				lastWasSeparator = false;
			currentWord = new StringBuilder ();
		}
		
		int GetTabWidth (int startCol)
		{
			int res = startCol % TabWidth;
			if (res == 0)
				return TabWidth;
			else
				return TabWidth - res;
		}
		
		void CreateIndentString ()
		{
			StringBuilder sb = StringBuilderCache.Allocate ();
			indentColumnWidth = AddIndentString (sb, indentString);
			sb.Append (new string (' ', paragraphStartMargin));
			paragFormattedIndentString = StringBuilderCache.ReturnAndFree (sb);
			paragIndentColumnWidth = indentColumnWidth + paragraphStartMargin;
			
			if (LeftMargin > 0) {
				sb.Append (' ', LeftMargin);
				indentColumnWidth += LeftMargin;
			}
			formattedIndentString = sb.ToString ();

			if (paragraphStart)
				curCol = paragIndentColumnWidth;
			else if (lineStart)
				curCol = indentColumnWidth;
		}
		
		int AddIndentString (StringBuilder sb, string txt)
		{
			if (string.IsNullOrEmpty (txt))
				return 0;
			int count = 0;
			foreach (char c in txt) {
				if (c == '\t') {
					int tw = GetTabWidth (count);
					count += tw;
					if (TabsAsSpaces)
						sb.Append (' ', tw);
					else
						sb.Append (c);
				}
				else {
					sb.Append (c);
					count++;
				}
			}
			return count;
		}
	}
}
