// 
// NRefactoryIssueWrapper.cs
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
using System;
using System.Linq;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Ide.Gui;
using System.Threading;
using MonoDevelop.CodeIssues;
using MonoDevelop.CSharp.Refactoring.CodeActions;
using MonoDevelop.Core;
using Mono.TextEditor;

namespace MonoDevelop.CSharp.Refactoring.CodeIssues
{
	class BaseNRefactoryIssueProvider : BaseCodeIssueProvider
	{
		NRefactoryIssueProvider parentIssue;
		SubIssueAttribute subIssue;

		public override BaseCodeIssueProvider Parent {
			get {
				return parentIssue;
			}
		}

		public override string MimeType {
			get {
				return parentIssue.MimeType;
			}
		}

		
		/// <summary>
		/// Gets the identifier string used as property ID tag.
		/// </summary>
		public override string IdString {
			get {
				return parentIssue.IdString + "." + subIssue.Title;
			}
		}

		public override ICSharpCode.NRefactory.Refactoring.IssueMarker IssueMarker {
			get {
				return parentIssue.IssueMarker;
			}
		}

		public BaseNRefactoryIssueProvider (NRefactoryIssueProvider parentIssue, SubIssueAttribute subIssue)
		{
			this.parentIssue = parentIssue;
			this.subIssue = subIssue;
			this.Title = subIssue.Title;
			this.Description = subIssue.Description;

			DefaultSeverity = subIssue.HasOwnSeverity ? subIssue.Severity : parentIssue.DefaultSeverity;
			UpdateSeverity ();
		}

		/// <summary>
		/// Gets all the code issues inside a document.
		/// </summary>
		public override IEnumerable<CodeIssue> GetIssues (object ctx, CancellationToken cancellationToken)
		{
			var context = ctx as MDRefactoringContext;
			if (context == null || context.IsInvalid || context.RootNode == null || context.ParsedDocument.HasErrors)
				yield break;
			foreach (var action in parentIssue.IssueProvider.GetIssues (context, subIssue.Title)) {
				if (cancellationToken.IsCancellationRequested)
					yield break;
				if (action.Actions == null) {
					LoggingService.LogError ("NRefactory actions == null in :" + Title);
					continue;
				}
				var actions = new List<MonoDevelop.CodeActions.CodeAction> ();
				foreach (var act in action.Actions) {
					if (cancellationToken.IsCancellationRequested)
						yield break;
					if (act == null) {
						LoggingService.LogError ("NRefactory issue action was null in :" + Title);
						continue;
					}
					actions.Add (new NRefactoryCodeAction (parentIssue.ProviderIdString, act.Description, act));
				}
				var issue = new CodeIssue (
					GettextCatalog.GetString (action.Description ?? ""),
					context.TextEditor.FileName,
					action.Start,
					action.End,
					IdString,
					actions
				);
				yield return issue;
			}
		}


	}
}
