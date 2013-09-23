// 
// QuickFixWidget.cs
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
using MonoDevelop.Ide;
using Mono.TextEditor;
using System.Collections.Generic;
using ICSharpCode.NRefactory;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.AnalysisCore.Fixes;
using MonoDevelop.Ide.Gui;
using MonoDevelop.CodeIssues;

using MonoDevelop.Ide.Gui.Content;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using MonoDevelop.CodeActions;
using MonoDevelop.Refactoring;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;
using ICSharpCode.NRefactory.Refactoring;
using System.Threading;

namespace MonoDevelop.CodeActions
{
	class CodeActionWidget : Gtk.EventBox
	{
//		CodeActionEditorExtension ext;
		MonoDevelop.Ide.Gui.Document document;
		IEnumerable<CodeAction> fixes;
		TextLocation loc;
		Gdk.Pixbuf icon;
		
		public CodeActionWidget (CodeActionEditorExtension ext, MonoDevelop.Ide.Gui.Document document)
		{
//			this.ext = ext;
			this.document = document;
			Events = Gdk.EventMask.AllEventsMask;
			icon = ImageService.GetPixbuf ("md-text-quickfix", Gtk.IconSize.Menu);
			SetSizeRequest (Math.Max ((int)document.Editor.LineHeight , icon.Width) + 4, (int)document.Editor.LineHeight + 4);
			document.Editor.Parent.EditorOptionsChanged += HandleDocumentEditorParentEditorOptionsChanged;
		}

		public void SetFixes (IEnumerable<CodeAction> fixes, TextLocation loc)
		{
			this.loc = loc;
			this.fixes = fixes;
		}

		void HandleDocumentEditorParentEditorOptionsChanged (object sender, EventArgs e)
		{
//			var container = this.Parent as TextEditorContainer;
			HeightRequest = (int)document.Editor.LineHeight;
		//	container.MoveTopLevelWidget (this, (int)document.Editor.Parent.TextViewMargin.XOffset + 4, (int)document.Editor.Parent.LineToY (loc.Line));
		}
		
		protected override void OnDestroyed ()
		{
			document.Editor.Parent.EditorOptionsChanged -= HandleDocumentEditorParentEditorOptionsChanged;
			base.OnDestroyed ();
		}
		
		public void PopupQuickFixMenu (Action<Gtk.Menu> menuAction = null)
		{
			PopupQuickFixMenu (null, menuAction);
		}

		static CodeActionWidget ()
		{
			var usages = PropertyService.Get<Properties> ("CodeActionUsages", new Properties ());
			foreach (var key in usages.Keys) {
				CodeActionUsages [key] = usages.Get<int> (key);
			}
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

		internal static bool IsAnalysisOrErrorFix (CodeAction act)
		{
			return act is AnalysisContextActionProvider.AnalysisCodeAction || act.Severity == Severity.Error;
		} 

		public void PopulateFixes (Gtk.Menu menu, ref int items)
		{
			int mnemonic = 1;
			bool gotImportantFix = false, addedSeparator = false;
			var fixesAdded = new List<string> ();
			foreach (var fix_ in fixes.OrderByDescending (i => Tuple.Create (IsAnalysisOrErrorFix(i), (int)i.Severity, GetUsage (i.IdString)))) {
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
				var thisInstanceMenuItem = new Gtk.MenuItem (label);
				thisInstanceMenuItem.Activated += new ContextActionRunner (fix, document, loc).Run;
				thisInstanceMenuItem.Activated += delegate {
					ConfirmUsage (fix.IdString);
					menu.Destroy ();
				};
				menu.Add (thisInstanceMenuItem);
				items++;
			}

			bool first = true;
			var settingsMenuFixes = fixes
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
						batchRunMenuItem.Activated += new ContextActionRunner (analysisFix, document, loc).BatchRun;
						subMenu.Add (batchRunMenuItem);
						subMenu.Add (new Gtk.SeparatorMenuItem ());
					}
				}

