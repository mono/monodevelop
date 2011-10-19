// 
// QuickFixHandler.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated options.Documentumentation files (the "Software"), to deal
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

using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide;
using System.Linq;

namespace MonoDevelop.Refactoring
{
	public class QuickFixHandler : AbstractRefactoringCommandHandler
	{
		
		protected override void Run (RefactoringOptions options)
		{
			Gtk.Menu menu = new Gtk.Menu ();
			
			bool resolveDirect;
			List<string> namespaces = GetResolveableNamespaces (options, out resolveDirect);
			
			foreach (string ns in namespaces) {
				// remove used namespaces for conflict resolving.
				if (options.Document.CompilationUnit.IsNamespaceUsedAt (ns, options.ResolveResult.ResolvedExpression.Region.Start))
					continue;
				Gtk.MenuItem menuItem = new Gtk.MenuItem (string.Format (GettextCatalog.GetString ("Add using '{0}'"), ns));
				CurrentRefactoryOperationsHandler.ResolveNameOperation resolveNameOperation = new CurrentRefactoryOperationsHandler.ResolveNameOperation (options.Dom, options.Document, options.ResolveResult, ns);
				menuItem.Activated += delegate {
					resolveNameOperation.AddImport ();
				};
				menu.Add (menuItem);
			}
			if (resolveDirect) {
				foreach (string ns in namespaces) {
					Gtk.MenuItem menuItem = new Gtk.MenuItem (string.Format (GettextCatalog.GetString ("Add '{0}'"), ns));
					CurrentRefactoryOperationsHandler.ResolveNameOperation resolveNameOperation = new CurrentRefactoryOperationsHandler.ResolveNameOperation (options.Dom, options.Document, options.ResolveResult, ns);
					menuItem.Activated += delegate {
						resolveNameOperation.ResolveName ();
					};
					menu.Add (menuItem);
				}
			}
			
			if (menu.Children != null && menu.Children.Length > 0) {
				menu.ShowAll ();
				
				ICompletionWidget widget = options.Document.GetContent<ICompletionWidget> ();
				CodeCompletionContext codeCompletionContext = widget.CreateCodeCompletionContext (options.GetTextEditorData ().Caret.Offset);

				menu.Popup (null, null, delegate (Gtk.Menu menu2, out int x, out int y, out bool pushIn) {
					x = codeCompletionContext.TriggerXCoord; 
					y = codeCompletionContext.TriggerYCoord; 
					pushIn = false;
				}, 0, Gtk.Global.CurrentEventTime);
				menu.SelectFirst (true);
			}
		}
	}
}

