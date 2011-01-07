// 
// OnTheFlyFormatter.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

using Mono.CSharp;
using Mono.TextEditor;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.Ide;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Refactoring;
using System;
using System.Collections.Generic;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.CSharp.Formatting
{
	public class OnTheFlyFormatter
	{
		public static void Format (TextEditorData data, ProjectDom dom)
		{
			Format (data, dom, DomLocation.Empty, false);
		}
		
		public static void Format (TextEditorData data, ProjectDom dom, DomLocation location)
		{
			Format (data, dom, location, false);
		}
		
		public static void Format (TextEditorData data, ProjectDom dom, DomLocation location, bool correctBlankLines)
		{
			PolicyContainer policyParent = dom != null && dom.Project != null? dom.Project.Policies
				: PolicyService.DefaultPolicies;
			var mimeTypeChain = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			Format (policyParent, mimeTypeChain, data, dom, location, correctBlankLines);
		}
		
		public static void Format (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain,
			TextEditorData data, ProjectDom dom, DomLocation location, bool correctBlankLines)
		{
			var compilationUnit = new MonoDevelop.CSharp.Parser.CSharpParser ().Parse (data);
			var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
			var domSpacingVisitor = new DomSpacingVisitor (policy, data) {
				AutoAcceptChanges = false,
			};
			compilationUnit.AcceptVisitor (domSpacingVisitor, null);
			
			var domIndentationVisitor = new DomIndentationVisitor (policy, data) {
				AutoAcceptChanges = false,
			};
			domIndentationVisitor.CorrectBlankLines = correctBlankLines;
			compilationUnit.AcceptVisitor (domIndentationVisitor, null);
			
			var changes = new List<Change> ();
			changes.AddRange (domSpacingVisitor.Changes);
			changes.AddRange (domIndentationVisitor.Changes);
			RefactoringService.AcceptChanges (null, null, changes);
		}
	}
}