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

namespace MonoDevelop.CSharp.Formatting
{
	public class OnTheFlyFormatter
	{
		public static void Format (TextEditorData data, ProjectDom dom, DomLocation location)
		{
			Format (data, dom, location, false);
		}
		public static void Format (TextEditorData data, ProjectDom dom, DomLocation location, bool correctBlankLines)
		{
			CSharp.Dom.CompilationUnit compilationUnit = new MonoDevelop.CSharp.Parser.CSharpParser ().Parse (data);
			IEnumerable<string> types = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			CSharpFormattingPolicy policy = dom != null && dom.Project.Policies != null ? dom.Project.Policies.Get<CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
			DomSpacingVisitor domSpacingVisitor = new DomSpacingVisitor (policy, data);
			domSpacingVisitor.AutoAcceptChanges = false;
			compilationUnit.AcceptVisitor (domSpacingVisitor, null);
			
			DomIndentationVisitor domIndentationVisitor = new DomIndentationVisitor (policy, data);
			domIndentationVisitor.AutoAcceptChanges = false;
			domIndentationVisitor.CorrectBlankLines = correctBlankLines;
			compilationUnit.AcceptVisitor (domIndentationVisitor, null);
			
			List<Change> changes = new List<Change> ();
			changes.AddRange (domSpacingVisitor.Changes);
			changes.AddRange (domIndentationVisitor.Changes);
			RefactoringService.AcceptChanges (null, null, changes);
		}
		
		public static void Format (TextEditorData data, ProjectDom dom)
		{
			CSharp.Dom.CompilationUnit compilationUnit = new MonoDevelop.CSharp.Parser.CSharpParser ().Parse (data);
			IEnumerable<string> types = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			CSharpFormattingPolicy policy = dom.Project.Policies != null ? dom.Project.Policies.Get<CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
			DomSpacingVisitor domSpacingVisitor = new DomSpacingVisitor (policy, data);
			domSpacingVisitor.AutoAcceptChanges = false;
			compilationUnit.AcceptVisitor (domSpacingVisitor, null);
			
			DomIndentationVisitor domIndentationVisitor = new DomIndentationVisitor (policy, data);
			domIndentationVisitor.AutoAcceptChanges = false;
			compilationUnit.AcceptVisitor (domIndentationVisitor, null);
			
			List<Change> changes = new List<Change> ();
			changes.AddRange (domSpacingVisitor.Changes);
			changes.AddRange (domIndentationVisitor.Changes);
			RefactoringService.AcceptChanges (null, null, changes);
		}
	}
}