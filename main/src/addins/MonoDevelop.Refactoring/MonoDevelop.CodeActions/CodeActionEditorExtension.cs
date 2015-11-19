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

namespace MonoDevelop.CodeActions
{
	class CodeActionEditorExtension : TextEditorExtension 
	{
		uint quickFixTimeout;

		const int menuTimeout = 250;
		uint smartTagPopupTimeoutId;
		uint menuCloseTimeoutId;
		FixMenuDescriptor codeActionMenu;

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
				document.Editor.Document.RemoveMarker (currentSmartTag);
				currentSmartTag = null;
				currentSmartTagBegin = DocumentLocation.Empty;
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
					var loc = Document.Editor.Caret.Location;
					RefactoringService.QueueQuickFixAnalysis (Document, loc, token, delegate(List<CodeAction> fixes) {
						if (!fixes.Any ()) {
							ICSharpCode.NRefactory.Semantics.ResolveResult resolveResult;
							AstNode node;
							if (ResolveCommandHandler.ResolveAt (document, out resolveResult, out node, token)) {
								var possibleNamespaces = ResolveCommandHandler.GetPossibleNamespaces (document, node, ref resolveResult);
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
			base.CursorPositionChanged ();
		}

		internal static bool IsAnalysisOrErrorFix (CodeAction act)
		{
			return act is AnalysisContextActionProvider.AnalysisCodeAction || act.Severity == Severity.Error;
		} 
			

		class FixMenuEntry
		{
			public static readonly FixMenuEntry Separator = new FixMenuEntry ("-", null);
			public readonly string Label;

			public readonly System.Action Action;

			public FixMenuEntry (string label, System.Action action)
			{
				this.Label = label;
				this.Action = action;
			}
		}

		class FixMenuDescriptor : FixMenuEntry
		{
			readonly List<FixMenuEntry> items = new List<FixMenuEntry> ();

			public IReadOnlyList<FixMenuEntry> Items {
				get {
					return items;
				}
			}

			public FixMenuDescriptor () : base (null, null)
			{
			}

			public FixMenuDescriptor (string label) : base (label, null)
			{
			}

			public void Add (FixMenuEntry entry)
			{
				items.Add (entry); 
			}

			public object MotionNotifyEvent {
				get;
				set;
			}
		}

		void PopupQuickFixMenu (Gdk.EventButton evt, Action<FixMenuDescriptor> menuAction)
		{
			FixMenuDescriptor menu = new FixMenuDescriptor ();
			var fixMenu = menu;
			ResolveResult resolveResult;
			ICSharpCode.NRefactory.CSharp.AstNode node;
			int items = 0;
			if (ResolveCommandHandler.ResolveAt (document, out resolveResult, out node)) {
				var possibleNamespaces = MonoDevelop.Refactoring.ResolveCommandHandler.GetPossibleNamespaces (
					document,
					node,
					ref resolveResult
				);

				foreach (var t in possibleNamespaces.Where (tp => tp.OnlyAddReference)) {
					menu.Add (new FixMenuEntry (t.GetImportText (), delegate {
						new ResolveCommandHandler.AddImport (document, resolveResult, null, t.Reference, true, node).Run ();
					}));
					items++;
				}

				bool addUsing = !(resolveResult is AmbiguousTypeResolveResult);
				if (addUsing) {
					foreach (var t in possibleNamespaces.Where (tp => tp.IsAccessibleWithGlobalUsing)) {
						string ns = t.Namespace;
						var reference = t.Reference;
						menu.Add (new FixMenuEntry (t.GetImportText (), 
							delegate {
								new ResolveCommandHandler.AddImport (document, resolveResult, ns, reference, true, node).Run ();
							})
						);
						items++;
					}
				}

				bool resolveDirect = !(resolveResult is UnknownMemberResolveResult);
				if (resolveDirect) {
					foreach (var t in possibleNamespaces) {
						string ns = t.Namespace;
						var reference = t.Reference;
						menu.Add (new FixMenuEntry (t.GetInsertNamespaceText (document.Editor.GetTextBetween (node.StartLocation, node.EndLocation)),
							delegate {
							new ResolveCommandHandler.AddImport (document, resolveResult, ns, reference, false, node).Run ();
						}));
						items++;
					}
				}

				if (menu.Items.Any () && Fixes.Any ()) {
					fixMenu = new FixMenuDescriptor (GettextCatalog.GetString ("Quick Fixes"));
					menu.Add (fixMenu);
					items++;
				}
			}
			PopulateFixes (fixMenu, ref items);
			if (items == 0) {
				return;
			}
			document.Editor.SuppressTooltips = true;
			document.Editor.Parent.HideTooltip ();
			if (menuAction != null)
				menuAction (menu);
			var container = document.Editor.Parent;

			var p = container.LocationToPoint (currentSmartTagBegin);
			var rect = new Gdk.Rectangle (
				p.X + container.Allocation.X , 
				p.Y + (int)document.Editor.LineHeight + container.Allocation.Y, 0, 0);

			ShowFixesMenu (document.Editor.Parent, rect, menu);
		}

		#if MAC
		class ClosingMenuDelegate : AppKit.NSMenuDelegate
		{
			readonly TextEditorData data;

			public ClosingMenuDelegate (TextEditorData editor_data)
			{
				data = editor_data;
			}

			public override void MenuWillHighlightItem (AppKit.NSMenu menu, AppKit.NSMenuItem item)
			{
			}

			public override void MenuDidClose (AppKit.NSMenu menu)
			{
				data.SuppressTooltips = false;
			}
		}
		#endif

		bool ShowFixesMenu (Gtk.Widget parent, Gdk.Rectangle evt, FixMenuDescriptor entrySet)
		{
			if (parent == null || parent.GdkWindow == null)
				return true;
			try {
				#if MAC
				parent.GrabFocus ();
				int x, y;
				x = (int)evt.X;
				y = (int)evt.Y;
				// Explicitly release the grab because the menu is shown on the mouse position, and the widget doesn't get the mouse release event
				Gdk.Pointer.Ungrab (Gtk.Global.CurrentEventTime);
				var menu = CreateNSMenu (entrySet);
				menu.Delegate = new ClosingMenuDelegate (document.Editor);
				var nsview = MonoDevelop.Components.Mac.GtkMacInterop.GetNSView (parent);
				var toplevel = parent.Toplevel as Gtk.Window;
				int trans_x, trans_y;
				parent.TranslateCoordinates (toplevel, (int)x, (int)y, out trans_x, out trans_y);

				// Window coordinates in gtk are the same for cocoa, with the exception of the Y coordinate, that has to be flipped.
				var pt = new CoreGraphics.CGPoint ((float)trans_x, (float)trans_y);
				int w,h;
				toplevel.GetSize (out w, out h);
				pt.Y = h - pt.Y;

				var tmp_event = AppKit.NSEvent.MouseEvent (AppKit.NSEventType.LeftMouseDown,
				pt,
				0, 0,
				MonoDevelop.Components.Mac.GtkMacInterop.GetNSWindow (toplevel).WindowNumber,
				null, 0, 0, 0);

				AppKit.NSMenu.PopUpContextMenu (menu, tmp_event, nsview);
				#else
				var menu = CreateGtkMenu (entrySet);
				menu.Events |= Gdk.EventMask.AllEventsMask;
				menu.SelectFirst (true);

				menu.Hidden += delegate {
					document.Editor.SuppressTooltips = false;
				};
				menu.ShowAll ();
				menu.SelectFirst (true);
				menu.MotionNotifyEvent += (o, args) => {
					if (args.Event.Window == Editor.Parent.TextArea.GdkWindow) {
						StartMenuCloseTimer ();
					} else {
						CancelMenuCloseTimer ();
					}
				};

				GtkWorkarounds.ShowContextMenu (menu, parent, null, evt);
				#endif
			} catch (Exception ex) {
				LoggingService.LogError ("Error while context menu popup.", ex);
			}
			return true;
		}
		#if MAC

		AppKit.NSMenu CreateNSMenu (FixMenuDescriptor entrySet)
		{
			var menu = new AppKit.NSMenu ();
			foreach (var item in entrySet.Items) {
				if (item == FixMenuEntry.Separator) {
					menu.AddItem (AppKit.NSMenuItem.SeparatorItem);
					continue;
				}
				var subMenu = item as FixMenuDescriptor;
				if (subMenu != null) {
					var gtkSubMenu = new AppKit.NSMenuItem (item.Label.Replace ("_", ""));
					gtkSubMenu.Submenu = CreateNSMenu (subMenu);
					menu.AddItem (gtkSubMenu); 
					continue;
				}
				var menuItem = new AppKit.NSMenuItem (item.Label.Replace ("_", ""));
				menuItem.Activated += delegate {
					item.Action ();
				};
				menu.AddItem (menuItem); 
			}
			return menu;
		}
		#endif

		static Menu CreateGtkMenu (FixMenuDescriptor entrySet)
		{
			var menu = new Menu ();
			foreach (var item in entrySet.Items) {
				if (item == FixMenuEntry.Separator) {
					menu.Add (new SeparatorMenuItem ()); 
					continue;
				}
				var subMenu = item as FixMenuDescriptor;
				if (subMenu != null) {
					var gtkSubMenu = new Gtk.MenuItem (item.Label);
					gtkSubMenu.Submenu = CreateGtkMenu (subMenu);
					menu.Add (gtkSubMenu); 
					continue;
				}
				var menuItem = new Gtk.MenuItem (item.Label);
				menuItem.Activated += delegate {
					item.Action ();
				};
				menu.Add (menuItem); 
			}
			return menu;
		}

		void PopulateFixes (FixMenuDescriptor menu, ref int items)
		{
			if (!RefactoringService.ShowFixes)
				return;
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
					menu.Add (FixMenuEntry.Separator);
					addedSeparator = true;
				}

				var fix = fix_;
				var escapedLabel = fix.Title.Replace ("_", "__");
				var label = (mnemonic <= 10)
					? "_" + (mnemonic++ % 10).ToString () + " " + escapedLabel
					: "  " + escapedLabel;
				var thisInstanceMenuItem = new FixMenuEntry (label, delegate {
					new ContextActionRunner (fix, document, currentSmartTagBegin).Run (null, EventArgs.Empty);
					ConfirmUsage (fix.IdString);
				});
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
					menu.Add (FixMenuEntry.Separator);
					first = false;
				}

				var subMenu = new FixMenuDescriptor ();
				foreach (var analysisFix_ in analysisFixGroup) {
					var analysisFix = analysisFix_;
					if (analysisFix.SupportsBatchRunning) {
						var batchRunMenuItem = new FixMenuEntry (
							string.Format (GettextCatalog.GetString ("Apply in file: {0}"), analysisFix.Title), 
							delegate {
								ConfirmUsage (analysisFix.IdString);
								new ContextActionRunner (analysisFix, document, this.currentSmartTagBegin).BatchRun (null, EventArgs.Empty);
							}
						);
						subMenu.Add (batchRunMenuItem);
						subMenu.Add (FixMenuEntry.Separator);
					}
				}

				var inspector = ir.Inspector;
				if (inspector.CanSuppressWithAttribute) {
					var menuItem = new FixMenuEntry (GettextCatalog.GetString ("_Suppress with attribute"),
						delegate {
							inspector.SuppressWithAttribute (document, arbitraryFixInGroup.DocumentRegion); 
						});
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableWithPragma) {
					var menuItem = new FixMenuEntry (GettextCatalog.GetString ("_Suppress with #pragma"),
						delegate {
							inspector.DisableWithPragma (document, arbitraryFixInGroup.DocumentRegion); 
						});
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableOnce) {
					var menuItem = new FixMenuEntry (GettextCatalog.GetString ("_Disable Once"),
						delegate {
							inspector.DisableOnce (document, arbitraryFixInGroup.DocumentRegion); 
						});
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableAndRestore) {
					var menuItem = new FixMenuEntry (GettextCatalog.GetString ("Disable _and Restore"),
						delegate {
							inspector.DisableAndRestore (document, arbitraryFixInGroup.DocumentRegion); 
						});
					subMenu.Add (menuItem);
				}
				var label = GettextCatalog.GetString ("_Options for \"{0}\"", InspectorResults.GetTitle (ir.Inspector));
				var subMenuItem = new FixMenuDescriptor (label);

				var optionsMenuItem = new FixMenuEntry (GettextCatalog.GetString ("_Configure Rule"),
					delegate {
						arbitraryFixInGroup.ShowOptions (null, EventArgs.Empty);
					});
				subMenuItem.Add (optionsMenuItem);

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

		class SmartTagMarker : TextSegmentMarker, IActionTextLineMarker
		{
			CodeActionEditorExtension codeActionEditorExtension;
			internal List<CodeAction> fixes;
			DocumentLocation loc;

			public SmartTagMarker (int offset, CodeActionEditorExtension codeActionEditorExtension, List<CodeAction> fixes, DocumentLocation loc) : base (offset, 0)
			{
				this.codeActionEditorExtension = codeActionEditorExtension;
				this.fixes = fixes;
				this.loc = loc;
			}

			public SmartTagMarker (int offset) : base (offset, 0)
			{
			}
			const double tagMarkerWidth = 8;
			const double tagMarkerHeight = 2;
			public override void Draw (TextEditor editor, Cairo.Context cr, Pango.Layout layout, bool selected, int startOffset, int endOffset, double y, double startXPos, double endXPos)
			{
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
				var line = editor.GetLine (loc.Line);
				if (line == null)
					return;
				var x = editor.ColumnToX (line, loc.Column) - editor.HAdjustment.Value + editor.TextViewMargin.TextStartPosition;
				var y = editor.LineToY (line.LineNumber + 1) - editor.VAdjustment.Value;
				const double xAdditionalSpace = tagMarkerWidth;
				if (args.X - x >= -xAdditionalSpace * editor.Options.Zoom && 
					args.X - x < (tagMarkerWidth + xAdditionalSpace) * editor.Options.Zoom /*&& 
				    args.Y - y < (editor.LineHeight / 2) * editor.Options.Zoom*/) {
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
					});
					codeActionEditorExtension.smartTagPopupTimeoutId = 0;
					return false;
				});
			}


