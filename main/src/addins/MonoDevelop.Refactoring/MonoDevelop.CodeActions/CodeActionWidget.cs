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

namespace MonoDevelop.CodeActions
{
	public class CodeActionWidget : Gtk.EventBox
	{
//		CodeActionEditorExtension ext;
		MonoDevelop.Ide.Gui.Document document;
		IEnumerable<CodeAction> fixes;
		TextLocation loc;
		Gdk.Pixbuf icon;
		
		public CodeActionWidget (CodeActionEditorExtension ext, MonoDevelop.Ide.Gui.Document document, TextLocation loc, IEnumerable<CodeAction> fixes)
		{
//			this.ext = ext;
			this.document = document;
			this.loc = loc;
			this.fixes = fixes;
			Events = Gdk.EventMask.AllEventsMask;
			icon = ImageService.GetPixbuf ("md-text-quickfix", Gtk.IconSize.Menu);
			SetSizeRequest (Math.Max ((int)document.Editor.LineHeight , icon.Width) + 4, (int)document.Editor.LineHeight + 4);
			ShowAll ();
			document.Editor.Parent.EditorOptionsChanged += HandleDocumentEditorParentEditorOptionsChanged;
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
		
		public void PopupQuickFixMenu ()
		{
			PopupQuickFixMenu (null);
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

		int GetUsage (string id)
		{
			int result;
			if (!CodeActionUsages.TryGetValue (id, out result)) 
				return 0;
			return result;
		}

		public void PopulateFixes (Gtk.Menu menu)
		{
			int mnemonic = 1;
			foreach (var fix_ in fixes.OrderByDescending (i => GetUsage (i.IdString))) {
				var fix = fix_;
				var escapedLabel = fix.Title.Replace ("_", "__");
				var label = (mnemonic <= 10)
						? "_" + (mnemonic++ % 10).ToString () + " " + escapedLabel
						: "  " + escapedLabel;
				var menuItem = new Gtk.MenuItem (label);
				menuItem.Activated += new ContextActionRunner (fix, document, loc).Run;
				menuItem.Activated += delegate {
					ConfirmUsage (fix.IdString);
					menu.Destroy ();
				};
				menu.Add (menuItem);
			}
			var first = true;
			var alreadyInserted = new HashSet<CodeIssueProvider> ();
			foreach (var analysisFix_ in fixes.OfType <AnalysisContextActionProvider.AnalysisCodeAction>().Where (f => f.Result is InspectorResults)) {
				var analysisFix = analysisFix_;
				var ir = analysisFix.Result as InspectorResults;
				if (ir == null)
					continue;
			
				if (first) {
					menu.Add (new Gtk.SeparatorMenuItem ());
					first = false;
				}
				if (alreadyInserted.Contains (ir.Inspector))
					continue;
				alreadyInserted.Add (ir.Inspector);
			
				var label = GettextCatalog.GetString ("_Inspection options for \"{0}\"", ir.Inspector.Title);
				var menuItem = new Gtk.MenuItem (label);
				menuItem.Activated += analysisFix.ShowOptions;
				menuItem.Activated += delegate {
					menu.Destroy ();
				};
				menu.Add (menuItem);
			}

		}
		
		void PopupQuickFixMenu (Gdk.EventButton evt)
		{
			var menu = new Gtk.Menu ();

			var caretOffset = document.Editor.Caret.Offset;
			Gtk.Menu fixMenu = menu;
			DomRegion region;
			var resolveResult = document.GetLanguageItem (caretOffset, out region);
			if (resolveResult != null) {
				var possibleNamespaces = MonoDevelop.Refactoring.ResolveCommandHandler.GetPossibleNamespaces (document, resolveResult);
	
				bool addUsing = !(resolveResult is AmbiguousTypeResolveResult);
				if (addUsing) {
					foreach (string ns_ in possibleNamespaces) {
						string ns = ns_;
						var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("Import Namespace {0}", ns));
						menuItem.Activated += delegate {
							new MonoDevelop.Refactoring.ResolveCommandHandler.AddImport (document, resolveResult, ns, true).Run ();
						};
						menu.Add (menuItem);
					}
				}
				
				bool resolveDirect = !(resolveResult is UnknownMemberResolveResult);
				if (resolveDirect) {
					foreach (string ns in possibleNamespaces) {
						var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("Use {0}", ns + "." + document.Editor.GetTextBetween (region.Begin, region.End)));
						menuItem.Activated += delegate {
							new MonoDevelop.Refactoring.ResolveCommandHandler.AddImport (document, resolveResult, ns, false).Run ();
						};
						menu.Add (menuItem);
					}
				}
				if (menu.Children.Any () && fixes.Any ()) {
					fixMenu = new Gtk.Menu ();
					var menuItem = new Gtk.MenuItem (GettextCatalog.GetString ("Quick Fixes"));
					menuItem.Submenu = fixMenu;
					menu.Add (menuItem);
				}
			}
			
			PopulateFixes (fixMenu);
			
			menu.ShowAll ();
			menu.SelectFirst (true);
			menuPushed = true;
			menu.Destroyed += delegate {
				menuPushed = false;
				QueueDraw ();
			};
			var container = (TextEditorContainer)document.Editor.Parent.Parent;
			var child = (TextEditorContainer.EditorContainerChild)container [this];
			GtkWorkarounds.ShowContextMenu (menu, document.Editor.Parent, null, new Gdk.Rectangle (child.X, child.Y + Allocation.Height - (int)document.Editor.VAdjustment.Value, 0, 0));
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
				// ensure that the Ast is recent.
				document.UpdateParseDocument ();
				act.Run (document, loc);

				document.Editor.Document.CommitUpdateAll ();
			}
		}
//		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (!evnt.TriggersContextMenu () && evnt.Button == 1)
				PopupQuickFixMenu (evnt);
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
				cr.Color = document.Editor.ColorStyle.Default.CairoBackgroundColor;
				cr.Fill ();
				
				FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr,
					true, true,
					0, 0, Allocation.Width / 2, 
					Allocation.Width, Allocation.Height);
				cr.Color = isMouseInside || menuPushed ? document.Editor.ColorStyle.Default.CairoColor : document.Editor.ColorStyle.FoldLine.CairoColor;
				cr.Stroke ();
				
				evnt.Window.DrawPixbuf (Style.BaseGC (State), icon, 
					0, 0, 
					(Allocation.Width - icon.Width) / 2, (Allocation.Height - icon.Height) / 2, 
					icon.Width, icon.Height, 
					Gdk.RgbDither.None, 0, 0);
			}
			
			return true;
		}
	}
}

