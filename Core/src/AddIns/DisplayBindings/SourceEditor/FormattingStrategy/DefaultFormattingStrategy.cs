// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Text;
using MonoDevelop.EditorBindings.FormattingStrategy;

namespace MonoDevelop.TextEditor.Document {
	/// <summary>
	/// This class handles the auto and smart indenting in the textbuffer while
	/// you type.
	/// </summary>
	public class DefaultFormattingStrategy : IFormattingStrategy {
		/// <summary>
		/// returns the whitespaces which are before a non white space character in the line line
		/// as a string.
		/// </summary>
		protected string GetIndentation (IFormattableDocument d, int lineNumber)
		{
			string lineText = d.GetLineAsString (lineNumber);
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
		protected virtual int AutoIndentLine (IFormattableDocument d, int lineNumber)
		{
			string indentation = lineNumber != 0 ? GetIndentation (d, lineNumber - 1) : "";
			
			if (indentation.Length > 0) {
				string newLineText = indentation + d.GetLineAsString (lineNumber).Trim ();
				d.ReplaceLine (lineNumber, newLineText);
			}
			
			return indentation.Length;
		}
		
		/// <summary>
		/// Could be overwritten to define more complex indenting.
		/// </summary>
		protected virtual int SmartIndentLine (IFormattableDocument d, int line)
		{
			return AutoIndentLine (d, line); // smart = autoindent in normal texts
		}
		
		/// <summary>
		/// This function formats a specific line after <code>ch</code> is pressed.
		/// </summary>
		/// <returns>
		/// the caret delta position the caret will be moved this number
		/// of bytes (e.g. the number of bytes inserted before the caret, or
		/// removed, if this number is negative)
		/// </returns>
		public virtual int FormatLine (IFormattableDocument d, int line, int cursorOffset, char ch)
		{
			if (ch == '\n')
				return IndentLine (d, line);
			
			return 0;
		}
		
		/// <summary>
		/// This function sets the indentation level in a specific line
		/// </summary>
		/// <returns>
		/// the number of inserted characters.
		/// </returns>
		public int IndentLine (IFormattableDocument d, int line)
		{
			switch (d.IndentStyle) {
				case IndentStyle.Auto  : return AutoIndentLine (d, line);
				case IndentStyle.Smart : return SmartIndentLine (d, line);
				case IndentStyle.None  :
				default                : return 0;
			}
		}
		
		/// <summary>
		/// This function sets the indentlevel in a range of lines.
		/// </summary>
		public void IndentLines (IFormattableDocument d, int begin, int end)
		{
			d.BeginAtomicUndo ();
			
			for (int i = begin; i <= end; ++i)
				IndentLine (d, i);
			
			d.EndAtomicUndo ();
		}
	}
}
