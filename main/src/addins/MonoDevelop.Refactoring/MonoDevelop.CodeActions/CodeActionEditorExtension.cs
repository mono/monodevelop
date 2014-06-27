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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using MonoDevelop.AnalysisCore.Fixes;
using ICSharpCode.NRefactory.Refactoring;
using MonoDevelop.Ide.Gui;
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

		public IEnumerable<CodeAction> Fixes {
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
				currentSmartTagBegin = MonoDevelop.Ide.Editor.DocumentLocation.Empty;
			}
			CancelSmartTagPopupTimeout ();

		}
		
		public override void Dispose ()
		{
			CancelMenuCloseTimer ();
			CancelQuickFixTimer ();
			Editor.CaretPositionChanged -= HandleCaretPositionChanged;
			Editor.SelectionChanged -= HandleSelectionChanged;
			Document.DocumentParsed -= HandleDocumentDocumentParsed;
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
			if (AnalysisOptions.EnableFancyFeatures &&  Document.ParsedDocument != null && !Debugger.DebuggingService.IsDebugging) {
				quickFixCancellationTokenSource = new CancellationTokenSource ();
				var token = quickFixCancellationTokenSource.Token;
				quickFixTimeout = GLib.Timeout.Add (100, delegate {
					var loc = Document.Editor.CaretLocation;
					RefactoringService.QueueQuickFixAnalysis (Document, loc, token, delegate(List<CodeAction> fixes) {
						if (!fixes.Any ()) {
							ICSharpCode.NRefactory.Semantics.ResolveResult resolveResult;
							AstNode node;
							if (ResolveCommandHandler.ResolveAt (Document, out resolveResult, out node, token)) {
								var possibleNamespaces = ResolveCommandHandler.GetPossibleNamespaces (Document, node, ref resolveResult);
								if (!possibleNamespaces.Any ()) {
									if (currentSmartTag != null)
										Application.Invoke (delegate { RemoveWidget (); });
									return;
								}
							} else {
								if (currentSmartTag != null)
									Application.Invoke (delegate { RemoveWidget (); });
								return;
							}
						}
						Application.Invoke (delegate {
							if (token.IsCancellationRequested)
								return;
							CreateSmartTag (fixes, loc);
						});
					});
					quickFixTimeout = 0;
					return false;
				});
			} else {
				RemoveWidget ();
			}
		}

		internal static bool IsAnalysisOrErrorFix (CodeAction act)
		{
			return act is AnalysisContextActionProvider.AnalysisCodeAction || act.Severity == Severity.Error;
		} 

		void PopupQuickFixMenu (Gdk.EventButton evt, Action<Gtk.Menu> menuAction)
		{
			var menu = new Gtk.Menu ();
			menu.Events |= Gdk.EventMask.AllEventsMask;
			Gtk.Menu fixMenu = menu;
			ResolveResult resolveResult;
			ICSharpCode.NRefactory.CSharp.AstNode node;
			int items = 0;
			if (ResolveCommandHandler.ResolveAt (Document, out resolveResult, out node)) {
				var possibleNamespaces = MonoDevelop.Refactoring.ResolveCommandHandler.GetPossibleNamespaces (
					Document,
					node,
					ref resolveResult
				);

				foreach (var t in possibleNamespaces.Where (tp => tp.OnlyAddReference)) {
					var menuItem = new Gtk.MenuItem (t.GetImportText ());
					menuItem.Activated += delegate {
						new ResolveCommandHandler.AddImport (Document, resolveResult, null, t.Reference, true, node).Run ();
						menu.Destroy ();
					};
					menu.Add (menuItem);
					items++;
				}

				bool addUsing = !(resolveResult is AmbiguousTypeResolveResult);
				if (addUsing) {
					foreach (var t in possibleNamespaces.Where (tp => tp.IsAccessibleWithGlobalUsing)) {
						string ns = t.Namespace;
						var reference = t.Reference;
						var menuItem = new Gtk.MenuItem (t.GetImportText ());
						menuItem.Activated += delegate {
							new ResolveCommandHandler.AddImport (Document, resolveResult, ns, reference, true, node).Run ();
							menu.Destroy ();
						};
						menu.Add (menuItem);
						items++;
					}
				}

				bool resolveDirect = !(resolveResult is UnknownMemberResolveResult);
				if (resolveDirect) {
					foreach (var t in possibleNamespaces) {
						string ns = t.Namespace;
						var reference = t.Reference;
						var menuItem = new Gtk.MenuItem (t.GetInsertNamespaceText (Editor.GetTextBetween (node.StartLocation, node.EndLocation)));
						menuItem.Activated += delegate {
							new ResolveCommandHandler.AddImport (Document, resolveResult, ns, reference, false, node).Run ();
							menu.Destroy ();
						};
						menu.Add (menuItem);
						items++;
					}
				}

				if (menu.Children.Any () && Fixes.Any ()) {
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
			var container = Editor;

			var p = container.LocationToPoint (currentSmartTagBegin);
			var widget = container.GetGtkWidget ();
			var rect = new Gdk.Rectangle (
				(int)p.X + widget.Allocation.X , 
				(int)p.Y + (int)Editor.LineHeight + widget.Allocation.Y, 0, 0);
			GtkWorkarounds.ShowContextMenu (menu, widget, null, rect);
		}

		public void PopulateFixes (Gtk.Menu menu, ref int items)
		{
			int mnemonic = 1;
			bool gotImportantFix = false, addedSeparator = false;
			var fixesAdded = new List<string> ();
			foreach (var fix_ in Fixes.OrderByDescending (i => Tuple.Create (IsAnalysisOrErrorFix(i), (int)i.Severity, GetUsage (i.IdString)))) {
				// filter out code actions that are already resolutions of a code issue
				if (fixesAdded.Any (f => fix_.IdString.IndexOf (f, StringComparison.Ordinal) >= 0))
					continue;
				fixesAdded.Add (fix_.IdString);
				if (IsAnalysisOrErrorFix (fix_))
					gotImportantFix = true;
				if (!addedSeparator && gotImportantFix && !IsAnalysisOrErrorFix(fix_)) {
					menu.Add (new Gtk.SeparatorMenuItem ());
					addedSeparator = true;
				}

				var fix = fix_;
				var escapedLabel = fix.Title.Replace ("_", "__");
				var label = (mnemonic <= 10)
					? "_" + (mnemonic++ % 10).ToString () + " " + escapedLabel
					: "  " + escapedLabel;
				var thisInstanceMenuItem = new MenuItem (label);
				thisInstanceMenuItem.Activated += new ContextActionRunner (fix, Document, currentSmartTagBegin).Run;
				thisInstanceMenuItem.Activated += delegate {
					ConfirmUsage (fix.IdString);
					menu.Destroy ();
				};
				menu.Add (thisInstanceMenuItem);
				items++;
			}

			bool first = true;
			var settingsMenuFixes = Fixes
				.OfType<AnalysisContextActionProvider.AnalysisCodeAction> ()
				.Where (f => f.Result is InspectorResults)
				.GroupBy (f => ((InspectorResults)f.Result).Inspector);
			foreach (var analysisFixGroup_ in settingsMenuFixes) {
				var analysisFixGroup = analysisFixGroup_;
				var arbitraryFixInGroup = analysisFixGroup.First ();
				var ir = (InspectorResults)arbitraryFixInGroup.Result;

				if (first) {
					menu.Add (new Gtk.SeparatorMenuItem ());
					first = false;
				}

				var subMenu = new Gtk.Menu ();
				foreach (var analysisFix_ in analysisFixGroup) {
					var analysisFix = analysisFix_;
					if (analysisFix.SupportsBatchRunning) {
						var batchRunMenuItem = new Gtk.MenuItem (string.Format (GettextCatalog.GetString ("Apply in file: {0}"), analysisFix.Title));
						batchRunMenuItem.Activated += delegate {
							ConfirmUsage (analysisFix.IdString);
							menu.Destroy ();
						};
						batchRunMenuItem.Activated += new ContextActionRunner (analysisFix, Document, this.currentSmartTagBegin).BatchRun;
						subMenu.Add (batchRunMenuItem);
						subMenu.Add (new Gtk.SeparatorMenuItem ());
					}
				}

				var inspector = ir.Inspector;
				if (inspector.CanSuppressWithAttribute) {
					var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("_Suppress with attribute"));
					menuItem.Activated += delegate {
						inspector.SuppressWithAttribute (Document, arbitraryFixInGroup.DocumentRegion); 
					};
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableWithPragma) {
					var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("_Suppress with #pragma"));
					menuItem.Activated += delegate {
						inspector.DisableWithPragma (Document, arbitraryFixInGroup.DocumentRegion); 
					};
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableOnce) {
					var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("_Disable Once"));
					menuItem.Activated += delegate {
						inspector.DisableOnce (Document, arbitraryFixInGroup.DocumentRegion); 
					};
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableAndRestore) {
					var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("Disable _and Restore"));
					menuItem.Activated += delegate {
						inspector.DisableAndRestore (Document, arbitraryFixInGroup.DocumentRegion); 
					};
					subMenu.Add (menuItem);
				}
				var label = GettextCatalog.GetString ("_Options for \"{0}\"", InspectorResults.GetTitle (ir.Inspector));
				var subMenuItem = new Gtk.MenuItem (label);

				var optionsMenuItem = new Gtk.MenuItem (GettextCatalog.GetString ("_Configure Rule"));
				optionsMenuItem.Activated += arbitraryFixInGroup.ShowOptions;

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
			CodeAction act;
			Document document;
			TextLocation loc;

			public ContextActionRunner (MonoDevelop.CodeActions.CodeAction act, MonoDevelop.Ide.Gui.Document document, ICSharpCode.NRefactory.TextLocation loc)
			{
				this.act = act;
				this.document = document;
				this.loc = loc;
			}

			public void Run (object sender, EventArgs e)
			{
				var context = document.ParsedDocument.CreateRefactoringContext (document, CancellationToken.None);
				RefactoringService.ApplyFix (act, context);
			}

			public void BatchRun (object sender, EventArgs e)
			{
				act.BatchRun (document, loc);
			}
		}

		ISmartTagMarker currentSmartTag;
		MonoDevelop.Ide.Editor.DocumentLocation currentSmartTagBegin;

		List<CodeAction> currentSmartTagfixes;

		void CreateSmartTag (List<CodeAction> fixes, MonoDevelop.Ide.Editor.DocumentLocation loc)
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
			if (Document.ParsedDocument == null || Document.ParsedDocument.IsInvalid) {
				RemoveWidget ();
				return;
			}

