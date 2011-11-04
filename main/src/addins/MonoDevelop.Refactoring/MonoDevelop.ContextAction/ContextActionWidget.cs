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
using MonoDevelop.Components.Commands;
using System.Collections.Generic;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.ContextAction
{
	public class ContextActionWidget : Gtk.EventBox
	{
		ContextActionEditorExtension ext;
		MonoDevelop.Ide.Gui.Document document;
		List<ContextAction> fixes;
		DomLocation loc;
		Gdk.Pixbuf icon;
		
		public ContextActionWidget (ContextActionEditorExtension ext, MonoDevelop.Ide.Gui.Document document, DomLocation loc, List<ContextAction> fixes)
		{
			this.ext = ext;
			this.document = document;
			this.loc = loc;
			this.fixes = fixes;
			Events = Gdk.EventMask.AllEventsMask;
			icon = ImageService.GetPixbuf ("md-text-quickfix", Gtk.IconSize.Menu);
			this.SetSizeRequest (Math.Max ((int)document.Editor.LineHeight , icon.Width) + 4, (int)document.Editor.LineHeight + 4);
			ShowAll ();
			document.Editor.Parent.EditorOptionsChanged += HandleDocumentEditorParentEditorOptionsChanged;
			;
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
		
		void PopupQuickFixMenu (Gdk.EventButton evt)
		{
			Gtk.Menu menu = new Gtk.Menu ();
			
			Dictionary<Gtk.MenuItem, ContextAction> fixTable = new Dictionary<Gtk.MenuItem, ContextAction> ();
			int mnemonic = 1;
			foreach (ContextAction fix in fixes) {
				var escapedLabel = fix.GetMenuText (document, loc).Replace ("_", "__");
				var label = (mnemonic <= 10)
						? "_" + (mnemonic++ % 10).ToString () + " " + escapedLabel
						: "  " + escapedLabel;
				Gtk.MenuItem menuItem = new Gtk.MenuItem (label);
				fixTable [menuItem] = fix;
				menuItem.Activated += delegate(object sender, EventArgs e) {
					// ensure that the Ast is recent.
					document.UpdateParseDocument ();
					var runFix = fixTable [(Gtk.MenuItem)sender];
					runFix.Run (document, loc);
					
					document.Editor.Document.CommitUpdateAll ();
					menu.Destroy ();
				};
				menu.Add (menuItem);
			}
			menu.ShowAll ();
			menu.SelectFirst (true);
			menuPushed = true;
			menu.Destroyed += delegate {
				menuPushed = false;
				QueueDraw ();
			};
			GtkWorkarounds.ShowContextMenu (menu, this, evt, Allocation);
		}
		
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

