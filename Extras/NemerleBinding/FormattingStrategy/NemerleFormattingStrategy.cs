// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

// Using C#'s Formatting Strategy for Nemerle

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Text;

using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.Document;
using MonoDevelop.SourceEditor.FormattingStrategy;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core;

namespace NemerleBinding.FormattingStrategy {
	/// <summary>
	/// This class handles the auto and smart indenting in the textbuffer while
	/// you type.
	/// </summary>
	public class NemerleFormattingStrategy : DefaultFormattingStrategy {
		
		/// <summary>
		/// Define CSharp specific smart indenting for a line :)
		/// </summary>
		protected override int SmartIndentLine (IFormattableDocument d, int lineNr)
		{
			if (lineNr > 0) {
				string  lineAboveText = d.GetLineAsString (lineNr - 1);
				string  trimlineAboveText = lineAboveText.Trim ();
				string  curLineText = d.GetLineAsString (lineNr);
				string  trimcurLineText = curLineText.Trim ();
				
				if ((trimlineAboveText.EndsWith (")") && trimcurLineText.StartsWith ("{")) ||       // after for, while, etc.
				    (trimlineAboveText.EndsWith ("else") && trimcurLineText.StartsWith ("{")))      // after else
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
						return AutoIndentLine (d, lineNr);
					
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
						return AutoIndentLine (d, lineNr);
					
					string closingBracketLineText = d.GetLineAsString (openLine).Trim ();
					
					string indentation = GetIndentation (d, openLine);
					
					// special handling for switch statement formatting.
					if (closingBracketLineText.StartsWith ("switch")) {
						if (! (trimlineAboveText.StartsWith ("break;") || trimlineAboveText.StartsWith ("goto") || trimlineAboveText.StartsWith ("return")))
							indentation += d.IndentString;
					}
					indentation += d.IndentString;
					
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
						string indentation = GetIndentation (d, lineNr - 1) + d.IndentString;
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
			}
			return AutoIndentLine (d, lineNr);
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
		
		bool IsInsideStringOrComment (IFormattableDocument d, int curLineOffset, int cursorOffset)
		{
			// scan cur line if it is inside a string or single line comment (//)
			bool isInsideString  = false;
			
			for (int i = curLineOffset; i < cursorOffset; ++i) {
				char ch = d.GetCharAt (i);
				if (ch == '"')
					isInsideString = !isInsideString;
				
				if (ch == '/' && i + 1 < cursorOffset && d.GetCharAt (i + 1) == '/')
					return true;
			}
			
			return isInsideString;
		}

		bool IsInsideDocumentationComment (IFormattableDocument d, int curLineOffset, int cursorOffset)
		{
			// scan cur line if it is inside a string or single line comment (//)
			bool isInsideString  = false;
			
			for (int i = curLineOffset; i < cursorOffset; ++i) {
				char ch = d.GetCharAt (i);
				if (ch == '"')
					isInsideString = !isInsideString;
					
				if (!isInsideString) {
					if (ch == '/' && i + 2 < cursorOffset && d.GetCharAt (i + 1) == '/' && d.GetCharAt (i + 2) == '/')
						return true;
				}
			}
			
			return false;
		}
		
		// used for comment tag formater/inserter
		public override int FormatLine (IFormattableDocument d, int lineNr, int cursorOffset, char ch)
		{
			int curLineOffset, curLineLength;
			d.GetLineLengthInfo (lineNr, out curLineOffset, out curLineLength);
			
			if (ch != '\n' && ch != '>' && IsInsideStringOrComment (d, curLineOffset, cursorOffset))
				return 0;
			
			switch (ch) {
				case '>':
					if (IsInsideDocumentationComment (d, curLineOffset, cursorOffset)) {
						string curLineText  = d.GetLineAsString (lineNr);
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
								d.Insert (cursorOffset, "</" + tag.Substring (1));
						}
					}
					break;
				case '}':
				case '{':
					return IndentLine (d, lineNr);
				case '\n':
					if (lineNr <= 0)
						return IndentLine (d, lineNr);
					
					return IndentLine (d, lineNr - 1);
			}
			return 0;
		}
	}
}
