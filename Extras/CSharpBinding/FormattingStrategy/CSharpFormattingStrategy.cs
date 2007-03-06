// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Text;

using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.FormattingStrategy;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace CSharpBinding.FormattingStrategy {
	/// <summary>
	/// This class handles the auto and smart indenting in the textbuffer while
	/// you type.
	/// </summary>
	public class CSharpFormattingStrategy : DefaultFormattingStrategy {

		protected override int SmartIndentLine (TextEditor d, int lineNr, string indentString)
		{
			// Smart indenting is handled elsewhere for c#
			return 0;
		}
		
		bool IsInsideStringOrComment (TextEditor d, int curLineOffset, int cursorOffset)
		{
			// scan cur line to see if it is inside a string or single line comment (//)
			bool isInsideString = false;
			bool isInsideChar = false;
			bool isVerbatim = false;
			bool isEscaped = false;
			
			for (int i = curLineOffset; i < cursorOffset; ++i) {
				char c = d.GetCharAt (i);
				if (c == '"') {
					if (isVerbatim) {
						if (i + 1 < cursorOffset && d.GetCharAt (i + 1) == '"') {
							// Still inside the verbatim-string-literal
							i++;
						} else {
							isInsideString = false;
							isVerbatim = false;
						}
					} else if (!isEscaped) {
						isInsideString = !isInsideString;
					}
					isEscaped = false;
				} else if (!isInsideString && c == '@') {
					if (i + 1 < cursorOffset && d.GetCharAt (i + 1) == '"') {
						// We are now inside a verbatim-string-literal
						isInsideString = true;
						isVerbatim = true;
						i++;
					}
					isEscaped = false;
				} else if (!isInsideString && c == '\'') {
					if (!isEscaped)
						isInsideChar = !isInsideChar;
					isEscaped = false;
				} else if (c == '\\') {
					if (isInsideString) {
						if (!isVerbatim)
							isEscaped = !isEscaped;
					} else if (isInsideChar) {
						isEscaped = !isEscaped;
					} else {
						isEscaped = false;
					}
				} else if (!isInsideString && !isInsideChar && c == '/') {
					if (i + 1 < cursorOffset && d.GetCharAt (i + 1) == '/') {
						// Inside a single-line comment (//)
						return true;
					}
					isEscaped = false;
				} else {
					isEscaped = false;
				}
			}
			
			return isInsideString;
		}

		bool IsInsideDocumentationComment (TextEditor d, int curLineOffset, int cursorOffset)
		{
			// scan cur line to see if it is inside a documentation comment (///)
			bool isInsideString = false;
			bool isInsideChar = false;
			bool isVerbatim = false;
			bool isEscaped = false;
			
			// TODO: share code with IsInsideStringOrComment?
			for (int i = curLineOffset; i < cursorOffset; ++i) {
				char c = d.GetCharAt (i);
				if (c == '"') {
					if (isVerbatim) {
						if (i + 1 < cursorOffset && d.GetCharAt (i + 1) == '"') {
							// Still inside the verbatim-string-literal
							i++;
						} else {
							isInsideString = false;
							isVerbatim = false;
						}
					} else if (!isEscaped) {
						isInsideString = !isInsideString;
					}
					isEscaped = false;
				} else if (!isInsideString && c == '@') {
					if (i + 1 < cursorOffset && d.GetCharAt (i + 1) == '"') {
						// We are now inside a verbatim-string-literal
						isInsideString = true;
						isVerbatim = true;
						i++;
					}
					isEscaped = false;
				} else if (!isInsideString && c == '\'') {
					if (!isEscaped)
						isInsideChar = !isInsideChar;
					isEscaped = false;
				} else if (c == '\\') {
					if (isInsideString) {
						if (!isVerbatim)
							isEscaped = !isEscaped;
					} else if (isInsideChar) {
						isEscaped = !isEscaped;
					} else {
						isEscaped = false;
					}
				} else if (!isInsideString && !isInsideChar && c == '/') {
					if (i + 2 < cursorOffset &&
					    d.GetCharAt (i + 1) == '/' &&
					    d.GetCharAt (i + 2) == '/') {
						// Inside a documentation comment (///)
						return true;
					}
					isEscaped = false;
				} else {
					isEscaped = false;
				}
			}
			
			return false;
		}
		
		// used for comment tag formater/inserter
		public override int FormatLine (TextEditor d, int lineNr, int cursorOffset, char ch, 
		                                string indentString, bool autoInsertCurlyBracket)
		{
			int curLineOffset = d.GetPositionFromLineColumn (lineNr, 1);
			
			if (ch != '>' || !IsInsideDocumentationComment (d, curLineOffset, cursorOffset))
				return 0;
			
			string curLineText  = d.GetLineText (lineNr);
			int column = cursorOffset - curLineOffset;
			int index = Math.Min (column - 1, curLineText.Length - 1);
			if (curLineText [index] == '/')
				return 0;
			
			while (index >= 0 && curLineText [index] != '<')
				--index;
			
			if (index > 0) {
				bool skipInsert = false;
				for (int i = index; i < curLineText.Length && i < column; ++i) {
					if (i < curLineText.Length && curLineText [i] == '/' && curLineText [i + 1] == '>')
						skipInsert = true;
					
					if (curLineText [i] == '>')
						break;
				}
				
				if (skipInsert)
					return 0;
				
				StringBuilder commentBuilder = new StringBuilder ("");
				for (int i = index; i < curLineText.Length && i < column && !Char.IsWhiteSpace (curLineText [i]); ++i)
					commentBuilder.Append (curLineText [i]);
				
				string tag = commentBuilder.ToString ().Trim ();
				if (!tag.EndsWith (">"))
					tag += ">";
				
				if (!tag.StartsWith ("/"))
					d.InsertText (cursorOffset, "</" + tag.Substring (1));
			}
			
			return 0;
		}
	}
}