				var inspector = ir.Inspector;
				if (inspector.CanSuppressWithAttribute) {
					var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("_Suppress with attribute"));
					menuItem.Activated += delegate {
						inspector.SuppressWithAttribute (document, arbitraryFixInGroup.DocumentRegion); 
					};
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableWithPragma) {
					var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("_Suppress with #pragma"));
					menuItem.Activated += delegate {
						inspector.DisableWithPragma (document, arbitraryFixInGroup.DocumentRegion); 
					};
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableOnce) {
					var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("_Disable once with comment"));
					menuItem.Activated += delegate {
						inspector.DisableOnce (document, arbitraryFixInGroup.DocumentRegion); 
					};
					subMenu.Add (menuItem);
				}

				if (inspector.CanDisableAndRestore) {
					var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("Disable _and restore with comments"));
					menuItem.Activated += delegate {
						inspector.DisableAndRestore (document, arbitraryFixInGroup.DocumentRegion); 
					};
					subMenu.Add (menuItem);
				}
				var label = GettextCatalog.GetString ("_Options for \"{0}\"", InspectorResults.GetTitle (ir.Inspector));
				var subMenuItem = new Gtk.MenuItem (label);

				var optionsMenuItem = new Gtk.MenuItem (GettextCatalog.GetString ("_Configure inspection"));
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
		
		void PopupQuickFixMenu (Gdk.EventButton evt, Action<Gtk.Menu> menuAction)
		{
			var menu = new Gtk.Menu ();
			menu.Events |= Gdk.EventMask.AllEventsMask;
			Gtk.Menu fixMenu = menu;
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
					var menuItem = new Gtk.MenuItem (t.GetImportText ());
					menuItem.Activated += delegate {
						new ResolveCommandHandler.AddImport (document, resolveResult, null, t.Reference, true, node).Run ();
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
							new ResolveCommandHandler.AddImport (document, resolveResult, ns, reference, true, node).Run ();
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
						var menuItem = new Gtk.MenuItem (t.GetInsertNamespaceText (document.Editor.GetTextBetween (node.StartLocation, node.EndLocation)));
						menuItem.Activated += delegate {
							new ResolveCommandHandler.AddImport (document, resolveResult, ns, reference, false, node).Run ();
							menu.Destroy ();
						};
						menu.Add (menuItem);
						items++;
					}
				}
				if (menu.Children.Any () && fixes.Any ()) {
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
			menuPushed = true;
			menu.Hidden += delegate {
				document.Editor.SuppressTooltips = false;
			};
			menu.Destroyed += delegate {
				menuPushed = false;
				Hide ();
			};
			var container = document.Editor.Parent;
			var child = (TextEditor.EditorContainerChild)container [this];

			Gdk.Rectangle rect;
/*			if (child != null) {
				rect = new Gdk.Rectangle (child.X, child.Y + Allocation.Height - (int)document.Editor.VAdjustment.Value, 0, 0);
			} else {*/
				var p = container.LocationToPoint (loc);
				rect = new Gdk.Rectangle (p.X + container.Allocation.X , p.Y + (int)document.Editor.LineHeight + container.Allocation.Y, 0, 0);
			//}
			GtkWorkarounds.ShowContextMenu (menu, document.Editor.Parent, null, rect);
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
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (!evnt.TriggersContextMenu () && evnt.Button == 1)
				PopupQuickFixMenu (evnt, null);
			return base.OnButtonPressEvent (evnt);
		}
		
		bool isMouseInside, menuPushed;
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			isMouseInside = true;
			QueueDraw ();
			return base.OnEnterNotifyEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			isMouseInside = false;
			QueueDraw ();
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
//			var alloc = Allocation;
			double border = 1.0;
//			var halfBorder = border / 2.0;
			
			using (var cr = Gdk.CairoHelper.Create (evnt.Window)) {
				cr.LineWidth = border;
				cr.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				cr.SetSourceColor (document.Editor.ColorStyle.PlainText.Background);
				cr.Fill ();
				
				FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr,
					true, true,
					0, 0, Allocation.Width / 2, 
					Allocation.Width, Allocation.Height);
				cr.Color = isMouseInside || menuPushed ? document.Editor.ColorStyle.PlainText.Foreground : document.Editor.ColorStyle.FoldLineColor.Color;
				cr.Stroke ();

				Gdk.CairoHelper.SetSourcePixbuf (cr, icon, (Allocation.Width - icon.Width) / 2, (Allocation.Height - icon.Height) / 2);
				cr.Paint ();
			}
			
			return true;
		}
	}
}

