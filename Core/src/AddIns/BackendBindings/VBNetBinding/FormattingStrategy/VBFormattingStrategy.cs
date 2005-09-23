// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Markus Palme" email="MarkusPalme@gmx.de"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Drawing;
using System.Text;

using MonoDevelop.TextEditor.Document;
using MonoDevelop.TextEditor.Actions;
using MonoDevelop.TextEditor;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;

using MonoDevelop.EditorBindings.FormattingStrategy;

namespace VBBinding.FormattingStrategy
{
	/// <summary>
	/// This class handles the auto and smart indenting in the textbuffer while
	/// you type.
	/// </summary>
	public class VBFormattingStrategy : DefaultFormattingStrategy
	{
		ArrayList statements;
		StringCollection keywords;
		
		bool doCasing;
		bool doInsertion;
		
		public VBFormattingStrategy()
		{
			
			statements = new ArrayList();
			statements.Add(new VBStatement("^if.*?then$", "^end ?if$", "End If", 1));
			statements.Add(new VBStatement("\\bclass \\w+$", "^end class$", "End Class", 1));
			statements.Add(new VBStatement("\\bnamespace \\w+$", "^end namespace$", "End Namespace", 1));
			statements.Add(new VBStatement("\\bmodule \\w+$", "^end module$", "End Module", 1));
			statements.Add(new VBStatement("\\bstructure \\w+$", "^end structure$", "End Structure", 1));
			statements.Add(new VBStatement("^while ", "^end while$", "End While", 1));
			statements.Add(new VBStatement("^select case", "^end select$", "End Select", 2));
			statements.Add(new VBStatement("(?<!\\bmustoverride )\\bsub \\w+", "^end sub$", "End Sub", 1));
			statements.Add(new VBStatement("(?<!\\bmustoverride (readonly |writeonly )?)\\bproperty \\w+", "^end property$", "End Property", 1));
			statements.Add(new VBStatement("(?<!\\bmustoverride )\\bfunction \\w+", "^end function$", "End Function", 1));
			statements.Add(new VBStatement("\\bfor .*?$", "^next( \\w+)?$", "Next", 1));
			statements.Add(new VBStatement("^synclock .*?$", "^end synclock$", "End SyncLock", 1));
			statements.Add(new VBStatement("^get$", "^end get$", "End Get", 1));
			statements.Add(new VBStatement("^with \\w+$", "^end with$", "End With", 1));
			statements.Add(new VBStatement("^set\\s*\\(.*?\\)$", "^end set$", "End Set", 1));
			statements.Add(new VBStatement("^try$", "^end try$", "End Try", 1));
			statements.Add(new VBStatement("^do .+?$", "^loop$", "Loop", 1));
			statements.Add(new VBStatement("^do$", "^loop .+?$", "Loop While ", 1));
			statements.Add(new VBStatement("\\benum .*?$", "^end enum$", "End Enum", 1));
			
			keywords = new StringCollection();
			keywords.AddRange(new string[] {
				"AddHandler", "AddressOf", "Alias", "And", "AndAlso", "Ansi", "As", "Assembly",
				"Auto", "Boolean", "ByRef", "Byte", "ByVal", "Call", "Case", "Catch",
				"CBool", "CByte", "CChar", "CDate", "CDec", "CDbl", "Char", "CInt", "Class",
				"CLng", "CObj", "Const", "CShort", "CSng", "CStr", "CType",
				"Date", "Decimal", "Declare", "Default", "Delegate", "Dim", "DirectCast", "Do",
				"Double", "Each", "Else", "ElseIf", "End", "Enum", "Erase", "Error",
				"Event", "Exit", "False", "Finally", "For", "Friend", "Function", "Get",
				"GetType", "GoSub", "GoTo", "Handles", "If", "Implements", "Imports", "In",
				"Inherits", "Integer", "Interface", "Is", "Let", "Lib", "Like", "Long",
				"Loop", "Me", "Mod", "Module", "MustInherit", "MustOverride", "MyBase", "MyClass",
				"Namespace", "New", "Next", "Not", "Nothing", "NotInheritable", "NotOverridable", "Object",
				"On", "Option", "Optional", "Or", "OrElse", "Overloads", "Overridable", "Overrides",
				"ParamArray", "Preserve", "Private", "Property", "Protected", "Public", "RaiseEvent", "ReadOnly",
				"ReDim", "Region", "REM", "RemoveHandler", "Resume", "Return", "Select", "Set", "Shadows",
				"Shared", "Short", "Single", "Static", "Step", "Stop", "String", "Structure",
				"Sub", "SyncLock", "Then", "Throw", "To", "True", "Try", "TypeOf",
				"Unicode", "Until", "Variant", "When", "While", "With", "WithEvents", "WriteOnly", "Xor"
			});
		}
		
