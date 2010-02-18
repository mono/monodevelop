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

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Refactoring;
using MonoDevelop.Refactoring.RefactorImports;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.Refactoring
{
	public class QuickFixHandler : AbstractRefactoringCommandHandler
	{
		protected override void Run (RefactoringOptions options)
		{
			Gtk.Menu menu = new Gtk.Menu ();
			IReturnType returnType = null; 
			INRefactoryASTProvider astProvider = RefactoringService.GetASTProvider (MonoDevelop.Core.Gui.DesktopService.GetMimeTypeForUri (options.Document.FileName));
			if (astProvider != null) 
				returnType = astProvider.ParseTypeReference (options.ResolveResult.ResolvedExpression.Expression).ConvertToReturnType ();
			if (returnType == null)
				returnType = DomReturnType.FromInvariantString (options.ResolveResult.ResolvedExpression.Expression);
			List<string> namespaces = new List<string> (options.Dom.ResolvePossibleNamespaces (returnType));
			if (options.SelectedItem == null || namespaces.Count > 1) {
//					resolveMenu.Text = GettextCatalog.GetString ("Resolve");
				if (options.SelectedItem == null) {
					foreach (string ns in namespaces) {
						Gtk.MenuItem menuItem = new Gtk.MenuItem (string.Format (GettextCatalog.GetString ("Add using '{0}'"), ns));
//						info.Icon = MonoDevelop.Core.Gui.Stock.AddNamespace;
						menuItem.Activated += delegate {
							new CurrentRefactoryOperationsHandler.ResolveNameOperation (options.Dom, options.Document, options.ResolveResult, ns).AddImport ();
						};
						menu.Add (menuItem);
					}
				} else {
					// remove all unused namespaces (for resolving conflicts)
					namespaces.RemoveAll (ns => !options.Document.CompilationUnit.IsNamespaceUsedAt (ns, options.ResolveResult.ResolvedExpression.Region.Start));
				}
				
				foreach (string ns in namespaces) {
					Gtk.MenuItem menuItem = new Gtk.MenuItem (string.Format (GettextCatalog.GetString ("Add '{0}'"), ns));
					menuItem.Activated += delegate {
						new CurrentRefactoryOperationsHandler.ResolveNameOperation (options.Dom, options.Document, options.ResolveResult, ns).ResolveName ();
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

