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
using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.CSharp.Refactoring.CodeIssues
{
	class BaseNRefactoryIssueProvider : BaseCodeIssueProvider
	{
		NRefactoryIssueProvider parentIssue;
		SubIssueAttribute subIssue;
		TimerCounter counter;

		public override CodeIssueProvider Parent {
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

		public BaseNRefactoryIssueProvider (NRefactoryIssueProvider parentIssue, SubIssueAttribute subIssue)
		{
			this.parentIssue = parentIssue;
			this.subIssue = subIssue;
			this.Title = subIssue.Title;
			this.Description = subIssue.Description;

			DefaultSeverity = subIssue.Severity.HasValue ? subIssue.Severity.Value : parentIssue.DefaultSeverity;
			IsEnabledByDefault = subIssue.IsEnabledByDefault.HasValue ? subIssue.IsEnabledByDefault.Value : parentIssue.IsEnabledByDefault;
			UpdateSeverity ();

			counter = InstrumentationService.CreateTimerCounter (IdString, "CodeIssueProvider run times");
		}

		/// <summary>
		/// Gets all the code issues inside a document.
		/// </summary>
		public override IEnumerable<CodeIssue> GetIssues (object ctx, CancellationToken cancellationToken)
		{
			var context = ctx as MDRefactoringContext;
			if (context == null || context.IsInvalid || context.RootNode == null || context.ParsedDocument.HasErrors)
				return new CodeIssue[0];
			// Holds all the actions in a particular sibling group.
			IList<ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue> issues;
			using (var timer = counter.BeginTiming ()) {
				// We need to enumerate here in order to time it. 
				// This shouldn't be a problem since there are current very few (if any) lazy providers.
				var _issues = parentIssue.IssueProvider.GetIssues (context, subIssue.Title);
				issues = _issues as IList<ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue> ?? _issues.ToList ();
			}
			return parentIssue.ToMonoDevelopRepresentation (cancellationToken, context, issues);
		}


	}
}