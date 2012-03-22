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
using ICSharpCode.NRefactory.CSharp.Refactoring;
using MonoDevelop.Ide.Gui;
using ICSharpCode.NRefactory;
using System.Threading;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp.Refactoring.CodeActions
{
	public class MDRefactoringContextAction : MonoDevelop.CodeActions.CodeAction
	{
		readonly Action<MDRefactoringContext> act;
		
		public MDRefactoringContextAction (string title, Action<MDRefactoringContext> act)
		{
			this.Title = title;
			this.act = act;
		}

		public override void Run (Document document, TextLocation loc)
		{
			var context = new MDRefactoringContext (document, loc);
			act (context);
		}
	}

	class NRefactoryContextActionWrapper : MonoDevelop.CodeActions.CodeActionProvider
	{
		ICSharpCode.NRefactory.CSharp.Refactoring.ICodeActionProvider provider;

		public NRefactoryContextActionWrapper (ICodeActionProvider provider, ContextActionAttribute attr)
		{
			this.provider = provider;
			this.Title = attr.Title;
			this.Description = attr.Description;
			this.Category = attr.Category;
			this.MimeType = "text/x-csharp";
		}

		public override IEnumerable<MonoDevelop.CodeActions.CodeAction> GetActions (MonoDevelop.Ide.Gui.Document document, TextLocation loc, CancellationToken cancellationToken)
		{
			var context = new MDRefactoringContext (document, loc);
			foreach (var action in provider.GetActions (context)) {
				yield return new MDRefactoringContextAction (action.Description, ctx => {
					using (var script = ctx.StartScript ())
						action.Run (script);
				});
			}
		}
	}

	public abstract class MDRefactoringContextActionProvider : MonoDevelop.CodeActions.CodeActionProvider
	{
		static MDRefactoringContextActionProvider ()
		{
			foreach (var t in typeof (ICSharpCode.NRefactory.CSharp.Refactoring.ICodeActionProvider).Assembly.GetTypes ()) {
				var attr = t.GetCustomAttributes (typeof(ContextActionAttribute), false);
				if (attr == null || attr.Length != 1)
					continue;
				RefactoringService.AddProvider (new NRefactoryContextActionWrapper ((ICSharpCode.NRefactory.CSharp.Refactoring.ICodeActionProvider)Activator.CreateInstance (t), (ContextActionAttribute)attr [0]));

			}
		}

		protected abstract IEnumerable<MonoDevelop.CodeActions.CodeAction> GetActions (MDRefactoringContext context);
		
		public override IEnumerable<MonoDevelop.CodeActions.CodeAction> GetActions (MonoDevelop.Ide.Gui.Document document, TextLocation loc, CancellationToken cancellationToken)
		{
			var context = new MDRefactoringContext (document, loc);
			return GetActions (context);
		}
	}
}