//			var container = editor.Parent;
//			if (container == null) {
//				RemoveWidget ();
//				return;
//			}
			bool first = true;
			var smartTagLocBegin = loc;
			foreach (var fix in fixes) {
				if (fix.DocumentRegion.IsEmpty)
					continue;
				if (first || loc < fix.DocumentRegion.Begin) {
					smartTagLocBegin = fix.DocumentRegion.Begin;
				}
				first = false;
			}
			if (smartTagLocBegin.Line != loc.Line)
				smartTagLocBegin = new DocumentLocation (loc.Line, 1);
			// got no fix location -> try to search word start
			if (first) {
				int offset = Editor.LocationToOffset (smartTagLocBegin);
				while (offset > 0) {
					char ch = Editor.GetCharAt (offset - 1);
					if (!char.IsLetterOrDigit (ch) && ch != '_')
						break;
					offset--;
				}
				smartTagLocBegin = Editor.OffsetToLocation (offset);
			}

			if (currentSmartTag != null && currentSmartTagBegin == smartTagLocBegin) {
				currentSmartTagfixes = fixes;
				return;
			}
			RemoveWidget ();
			currentSmartTagBegin = smartTagLocBegin;
			var line = Editor.GetLine (smartTagLocBegin.Line);
			currentSmartTag = Editor.MarkerHost.CreateSmartTagMarker ((line.NextLine ?? line).Offset, smartTagLocBegin);
			currentSmartTag.MouseHover += delegate(object sender, TextMarkerMouseEventArgs args) {
				if (currentSmartTag.IsInsideSmartTag (args.X, args.Y)) {
					args.OverwriteCursor = null;
					CurrentSmartTagPopup ();
				} else {
					CancelSmartTagPopupTimeout ();
				}

			};
			Editor.AddMarker (currentSmartTag);
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
				Fixes = RefactoringService.GetValidActions (Document, Document.Editor.CaretLocation).Result;
				currentSmartTagBegin = Document.Editor.CaretLocation;
				PopupQuickFixMenu (null, null); 

				return;
			}
			if (currentSmartTag == null)
				return;
			CurrentSmartTagPopup ();
		}

		internal List<CodeAction> GetCurrentFixes ()
		{
			if (currentSmartTag == null)
				return RefactoringService.GetValidActions (Document, Editor.CaretLocation).Result.ToList ();
			return currentSmartTagfixes;
		}
	}
}

