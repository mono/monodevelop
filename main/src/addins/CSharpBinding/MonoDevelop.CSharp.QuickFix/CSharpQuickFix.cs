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

namespace MonoDevelop.CSharp.QuickFix
{
	public abstract class CSharpQuickFix : MonoDevelop.QuickFix.QuickFix
	{
		protected static string OutputNode (ProjectDom dom, AstNode node, string indent)
		{
			var w = new System.IO.StringWriter ();
			var policyParent = dom != null && dom.Project != null ? dom.Project.Policies : null;
			IEnumerable<string> types = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			CSharpFormattingPolicy codePolicy = policyParent != null ? policyParent.Get<CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
			var formatter = new TextWriterOutputFormatter (w);
			int col = MonoDevelop.CSharp.Refactoring.CSharpNRefactoryASTProvider.GetColumn (indent, 0, 4);
			formatter.Indentation = 1 + System.Math.Max (0, col / 4);
			OutputVisitor visitor = new OutputVisitor (formatter, codePolicy.CreateOptions ());
			node.AcceptVisitor (visitor, null);
			return w.ToString ().TrimEnd ();
		}
	}
}

