// 
// CSharpFormatter.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.CSharp;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.PrettyPrinter;
using FormattingStrategy;
using MonoDevelop.Projects.Dom;
using MonoDevelop.CSharpBinding;
using MonoDevelop.Projects.Dom.Parser;
using Mono.TextEditor;


namespace CSharpBinding.Parser
{
	public class CSharpFormatter : AbstractPrettyPrinter
	{
		static internal readonly string MimeType = "text/x-csharp";

		public CSharpFormatter ()
		{
		}
		static int GetTrailingWhitespaces (StringBuilder sb)
		{
			string str = sb.ToString ();
			int i = str.Length - 1;
			while (i >= 0 && Char.IsWhiteSpace (str[i])) {
				i--;
			}
//			Console.WriteLine (str.Length - 1 - i);
			
			return str.Length - 1 - i;
		}
		static string CreateWrapperClassForMember (IMember member, bool generateOuterClass, TextEditorData data, out int end)
		{
			if (member == null) {
				end = -1;
				return "";
			}
			StringBuilder result = new StringBuilder ();
			int offset = data.Document.LocationToOffset (member.Location.Line - 1, 0);
			int start = offset;
			while (offset < data.Document.Length && data.Document.GetCharAt (offset) != '{') {
				offset++;
			}
			if (data.Caret.Offset < offset) {
				end = -1;
				return "";
			}
			end = data.Document.GetMatchingBracketOffset (offset);
			if (end < 0)
				return "";
			
			if (generateOuterClass)
				result.Append ("class " + (member.DeclaringType != null ? member.DeclaringType.Name : "GenericClass") + " {");
			for (int i = start; i <= end; i++) {
				char ch = data.Document.GetCharAt (i);
				switch (ch) {
				case '/':
					if (i + 1 <= end && data.Document.GetCharAt (i + 1) == '/') {
						while (i <= end) {
							ch = data.Document.GetCharAt (i);
							result.Append (ch);
							if (i == '\n' || i == '\r')
								break;
							i++;
						}
					}
					break;
				case '\n':
				case '\r':
					result.Length -= GetTrailingWhitespaces (result);
					break;
				default:
					result.Append (ch);
					break;
				}
			}
			if (generateOuterClass)
				result.Append ("}");
			return result.ToString ();
		}

		public static int GetColumn (string wrapper, int i, int tabSize)
		{
			int j = i;
			int col = 0;
			for (; j < wrapper.Length && (wrapper[j] == ' ' || wrapper[j] == '\t'); j++) {
				if (wrapper[j] == ' ') {
					col++;
				} else {
					col = GetNextTabstop (col, tabSize);
				}
			}
			return col;
		}
		
		public override bool SupportsOnTheFlyFormatting {
			get {
				return true;
			}
		}
		
		public override void OnTheFlyFormat (object textEditorData, IType type, IMember member, ProjectDom dom, ICompilationUnit unit, DomLocation caretLocation)
		{
			Format ((TextEditorData)textEditorData, type, member, dom, unit, caretLocation);
		}
		
		public static void Format (TextEditorData data, ProjectDom dom, ICompilationUnit unit, DomLocation caretLocation)
		{
			IType type = NRefactoryResolver.GetTypeAtCursor (unit, unit.FileName, caretLocation);
			if (type == null)
				return;
			Format (data, type, NRefactoryResolver.GetMemberAt (type, caretLocation), dom, unit, caretLocation);
		}
		
		static string GetIndent (string text)
		{
			StringBuilder result = new StringBuilder ();
			foreach (char ch in text) {
				if (!char.IsWhiteSpace (ch))
					break;
				result.Append (ch);
			}
			return result.ToString ();
		}
		
		static string RemoveIndent (string text, string indent)
		{
			Document doc = new Document ();
			doc.Text = text;
			StringBuilder result = new StringBuilder ();
			foreach (LineSegment line in doc.Lines) {
				string curLineIndent = line.GetIndentation (doc);
				int offset = Math.Min (curLineIndent.Length, indent.Length);
				result.Append (doc.GetTextBetween (line.Offset + offset, line.EndOffset));
			}
			return result.ToString ();
		}
		
		static string AddIndent (string text, string indent)
		{
			Document doc = new Document ();
			doc.Text = text;
			StringBuilder result = new StringBuilder ();
			foreach (LineSegment line in doc.Lines) {
				if (result.Length > 0)
					result.Append (indent);
				result.Append (doc.GetTextAt (line));
			}
			return result.ToString ();
		}
		
		static string GetIndent (TextEditorData data, int lineNumber)
		{
			return data.Document.GetLine (lineNumber).GetIndentation (data.Document);
		}
		
