
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
using System.Collections.Generic;
using MonoDevelop.Components.Commands;
using System.Linq;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.AnalysisCore.Fixes;
using ICSharpCode.NRefactory.Refactoring;
using MonoDevelop.Ide.Gui;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;
using MonoDevelop.CodeIssues;
using MonoDevelop.AnalysisCore.Gui;
using MonoDevelop.Ide.Tasks;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide;
using Microsoft.CodeAnalysis.CodeActions;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using MonoDevelop.AnalysisCore;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.CodeActions
{

	class CodeActionEditorExtension : TextEditorExtension 
	{
		uint quickFixTimeout;

		const int menuTimeout = 250;
		uint smartTagPopupTimeoutId;
		uint menuCloseTimeoutId;
		Menu codeActionMenu;

		public CodeActionContainer Fixes {
			get;
			private set;
		}

		static CodeActionEditorExtension ()
		{
			var usages = PropertyService.Get<Properties> ("CodeActionUsages", new Properties ());
			foreach (var key in usages.Keys) {
				CodeActionUsages [key] = usages.Get<int> (key);
			}
		}

		void CancelSmartTagPopupTimeout ()
		{

			if (smartTagPopupTimeoutId != 0) {
				GLib.Source.Remove (smartTagPopupTimeoutId);
				smartTagPopupTimeoutId = 0;
			}
		}

		void CancelMenuCloseTimer ()
		{
			if (menuCloseTimeoutId != 0) {
				GLib.Source.Remove (menuCloseTimeoutId);
				menuCloseTimeoutId = 0;
			}
		}
		
		void RemoveWidget ()
		{
			if (currentSmartTag != null) {
				Editor.RemoveMarker (currentSmartTag);
				currentSmartTag = null;
				currentSmartTagBegin = -1;
			}
			CancelSmartTagPopupTimeout ();

		}
		
		public override void Dispose ()
		{
			CancelMenuCloseTimer ();
			CancelQuickFixTimer ();
			Editor.CaretPositionChanged -= HandleCaretPositionChanged;
			Editor.SelectionChanged -= HandleSelectionChanged;
			DocumentContext.DocumentParsed -= HandleDocumentDocumentParsed;
			Editor.BeginMouseHover -= HandleBeginHover;
			RemoveWidget ();
			Fixes = null;
			base.Dispose ();
		}

		static readonly Dictionary<string, int> CodeActionUsages = new Dictionary<string, int> ();

		static void ConfirmUsage (string id)
		{
			if (!CodeActionUsages.ContainsKey (id)) {
				CodeActionUsages [id] = 1;
			} else {
				CodeActionUsages [id]++;
			}
			var usages = PropertyService.Get<Properties> ("CodeActionUsages", new Properties ());
			usages.Set (id, CodeActionUsages [id]);
		}

		internal static int GetUsage (string id)
		{
			int result;
			if (!CodeActionUsages.TryGetValue (id, out result)) 
				return 0;
			return result;
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

		void HandleCaretPositionChanged (object sender, EventArgs e)
		{
			CancelQuickFixTimer ();
			if (AnalysisOptions.EnableFancyFeatures &&  DocumentContext.ParsedDocument != null && !Debugger.DebuggingService.IsDebugging) {
				quickFixCancellationTokenSource = new CancellationTokenSource ();
				var token = quickFixCancellationTokenSource.Token;
				quickFixTimeout = GLib.Timeout.Add (100, delegate {
					var loc = Editor.CaretOffset;
					var ad = DocumentContext.AnalysisDocument;
					if (ad == null) {
						quickFixTimeout = 0;
						return false;
					}

					TextSpan span;
					if (Editor.IsSomethingSelected)
						span = TextSpan.FromBounds (Editor.SelectionRange.Offset, Editor.SelectionRange.EndOffset) ;
					else 
						span = TextSpan.FromBounds (loc, loc + 1);

					var diagnosticsAtCaret =
						Editor.GetTextSegmentMarkersAt (Editor.CaretOffset)
							.OfType<IGenericTextSegmentMarker> ()
							.Select (rm => rm.Tag)
							.OfType<DiagnosticResult> ()
							.Select (dr => dr.Diagnostic)
							.ToList ();
					System.Threading.Tasks.Task.Factory.StartNew (delegate {
						var codeIssueFixes = new List<Tuple<CodeFixDescriptor, CodeAction>> ();
						var diagnosticIds = diagnosticsAtCaret.Select (diagnostic => diagnostic.Id).ToList ();
						foreach (var cfp in CodeDiagnosticService.GetCodeFixDescriptor (CodeRefactoringService.MimeTypeToLanguage(Editor.MimeType))) {
							if (token.IsCancellationRequested)
								return;
							var provider = cfp.GetCodeFixProvider ();
							if (!provider.GetFixableDiagnosticIds ().Any (diagnosticIds.Contains))
								continue;

							List<CodeAction> fixes;
							try {
								fixes = provider.GetFixesAsync (new CodeFixContext (ad, span, diagnosticsAtCaret, token)).Result.ToList ();
							} catch (Exception ex) {
								LoggingService.LogError ("Error while getting refactorings from code fix provider " + cfp.Name, ex); 
								continue;
							}
							foreach (var fix in fixes)
								codeIssueFixes.Add (Tuple.Create (cfp, fix));
						}
						var codeActions = new List<Tuple<CodeRefactoringDescriptor, CodeAction>> ();
						foreach (var action in CodeRefactoringService.GetValidActionsAsync (Editor, DocumentContext, span, token).Result) {
							codeActions.Add (action); 
						}
						Application.Invoke (delegate {
							if (token.IsCancellationRequested)
								return;
							if (codeIssueFixes.Count == 0 && codeActions.Count == 0) {
								RemoveWidget ();
								return;
							}
							CreateSmartTag (new CodeActionContainer (codeIssueFixes, codeActions), loc);
						});
					});

					
//					RefactoringService.QueueQuickFixAnalysis (Document, loc, token, delegate(List<CodeAction> fixes) {
//						if (!fixes.Any ()) {
//							ICSharpCode.NRefactory.Semantics.ResolveResult resolveResult;
//							AstNode node;
//							if (ResolveCommandHandler.ResolveAt (document, out resolveResult, out node, token)) {
//								var possibleNamespaces = ResolveCommandHandler.GetPossibleNamespaces (document, node, ref resolveResult);
//								if (!possibleNamespaces.Any ()) {
//									if (currentSmartTag != null)
//										Application.Invoke (delegate { RemoveWidget (); });
//									return;
//								}
//							} else {
//								if (currentSmartTag != null)
//									Application.Invoke (delegate { RemoveWidget (); });
//								return;
//							}
//						}
//						Application.Invoke (delegate {
//							if (token.IsCancellationRequested)
//								return;
//							CreateSmartTag (fixes, loc);
//						});
//					});
					quickFixTimeout = 0;
					return false;
				});
			} else {
				RemoveWidget ();
			}
		}

		internal static bool IsAnalysisOrErrorFix (Microsoft.CodeAnalysis.CodeActions.CodeAction act)
		{
			return false;
		} 

		void PopupQuickFixMenu (Gdk.EventButton evt, Action<Gtk.Menu> menuAction)
		{
			var menu = new Gtk.Menu ();
			menu.Events |= Gdk.EventMask.AllEventsMask;
			Gtk.Menu fixMenu = menu;
			int items = 0;
			
			var possibleNamespaces = MonoDevelop.Refactoring.ResolveCommandHandler.GetPossibleNamespaces (
					Editor,
					DocumentContext,
					Editor.CaretLocation);
			if (possibleNamespaces.Count > 0) {

				foreach (var t in possibleNamespaces.Where (tp => tp.OnlyAddReference)) {
					var menuItem = new Gtk.MenuItem (t.GetImportText ());
					menuItem.Activated += delegate {
						new ResolveCommandHandler.AddImport (Editor, DocumentContext, possibleNamespaces.ResolveResult, null, t.Reference, true, possibleNamespaces.Node).Run ();
						menu.Destroy ();
					};
					menu.Add (menuItem);
					items++;
				}

				if (possibleNamespaces.AddUsings) {
					foreach (var t in possibleNamespaces.Where (tp => tp.IsAccessibleWithGlobalUsing)) {
						string ns = t.Namespace;
						var reference = t.Reference;
						var menuItem = new Gtk.MenuItem (t.GetImportText ());
						menuItem.Activated += delegate {
							new ResolveCommandHandler.AddImport (Editor, DocumentContext, possibleNamespaces.ResolveResult, ns, reference, true, possibleNamespaces.Node).Run ();
							menu.Destroy ();
						};
						menu.Add (menuItem);
						items++;
					}
				}

				if (possibleNamespaces.AddFullyQualifiedName) {
					foreach (var t in possibleNamespaces) {
						string ns = t.Namespace;
						var reference = t.Reference;
						var node = possibleNamespaces.Node;
						var menuItem = new Gtk.MenuItem (t.GetInsertNamespaceText (Editor.GetTextBetween (node.Span.Start, node.Span.End)));
						menuItem.Activated += delegate {
							new ResolveCommandHandler.AddImport (Editor, DocumentContext, possibleNamespaces.ResolveResult, ns, reference, false, node).Run ();
							menu.Destroy ();
						};
						menu.Add (menuItem);
						items++;
					}
				}

				if (menu.Children.Any () && Fixes != null && !Fixes.IsEmpty) {
					fixMenu = new Gtk.Menu ();
					var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("Quick Fixes"));
					menuItem.Submenu = fixMenu;
					menu.Add (menuItem);
					items++;
				}
			}

			PopulateFixes (fixMenu, ref items);
			if (items == 0) {
				menu.Destroy ();
				return;
			}
//			document.Editor.SuppressTooltips = true;
//			document.Editor.Parent.HideTooltip ();
			if (menuAction != null)
				menuAction (menu);
			menu.ShowAll ();
			menu.SelectFirst (true);	
			menu.Hidden += delegate {
				// document.Editor.SuppressTooltips = false;
			};
 
			var p = Editor.LocationToPoint (Editor.OffsetToLocation (currentSmartTagBegin));
			Gtk.Widget widget = Editor;
			var rect = new Gdk.Rectangle (
				(int)p.X + widget.Allocation.X , 
				(int)p.Y + (int)Editor.LineHeight + widget.Allocation.Y, 0, 0);
			GtkWorkarounds.ShowContextMenu (menu, widget, null, rect);
		}

		void AddFix (Menu menu, ref int mnemonic, CodeAction action)
		{
			var escapedLabel = action.Title.Replace ("_", "__");
			var label = (mnemonic <= 10) ? "_" + (mnemonic++ % 10) + " " + escapedLabel : "  " + escapedLabel;
			var thisInstanceMenuItem = new MenuItem (label);
			thisInstanceMenuItem.Activated += new ContextActionRunner (action, Editor, DocumentContext, currentSmartTagBegin).Run;
			thisInstanceMenuItem.Activated += delegate {
				ConfirmUsage (action.Title);
				menu.Destroy ();
			};
			menu.Add (thisInstanceMenuItem);
		}

		TextSpan GetTextSpan (CodeAction action)
		{
			var nrc = action as NRefactoryCodeAction;
			if (nrc == null)
				return TextSpan.FromBounds (0, 0);
			return nrc.TextSpan;
		}

		public void PopulateFixes (Menu menu, ref int items)
		{
			int mnemonic = 1;

			foreach (var fix in Fixes.CodeRefactoringActions) {
				AddFix (menu, ref mnemonic, fix.Item2);
				items++;
			}

			if (Fixes.CodeRefactoringActions.Count > 0 && Fixes.CodeDiagnosticActions.Count > 0) {
				menu.Add (new SeparatorMenuItem ());
			}

			foreach (var fix in Fixes.CodeDiagnosticActions) {
				AddFix (menu, ref mnemonic, fix.Item2);
				items++;
			}

			if (Fixes.CodeDiagnosticActions.Count > 0) {
				menu.Add (new SeparatorMenuItem ());
			}

			foreach (var fix_ in Fixes.CodeDiagnosticActions) {
				var fix = fix_;
				var subMenu = new Menu ();

				var inspector = fix.Item1.GetCodeDiagnosticDescriptor (null);
				if (inspector.CanSuppressWithAttribute) {
					var menuItem = new MenuItem (GettextCatalog.GetString ("_Suppress with attribute"));
					menuItem.Activated += delegate {
						inspector.SuppressWithAttribute (Editor, DocumentContext, GetTextSpan (fix.Item2)); 
					};
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableWithPragma) {
					var menuItem = new MenuItem (GettextCatalog.GetString ("_Suppress with #pragma"));
					menuItem.Activated += delegate {
						inspector.DisableWithPragma (Editor, DocumentContext, GetTextSpan (fix.Item2)); 
					};
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableOnce) {
					var menuItem = new MenuItem (GettextCatalog.GetString ("_Disable Once"));
					menuItem.Activated += delegate {
						inspector.DisableOnce (Editor, DocumentContext, GetTextSpan (fix.Item2)); 
					};
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableAndRestore) {
					var menuItem = new MenuItem (GettextCatalog.GetString ("Disable _and Restore"));
					menuItem.Activated += delegate {
						inspector.DisableAndRestore (Editor, DocumentContext, GetTextSpan (fix.Item2)); 
					};
					subMenu.Add (menuItem);
				}
				var label = GettextCatalog.GetString ("_Options for \"{0}\"", inspector.Name);
				var subMenuItem = new MenuItem (label);

				var optionsMenuItem = new MenuItem (GettextCatalog.GetString ("_Configure Rule"));
				optionsMenuItem.Activated += delegate {
					IdeApp.Workbench.ShowGlobalPreferencesDialog (null, "CodeIssuePanel", dialog => {
						var panel = dialog.GetPanel<CodeIssuePanel> ("CodeIssuePanel");
						if (panel == null)
							return;
						panel.Widget.SelectCodeIssue (inspector.IdString);
					});
				};

				optionsMenuItem.Activated += delegate {
					menu.Destroy ();
				};
				subMenu.Add (optionsMenuItem);
				subMenuItem.Submenu = subMenu;
				menu.Add (subMenuItem);
				items++;
			}

		}

		class ContextActionRunner
		{
			readonly CodeAction act;
			TextEditor editor;
			DocumentContext documentContext;
			int loc;

			public ContextActionRunner (CodeAction act, TextEditor editor, DocumentContext documentContext, int loc)
			{
				this.editor = editor;
				this.act = act;
				this.documentContext = documentContext;
				this.loc = loc;
			}

			public void Run (object sender, EventArgs e)
			{
				var token = default(CancellationToken);
				foreach (var op in act.GetOperationsAsync (token).Result) {
					op.Apply (documentContext.RoslynWorkspace, token); 
				}
			}

			public void BatchRun (object sender, EventArgs e)
			{
				//act.BatchRun (editor, documentContext, loc);
			}
		}

		ISmartTagMarker currentSmartTag;
		int currentSmartTagBegin;

		void CreateSmartTag (CodeActionContainer fixes, int offset)
		{
			Fixes = fixes;
			if (!AnalysisOptions.EnableFancyFeatures) {
				RemoveWidget ();
				return;
			}
			var editor = Editor;
			if (editor == null) {
				RemoveWidget ();
				return;
			}
			if (DocumentContext.ParsedDocument == null || DocumentContext.ParsedDocument.IsInvalid) {
				RemoveWidget ();
				return;
			}

//			var container = editor.Parent;
//			if (container == null) {
//				RemoveWidget ();
//				return;
//			}
			bool first = true;
			var smartTagLocBegin = offset;
			foreach (var fix in fixes.CodeDiagnosticActions.Select (i => i.Item2).Concat (fixes.CodeRefactoringActions.Select (i => i.Item2))) {
				var textSpan = GetTextSpan (fix);
				if (textSpan.IsEmpty)
					continue;
				if (first || offset < textSpan.Start) {
					smartTagLocBegin = textSpan.Start;
				}
				first = false;
			}
//			if (smartTagLocBegin.Line != loc.Line)
//				smartTagLocBegin = new DocumentLocation (loc.Line, 1);
			// got no fix location -> try to search word start
//			if (first) {
//				int offset = document.Editor.LocationToOffset (smartTagLocBegin);
//				while (offset > 0) {
//					char ch = document.Editor.GetCharAt (offset - 1);
//					if (!char.IsLetterOrDigit (ch) && ch != '_')
//						break;
//					offset--;
//				}
//				smartTagLocBegin = document.Editor.OffsetToLocation (offset);
//			}

			if (currentSmartTag != null && currentSmartTagBegin == smartTagLocBegin) {
				return;
			}
			RemoveWidget ();
			currentSmartTagBegin = smartTagLocBegin;
			var realLoc  = Editor.OffsetToLocation (smartTagLocBegin);

			currentSmartTag = TextMarkerFactory.CreateSmartTagMarker (Editor, smartTagLocBegin, realLoc); 
			currentSmartTag.Tag = fixes;
			editor.AddMarker (currentSmartTag);
		}

		protected override void Initialize ()
		{
			base.Initialize ();
//			document.DocumenInitializetParsed += HandleDocumentDocumentParsed;
//			document.Editor.SelectionChanged += HandleSelectionChanged;
			Editor.BeginMouseHover += HandleBeginHover;
			Editor.CaretPositionChanged += HandleCaretPositionChanged;
		}

		void HandleBeginHover (object sender, EventArgs e)
		{
			CancelSmartTagPopupTimeout ();
			CancelMenuCloseTimer ();
		}

		void StartMenuCloseTimer ()
		{
			CancelMenuCloseTimer ();
			menuCloseTimeoutId = GLib.Timeout.Add (menuTimeout, delegate {
				if (codeActionMenu != null) {
					codeActionMenu.Destroy ();
					codeActionMenu = null;
				}
				menuCloseTimeoutId = 0;
				return false;
			});
		}

		void HandleSelectionChanged (object sender, EventArgs e)
		{
			HandleCaretPositionChanged (null, EventArgs.Empty);
		}

		
		
		void HandleDocumentDocumentParsed (object sender, EventArgs e)
		{
			HandleCaretPositionChanged (null, EventArgs.Empty);
		}
		
		[CommandUpdateHandler(RefactoryCommands.QuickFix)]
		public void UpdateQuickFixCommand (CommandInfo ci)
		{
			if (AnalysisOptions.EnableFancyFeatures) {
				ci.Enabled = currentSmartTag != null;
			} else {
				ci.Enabled = true;
			}
		}

		void CurrentSmartTagPopup ()
		{
			CancelSmartTagPopupTimeout ();
			smartTagPopupTimeoutId = GLib.Timeout.Add (menuTimeout, delegate {
				PopupQuickFixMenu (null, menu => {
					codeActionMenu = menu;
					menu.MotionNotifyEvent += (o, args) => {
						if (currentSmartTag.IsInsideWindow (args)) {
							StartMenuCloseTimer ();
						} else {
							CancelMenuCloseTimer ();
						}
					};
				});
				smartTagPopupTimeoutId = 0;
				return false;
			});
		}
		
		[CommandHandler(RefactoryCommands.QuickFix)]
		void OnQuickFixCommand ()
		{
			if (!AnalysisOptions.EnableFancyFeatures) {
				//Fixes = RefactoringService.GetValidActions (Editor, DocumentContext, Editor.CaretLocation).Result;
				currentSmartTagBegin = Editor.CaretOffset;
				PopupQuickFixMenu (null, null); 

				return;
			}
			if (currentSmartTag == null)
				return;
			CurrentSmartTagPopup ();
		}


		internal CodeActionContainer GetCurrentFixes ()
		{
			if (currentSmartTag == null)
				return CodeActionContainer.Empty;
			return (CodeActionContainer)currentSmartTag.Tag;
		}
	}
}

