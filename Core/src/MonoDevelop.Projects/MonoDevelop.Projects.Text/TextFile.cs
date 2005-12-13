//
// TextFile.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Text;

namespace MonoDevelop.Projects.Text
{
	public class TextFile: IEditableTextFile
	{
		string name;
		StringBuilder text;
		
		public TextFile (string name)
		{
			this.name = name;
			StreamReader sr = new StreamReader (name);
			text = new StringBuilder (sr.ReadToEnd ());
			sr.Close ();
		}
		
		public string Name {
			get { return name; } 
		}

		public string Text {
			get { return text.ToString (); }
			set { text = new StringBuilder (value); }
		}
		
		public int Length {
			get { return text.Length;  } 
		}
		
		public string GetText (int startPosition, int endPosition)
		{
			return text.ToString ().Substring (startPosition, endPosition - startPosition);
		}
		
		public int GetPositionFromLineColumn (int line, int column)
		{
			int lin = 1;
			int col = 1;
			for (int i = 0; i < text.Length; i++) {
				if (line == lin && column == col)
					return i;
				if (text[i] == '\n') {
					lin++; col = 1;
				} else
					col++;
			}
			return -1;
		}
		
		public void GetLineColumnFromPosition (int position, out int line, out int column)
		{
			int lin = 1;
			int col = 1;
			for (int i = 0; i < position; i++) {
				if (text[i] == '\n') {
					lin++; col = 1;
				} else
					col++;
			}
			line = lin;
			column = col;
		}
		
		public void InsertText (int position, string text)
		{
			text.Insert (position, text);
		}
		
		public void DeleteText (int position, int length)
		{
			text.Remove (position, length);
		}
		
		public void Save ()
		{
			StreamWriter sw = new StreamWriter (name);
			sw.Write (text.ToString ());
			sw.Close ();
		}
	}
}
