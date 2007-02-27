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
		
		/// <summary>
		/// Define CSharp specific smart indenting for a line :)
		/// </summary>
		protected override int SmartIndentLine (TextEditor d, int lineNr, string indentString)
		{
			if (lineNr <= 0)
				return AutoIndentLine (d, lineNr, indentString);
			
			string lineAboveText = d.GetLineText (lineNr - 1);
			string trimlineAboveText = lineAboveText.Trim ();
			string curLineText = d.GetLineText (lineNr);
			string trimcurLineText = curLineText.Trim ();
			
			if ((trimlineAboveText.EndsWith (")") &&
			     trimcurLineText.StartsWith ("{")) ||       // after for, while, etc.
			    (trimlineAboveText.EndsWith ("else") &&
			     trimcurLineText.StartsWith ("{")))         // after else
			{
				string indentation = GetIndentation (d, lineNr - 1);
				d.ReplaceLine (lineNr, indentation + trimcurLineText);
				return indentation.Length;
			}
			
			// indent closing bracket.
			if (trimcurLineText.StartsWith ("}")) {
				int openLine;
				int closingBracketOffset = d.GetClosingBraceForLine (lineNr, out openLine);
				
				// no closing bracket found -> autoindent
				if (closingBracketOffset == -1)
					return AutoIndentLine (d, lineNr, indentString);
				
				string indentation = GetIndentation (d, openLine);
				
				d.ReplaceLine (lineNr, indentation + trimcurLineText);
				return indentation.Length;
			}
			
			// expression ended, reset to valid indent.
			if (lineAboveText.EndsWith (";")) {
				int openLine;
				int closingBracketOffset = d.GetClosingBraceForLine (lineNr, out openLine);
				
				// no closing bracket found -> autoindent
				if (closingBracketOffset == -1)
					return AutoIndentLine (d, lineNr, indentString);
				
				string closingBracketLineText = d.GetLineText (openLine).Trim ();
				
				string indentation = GetIndentation (d, openLine);
				
				// special handling for switch statement formatting.
				if (closingBracketLineText.StartsWith ("switch")) {
					if (!(trimlineAboveText.StartsWith ("break;") ||
					      trimlineAboveText.StartsWith ("goto") ||
					      trimlineAboveText.StartsWith ("return")))
						indentation += indentString;
				}
				indentation += indentString;
				
				d.ReplaceLine (lineNr, indentation + trimcurLineText);
				return indentation.Length;
			}
			
			if (trimlineAboveText.EndsWith ("{") || // indent opening bracket.
			    trimlineAboveText.EndsWith (":") || // indent case xyz:
			    (trimlineAboveText.EndsWith (")") &&  // indent single line if, for ... etc
			     (trimlineAboveText.StartsWith ("if") ||
			      trimlineAboveText.StartsWith ("while") ||
			      trimlineAboveText.StartsWith ("for"))) ||
			    trimlineAboveText.EndsWith ("else")) {
				string indentation = GetIndentation (d, lineNr - 1) + indentString;
				d.ReplaceLine (lineNr, indentation + trimcurLineText);
				return indentation.Length;
			} else {
				// try to indent linewrap
				ArrayList bracketPos = new ArrayList ();
				
				// search for a ( bracket that isn't closed
				for (int i = 0; i < lineAboveText.Length; ++i) {
					if (lineAboveText [i] == '(')
						bracketPos.Add (i);
					else if (lineAboveText [i] == ')' && bracketPos.Count > 0)
						bracketPos.RemoveAt (bracketPos.Count - 1);
				}
				
				if (bracketPos.Count > 0) {
					int bracketIndex = (int)bracketPos [bracketPos.Count - 1];
					string indentation = GetIndentation (d, lineNr - 1);
					
					// insert enough spaces to match brace start in the next line
					for (int i = 0; i <= bracketIndex; ++i)
						indentation += " ";
					
					d.ReplaceLine (lineNr, indentation + trimcurLineText);
					return indentation.Length;
				}
			}
			
			return AutoIndentLine (d, lineNr, indentString);
		}
		
		bool NeedCurlyBracket (string text)
		{
			int curlyCounter = 0;
			
			bool inString = false;
			bool inChar   = false;
			bool verbatim = false;
			
			bool lineComment  = false;
			bool blockComment = false;
			
			for (int i = 0; i < text.Length; ++i) {
				switch (text [i]) {
					case '\r':
					case '\n':
						lineComment = false;
						inChar = false;
						if (!verbatim) inString = false;
						break;
					case '/':
						if (blockComment) {
							Debug.Assert (i > 0);
							if (text [i - 1] == '*')
								blockComment = false;
								
						}
						
						if (!inString && !inChar && i + 1 < text.Length) {
							if (!blockComment && text [i + 1] == '/')
								lineComment = true;
							
							if (!lineComment && text [i + 1] == '*')
								blockComment = true;
						}
						break;
					case '"':
						if (!(inChar || lineComment || blockComment)) {
							if (inString && verbatim) {
								++i; // skip escaped quote
								inString = false; // let the string go on
							}
							else if (!inString && i > 0 && text[i -1] == '@') {
								verbatim = true;
							}
							inString = !inString;
						}
						
						break;
					case '\'':
						if (!(inString || lineComment || blockComment))
							inChar = !inChar;
						
						break;
					case '{':
						if (!(inString || inChar || lineComment || blockComment))
							++curlyCounter;
						
						break;
					case '}':
						if (!(inString || inChar || lineComment || blockComment))
							--curlyCounter;
						
						break;
				}
			}
			return curlyCounter > 0;
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
		public override int FormatLine (TextEditor d, int lineNr, int cursorOffset, char ch, string indentString, bool autoInsertCurlyBracket)
		{
			int curLineOffset = d.GetPositionFromLineColumn (lineNr, 1);
			
			if (ch != '\n' && ch != '>' && IsInsideStringOrComment (d, curLineOffset, cursorOffset))
				return 0;
			
			switch (ch) {
				case '>':
					if (IsInsideDocumentationComment (d, curLineOffset, cursorOffset)) {
						string curLineText  = d.GetLineText (lineNr);
						int column = cursorOffset - curLineOffset;
						int index = Math.Min (column - 1, curLineText.Length - 1);
						if (curLineText [index] == '/')
							break;
						
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
								break;
						
							StringBuilder commentBuilder = new StringBuilder ("");
							for (int i = index; i < curLineText.Length && i < column && !Char.IsWhiteSpace (curLineText [i]); ++i)
								commentBuilder.Append (curLineText [i]);
								
							string tag = commentBuilder.ToString ().Trim ();
							if (!tag.EndsWith (">"))
								tag += ">";
							
							if (!tag.StartsWith ("/"))
								d.InsertText (cursorOffset, "</" + tag.Substring (1));
						}
					}
					break;
				case '}':
				case '{':
					return IndentLine (d, lineNr, indentString);
				case '\n':
					if (lineNr <= 0)
						return IndentLine (d, lineNr, indentString);
					
					return IndentLine (d, lineNr - 1, indentString);
			}
			return 0;
		}
	}
}