			#endregion
		}

		SmartTagMarker currentSmartTag;
		DocumentLocation currentSmartTagBegin;
		void CreateSmartTag (List<CodeAction> fixes, DocumentLocation loc)
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
			DocumentLocation smartTagLocBegin = loc;
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
				int offset = document.Editor.LocationToOffset (smartTagLocBegin);
				while (offset > 0) {
					char ch = document.Editor.GetCharAt (offset - 1);
					if (!char.IsLetterOrDigit (ch) && ch != '_')
						break;
					offset--;
				}
				smartTagLocBegin = document.Editor.OffsetToLocation (offset);
			}

			if (currentSmartTag != null && currentSmartTagBegin == smartTagLocBegin) {
				currentSmartTag.fixes = fixes;
				return;
			}
			RemoveWidget ();
			currentSmartTagBegin = smartTagLocBegin;
			var line = document.Editor.GetLine (smartTagLocBegin.Line);
			currentSmartTag = new SmartTagMarker ((line.NextLine ?? line).Offset, this, fixes, smartTagLocBegin);
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
				/*if (codeActionMenu != null) {
					codeActionMenu.Destroy ();
					codeActionMenu = null;
				}*/
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
				Fixes = RefactoringService.GetValidActions (Document, Document.Editor.Caret.Location);
				currentSmartTagBegin = Document.Editor.Caret.Location;
				PopupQuickFixMenu (null, null); 

				return;
			}
			if (currentSmartTag == null)
				return;
			currentSmartTag.Popup ();
		}

		static readonly List<CodeAction> emptyList = new List<CodeAction> ();
		internal List<CodeAction> GetCurrentFixes ()
		{
			return currentSmartTag == null ? emptyList : currentSmartTag.fixes;
		}
	}
}