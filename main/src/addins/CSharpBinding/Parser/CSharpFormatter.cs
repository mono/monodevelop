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
		const string MimeType = "text/x-csharp";
		
		public CSharpFormatter()
		{
		}

		static string CreateWrapperClassForMember (IMember member, TextEditorData data, out int end)
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
			end = data.Document.BracketMatcher.SearchMatchingBracketForward (data.Document, offset, '}', '{');
			if (end < 0) 
				return ""; 

			result.Append ("class " + member.DeclaringType.Name + " {");
			result.Append (data.Document.GetTextBetween (start, end));
			result.Append ("}");

			return result.ToString ();
		}
		
		
		public CSharpFormatter(TextEditorData data, ProjectDom dom, ICompilationUnit unit, MonoDevelop.Ide.Gui.TextEditor editor, DomLocation caretLocation)
		{
			IType type = NRefactoryResolver.GetTypeAtCursor (unit, unit.FileName, caretLocation);
			if (type == null) 
				return; 

			IMember member = NRefactoryResolver.GetMemberAt (type, caretLocation);
			if (member == null) 
				return; 
			int endPos;
			string wrapper = CreateWrapperClassForMember (member, data, out endPos);
			if (string.IsNullOrEmpty (wrapper) || endPos < 0) 
				return; 

			int i = wrapper.IndexOf ('{') + 1;
			int j = i;
			for (; j < wrapper.Length && Char.IsWhiteSpace (wrapper[j]); j++)
				;
			string indent = wrapper.Substring (i, j - i);
			startIndentLevel = indent.Length - 1;
			//Console.WriteLine ("startIndentLevel:" + startIndentLevel);
			int suffixLen = 2;
			string formattedText = InternalFormat (dom.Project, MimeType, wrapper, 0, wrapper.Length);

			int startLine = member.Location.Line;
			int endLine = member.Location.Line;

			if (!member.BodyRegion.IsEmpty) 
				endLine = member.BodyRegion.End.Line + 1; 

			int startPos = data.Document.LocationToOffset (member.Location.Line - 1, 0);
			InFormat = true;
			data.Document.BeginAtomicUndo ();
			DocumentLocation loc = data.Caret.Location;
			data.Caret.AutoScrollToCaret = false;
			int len1 = formattedText.IndexOf ('{') + 1;
			int last = formattedText.LastIndexOf ('}');
		/*	Console.WriteLine (Environment.StackTrace);
			Console.WriteLine ("start:" + startPos);
			Console.WriteLine ("end:" + endPos);
			Console.WriteLine (data.Document.Length);
			Console.WriteLine ("----");
			
			Console.WriteLine (formattedText);
			Console.WriteLine ("----");
			Console.WriteLine (startPos + " - " + endPos);
			Console.WriteLine ("----");
			Console.WriteLine (formattedText.Substring (len1, last - len1 - 1));*/
			data.Replace (startPos - 1, endPos - startPos, formattedText.Substring (len1, last - len1 - 1));
//			data.Remove (startPos - 1, endPos - startPos);
//			data.Insert (startPos - 1, formattedText.Substring (len1, last - len1 - 1));
			data.Caret.Location = loc;
			data.Caret.AutoScrollToCaret = true;
			data.Document.EndAtomicUndo ();
			InFormat = false;
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

		bool hasErrors = false;
		int startIndentLevel = 0;
		protected override string InternalFormat (SolutionItem policyParent, string mimeType, string input, int startOffset, int endOffset)
		{
			hasErrors = false;
			if (string.IsNullOrEmpty (input)) 
				return input; 

			IEnumerable<string> types = MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeInheritanceChain (mimeType);
			TextStylePolicy currentPolicy = policyParent != null ? policyParent.Policies.Get<TextStylePolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> (types);
			CSharpFormattingPolicy codePolicy = policyParent != null ? policyParent.Policies.Get<CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);

			CSharpOutputVisitor outputVisitor = new CSharpOutputVisitor ();
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
			
			outputVisitor.OutputFormatter.IndentationLevel = startIndentLevel;
			using (ICSharpCode.NRefactory.IParser parser = ParserFactory.CreateParser (SupportedLanguage.CSharp, new StringReader (input))) {
				parser.Parse ();
				hasErrors = parser.Errors.Count != 0;
				if (hasErrors)
					Console.WriteLine (parser.Errors.ErrorOutput);
				IList<ISpecial> specials = parser.Lexer.SpecialTracker.RetrieveSpecials ();
				if (parser.Errors.Count == 0) {
					using (SpecialNodesInserter.Install (specials, outputVisitor)) {
						parser.CompilationUnit.AcceptVisitor (outputVisitor, null);
					}
					return outputVisitor.Text;
				}
			}
			return input;
		}
	}
}
