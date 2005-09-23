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

using MonoDevelop.TextEditor;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.EditorBindings.FormattingStrategy;

namespace JavaBinding.FormattingStrategy
{
	/// <summary>
	/// This class handles the auto and smart indenting in the textbuffer while
	/// you type.
	/// </summary>
	public class JavaFormattingStrategy : DefaultFormattingStrategy
	{
		public JavaFormattingStrategy()
		{
		}
				
		/// <summary>
		/// Define Java specific smart indenting for a line :)
		/// </summary>
		protected override int SmartIndentLine(IFormattableDocument d, int lineNr)
		{
			if (lineNr > 0) {
				string  lineAboveText = d.GetLineAsString (lineNr - 1);
                string  trimlineAboveText = lineAboveText.Trim ();
                string  curLineText = d.GetLineAsString (lineNr);
                string  trimcurLineText = curLineText.Trim ();

				if (lineAboveText.EndsWith(")") && curLineText.StartsWith("{")) {
					string indentation = GetIndentation(d, lineNr - 1);
					d.ReplaceLine (lineNr, indentation + curLineText);
					return indentation.Length;
				}
				
				if (curLineText.StartsWith("}")) { // indent closing bracket.
					int openLine;
					int closingBracketOffset = d.GetClosingBraceForLine (lineNr, out openLine);
					if (closingBracketOffset == -1) {  // no closing bracket found -> autoindent
						return AutoIndentLine(d, lineNr);
					}
					
					string indentation = GetIndentation (d, lineNr - 1);
					
					d.ReplaceLine (lineNr, indentation + curLineText);
					return indentation.Length;
				}
				
				if (lineAboveText.EndsWith(";")) { // expression ended, reset to valid indent.
					int openLine;
					int closingBracketOffset = d.GetClosingBraceForLine (lineNr, out openLine);	
					if (closingBracketOffset == -1) {  // no closing bracket found -> autoindent
						return AutoIndentLine(d, lineNr);
					}
					
					string closingBracketLineText = d.GetLineAsString (openLine).Trim ();
					
					string indentation = GetIndentation (d, openLine);
					
					// special handling for switch statement formatting.
					if (closingBracketLineText.StartsWith("switch")) {
						if (lineAboveText.StartsWith("break;") || 
						    lineAboveText.StartsWith("goto")   || 
						    lineAboveText.StartsWith("return")) {
					    } else {
					    	indentation += d.IndentString;
					    }
					}
			    	indentation += d.IndentString;
					
					d.ReplaceLine (lineNr, indentation + curLineText);
					return indentation.Length;
				}
				
				if (lineAboveText.EndsWith("{") || // indent opening bracket.
				    lineAboveText.EndsWith(":") || // indent case xyz:
				    (lineAboveText.EndsWith(")") &&  // indent single line if, for ... etc
			    	(lineAboveText.StartsWith("if") ||
			    	 lineAboveText.StartsWith("while") ||
			    	 lineAboveText.StartsWith("for"))) ||
			    	 lineAboveText.EndsWith("else")) {
						string indentation = GetIndentation (d, lineNr - 1) + d.IndentString;
                        d.ReplaceLine (lineNr, indentation + curLineText);
						return indentation.Length;
			    } else {
			    	// try to indent linewrap
			    	ArrayList bracketPos = new ArrayList();
			    	for (int i = 0; i < lineAboveText.Length; ++i) { // search for a ( bracket that isn't closed
						switch (lineAboveText[i]) {
							case '(':
								bracketPos.Add(i);
								break;
							case ')':
								if (bracketPos.Count > 0) {
									bracketPos.RemoveAt(bracketPos.Count - 1);
								}
								break;
						}
			    	}
			    	if (bracketPos.Count > 0) {
			    		int bracketIndex = (int)bracketPos[bracketPos.Count - 1];
			    		string indentation = GetIndentation (d, lineNr - 1);
			    		
			    		for (int i = 0; i <= bracketIndex; ++i) { // insert enough spaces to match
			    			indentation += " ";                   // brace start in the next line
			    		}
			    		
						d.ReplaceLine (lineNr, indentation + curLineText);
						return indentation.Length;
			    	}
			    }
			}
			return AutoIndentLine (d, lineNr);
		}
		
		bool NeedCurlyBracket(string text) 
		{
			int curlyCounter = 0;
			
			bool inString = false;
			bool inChar   = false;
			
			bool lineComment  = false;
			bool blockComment = false;
			
			for (int i = 0; i < text.Length; ++i) {
				switch (text[i]) {
					case '\r':
					case '\n':
						lineComment = false;
						break;
					case '/':
						if (blockComment) {
							Debug.Assert(i > 0);
							if (text[i - 1] == '*') {
								blockComment = false;
							}
						}
						if (!inString && !inChar && i + 1 < text.Length) {
							if (!blockComment && text[i + 1] == '/') {
								lineComment = true;
							}
							if (!lineComment && text[i + 1] == '*') {
								blockComment = true;
							}
						}
						break;
					case '"':
						inString = !inString;
						break;
					case '\'':
						inChar = !inChar;
						break;
					case '{':
						if (!(inString || inChar || lineComment || blockComment)) {
							++curlyCounter;
						}
						break;
					case '}':
						if (!(inString || inChar || lineComment || blockComment)) {
							--curlyCounter;
						}
						break;
				}
			}
			return curlyCounter > 0;
		}
		
		// used for comment tag formater/inserter
		public override int FormatLine (IFormattableDocument d, int lineNr, int cursorOffset, char ch)
		{
			switch (ch) {
				//case '}':
				//case '{':
				//	return d.FormattingStrategy.IndentLine (d, lineNr);
				case '\n':
					if (lineNr <= 0) {
						return IndentLine(d, lineNr);
					}
					
					if (d.AutoInsertCurlyBracket) {
						string oldLineText = d.GetLineAsString (lineNr - 1);
						if (oldLineText.EndsWith ("{") && NeedCurlyBracket (d.TextContent)) {
								d.Insert (cursorOffset, "\n}");
								IndentLine(d, lineNr + 1);
						}
					}
					
					string  lineAboveText = d.GetLineAsString (lineNr - 1);
					
					
#if NON_PORTABLE_CODE
					if (lineAbove.HighlightSpanStack != null && lineAbove.HighlightSpanStack.Count > 0) {				
						if (!((Span)lineAbove.HighlightSpanStack.Peek()).StopEOL) {	// case for /* style comments
							int index = lineAboveText.IndexOf("/*");
							
							if (index > 0) {
								string indentation = GetIndentation(d, lineNr - 1);
								for (int i = indentation.Length; i < index; ++ i) {
									indentation += ' ';
								}
								d.Document.Replace(curLine.Offset, cursorOffset - curLine.Offset, indentation + " * ");
								return indentation.Length + 3;
							}
							
							index = lineAboveText.IndexOf("*");
							if (index > 0) {
								string indentation = GetIndentation(d, lineNr - 1);
								for (int i = indentation.Length; i < index; ++ i) {
									indentation += ' ';
								}
								d.Document.Replace(curLine.Offset, cursorOffset - curLine.Offset, indentation + "* ");
								return indentation.Length + 2;
							}
						}
					}
#endif
					return IndentLine(d, lineNr);
			}
			return 0;
		}
	}
}
