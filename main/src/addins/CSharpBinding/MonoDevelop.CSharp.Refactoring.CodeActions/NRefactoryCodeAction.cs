// 
// NRefactoryCodeAction.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using MonoDevelop.Ide.Gui;
using ICSharpCode.NRefactory;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Refactoring.CodeActions
{
	class NRefactoryCodeAction : MonoDevelop.CodeActions.CodeAction
	{
		readonly CodeAction act;
		
		public NRefactoryCodeAction (string id, string title, CodeAction act, object siblingKey = null)
		{
			this.IdString = id;
			this.Title = title;
			this.act = act;
			this.SiblingKey = siblingKey;
			this.Severity = act.Severity;
			this.DocumentRegion = new Mono.TextEditor.DocumentRegion (act.Start, act.End);
		}

		public override void Run (IRefactoringContext context, object script)
		{
			act.Run ((Script) script);
		}

		/// <summary>
		/// All the sibling actions of this action, ie those actions which represent the same kind
		/// of fix. This list includes the current action. 
		/// </summary>
		/// <value>The sibling actions.</value>
		public IList<MonoDevelop.CodeActions.CodeAction> SiblingActions { get; set; }
		
		public override bool SupportsBatchRunning {
			get{
				return SiblingActions != null;// && SiblingActions.Count > 1;
			}
		}
		
		public override void BatchRun (Document document, TextLocation loc)
		{
			base.BatchRun (document, loc);
			var context = MDRefactoringContext.Create (document, loc);
			if (context == null)
				return;
			using (var script = context.StartScript ()) {
				foreach (var action in SiblingActions) {
					context.SetLocation (action.DocumentRegion.Begin);
					action.Run (context, script);
				}
			}
		}
	}
	
}
