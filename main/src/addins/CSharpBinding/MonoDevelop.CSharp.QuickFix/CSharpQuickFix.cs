// 
// CSharpQuickFix.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Ide;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Resolver;

namespace MonoDevelop.CSharp.QuickFix
{
	public abstract class CSharpQuickFix : MonoDevelop.QuickFix.QuickFix
	{
		internal static string GetSingleIndent (Mono.TextEditor.TextEditorData editor)
		{
			return editor.Options.TabsToSpaces ? new string (' ', editor.Options.TabSize) : "\t";
		}
		
		internal static string OutputNode (ProjectDom dom, AstNode node, string indent, Action<int, AstNode> outputStarted = null)
		{
			var policyParent = dom != null && dom.Project != null ? dom.Project.Policies : null;
			IEnumerable<string> types = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			CSharpFormattingPolicy codePolicy = policyParent != null ? policyParent.Get<CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
			var formatter = new StringBuilderOutputFormatter ();
			int col = MonoDevelop.CSharp.Refactoring.CSharpNRefactoryASTProvider.GetColumn (indent, 0, 4);
			formatter.Indentation = 1 + System.Math.Max (0, col / 4);
			OutputVisitor visitor = new OutputVisitor (formatter, codePolicy.CreateOptions ());
			if (outputStarted != null)
				visitor.OutputStarted += (sender, e) => outputStarted (formatter.Length, e.AstNode);
			node.AcceptVisitor (visitor, null);
			return formatter.ToString ().TrimEnd ();
		}
		
		public static NRefactoryResolver GetResolver (MonoDevelop.Ide.Gui.Document doc)
		{
			return new NRefactoryResolver (doc.Dom, doc.CompilationUnit, doc.Editor, doc.FileName); 
		}
	}
}

