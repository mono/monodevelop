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
					var loc = Editor.CaretLocation;
					var snapshot = Editor.CreateDocumentSnapshot ();
					RefactoringService.QueueQuickFixAnalysis (Editor, DocumentContext, loc, token, delegate(List<CodeAction> fixes) {
						if (!fixes.Any ()) {
							ICSharpCode.NRefactory.Semantics.ResolveResult resolveResult;
							AstNode node;
							if (ResolveCommandHandler.ResolveAt (snapshot, loc, DocumentContext, out resolveResult, out node, token)) {
								var possibleNamespaces = ResolveCommandHandler.GetPossibleNamespaces (snapshot, loc, DocumentContext, node, ref resolveResult);
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
			if (ResolveCommandHandler.ResolveAt (Editor, Editor.CaretLocation, DocumentContext, out resolveResult, out node)) {
				var possibleNamespaces = MonoDevelop.Refactoring.ResolveCommandHandler.GetPossibleNamespaces (
					Editor,
					Editor.CaretLocation,
					DocumentContext,
					node,
					ref resolveResult
				);

				foreach (var t in possibleNamespaces.Where (tp => tp.OnlyAddReference)) {
					menu.Add (new FixMenuEntry (t.GetImportText (), delegate {
						new ResolveCommandHandler.AddImport (Editor, DocumentContext, resolveResult, null, t.Reference, true, node).Run ();
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
								new ResolveCommandHandler.AddImport (Editor, DocumentContext, resolveResult, ns, reference, true, node).Run ();
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
						menu.Add (new FixMenuEntry (t.GetInsertNamespaceText (Editor.GetTextBetween (node.StartLocation, node.EndLocation)),
							delegate {
								new ResolveCommandHandler.AddImport (Editor, DocumentContext, resolveResult, ns, reference, false, node).Run ();
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
//			document.Editor.SuppressTooltips = true;
//			document.Editor.Parent.HideTooltip ();
			if (menuAction != null)
				menuAction (menu);
			var container = document.Editor.Parent;

			var p = Editor.LocationToPoint (currentSmartTagBegin);
			Gtk.Widget widget = Editor;
			var rect = new Gdk.Rectangle (
				(int)p.X + widget.Allocation.X , 
				(int)p.Y + (int)Editor.LineHeight + widget.Allocation.Y, 0, 0);

			ShowFixesMenu (document.Editor.Parent, rect, menu);
		}

		bool ShowFixesMenu (Gtk.Widget parent, Gdk.Rectangle evt, FixMenuDescriptor entrySet)
		{
			#if MAC
			parent.GrabFocus ();
			int x, y;
			x = (int)evt.X;
			y = (int)evt.Y;

			Gtk.Application.Invoke (delegate {
			// Explicitly release the grab because the menu is shown on the mouse position, and the widget doesn't get the mouse release event
			Gdk.Pointer.Ungrab (Gtk.Global.CurrentEventTime);
			var menu = CreateNSMenu (entrySet);
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
			});
			#else
			var menu = CreateGtkMenu (entrySet);
			menu.Events |= Gdk.EventMask.AllEventsMask;
			menu.SelectFirst (true);

			menu.Hidden += delegate {
				document.Editor.SuppressTooltips = false;
			};
			menu.ShowAll ();
			menu.MotionNotifyEvent += (o, args) => {
				if (args.Event.Window == Editor.Parent.TextArea.GdkWindow) {
					StartMenuCloseTimer ();
				} else {
					CancelMenuCloseTimer ();
				}
			};

			GtkWorkarounds.ShowContextMenu (menu, parent, null, evt);
			#endif
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
					new ContextActionRunner (fix, Editor, DocumentContext, currentSmartTagBegin).Run (null, EventArgs.Empty);
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
								new ContextActionRunner (analysisFix, Editor, DocumentContext, this.currentSmartTagBegin).BatchRun (null, EventArgs.Empty);
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
							inspector.SuppressWithAttribute (Editor, DocumentContext, arbitraryFixInGroup.DocumentRegion); 
						});
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableWithPragma) {
					var menuItem = new FixMenuEntry (GettextCatalog.GetString ("_Suppress with #pragma"),
						delegate {
							inspector.DisableWithPragma (Editor, DocumentContext, arbitraryFixInGroup.DocumentRegion); 
						});
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableOnce) {
					var menuItem = new FixMenuEntry (GettextCatalog.GetString ("_Disable Once"),
						delegate {
							inspector.DisableOnce (Editor, DocumentContext, arbitraryFixInGroup.DocumentRegion); 
						});
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableAndRestore) {
					var menuItem = new FixMenuEntry (GettextCatalog.GetString ("Disable _and Restore"),
						delegate {
							inspector.DisableAndRestore (Editor, DocumentContext, arbitraryFixInGroup.DocumentRegion); 
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
			DocumentContext documentContext;
			TextLocation loc;
			TextEditor editor;

			public ContextActionRunner (MonoDevelop.CodeActions.CodeAction act, TextEditor editor, DocumentContext documentContext, ICSharpCode.NRefactory.TextLocation loc)
			{
				this.editor = editor;
				this.act = act;
				this.documentContext = documentContext;
				this.loc = loc;
			}

			public void Run (object sender, EventArgs e)
			{
				var context = documentContext.ParsedDocument.CreateRefactoringContext (editor, editor.CaretLocation, documentContext, CancellationToken.None);
				RefactoringService.ApplyFix (act, context);
			}

			public void BatchRun (object sender, EventArgs e)
			{
				act.BatchRun (editor, documentContext, loc);
			}
		}

		ISmartTagMarker currentSmartTag;
		MonoDevelop.Ide.Editor.DocumentLocation currentSmartTagBegin;

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
				return;
			}
			RemoveWidget ();
			currentSmartTagBegin = smartTagLocBegin;
			var line = Editor.GetLine (smartTagLocBegin.Line);
			currentSmartTag = TextMarkerFactory.CreateSmartTagMarker (Editor, (line.NextLine ?? line).Offset, smartTagLocBegin);
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
				Fixes = RefactoringService.GetValidActions (Editor, DocumentContext, Editor.CaretLocation).Result;
				currentSmartTagBegin = Editor.CaretLocation;
				PopupQuickFixMenu (null, null); 

				return;
			}
			if (currentSmartTag == null)
				return;
			CurrentSmartTagPopup ();
		}

		static readonly List<CodeAction> emptyList = new List<CodeAction> ();
		internal List<CodeAction> GetCurrentFixes ()
		{
			return currentSmartTag == null ? emptyList : Fixes;
		}
	}
}
