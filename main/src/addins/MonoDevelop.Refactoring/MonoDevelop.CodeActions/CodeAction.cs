// 
// Result.cs
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
using ICSharpCode.NRefactory;
using System;
using Mono.TextEditor;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.Refactoring;

namespace MonoDevelop.CodeActions
{
	/// <summary>
	/// A code action represents a menu entry that does edit operation in one document.
	/// </summary>
	public abstract class CodeAction
	{
		/// <summary>
		/// Gets or sets the menu item text.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// Gets or sets the id string. The id is used to identify a specific code action.
		/// </summary>
		public string IdString { get; set; }

		/// <summary>
		/// Gets or sets the code issue this action is bound to. 
		/// This allows to split the action and the issue provider.
		/// </summary>
		public Type BoundToIssue { get; set; }

		/// <summary>
		/// The region of the code action.
		/// </summary>
		public DocumentRegion DocumentRegion { get; set; }
		
		/// <summary>
		/// Gets or sets the type of the inspector that generated this action.
		/// </summary>
		/// <remarks>
		/// While this looks the same as <see cref="BoundToIssue"/>, this is not the case.
		/// BoundToIssue is used when an Action has been explicitly bound to an inspector,
		/// while this property holds the type of the inspector that generated the action.
		/// </remarks>
		/// <value>The type of the inspector.</value>
		public Type InspectorType { get; set; }
		
		/// <summary>
		/// Gets or sets the sibling key.
		/// </summary>
		/// <value>The sibling key.</value>
		public object SiblingKey { get; set; }

		/// <summary>
		/// Gets or sets the severity of the code action.
		/// </summary>
		/// <value>The severity.</value>
		public Severity Severity { get; set; }

		protected CodeAction ()
		{
			IdString = GetType ().FullName;
		}

		/// <summary>
		/// Performs the specified code action in document at loc.
		/// </summary>
		public abstract void Run (object context, object script);

		/// <summary>
		/// True if <see cref="BatchRun"/> can be used on the current instance.
		/// </summary>
		/// <value><c>true</c> if supports batch running; otherwise, <c>false</c>.</value>
		public virtual bool SupportsBatchRunning {
			get{
				return false;
			}
		}
		
		public virtual void BatchRun (MonoDevelop.Ide.Gui.Document document, TextLocation loc)
		{
			if (!SupportsBatchRunning) {
				throw new InvalidOperationException ("Batch running is not supported.");
			}
		}
	}

	public class DefaultCodeAction : CodeAction
	{
		public Action<RefactoringContext, Script> act;

		public DefaultCodeAction (string title, Action<RefactoringContext, Script> act)
		{
			Title = title;
			this.act = act;
		}

		public override void Run (object context, object script)
		{
			act ((RefactoringContext)context, (Script)script);
		}
	}

}
