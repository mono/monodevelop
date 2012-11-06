// 
// QuickFixEditorExtension.cs
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
using MonoDevelop.Ide.Gui.Content;
using Gtk;
using Mono.TextEditor;
using System.Collections.Generic;
using MonoDevelop.Components.Commands;
using MonoDevelop.SourceEditor.QuickTasks;
using System.Linq;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory;
using System.Threading;

namespace MonoDevelop.CodeActions
{
	class CodeActionEditorExtension : TextEditorExtension 
	{
		CodeActionWidget widget;
		uint quickFixTimeout;
		
		public IEnumerable<CodeAction> Fixes {
			get;
			private set;
		}
		
		void RemoveWidget ()
		{
			/*
			if (widget == null)
				return;
			widget.Hide ();*/
			if (widget != null) {
				widget.Destroy ();
				widget = null;
			}
		}
		
		public override void Dispose ()
		{
			CancelQuickFixTimer ();
			document.Editor.SelectionChanged -= HandleSelectionChanged;
			document.DocumentParsed -= HandleDocumentDocumentParsed;
			if (widget != null) {
				widget.Destroy ();
				widget = null;
			}
			base.Dispose ();
		}
		
		void CreateWidget (IEnumerable<CodeAction> fixes, TextLocation loc)
		{
			Fixes = fixes;
			if (!QuickTaskStrip.EnableFancyFeatures) {
				RemoveWidget ();
				return;
			}
			var editor = document.Editor;
			if (editor == null || editor.Parent == null || !editor.Parent.IsRealized) {
				RemoveWidget ();
				return;
			}
			if (document.ParsedDocument == null || document.ParsedDocument.IsInvalid) {
				RemoveWidget ();
				return;
			}

			var container = editor.Parent;
			if (container == null) {
				RemoveWidget ();
				return;
			}
			if (widget == null) {
				widget = new CodeActionWidget (this, Document);
				container.AddTopLevelWidget (widget,
					2 + (int)editor.Parent.TextViewMargin.XOffset,
					-2 + (int)editor.Parent.LineToY (document.Editor.Caret.Line));
				widget.Show ();
			} else {
				if (!widget.Visible)
					widget.Show ();
				container.MoveTopLevelWidget (widget,
					2 + (int)editor.Parent.TextViewMargin.XOffset,
					-2 + (int)editor.Parent.LineToY (document.Editor.Caret.Line));
			}
			widget.SetFixes (fixes, loc);
		}

		public void CancelQuickFixTimer ()
		{
			if (quickFixCancellationTokenSource != null)
				quickFixCancellationTokenSource.Cancel ();
			if (quickFixTimeout != 0) {
				GLib.Source.Remove (quickFixTimeout);
				quickFixTimeout = 0;
			}
		}

		CancellationTokenSource quickFixCancellationTokenSource;

		public override void CursorPositionChanged ()
		{
			CancelQuickFixTimer ();
			if (QuickTaskStrip.EnableFancyFeatures &&  Document.ParsedDocument != null && !Debugger.DebuggingService.IsDebugging) {
				quickFixCancellationTokenSource = new CancellationTokenSource ();
				var token = quickFixCancellationTokenSource.Token;
				quickFixTimeout = GLib.Timeout.Add (100, delegate {
					var loc = Document.Editor.Caret.Location;
					RefactoringService.QueueQuickFixAnalysis (Document, loc, token, delegate(List<CodeAction> fixes) {
						if (!fixes.Any ()) {
							ICSharpCode.NRefactory.Semantics.ResolveResult resolveResult;
							ICSharpCode.NRefactory.CSharp.AstNode node;
							if (ResolveCommandHandler.ResolveAt (document, out resolveResult, out node, token)) {
								var possibleNamespaces = ResolveCommandHandler.GetPossibleNamespaces (document, node, ref resolveResult);
								if (!possibleNamespaces.Any ()) {
									if (widget != null)
										Application.Invoke (delegate { RemoveWidget (); });
									return;
								}
							} else {
								if (widget != null)
									Application.Invoke (delegate { RemoveWidget (); });
								return;
							}
						}
						Application.Invoke (delegate {
							if (token.IsCancellationRequested)
								return;
							CreateWidget (fixes, loc);
							quickFixTimeout = 0;
						});
					});
					return false;
				});
			} else {
				RemoveWidget ();
			}
			base.CursorPositionChanged ();
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			document.DocumentParsed += HandleDocumentDocumentParsed;
			document.Editor.SelectionChanged += HandleSelectionChanged;
		}

		void HandleSelectionChanged (object sender, EventArgs e)
		{
			CursorPositionChanged ();
		}
		
		void HandleDocumentDocumentParsed (object sender, EventArgs e)
		{
			CursorPositionChanged ();
		}
		
		[CommandUpdateHandler(RefactoryCommands.QuickFix)]
		public void UpdateQuickFixCommand (CommandInfo ci)
		{
			if (QuickTaskStrip.EnableFancyFeatures) {
				ci.Enabled = widget != null && widget.Visible;
			} else {
				ci.Enabled = true;
			}
		}
		
		[CommandHandler(RefactoryCommands.QuickFix)]
		void OnQuickFixCommand ()
		{

			if (!QuickTaskStrip.EnableFancyFeatures) {
				var w = new CodeActionWidget (this, Document);
				w.SetFixes (RefactoringService.GetValidActions (Document, Document.Editor.Caret.Location).Result, Document.Editor.Caret.Location);
				w.PopupQuickFixMenu ();
				w.Destroy ();
				return;
			}
			if (widget == null || !widget.Visible)
				return;
			widget.PopupQuickFixMenu ();
		}
	}
}