		/// <summary>
		/// Define VB.net specific smart indenting for a line :)
		/// </summary>
		protected override int SmartIndentLine(IFormattableDocument textArea, int lineNr)
		{
			PropertyService propertyService = (PropertyService)ServiceManager.GetService(typeof(PropertyService));
			doCasing = propertyService.GetProperty("VBBinding.TextEditor.EnableCasing", true);
			IFormattableDocument document = textArea;
			if (lineNr <= 0)
				return AutoIndentLine(textArea, lineNr);
			//LineSegment lineAbove = document.GetLineSegment(lineNr - 1);
			//string lineAboveText = document.GetText(lineAbove.Offset, lineAbove.Length).Trim();
			string lineAboveText=document.GetLineAsString(lineNr-1).Trim();
			
			//LineSegment curLine = document.GetLineSegment(lineNr);
			//string oldLineText = document.GetText(curLine.Offset, curLine.Length);
			string oldLineText=document.GetLineAsString(lineNr);
			string curLineText = oldLineText.Trim();
			
			// remove comments
			string texttoreplace = Regex.Replace(lineAboveText, "'.*$", "", RegexOptions.Singleline).Trim();
			// remove string content
			foreach (Match match in Regex.Matches(texttoreplace, "\"[^\"]*?\"")) {
				texttoreplace = texttoreplace.Remove(match.Index, match.Length).Insert(match.Index, new String('-', match.Length));
			}
			
			string curLineReplace = Regex.Replace(curLineText, "'.*$", "", RegexOptions.Singleline).Trim();
			// remove string content
			foreach (Match match in Regex.Matches(curLineReplace, "\"[^\"]*?\"")) {
				curLineReplace = curLineReplace.Remove(match.Index, match.Length).Insert(match.Index, new String('-', match.Length));
			}
			
			StringBuilder b = new StringBuilder(GetIndentation(textArea, lineNr - 1));
			
			//string indentString = Tab.GetIndentationString(document);
			string indentString="\t";
			
			if (texttoreplace.IndexOf(':') > 0)
				texttoreplace = texttoreplace.Substring(0, texttoreplace.IndexOf(':')).TrimEnd();
			
			bool matched = false;
			foreach (VBStatement statement in statements) {
				if (statement.IndentPlus == 0) continue;
				if (Regex.IsMatch(curLineReplace, statement.EndRegex, RegexOptions.IgnoreCase)) {
					for (int i = 0; i < statement.IndentPlus; ++i) {
						RemoveIndent(b);
					}
					if (doCasing && !statement.EndStatement.EndsWith(" "))
						curLineText = statement.EndStatement;
					matched = true;
				}
				if (Regex.IsMatch(texttoreplace, statement.StartRegex, RegexOptions.IgnoreCase)) {
					for (int i = 0; i < statement.IndentPlus; ++i) {
						b.Append(indentString);
					}
					matched = true;
				}
				if (matched)
					break;
			}
			
			if (lineNr >= 2) {
				if (texttoreplace.EndsWith("_")) {
					// Line continuation
					char secondLastChar = ' ';
					for (int i = texttoreplace.Length - 2; i >= 0; --i) {
						secondLastChar = texttoreplace[i];
						if (!Char.IsWhiteSpace(secondLastChar))
							break;
					}
					if (secondLastChar != '>') {
						// is not end of attribute
						//LineSegment line2Above = document.GetLineSegment(lineNr - 2);
						//string lineAboveText2 = document.GetText(line2Above.Offset, line2Above.Length).Trim();
						string lineAboveText2=document.GetLineAsString(lineNr-2).Trim();
						lineAboveText2 = Regex.Replace(lineAboveText2, "'.*$", "", RegexOptions.Singleline).Trim();
						if (!lineAboveText2.EndsWith("_")) {
							b.Append(indentString);
						}
					}
				} else {
					//LineSegment line2Above = document.GetLineSegment(lineNr - 2);
					//string lineAboveText2 = document.GetText(line2Above.Offset, line2Above.Length).Trim();
					string lineAboveText2=document.GetLineAsString(lineNr-2).Trim();
					lineAboveText2 = StripComment(lineAboveText2);
					if (lineAboveText2.EndsWith("_")) {
						char secondLastChar = ' ';
						for (int i = texttoreplace.Length - 2; i >= 0; --i) {
							secondLastChar = texttoreplace[i];
							if (!Char.IsWhiteSpace(secondLastChar))
								break;
						}
						if (secondLastChar != '>')
							RemoveIndent(b);
					}
				}
			}
			
			if (IsElseConstruct(curLineText))
				RemoveIndent(b);
			
			if (IsElseConstruct(lineAboveText))
				b.Append(indentString);
			
			int indentLength = b.Length;
			b.Append(curLineText);
			if (b.ToString() != oldLineText)
				textArea.ReplaceLine(lineNr, b.ToString());
			return indentLength;
		}
		
