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
using MonoDevelop.Core;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
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

namespace MonoDevelop.CodeActions
{
	class CodeActionEditorExtension : TextEditorExtension 
	{
		uint quickFixTimeout;

		const int menuTimeout = 250;
		uint smartTagPopupTimeoutId;
		uint menuCloseTimeoutId;
		Menu codeActionMenu;

		public IEnumerable<Microsoft.CodeAnalysis.CodeActions.CodeAction> Fixes {
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
				document.Editor.Document.RemoveMarker (currentSmartTag);
				currentSmartTag = null;
				currentSmartTagBegin = -1;
			}
			CancelSmartTagPopupTimeout ();

		}
		
		public override void Dispose ()
		{
			CancelMenuCloseTimer ();
			CancelQuickFixTimer ();
			document.Editor.SelectionChanged -= HandleSelectionChanged;
			document.DocumentParsed -= HandleDocumentDocumentParsed;
			document.Editor.Parent.BeginHover -= HandleBeginHover;
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

		public override void CursorPositionChanged ()
		{
			CancelQuickFixTimer ();
			if (QuickTaskStrip.EnableFancyFeatures &&  Document.ParsedDocument != null && !Debugger.DebuggingService.IsDebugging) {
				quickFixCancellationTokenSource = new CancellationTokenSource ();
				var token = quickFixCancellationTokenSource.Token;
				quickFixTimeout = GLib.Timeout.Add (100, delegate {
					var loc = Document.Editor.Caret.Offset;
					var ad = Document.AnalysisDocument;

					var diagnosticsAtCaret =
						Document.Editor.Document.GetTextSegmentMarkersAt (document.Editor.Caret.Offset)
						.OfType<ResultMarker> ()
						.Select (rm => rm.Result)
						.OfType<DiagnosticResult> ()
						.Select (dr => dr.Diagnostic)
						.ToList ();
					
					System.Threading.Tasks.Task.Factory.StartNew (delegate {
						var fixes = new List<Microsoft.CodeAnalysis.CodeActions.CodeAction> ();

						foreach (var cfp in CodeAnalysisRunner.CodeFixProvider) {
							fixes.AddRange (cfp.GetFixesAsync (ad, TextSpan.FromBounds (loc, loc + 1), diagnosticsAtCaret, default(CancellationToken)).Result);
						}
						Application.Invoke (delegate {
							if (token.IsCancellationRequested)
								return;
							CreateSmartTag (fixes, loc);
							quickFixTimeout = 0;
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
//							quickFixTimeout = 0;
//						});
//					});
					return false;
				});
			} else {
				RemoveWidget ();
			}
			base.CursorPositionChanged ();
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
			
			var possibleNamespaces = ResolveCommandHandler.GetPossibleNamespaces (document);

			if (possibleNamespaces.Count > 0) {

				foreach (var t in possibleNamespaces.Where (tp => tp.OnlyAddReference)) {
					var menuItem = new Gtk.MenuItem (t.GetImportText ());
					menuItem.Activated += delegate {
						new ResolveCommandHandler.AddImport (document, possibleNamespaces.ResolveResult, null, t.Reference, true, possibleNamespaces.Node).Run ();
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
							new ResolveCommandHandler.AddImport (document, possibleNamespaces.ResolveResult, ns, reference, true, possibleNamespaces.Node).Run ();
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
						var menuItem = new Gtk.MenuItem (t.GetInsertNamespaceText (document.Editor.GetTextBetween (node.Span.Start, node.Span.End)));
						menuItem.Activated += delegate {
							new ResolveCommandHandler.AddImport (document, possibleNamespaces.ResolveResult, ns, reference, false, node).Run ();
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
			document.Editor.SuppressTooltips = true;
			document.Editor.Parent.HideTooltip ();
			if (menuAction != null)
				menuAction (menu);
			menu.ShowAll ();
			menu.SelectFirst (true);
			menu.Hidden += delegate {
				document.Editor.SuppressTooltips = false;
			};
			var container = document.Editor.Parent;

			var p = container.LocationToPoint (container.OffsetToLocation (currentSmartTagBegin));
			var rect = new Gdk.Rectangle (
				p.X + container.Allocation.X , 
				p.Y + (int)document.Editor.LineHeight + container.Allocation.Y, 0, 0);
			GtkWorkarounds.ShowContextMenu (menu, document.Editor.Parent, null, rect);
		}

		public void PopulateFixes (Gtk.Menu menu, ref int items)
		{
			int mnemonic = 1;
			bool gotImportantFix = false, addedSeparator = false;
			var fixesAdded = new List<string> ();
			
			foreach (var fix in Fixes) {
				var thisInstanceMenuItem = new MenuItem (fix.Description);
				thisInstanceMenuItem.Activated += new ContextActionRunner (fix, document, currentSmartTagBegin).Run;
				thisInstanceMenuItem.Activated += delegate {
					ConfirmUsage (fix.Description);
					menu.Destroy ();
				};
				menu.Add (thisInstanceMenuItem);
				items++;
			}
			
//			foreach (var fix_ in Fixes.OrderByDescending (i => Tuple.Create (IsAnalysisOrErrorFix(i), (int)i.Severity, GetUsage (i.IdString)))) {
//				// filter out code actions that are already resolutions of a code issue
//				if (fixesAdded.Any (f => fix_.IdString.IndexOf (f, StringComparison.Ordinal) >= 0))
//					continue;
//				fixesAdded.Add (fix_.IdString);
//				if (IsAnalysisOrErrorFix (fix_))
//					gotImportantFix = true;
//				if (!addedSeparator && gotImportantFix && !IsAnalysisOrErrorFix (fix_)) {
//					menu.Add (new Gtk.SeparatorMenuItem ());
//					addedSeparator = true;
//				}
//
//				var fix = fix_;
//				var escapedLabel = fix.Title.Replace ("_", "__");
//				var label = (mnemonic <= 10)
//					? "_" + (mnemonic++ % 10).ToString () + " " + escapedLabel
//					: "  " + escapedLabel;
//				var thisInstanceMenuItem = new MenuItem (label);
//				thisInstanceMenuItem.Activated += new ContextActionRunner (fix, document, currentSmartTagBegin).Run;
//				thisInstanceMenuItem.Activated += delegate {
//					ConfirmUsage (fix.IdString);
//					menu.Destroy ();
//				};
//				menu.Add (thisInstanceMenuItem);
//				items++;
//			}
//
//			bool first = true;
//			var settingsMenuFixes = Fixes
//				.OfType<AnalysisContextActionProvider.AnalysisCodeAction> ()
//				.Where (f => f.Result is InspectorResults)
//				.GroupBy (f => ((InspectorResults)f.Result).Inspector);
//			foreach (var analysisFixGroup_ in settingsMenuFixes) {
//				var analysisFixGroup = analysisFixGroup_;
//				var arbitraryFixInGroup = analysisFixGroup.First ();
//				var ir = (InspectorResults)arbitraryFixInGroup.Result;
//
//				if (first) {
//					menu.Add (new Gtk.SeparatorMenuItem ());
//					first = false;
//				}
//
//				var subMenu = new Gtk.Menu ();
//				foreach (var analysisFix_ in analysisFixGroup) {
//					var analysisFix = analysisFix_;
//					if (analysisFix.SupportsBatchRunning) {
//						var batchRunMenuItem = new Gtk.MenuItem (string.Format (GettextCatalog.GetString ("Apply in file: {0}"), analysisFix.Title));
//						batchRunMenuItem.Activated += delegate {
//							ConfirmUsage (analysisFix.IdString);
//							menu.Destroy ();
//						};
//						batchRunMenuItem.Activated += new ContextActionRunner (analysisFix, document, this.currentSmartTagBegin).BatchRun;
//						subMenu.Add (batchRunMenuItem);
//						subMenu.Add (new Gtk.SeparatorMenuItem ());
//					}
//				}
//
//				var inspector = ir.Inspector;
//				if (inspector.CanSuppressWithAttribute) {
//					var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("_Suppress with attribute"));
//					menuItem.Activated += delegate {
//						inspector.SuppressWithAttribute (document, arbitraryFixInGroup.DocumentRegion); 
//					};
//					subMenu.Add (menuItem);
//				}
//
//				if (inspector.CanDisableWithPragma) {
//					var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("_Suppress with #pragma"));
//					menuItem.Activated += delegate {
//						inspector.DisableWithPragma (document, arbitraryFixInGroup.DocumentRegion); 
//					};
//					subMenu.Add (menuItem);
//				}
//
//				if (inspector.CanDisableOnce) {
//					var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("_Disable Once"));
//					menuItem.Activated += delegate {
//						inspector.DisableOnce (document, arbitraryFixInGroup.DocumentRegion); 
//					};
//					subMenu.Add (menuItem);
//				}
//
//				if (inspector.CanDisableAndRestore) {
//					var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("Disable _and Restore"));
//					menuItem.Activated += delegate {
//						inspector.DisableAndRestore (document, arbitraryFixInGroup.DocumentRegion); 
//					};
//					subMenu.Add (menuItem);
//				}
//				var label = GettextCatalog.GetString ("_Options for \"{0}\"", InspectorResults.GetTitle (ir.Inspector));
//				var subMenuItem = new Gtk.MenuItem (label);
//
//				var optionsMenuItem = new Gtk.MenuItem (GettextCatalog.GetString ("_Configure Rule"));
//				optionsMenuItem.Activated += arbitraryFixInGroup.ShowOptions;
//
//				optionsMenuItem.Activated += delegate {
//					menu.Destroy ();
//				};
//				subMenu.Add (optionsMenuItem);
//				subMenuItem.Submenu = subMenu;
//				menu.Add (subMenuItem);
//				items++;
//			}
			
		}


		class ContextActionRunner
		{
			Microsoft.CodeAnalysis.CodeActions.CodeAction act;
			Document document;
			int loc;

			public ContextActionRunner (Microsoft.CodeAnalysis.CodeActions.CodeAction act, MonoDevelop.Ide.Gui.Document document, int loc)
			{
				this.act = act;
				this.document = document;
				this.loc = loc;
			}

			public void Run (object sender, EventArgs e)
			{
				var context = document.ParsedDocument.CreateRefactoringContext (document, CancellationToken.None);
				foreach (var op in act.GetOperationsAsync (default(CancellationToken)).Result) {
					op.Apply (RoslynTypeSystemService.Workspace, default(CancellationToken)); 
				}
			}

			public void BatchRun (object sender, EventArgs e)
			{
				//act.BatchRun (document, loc);
			}
		}

		class SmartTagMarker : TextSegmentMarker, IActionTextLineMarker
		{
			CodeActionEditorExtension codeActionEditorExtension;
			internal List<Microsoft.CodeAnalysis.CodeActions.CodeAction> fixes;
			int offset;

			public SmartTagMarker (int offset, CodeActionEditorExtension codeActionEditorExtension, List<Microsoft.CodeAnalysis.CodeActions.CodeAction> fixes) : base (offset, 0)
			{
				this.codeActionEditorExtension = codeActionEditorExtension;
				this.fixes = fixes;
				this.offset = offset;
			}

			public SmartTagMarker (int offset) : base (offset, 0)
			{
			}
			const double tagMarkerWidth = 8;
			const double tagMarkerHeight = 2;
			public override void Draw (TextEditor editor, Cairo.Context cr, Pango.Layout layout, bool selected, int startOffset, int endOffset, double y, double startXPos, double endXPos)
			{
				var loc = editor.OffsetToLocation (offset);
				var line = editor.GetLine (loc.Line);
				var x = editor.ColumnToX (line, loc.Column) - editor.HAdjustment.Value + editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition;

				cr.Rectangle (Math.Floor (x) + 0.5, Math.Floor (y) + 0.5 + (line == editor.GetLineByOffset (startOffset) ? editor.LineHeight - tagMarkerHeight - 1 : 0), tagMarkerWidth * cr.LineWidth, tagMarkerHeight * cr.LineWidth);

				if (HslColor.Brightness (editor.ColorStyle.PlainText.Background) < 0.5) {
					cr.SetSourceRGBA (0.8, 0.8, 1, 0.9);
				} else {
					cr.SetSourceRGBA (0.2, 0.2, 1, 0.9);
				}
				cr.Stroke ();
			}

			#region IActionTextLineMarker implementation

			bool IActionTextLineMarker.MousePressed (TextEditor editor, MarginMouseEventArgs args)
			{
				return false;
			}

			void IActionTextLineMarker.MouseHover (TextEditor editor, MarginMouseEventArgs args, TextLineMarkerHoverResult result)
			{
				if (args.Button != 0)
					return;
				var loc = editor.OffsetToLocation (offset);

				var line = editor.GetLine (loc.Line);
				if (line == null)
					return;
				var x = editor.ColumnToX (line, loc.Column) - editor.HAdjustment.Value + editor.TextViewMargin.TextStartPosition;
				var y = editor.LineToY (line.LineNumber + 1) - editor.VAdjustment.Value;
				if (args.X - x >= 0 * editor.Options.Zoom && 
				    args.X - x < tagMarkerWidth * editor.Options.Zoom && 
				    args.Y - y < (editor.LineHeight / 2) * editor.Options.Zoom) {
					result.Cursor = null;
					Popup ();
				} else {
					codeActionEditorExtension.CancelSmartTagPopupTimeout ();
				}
			}

			public void Popup ()
			{
				codeActionEditorExtension.smartTagPopupTimeoutId = GLib.Timeout.Add (menuTimeout, delegate {
					codeActionEditorExtension.PopupQuickFixMenu (null, menu => {
						codeActionEditorExtension.codeActionMenu = menu;
						menu.MotionNotifyEvent += (o, args) => {
							if (args.Event.Window == codeActionEditorExtension.Editor.Parent.TextArea.GdkWindow) {
								codeActionEditorExtension.StartMenuCloseTimer ();
							} else {
								codeActionEditorExtension.CancelMenuCloseTimer ();
							}
						};
					});
					codeActionEditorExtension.smartTagPopupTimeoutId = 0;
					return false;
				});
			}


			#endregion
		}

		SmartTagMarker currentSmartTag;
		int currentSmartTagBegin;
		void CreateSmartTag (List<Microsoft.CodeAnalysis.CodeActions.CodeAction> fixes, int loc)
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
			bool first = true;
			var smartTagLocBegin = loc;
//			foreach (var fix in fixes) {
//				if (fix.DocumentRegion.IsEmpty)
//					continue;
//				if (first || loc < fix.DocumentRegion.Begin) {
//					smartTagLocBegin = fix.DocumentRegion.Begin;
//				}
//				first = false;
//			}
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
				currentSmartTag.fixes = fixes;
				return;
			}
			RemoveWidget ();
			currentSmartTagBegin = smartTagLocBegin;
			var line = document.Editor.GetLineByOffset (smartTagLocBegin);
			currentSmartTag = new SmartTagMarker (smartTagLocBegin, this, fixes);
			document.Editor.Document.AddMarker (currentSmartTag);
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			document.DocumentParsed += HandleDocumentDocumentParsed;
			document.Editor.SelectionChanged += HandleSelectionChanged;
			document.Editor.Parent.BeginHover += HandleBeginHover;
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
				ci.Enabled = currentSmartTag != null;
			} else {
				ci.Enabled = true;
			}
		}
		
		[CommandHandler(RefactoryCommands.QuickFix)]
		void OnQuickFixCommand ()
		{
			if (!QuickTaskStrip.EnableFancyFeatures) {
				//Fixes = RefactoringService.GetValidActions (Document, Document.Editor.Caret.Location).Result;
				currentSmartTagBegin = Document.Editor.Caret.Offset;
				PopupQuickFixMenu (null, null); 

				return;
			}
			if (currentSmartTag == null)
				return;
			currentSmartTag.Popup ();
		}

		internal List<Microsoft.CodeAnalysis.CodeActions.CodeAction> GetCurrentFixes ()
		{
			if (currentSmartTag == null)
				return new List<Microsoft.CodeAnalysis.CodeActions.CodeAction> ();
//				return RefactoringService.GetValidActions (document, document.Editor.Caret.Location).Result.ToList ();
			return currentSmartTag.fixes;
		}
	}
}