		public static void Format (TextEditorData data, IType type, IMember member, ProjectDom dom, ICompilationUnit unit, DomLocation caretLocation)
		{
			if (type == null)
				return;
			if (member == null) {
//				member = type;
				return;
			}
			string wrapper;
			int endPos;
			wrapper = CreateWrapperClassForMember (member, member != type, data, out endPos);
			if (string.IsNullOrEmpty (wrapper) || endPos < 0)
				return;
//			Console.WriteLine (wrapper);
			
			int i = wrapper.IndexOf ('{') + 1;
			int col = GetColumn (wrapper, i, data.Options.TabSize);
			
			CSharpFormatter formatter = new CSharpFormatter ();
			formatter.startIndentLevel = System.Math.Max (0, col / data.Options.TabSize - 1);
			
			string formattedText = formatter.InternalFormat (dom.Project, MimeType, wrapper, 0, wrapper.Length);
			
			if (formatter.hasErrors)
				return;

			int startPos = data.Document.LocationToOffset (member.Location.Line - 1, 0) - 1;
			InFormat = true;
			if (member != type) {
				int len1 = formattedText.IndexOf ('{') + 1;
				int last = formattedText.LastIndexOf ('}');
				formattedText = formattedText.Substring (len1, last - len1 - 1).TrimStart ('\n', '\r');
			} else {
				startPos++;
			}
			
//			Console.WriteLine ("formattedText0:" + formattedText.Replace ("\t", "->").Replace (" ", "°"));
			if (member != type) {
				string indentToRemove = GetIndent (formattedText);
//				Console.WriteLine ("Remove:" + indentToRemove.Replace ("\t", "->").Replace (" ", "°"));
				formattedText = RemoveIndent (formattedText, indentToRemove);
			} else {
				formattedText = formattedText.TrimStart ();
			}
//			Console.WriteLine ("Indent:" + GetIndent (data, member.Location.Line - 1).Replace ("\t", "->").Replace (" ", "°"));
//			Console.WriteLine ("formattedText1:" + formattedText.Replace ("\t", "->").Replace (" ", "°"));
			formattedText = AddIndent (formattedText, GetIndent (data, member.Location.Line - 1));
			
//			Console.WriteLine ("formattedText2:" + formattedText.Replace ("\t", "->").Replace (" ", "°"));
	
			int textLength = CanInsertFormattedText (data, startPos, data.Document.LocationToOffset (caretLocation.Line, caretLocation.Column), formattedText);
			if (textLength > 0) {
//				Console.WriteLine (formattedText.Substring (0, textLength));
				InsertFormattedText (data, startPos, formattedText.Substring (0, textLength).TrimEnd ());
			} else {
				Console.WriteLine ("Can't insert !!!");
			}
			InFormat = false;
		}

		static int CanInsertFormattedText (TextEditorData data, int offset, int endOffset, string formattedText)
		{
			int textOffset = 0;
			endOffset = System.Math.Min (data.Document.Length, endOffset);
			
			while (textOffset < formattedText.Length && offset < endOffset) {
				if (offset < 0) {
					offset++;
					textOffset++;
					continue;
				}
				char ch1 = data.Document.GetCharAt (offset);
				char ch2 = formattedText[textOffset];
				bool ch1Ws = Char.IsWhiteSpace (ch1);
				bool ch2Ws = Char.IsWhiteSpace (ch2);
				//Console.WriteLine ("ch1={0}, ch2={1}", ch1, ch2);
				if (ch1 == ch2) {
					textOffset++;
					offset++;
					continue;
				} else if (ch1 == '\n') {
					// skip Ws
					while (textOffset < formattedText.Length && IsPlainWhitespace (formattedText[textOffset])) {
						textOffset++;
					}

					offset++;
					while (offset < data.Caret.Offset && IsPlainWhitespace (data.Document.GetCharAt (offset))) {
						offset++;
					}
					continue;
				}
				
				if (ch2Ws && !ch1Ws) {
					textOffset++;
					continue;
				}
				if ((!ch2Ws || ch2 == '\n') && ch1Ws) {
					offset++;
					continue;
				}
				
				if (ch1Ws && ch2Ws) {
					textOffset++;
					offset++;
					continue;
				}
				
				return -1;
			}
			return textOffset - 1;
		}