		bool IsElseConstruct(string line)
		{
			string t = StripComment(line).ToLower();
			if (t.StartsWith("case ")) return true;
			if (t == "else" || t.StartsWith("elseif ")) return true;
			if (t == "catch" || t.StartsWith("catch ")) return true;
			if (t == "finally") return true;
			
			return false;
		}
		
		string StripComment(string text)
		{
			return Regex.Replace(text, "'.*$", "", RegexOptions.Singleline).Trim();
		}
		
		void RemoveIndent(StringBuilder b)
		{
			if (b.Length == 0) return;
			if (b[b.Length - 1] == '\t') {
				b.Remove(b.Length - 1, 1);
			} else {
				for (int j = 0; j < 4; ++j) {
					if (b.Length == 0) return;
					if (b[b.Length - 1] != ' ')
						break;
					b.Remove(b.Length - 1, 1);
				}
			}
		}
		
		public override int FormatLine(IFormattableDocument textArea, int lineNr, int cursorOffset, char ch)
		{
			PropertyService propertyService = (PropertyService)ServiceManager.GetService(typeof(PropertyService));
			doCasing = propertyService.GetProperty("VBBinding.TextEditor.EnableCasing", true);
			doInsertion = propertyService.GetProperty("VBBinding.TextEditor.EnableEndConstructs", true);
			
			if (lineNr > 0) {
				//LineSegment curLine = textArea.Document.GetLineSegment(lineNr);
				//LineSegment lineAbove = lineNr > 0 ? textArea.Document.GetLineSegment(lineNr - 1) : null;
				
				//string curLineText = textArea.Document.GetText(curLine.Offset, curLine.Length);
				//string lineAboveText = textArea.Document.GetText(lineAbove.Offset, lineAbove.Length);
				string curLineText=textArea.GetLineAsString(lineNr).Trim();
				string lineAboveText=textArea.GetLineAsString(lineNr-1).Trim();
				
				if (ch == '\n' && lineAboveText != null) {
					int undoCount = 1;
					
					// remove comments
					string texttoreplace = Regex.Replace(lineAboveText, "'.*$", "", RegexOptions.Singleline);
					// remove string content
					MatchCollection strmatches = Regex.Matches(texttoreplace, "\"[^\"]*?\"", RegexOptions.Singleline);
					foreach (Match match in strmatches) {
						texttoreplace = texttoreplace.Remove(match.Index, match.Length).Insert(match.Index, new String('-', match.Length));
					}
					
					if (doCasing) {
						foreach (string keyword in keywords) {
							string regex = "(?:\\W|^)(" + keyword + ")(?:\\W|$)";
							MatchCollection matches = Regex.Matches(texttoreplace, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
							foreach (Match match in matches) {
								textArea.ReplaceLine(lineNr-1 + match.Groups[1].Index, keyword);
								++undoCount;
							}
						}
					}
					
					if (doInsertion) {
						foreach (VBStatement statement in statements) {
							if (Regex.IsMatch(texttoreplace.Trim(), statement.StartRegex, RegexOptions.IgnoreCase)) {
								string indentation = GetIndentation(textArea, lineNr - 1);
								if (isEndStatementNeeded(textArea, statement, lineNr)) {
									//textArea.Insert(textArea.Caret.Offset, "\n" + indentation + statement.EndStatement);
									//++undoCount;
								}
								for (int i = 0; i < statement.IndentPlus; i++) {
									indentation += "\t";	//Tab.GetIndentationString(textArea.Document);
								}
								
								textArea.ReplaceLine(lineNr, indentation + curLineText.Trim());
								//Is this automagic now?
								//textArea.Document.UndoStack.UndoLast(undoCount + 1);	
								return indentation.Length;
							}
						}
					}
					
					
					if (IsInString(lineAboveText)) {
						if (IsFinishedString(curLineText)) {
							textArea.Insert(lineNr-1 + lineAboveText.Length,
							                         "\" & _");
							curLineText = textArea.GetLineAsString(lineNr);
							textArea.Insert(lineNr, "\"");
							
							if (IsElseConstruct(lineAboveText))
								SmartIndentLine(textArea, lineNr - 1);
							int result = SmartIndentLine(textArea, lineNr) + 1;
							//textArea.UndoStack.UndoLast(undoCount + 3);
							return result;
						} else {
							textArea.Insert(lineNr-1 + lineAboveText.Length,
							                         "\"");
							if (IsElseConstruct(lineAboveText))
								SmartIndentLine(textArea, lineNr - 1);
							int result = SmartIndentLine(textArea, lineNr);
							//textArea.Document.UndoStack.UndoLast(undoCount + 2);
							return result;
						}
					} else {
						string indent = GetIndentation(textArea, lineNr - 1);
						if (indent.Length > 0) {
							//string newLineText = indent + TextUtilities.GetLineAsString(textArea.Document, lineNr).Trim();
							string newLineText=indent + textArea.GetLineAsString(lineNr).Trim();
							//curLine = textArea.GetLineAsString(lineNr);
							textArea.ReplaceLine(lineNr, newLineText);
							//++undoCount;
						}
						if (IsElseConstruct(lineAboveText))
							SmartIndentLine(textArea, lineNr - 1);
						//textArea.Document.UndoStack.UndoLast(undoCount);
						return indent.Length;
					}
				}
			}
			return 0;
		}
		
		bool IsInString(string start)
		{
			bool inString = false;
			for (int i = 0; i < start.Length; i++) {
				if (start[i] == '"')
					inString = !inString;
				if (!inString && start[i] == '\'')
					return false;
			}
			return inString;
		}
		bool IsFinishedString(string end)
		{
			bool inString = true;
			for (int i = 0; i < end.Length; i++) {
				if (end[i] == '"')
					inString = !inString;
				if (!inString && end[i] == '\'')
					break;
			}
			return !inString;
		}
		
		bool isEndStatementNeeded(IFormattableDocument textArea, VBStatement statement, int lineNr)
		{
			int count = 0;
			int i=0;
			
			//for (int i = 0; i < textArea.TotalNumberOfLines; i++) {
			try{
				while(true){
					//LineSegment line = textArea.Document.GetLineSegment(i);
					//string lineText = textArea.Document.GetText(line.Offset, line.Length).Trim();
					string lineText=textArea.GetLineAsString(i++).Trim();
					
					if (lineText.StartsWith("'")) {
						continue;
					}
					
					if (Regex.IsMatch(lineText, statement.StartRegex, RegexOptions.IgnoreCase)) {
						count++;
					} else if (Regex.IsMatch(lineText, statement.EndRegex, RegexOptions.IgnoreCase)) {
						count--;
					}
				}
			} catch(Exception ex){
				//exit while
			}//try
			return count > 0;
		}
		
		class VBStatement
		{
			public string StartRegex   = "";
			public string EndRegex     = "";
			public string EndStatement = "";
			
			public int IndentPlus = 0;
			
			public VBStatement()
			{
			}
			
			public VBStatement(string startRegex, string endRegex, string endStatement, int indentPlus)
			{
				StartRegex = startRegex;
				EndRegex   = endRegex;
				EndStatement = endStatement;
				IndentPlus   = indentPlus;
			}
		}
		
		
		#region SearchBracket
		public int SearchBracketBackward(IFormattableDocument document, int offset, char openBracket, char closingBracket)
		{
			bool inString  = false;
			char ch;
			int brackets = -1;
			for (int i = offset; i > 0; --i) {
				ch = document.GetCharAt(i);
				if (ch == openBracket && !inString) {
					++brackets;
					if (brackets == 0) return i;
				} else if (ch == closingBracket && !inString) {
					--brackets;
				} else if (ch == '"') {
					inString = !inString;
				} else if (ch == '\n') {
					int lineStart = ScanLineStart(document, i);
					if (lineStart >= 0) { // line could have a comment
						inString = false;
						for (int j = lineStart; j < i; ++j) {
							ch = document.GetCharAt(j);
							if (ch == '"') inString = !inString;
							if (ch == '\'' && !inString) {
								// comment found!
								// Skip searching in the comment:
								i = j;
								break;
							}
						}
					}
					inString = false;
				}
			}
			return -1;
		}
		
		static int ScanLineStart(IFormattableDocument document, int offset)
		{
			bool hasComment = false;
			for (int i = offset - 1; i > 0; --i) {
				char ch = document.GetCharAt(i);
				if (ch == '\n') {
					if (!hasComment) return -1;
					return i + 1;
				} else if (ch == '\'') {
					hasComment = true;
				}
			}
			return 0;
		}
		
		public int SearchBracketForward(IFormattableDocument document, int offset, char openBracket, char closingBracket)
		{
			bool inString  = false;
			bool inComment = false;
			int  brackets  = 1;
			for (int i = offset; i < document.TextLength; ++i) {
				char ch = document.GetCharAt(i);
				if (ch == '\n') {
					inString  = false;
					inComment = false;
				}
				if (inComment) continue;
				if (ch == '"') inString = !inString;
				if (inString)  continue;
				if (ch == '\'') {
					inComment = true;
				} else if (ch == openBracket) {
					++brackets;
				} else if (ch == closingBracket) {
					--brackets;
					if (brackets == 0) return i;
				}
			}
			return -1;
		}
		#endregion
	}
}
