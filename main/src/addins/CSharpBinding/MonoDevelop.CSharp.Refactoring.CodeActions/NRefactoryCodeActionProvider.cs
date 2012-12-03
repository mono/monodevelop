// 
// NRefactoryContextActionWrapper.cs
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using MonoDevelop.Ide.Gui;
using ICSharpCode.NRefactory;
using System.Threading;
using MonoDevelop.Refactoring;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.Refactoring.CodeActions
{
	class NRefactoryCodeActionProvider : MonoDevelop.CodeActions.CodeActionProvider
	{
		readonly List<string> actionId = new List<string> ();
		readonly ICodeActionProvider provider;

		public NRefactoryCodeActionProvider (ICodeActionProvider provider, ContextActionAttribute attr)
		{
			if (provider == null)
				throw new System.ArgumentNullException ("provider");
			if (attr == null)
				throw new System.ArgumentNullException ("attr");
			this.provider = provider;
			Title = GettextCatalog.GetString (attr.Title ?? "");
			Description = GettextCatalog.GetString (attr.Description ?? "");
			Category = GettextCatalog.GetString (attr.Category ?? "");
			MimeType = "text/x-csharp";
			BoundToIssue = attr.BoundToIssue;
		}

		public override IEnumerable<MonoDevelop.CodeActions.CodeAction> GetActions (MonoDevelop.Ide.Gui.Document document, object _context, TextLocation loc, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;
			var context = (MDRefactoringContext)_context;
			if (context.IsInvalid || context.RootNode == null)
				yield break;
			var actions = provider.GetActions (context);
			if (actions == null) {
				LoggingService.LogWarning (provider + " returned null actions.");
				yield break;
			}
			int num = 0;
			foreach (var action_ in actions) {
				var action = action_;
				if (actionId.Count <= num) {
					actionId.Add (provider.GetType ().FullName + "'" + num);
				}
				yield return new NRefactoryCodeAction (actionId[num], GettextCatalog.GetString (action.Description ?? ""), action) {
					BoundToIssue = this.BoundToIssue
				};
				num++;
			}
		}

		public override string IdString {
			get {
				return provider.GetType ().FullName;
			}
		}

	}
}