		static void InsertFormattedText (TextEditorData data, int offset, string formattedText)
		{
			data.Document.BeginAtomicUndo ();
//			DocumentLocation caretLocation = data.Caret.Location;

			int selAnchor = data.IsSomethingSelected ? data.Document.LocationToOffset (data.MainSelection.Anchor) : -1;
			int selLead = data.IsSomethingSelected ? data.Document.LocationToOffset (data.MainSelection.Lead) : -1;
			int textOffset = 0;
			int caretOffset = data.Caret.Offset;
			
//			Console.WriteLine ("formattedText:" + formattedText);
			
			while (textOffset < formattedText.Length /*&& offset < caretOffset*/) {
				if (offset < 0) {
					offset++;
					textOffset++;
					continue;
				}
				char ch1 = data.Document.GetCharAt (offset);
				char ch2 = formattedText[textOffset];
//				Console.WriteLine (((int)ch1) + ":" + ch1 + " -- " + ((int)ch2) + ": " + ch2);
				if (ch1 == ch2) {
					textOffset++;
					offset++;
					continue;
				} else if (ch1 == '\n') {
					//LineSegment line = data.Document.GetLineByOffset (offset);
					// skip all white spaces in formatted text - we had a line break
					while (textOffset < formattedText.Length && IsPlainWhitespace (formattedText[textOffset])) {
						textOffset++;
					}
					offset++;
					while (offset < data.Caret.Offset && IsPlainWhitespace (data.Document.GetCharAt (offset))) {
						offset++;
					}
					continue;
				}
				bool ch1Ws = Char.IsWhiteSpace (ch1);
				bool ch2Ws = Char.IsWhiteSpace (ch2);

				if (ch2Ws && !ch1Ws) {
					data.Insert (offset, ch2.ToString ());
					if (offset < caretOffset)
						caretOffset++;
					if (offset < selAnchor)
						selAnchor++;
					if (offset < selLead)
						selLead++;
					textOffset++;
					offset++;
					continue;
				}

				if ((!ch2Ws || ch2 == '\n') && ch1Ws) {
					if (offset < caretOffset)
						caretOffset--;
					if (offset < selAnchor)
						selAnchor--;
					if (offset < selLead)
						selLead--;
					data.Remove (offset, 1);
					continue;
				}
				if (ch1Ws && ch2Ws) {
					data.Replace (offset, 1, ch2.ToString ());

					textOffset++;
					offset++;
					continue;
				}
//				Console.WriteLine ("BAIL OUT");
				break;
			}
			data.Caret.Offset = caretOffset;
			
			if (selAnchor >= 0)
				data.MainSelection = new Selection (data.Document.OffsetToLocation (selAnchor), data.Document.OffsetToLocation (selLead));
			data.Document.EndAtomicUndo ();
		}

		static bool IsPlainWhitespace (char ch)
		{
			return ch == ' ' || ch == '\t';
		}

		public static bool InFormat = false;

		public override bool CanFormat (string mimeType)
		{
			return mimeType == MimeType;
		}

		static int GetNextTabstop (int currentColumn, int tabSize)
		{
			int result = currentColumn + tabSize;
			return (result / tabSize) * tabSize;
		}

		public static void SetFormatOptions (CSharpOutputVisitor outputVisitor, SolutionItem policyParent)
		{
			IEnumerable<string> types = MonoDevelop.Core.Gui.DesktopService.GetMimeTypeInheritanceChain (MimeType);
			TextStylePolicy currentPolicy = policyParent != null ? policyParent.Policies.Get<TextStylePolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> (types);
			CSharpFormattingPolicy codePolicy = policyParent != null ? policyParent.Policies.Get<CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);

			outputVisitor.Options.IndentationChar = currentPolicy.TabsToSpaces ? ' ' : '\t';
			outputVisitor.Options.TabSize = currentPolicy.TabWidth;
			outputVisitor.Options.IndentSize = currentPolicy.TabWidth;

			CodeFormatDescription descr = CSharpFormattingPolicyPanel.CodeFormatDescription;
			Type optionType = outputVisitor.Options.GetType ();

			foreach (CodeFormatOption option in descr.AllOptions) {
				KeyValuePair<string, string> val = descr.GetValue (codePolicy, option);
				PropertyInfo info = optionType.GetProperty (option.Name);
				if (info == null) {
					System.Console.WriteLine ("option : " + option.Name + " not found.");
					continue;
				}
				object cval = null;
				if (info.PropertyType.IsEnum) {
					cval = Enum.Parse (info.PropertyType, val.Key);
				} else if (info.PropertyType == typeof(bool)) {
					cval = Convert.ToBoolean (val.Key);
				} else {
					cval = Convert.ChangeType (val.Key, info.PropertyType);
				}
				//System.Console.WriteLine("set " + option.Name + " to " + cval);
				info.SetValue (outputVisitor.Options, cval, null);
			}
		}
		
		
		bool hasErrors = false;
		int startIndentLevel = 0;
		protected override string InternalFormat (SolutionItem policyParent, string mimeType, string input, int startOffset, int endOffset)
		{
			string text = GetFormattedText (policyParent, input);
			if (startOffset == 0 && endOffset >= input.Length - 1)
				return text;
			int newStartOffset = TranslateOffset (input, text, startOffset);
			int newEndOffset = TranslateOffset (input, text, endOffset);
			if (newStartOffset < 0 || newEndOffset < 0)
				return input.Substring (startOffset, System.Math.Max (0, System.Math.Min (endOffset - startOffset, input.Length - startOffset)));
			
			return text.Substring (newStartOffset, newEndOffset - newStartOffset);
		}

