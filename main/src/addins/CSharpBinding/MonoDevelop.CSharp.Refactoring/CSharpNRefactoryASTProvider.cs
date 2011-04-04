// 
// CSharpNRefactoryASTProvider.cs
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
using MonoDevelop.Refactoring;
using ICSharpCode.OldNRefactory.Ast;
using MonoDevelop.Projects.Dom.Parser;
using ICSharpCode.OldNRefactory.PrettyPrinter;
using ICSharpCode.OldNRefactory;
using System.IO;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Text;
using System.Reflection;
using System.Collections.Generic;
using MonoDevelop.Ide;

namespace MonoDevelop.CSharp.Refactoring
{
	public class CSharpNRefactoryASTProvider : INRefactoryASTProvider
	{
		public bool CanGenerateASTFrom (string mimeType)
		{
			return mimeType == CSharpFormatter.MimeType;
		}

		public string OutputNode (ProjectDom dom, INode node)
		{
			return OutputNode (dom, node, "");
		}
		
		public string OutputNode (ProjectDom dom, INode node, string indent)
		{
			CSharpOutputVisitor outputVisitor = new CSharpOutputVisitor ();
			SetFormatOptions (outputVisitor, dom != null && dom.Project != null ? dom.Project.Policies : null);
			int col = GetColumn (indent, 0, 4);
			outputVisitor.OutputFormatter.IndentationLevel = System.Math.Max (0, col / 4);
			node.AcceptVisitor (outputVisitor, null);
			return outputVisitor.Text;
		}
		
		static void SetFormatOptions (CSharpOutputVisitor outputVisitor, PolicyContainer policyParent)
		{
			IEnumerable<string> types = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			TextStylePolicy currentPolicy = policyParent != null ? policyParent.Get<TextStylePolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> (types);
			CSharpFormattingPolicy codePolicy = policyParent != null ? policyParent.Get<CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);

			outputVisitor.Options.IndentationChar = currentPolicy.TabsToSpaces ? ' ' : '\t';
			outputVisitor.Options.TabSize = currentPolicy.TabWidth;
			outputVisitor.Options.IndentSize = currentPolicy.TabWidth;

			outputVisitor.Options.EolMarker = TextStylePolicy.GetEolMarker(currentPolicy.EolMarker);
			Type optionType = outputVisitor.Options.GetType ();

			foreach (var property in typeof (CSharpFormattingPolicy).GetProperties ()) {
				PropertyInfo info = optionType.GetProperty (property.Name);
				if (info == null) 
					continue;
				object val = property.GetValue (codePolicy, null);
				object cval = null;
				if (info.PropertyType.IsEnum) {
					cval = Enum.Parse (info.PropertyType, val.ToString ());
				} else if (info.PropertyType == typeof(bool)) {
					cval = Convert.ToBoolean (val);
				} else {
					cval = Convert.ChangeType (val, info.PropertyType);
				}
				info.SetValue (outputVisitor.Options, cval, null);
			}
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
		static int GetNextTabstop (int currentColumn, int tabSize)
		{
			int result = currentColumn + tabSize;
			return (result / tabSize) * tabSize;
		}
		
		public ICSharpCode.OldNRefactory.Parser.Errors LastErrors {
			get;
			private set;
		}
		public Expression ParseExpression (string expressionText)
		{
			expressionText = expressionText.Trim ();
			using (ICSharpCode.OldNRefactory.IParser parser = ICSharpCode.OldNRefactory.ParserFactory.CreateParser (SupportedLanguage.CSharp, new StringReader (expressionText))) {
				Expression result = null;
				try {
					result = parser.ParseExpression ();
					LastErrors = parser.Errors;
				} catch (Exception) {
				}
				return result;
			}
		}
		
		public INode ParseText (string text)
		{
			text = text.Trim ();
			using (ICSharpCode.OldNRefactory.IParser parser = ICSharpCode.OldNRefactory.ParserFactory.CreateParser (SupportedLanguage.CSharp, new StringReader (text))) {
				if (text.EndsWith (";") || text.EndsWith ("}")) {
					BlockStatement block = null ;
					try {
						block = parser.ParseBlock ();
						LastErrors = parser.Errors;
					} catch (Exception) {
					}
					if (block != null)
						return block;
				}
				return parser.ParseExpression ();
			}
		}
		
		public CompilationUnit ParseFile (string content)
		{
			using (ICSharpCode.OldNRefactory.IParser parser = ICSharpCode.OldNRefactory.ParserFactory.CreateParser (SupportedLanguage.CSharp, new StringReader (content))) {
				try {
					parser.Parse ();
					LastErrors = parser.Errors;
				} catch (Exception) {
				}
				return parser.CompilationUnit;
			}
		}

		public TypeReference ParseTypeReference (string content)
		{
			content = content.Trim ();
			using (ICSharpCode.OldNRefactory.IParser parser = ICSharpCode.OldNRefactory.ParserFactory.CreateParser (SupportedLanguage.CSharp, new StringReader (content))) {
				try {
					var result = parser.ParseTypeReference ();
					LastErrors = parser.Errors;
					return result;
				} catch (Exception) {
				}
			}
			return null;
		}
	}
}
