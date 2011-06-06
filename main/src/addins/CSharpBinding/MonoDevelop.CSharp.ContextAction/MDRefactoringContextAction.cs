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
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.CSharp.ContextAction
{
	public abstract class MDRefactoringContextAction : MonoDevelop.ContextAction.ContextAction
	{
		internal static string GetSingleIndent (Mono.TextEditor.TextEditorData editor)
		{
			return editor.Options.TabsToSpaces ? new string (' ', editor.Options.TabSize) : "\t";
		}
		
		protected virtual string GetMenuText (MDRefactoringContext context)
		{
			return Node.Title;
		}
		
		public sealed override string GetMenuText (MonoDevelop.Ide.Gui.Document document, MonoDevelop.Projects.Dom.DomLocation loc)
		{
			var context = new MDRefactoringContext (document, loc);
			if (context.Unit == null)
				return "invalid";
			return GetMenuText (context);
		}
		
		protected abstract bool IsValid (MDRefactoringContext context);
		
		public sealed override bool IsValid (MonoDevelop.Ide.Gui.Document document, MonoDevelop.Projects.Dom.DomLocation loc)
		{
			var context = new MDRefactoringContext (document, loc);
			if (context.Unit == null)
				return false;
			return IsValid (context);
		}
		
		protected abstract void Run (MDRefactoringContext context);
		
		public sealed override void Run (MonoDevelop.Ide.Gui.Document document, MonoDevelop.Projects.Dom.DomLocation loc)
		{
			var context = new MDRefactoringContext (document, loc);
			if (context.Unit == null)
				return;
			if (!IsValid (context))
				return;
			
			Run (context);
		}
	}
}