		static int TranslateOffset (string baseInput, string formattedInput, int offset)
		{
			int i = 0;
			int j = 0;
			while (i < baseInput.Length && j < formattedInput.Length && i < offset) {
				char ch1 = baseInput[i];
				char ch2 = formattedInput[j];
				bool ch1IsWs = Char.IsWhiteSpace (ch1);
				bool ch2IsWs = Char.IsWhiteSpace (ch2);
//				Console.WriteLine ("ch1={0}, ch2={1}, ch1IsWs={2}, ch2IsWs={3}", ch1, ch2, ch1IsWs, ch2IsWs);
				if (ch1 == ch2 || ch1IsWs && ch2IsWs) {
					i++;
					j++;
				} else if (!ch1IsWs && ch2IsWs) {
					j++;
				} else if (ch1IsWs && !ch2IsWs) {
					i++;
				} else {
					return -1;
				}
			}
			return j;
		}
		
		string GetFormattedText (SolutionItem policyParent, string input)
		{
			hasErrors = false;
			if (string.IsNullOrEmpty (input))
				return input;
			
			CSharpOutputVisitor outputVisitor = new CSharpOutputVisitor ();
			SetFormatOptions (outputVisitor, policyParent);
			
			outputVisitor.OutputFormatter.IndentationLevel = startIndentLevel;
			using (ICSharpCode.NRefactory.IParser parser = ParserFactory.CreateParser (SupportedLanguage.CSharp, new StringReader (input))) {
				parser.Parse ();
				hasErrors = parser.Errors.Count != 0;
		//				if (hasErrors)
		//					Console.WriteLine (parser.Errors.ErrorOutput);
				IList<ISpecial> specials = parser.Lexer.SpecialTracker.RetrieveSpecials ();
				if (parser.Errors.Count == 0) {
					using (SpecialNodesInserter.Install (specials, outputVisitor)) {
						parser.CompilationUnit.AcceptVisitor (outputVisitor, null);
					}
					return outputVisitor.Text;
				}
			}
			//			Console.WriteLine ("trying to parse block.");
			using (ICSharpCode.NRefactory.IParser parser = ParserFactory.CreateParser (SupportedLanguage.CSharp, new StringReader (input))) {
				BlockStatement blockStatement = parser.ParseBlock ();
				hasErrors = parser.Errors.Count != 0;
				//				if (hasErrors)
				//					Console.WriteLine (parser.Errors.ErrorOutput);
				IList<ISpecial> specials = parser.Lexer.SpecialTracker.RetrieveSpecials ();
				if (parser.Errors.Count == 0) {
					StringBuilder result = new StringBuilder ();
					using (var inserter = SpecialNodesInserter.Install (specials, outputVisitor)) {
						foreach (INode node in blockStatement.Children) {
							node.AcceptVisitor (outputVisitor, null);
							//							result.AppendLine (outputVisitor.Text);
						}
						if (!outputVisitor.OutputFormatter.LastCharacterIsNewLine)
							outputVisitor.OutputFormatter.NewLine ();
						inserter.Finish ();
						result.AppendLine (outputVisitor.Text);
					}
					return result.ToString ();
				}
			}
			//			Console.WriteLine ("trying to parse expression.");
			using (ICSharpCode.NRefactory.IParser parser = ParserFactory.CreateParser (SupportedLanguage.CSharp, new StringReader (input))) {
				Expression expression = parser.ParseExpression ();
				hasErrors = parser.Errors.Count != 0;
				//				if (hasErrors)
				//					Console.WriteLine (parser.Errors.ErrorOutput);
				IList<ISpecial> specials = parser.Lexer.SpecialTracker.RetrieveSpecials ();
				if (parser.Errors.Count == 0) {
					using (SpecialNodesInserter.Install (specials, outputVisitor)) {
						expression.AcceptVisitor (outputVisitor, null);
					}
					return outputVisitor.Text;
				}
			}
			return input;
		}
	}
}
