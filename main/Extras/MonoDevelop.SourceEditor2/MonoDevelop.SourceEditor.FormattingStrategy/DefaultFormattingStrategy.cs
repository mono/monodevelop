//  DefaultFormattingStrategy.cs
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
using System.Text;
using System.Collections;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.SourceEditor.FormattingStrategy
{
	/// <summary>
	/// This class handles the auto and smart indenting in the textbuffer while
	/// you type.
	/// </summary>
	public class DefaultFormattingStrategy : IFormattingStrategy {
		/// <summary>
		/// returns the whitespaces which are before a non white space character in the line
		/// as a string.
		/// </summary>
		protected string GetIndentation (TextEditor d, int lineNumber)
		{
			string lineText = d.GetLineText (lineNumber);
			StringBuilder whitespaces = new StringBuilder ();
			
			foreach (char ch in lineText) {
				if (! Char.IsWhiteSpace (ch))
					break;
				whitespaces.Append (ch);
			}
			
			return whitespaces.ToString ();
		}
		
		/// <summary>
		/// Could be overwritten to define more complex indenting.
		/// </summary>
		protected virtual int AutoIndentLine (TextEditor d, int lineNumber, string indentString)
		{
			string indentation = lineNumber != 0 ? GetIndentation (d, lineNumber - 1) : "";
			
			if (indentation.Length > 0) {
				string newLineText = indentation + d.GetLineText (lineNumber).Trim ();
				d.ReplaceLine (lineNumber, newLineText);
			}
			
			return indentation.Length;
		}
		
		/// <summary>
		/// Could be overwritten to define more complex indenting.
		/// </summary>
		protected virtual int SmartIndentLine (TextEditor d, int line, string indentString)
		{
			return AutoIndentLine (d, line, indentString); // smart = autoindent in normal texts
		}
		
		/// <summary>
		/// This function formats a specific line after <code>ch</code> is pressed.
		/// </summary>
		/// <returns>
		/// the caret delta position the caret will be moved this number
		/// of bytes (e.g. the number of bytes inserted before the caret, or
		/// removed, if this number is negative)
		/// </returns>
		public virtual int FormatLine (TextEditor d, int line, int cursorOffset, char ch, string indentString, bool autoInsertCurlyBracket)
		{
			if (ch == '\n')
				return IndentLine (d, line, indentString);
			
			return 0;
		}
		
		/// <summary>
		/// This function sets the indentation level in a specific line
		/// </summary>
		/// <returns>
		/// the number of inserted characters.
		/// </returns>
		public int IndentLine (TextEditor d, int line, string indentString)
		{
			switch (TextEditorProperties.IndentStyle) {
				case IndentStyle.Auto  : return AutoIndentLine (d, line, indentString);
				case IndentStyle.Smart : return SmartIndentLine (d, line, indentString);
				case IndentStyle.None  :
				default                : return 0;
			}
		}
	}
}
