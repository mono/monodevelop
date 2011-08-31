//
// WindowSwitcher.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections.Generic;
using System.Linq;
using Gdk;
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Core;
//using Mono.TextEditor;


namespace MonoDevelop.Ide
{
	/*class DocumentList : Gtk.DrawingArea
	{ 
		List<Category> categories = new List<Category> ();
		int maxLength = 15;
		const int maxItems = 20;
		const int padding = 6;
		const int headerDistance = 4;
		const int maxRows = 2;
		
		Item activeItem;
		public Item ActiveItem {
			get { return activeItem; }
			set {
				activeItem = value;
				OnActiveItemChanged (EventArgs.Empty);
				QueueDraw ();
			}
		}
		
		Item hoverItem;

		protected virtual void OnActiveItemChanged (EventArgs e)
		{
			EventHandler handler = this.ActiveItemChanged;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler ActiveItemChanged;
		
		public DocumentList ()
		{
			this.CanFocus = true;
			Events |= EventMask.KeyPressMask | EventMask.KeyReleaseMask | EventMask.PointerMotionMask | EventMask.ButtonPressMask | EventMask.LeaveNotifyMask;
		}
		
		public void AddCategory (Category category)
		{
			categories.Add (category);
		}
		
		Item GetItemAt (double x, double y)
		{
			double xPos = padding, yPos = padding;
			using (var layout = PangoUtil.CreateLayout (this)) {
				int w, h;
					
				foreach (Category cat in categories) {
					yPos = padding;
					layout.SetMarkup ("<b>" + cat.Title + "</b>");
					layout.SetMarkup ("");
					layout.GetPixelSize (out w, out h);
					yPos += h;
					yPos += headerDistance;
					var startY = yPos;
					int curItem = 0;
					layout.SetText (new string ('X', maxLength));
					layout.GetPixelSize (out w, out h);
					var iconHeight = Math.Max (h, cat.Items[0].Icon.Height + 2);
					if (cat.FirstVisibleItem > 0) {
						yPos += iconHeight;
						curItem++;
					}
					for (int i = cat.FirstVisibleItem; i < cat.Items.Count; i++) {
						var item = cat.Items[i];
						if (xPos <= x && yPos <= y && x < xPos + w + item.Icon.Width + 2 && y < yPos + iconHeight)
							return item;
						yPos += iconHeight;
						if (++curItem >= maxItems) {
							curItem = 0;
							yPos = startY;
							xPos += w + cat.Items[0].Icon.Width + 2 + padding;
						}
					}
					xPos += w + cat.Items[0].Icon.Width + 2 + padding;
				}
			}
			return null;
		}
		
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (evnt.Button == 1 && hoverItem != null)
				ActiveItem = hoverItem;
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			var item = GetItemAt (evnt.X, evnt.Y);
			if (item != hoverItem) {
				hoverItem = item;
				QueueDraw ();
			}
			return base.OnMotionNotifyEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			if (hoverItem != null) {
				hoverItem = null;
				QueueDraw ();
			}
			return base.OnLeaveNotifyEvent (evnt);
		}

		
		static string Ellipsize (string str, int maxLength)
		{
			if (str != null && str.Length > maxLength)
				return str.Substring (0, maxLength - 3) + "...";
			return str;
		}
		
		const int upperGradientHeight = 16;
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window)) {
				cr.Rectangle (0, 0, Allocation.Width, upperGradientHeight);
				using (var pattern = new Cairo.LinearGradient (0, 0, 0, upperGradientHeight)) {
					pattern.AddColorStopRgb (0, new Cairo.Color (0.6, 0.8, 0.6));
					pattern.AddColorStopRgb (1, new Cairo.Color (0.8, 1.0, 0.8));
					cr.Pattern = pattern;
					cr.Fill ();
				}
				
				cr.Rectangle (0, upperGradientHeight, Allocation.Width, Allocation.Height - upperGradientHeight);
				using (var pattern = new Cairo.LinearGradient (0, upperGradientHeight, 0, Allocation.Height)) {
					pattern.AddColorStop (0, new Cairo.Color (0.8, 1.0, 0.8));
					pattern.AddColorStop (1, new Cairo.Color (0.6, 0.8, 0.6));
					cr.Pattern = pattern;
					cr.Fill ();
				}
				
				cr.LineWidth = 1;
				cr.Line (0, 0, Allocation.Width, 0);
				cr.Color = new Cairo.Color (0.4, 0.6, 0.4);
				cr.Stroke ();
				
				double xPos = padding, yPos = padding;
				var layout = PangoUtil.CreateLayout (this);
				int w, h;
				layout.SetText (new string ('X', maxLength));
				layout.GetPixelSize (out w, out h);
				
				foreach (Category cat in categories) {
					yPos = padding;
					cr.MoveTo (xPos, yPos);
					layout.SetMarkup ("<b>" + cat.Title + "</b>");
					cr.Color = (HslColor)Style.Text (StateType.Normal);
					cr.ShowLayout (layout);
					layout.SetMarkup ("");
					int w2, h2;
					layout.GetPixelSize (out w2, out h2);
					yPos += h2;
					yPos += headerDistance;
					var startY = yPos;
					int curItem = 0;
					int row = 0;
					var iconHeight = Math.Max (h, cat.Items [0].Icon.Height + 2);
					if (cat.FirstVisibleItem > 0) {
						Gtk.Style.PaintArrow (Style, e.Window, State, ShadowType.None, 
								new Rectangle ((int)xPos, (int)yPos, w, h), 
								this, 
								"", 
								ArrowType.Up, 
								true, 
								(int)xPos, 
								(int)yPos, 
								w, 
								h);
						yPos += iconHeight;
						curItem++;
					}
					
					for (int i = cat.FirstVisibleItem; i < cat.Items.Count; i++) {
						var item = cat.Items [i];
						
						if (curItem + 1 >= maxItems && row + 1 >= maxRows && i + 1 < cat.Items.Count) {
							Gtk.Style.PaintArrow (Style, e.Window, State, ShadowType.None, 
								new Rectangle ((int)xPos, (int)yPos, w, h), 
								this, 
								"", 
								ArrowType.Down, 
								true, 
								(int)xPos, 
								(int)yPos, 
								w, 
								h);
							break;
						}
						
						if (item == ActiveItem) {
							cr.Rectangle (xPos + 0.5, yPos + 0.5, w + item.Icon.Width + 2, iconHeight);
							
							using (var pattern = new Cairo.LinearGradient (xPos, yPos, xPos, yPos + iconHeight * 2)) {
								pattern.AddColorStop (0, (HslColor)Style.Base (StateType.Selected));
								pattern.AddColorStop (1, new Cairo.Color (0.8, 1.0, 0.8));
								cr.Pattern = pattern;
								cr.FillPreserve ();
							}
							
							cr.Color = (HslColor)Style.Base (StateType.Selected);
							cr.Stroke ();
							cr.Color = (HslColor)Style.Text (StateType.Selected);
						} else if (item == hoverItem) {
							cr.Rectangle (xPos + 0.5, yPos + 0.5, w + item.Icon.Width + 2, iconHeight);
							using (var pattern = new Cairo.LinearGradient (xPos, yPos, xPos, yPos + iconHeight)) {
								pattern.AddColorStop (0, new Cairo.Color (0.6, 0.8, 0.6));
								pattern.AddColorStop (1, new Cairo.Color (0.8, 1.0, 0.8));
								cr.Pattern = pattern;
								cr.Fill ();
							}
							cr.Color = (HslColor)Style.Text (StateType.Normal);
						} else {
							cr.Color = (HslColor)Style.Text (StateType.Normal);
						}
						cr.MoveTo (xPos + item.Icon.Width + 2, yPos + (iconHeight - h) / 2);
						layout.SetText (Ellipsize (item.ListTitle ?? item.Title, maxLength));
						cr.ShowLayout (layout);
						e.Window.DrawPixbuf (Style.BaseGC (StateType.Normal), item.Icon, 0, 0, (int)xPos, (int)(yPos + (iconHeight - item.Icon.Height) / 2), item.Icon.Width, item.Icon.Height, RgbDither.None, 0, 0);
						
						yPos += iconHeight;
						if (++curItem >= maxItems) {
							curItem = 0;
							yPos = startY;
							xPos += w + cat.Items [0].Icon.Width + 2 + padding;
							row++;
						}
					}
					
				
					xPos += w + cat.Items [0].Icon.Width + 2 + padding;
				}
				layout.Dispose ();
			}
			return true;
		}
		
		protected override bool OnKeyPressEvent (EventKey evnt)
		{
			Gdk.Key key;
			Gdk.ModifierType mod;
			KeyBindingManager.MapRawKeys (evnt, out key, out mod);
			
			switch (key) {
			case Gdk.Key.Left:
				LeftItem ();
				break;
			case Gdk.Key.Right:
				RightItem ();
				break;
			case Gdk.Key.Up:
				PrevItem ();
				break;
			case Gdk.Key.Down:
				NextItem ();
				break;
			case Gdk.Key.Tab:
				if ((mod & ModifierType.ShiftMask) == 0)
					NextItem ();
				else
					PrevItem ();
				break;
			}
			return base.OnKeyPressEvent (evnt);
		}
		
		protected override bool OnKeyReleaseEvent (Gdk.EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Control_L || evnt.Key == Gdk.Key.Control_R) {
				OnRequestClose (EventArgs.Empty);
			}
			return base.OnKeyReleaseEvent (evnt);
		}
		
		protected virtual void OnRequestClose (EventArgs e)
		{
			EventHandler handler = this.RequestClose;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler RequestClose;
		
		void LeftItem ()
		{
			for (int i = 0; i < categories.Count; i++) {
				var cat = categories[i];
				int idx = cat.Items.IndexOf (ActiveItem);
				if (idx < 0)
					continue;
				int relIndex = idx - cat.FirstVisibleItem;
				if (relIndex / maxItems == 0) {
					if (i > 0) {
						int newIndex = Math.Min (categories[i -1].Items.Count - 1, categories[i - 1].FirstVisibleItem + relIndex);
						ActiveItem = categories[i - 1].Items [newIndex];
					}
				} else {
					ActiveItem = cat.Items [relIndex - maxItems];
				}
				
			}
		}
		
		void RightItem ()
		{
			for (int i = 0; i < categories.Count; i++) {
				var cat = categories[i];
				int idx = cat.Items.IndexOf (ActiveItem);
				if (idx < 0)
					continue;
				int relIndex = idx - cat.FirstVisibleItem;
				if (relIndex / maxItems == maxRows - 1 || relIndex + maxItems >= cat.Items.Count) {
					if (i + 1 < categories.Count) {
						int newIndex = Math.Min (categories[i + 1].Items.Count - 1, categories[i + 1].FirstVisibleItem + relIndex);
						ActiveItem = categories[i + 1].Items [newIndex];
					}
				} else {
					ActiveItem = cat.Items [relIndex + maxItems];
				}
			}
		}
		
		public void NextItem ()
		{
			foreach (Category cat in categories) {
				int idx = cat.Items.IndexOf (ActiveItem);
				if (idx < 0)
					continue;
				if (idx + 1 < cat.Items.Count) {
					if (idx + 1 > cat.FirstVisibleItem + maxItems * maxRows - 3)
						cat.FirstVisibleItem++;
					ActiveItem = cat.Items[idx + 1];
				} else {
					cat.FirstVisibleItem = 0;
					ActiveItem = cat.Items[0];
				}
				break;
			}
		}
		
		public void PrevItem ()
		{
			foreach (Category cat in categories) {
				int idx = cat.Items.IndexOf (ActiveItem);
				if (idx < 0)
					continue;
				if (idx - 1 >= 0) {
					if (idx - 1 < cat.FirstVisibleItem)
						cat.FirstVisibleItem--;
					ActiveItem = cat.Items[idx - 1];
				} else {
					if (cat.Items.Count - 1 > cat.FirstVisibleItem + maxItems * maxRows)
						cat.FirstVisibleItem = cat.Items.Count - maxItems * maxRows + 1;
					ActiveItem = cat.Items[cat.Items.Count - 1];
				}
				break;
			}
		}
		
		protected override void OnSizeRequested (ref Requisition req)
		{
			maxLength = 15;
			foreach (var cat in categories) {
				foreach (var item in cat.Items) {
					maxLength = Math.Min (30, Math.Max (maxLength, (item.ListTitle ?? item.Title).Length));
				}
			}
			
			var layout = PangoUtil.CreateLayout (this);
			int w, h;
			layout.SetText (new string ('X', maxLength));
			layout.GetPixelSize (out w, out h);
			layout.Dispose ();
			int totalWidth = 0;
			int totalHeight = 0;
				
			foreach (var cat in categories) {
				var iconHeight = Math.Max (h, cat.Items[0].Icon.Height + 2);
				var iconWidth = cat.Items[0].Icon.Width + 2 + w;
				var headerHeight = h + headerDistance;
				totalHeight = Math.Max (totalHeight, headerHeight + (Math.Min (cat.Items.Count, maxItems)) * iconHeight);
				totalWidth += (1 + Math.Min (maxRows - 1, cat.Items.Count / maxItems)) * iconWidth;
			}
			req.Width = totalWidth + padding * 2 + (categories.Count - 1) * padding;
			req.Height = totalHeight + padding * 2;
		}
		
		public class Item
		{
			public string Title {
				get;
				set;
			}
			
			public string ListTitle {
				get;
				set;
			}
			
			public Pixbuf Icon {
				get;
				set;
			}
			
			public string Description {
				get;
				set;
			}
			
			public string Path {
				get;
				set;
			}
			
			public object Tag {
				get;
				set;
			}
			
			public override string ToString ()
			{
				return string.Format ("[Item: Title={0}]", Title);
			}

		}
		
		public class Category
		{
			public string Title {
				get;
				set;
			}
			
			List<Item> items = new List<Item> ();
			public List<Item> Items {
				get {
					return items;
				}
			}
			
			public int FirstVisibleItem {
				get;
				set;
			}
			
			public Category (string title)
			{
				this.Title = title;
			}
			
			public void AddItem (Item item)
			{
				items.Add (item);
			}
			
			public override string ToString ()
			{
				return string.Format ("[Category: Title={0}]", Title);
			}
		}
	}
	
	internal class DocumentSwitcher : Gtk.Window
	{
		List<MonoDevelop.Ide.Gui.Document> documents;
		Gtk.Image imageTitle = new Gtk.Image ();
		Label labelFileName = new Label ();
		Label labelType     = new Label ();
		Label labelTitle    = new Label ();
		DocumentList documentList = new DocumentList ();
		
		public DocumentSwitcher (Gtk.Window parent, bool startWithNext) : base(Gtk.WindowType.Toplevel)
		{
			IdeApp.CommandService.IsEnabled = false;
			this.documents = new List<MonoDevelop.Ide.Gui.Document> (IdeApp.Workbench.Documents.OrderByDescending (d => d.LastTimeActive));
			this.TransientFor = parent;
			
			this.Decorated = false;
			this.DestroyWithParent = true;
			this.CanDefault = true;
			
			this.Modal = true;
			this.WindowPosition = Gtk.WindowPosition.CenterOnParent;
			this.TypeHint = WindowTypeHint.Dialog;
			
			this.ModifyBg (StateType.Normal, this.Style.Base (StateType.Normal));
			
			VBox vBox = new VBox ();
			HBox hBox = new HBox ();
			
			var hBox2 = new HBox ();
			hBox2.PackStart (hBox, false, false, 8);
			
			hBox.PackStart (imageTitle, true, false, 2);
			labelTitle.Xalign = 0;
			labelTitle.HeightRequest = 24;
			hBox.PackStart (labelTitle, true, true, 2);
			vBox.PackStart (hBox2, false, false, 6);
			
			labelType.Xalign = 0;
			labelType.HeightRequest = 16;
			hBox = new HBox ();
			hBox.PackStart (labelType, false, false, 8);
			vBox.PackStart (hBox, false, false, 2);
			
			hBox = new HBox ();
			hBox.PackStart (documentList, true, true, 1);
			vBox.PackStart (hBox, false, false, 0);
			
			labelFileName.Xalign = 0;
			labelFileName.Ellipsize = Pango.EllipsizeMode.Start;
			hBox = new HBox ();
			hBox.PackStart (labelFileName, true, true, 8);
			vBox.PackEnd (hBox, false, false, 6);
			
			Add (vBox);
			
			var padCategory = new  DocumentList.Category (GettextCatalog.GetString ("Pads"));
			DocumentList.Item activeItem = null;
			
			foreach (Pad pad in IdeApp.Workbench.Pads) {
				if (!pad.Visible)
					continue;
				var item = new DocumentList.Item () {
					Icon = ImageService.GetPixbuf (pad.Icon.Name ?? MonoDevelop.Ide.Gui.Stock.MiscFiles, IconSize.Menu),
					Title = pad.Title,
					Tag = pad
				};
				if (pad.Window.Content.Control.HasFocus)
					activeItem = item;
				padCategory.AddItem (item);
			}
			documentList.AddCategory (padCategory);
			
			var documentCategory = new  DocumentList.Category (GettextCatalog.GetString ("Documents"));
			foreach (var doc in documents) {
				var item = new DocumentList.Item () {
					Icon = GetIconForDocument (doc, IconSize.Menu),
					Title = System.IO.Path.GetFileName (doc.Name),
					ListTitle = doc.Window.Title,
					Description = doc.Window.DocumentType,
					Path = doc.Name,
					Tag = doc
				};
				if (doc.Window.ActiveViewContent.Control.HasFocus)
					activeItem = item;
				documentCategory.AddItem (item);
			}
			documentList.AddCategory (documentCategory);
		
			documentList.ActiveItemChanged += delegate {
				if (documentList.ActiveItem == null) {
					labelFileName.Text = labelType.Text = labelTitle.Text = "";
					return;
				}
				imageTitle.Pixbuf = documentList.ActiveItem.Icon;
				labelFileName.Text = documentList.ActiveItem.Path;
				labelType.Markup = "<span size=\"small\">" + documentList.ActiveItem.Description + "</span>";
				labelTitle.Markup =  "<span size=\"xx-large\" weight=\"bold\">" + documentList.ActiveItem.Title + "</span>";
			};
			
			if (activeItem == null) {
				if (documentCategory.Items.Count > 0) {
					activeItem = documentCategory.Items[0];
				} else if (padCategory.Items.Count > 0) {
					activeItem = padCategory.Items[0];
				} else {
					Destroy ();
					return;
				}
			}
			
			documentList.ActiveItem = activeItem;
			documentList.NextItem ();
			documentList.RequestClose += delegate {
				if (documentList.ActiveItem.Tag is Pad) {
					((Pad)documentList.ActiveItem.Tag).BringToFront (true);
				} else {
					((MonoDevelop.Ide.Gui.Document)documentList.ActiveItem.Tag).Select ();
				}
				Destroy ();
			};
			
			this.ShowAll ();
			documentList.GrabFocus ();
			this.GrabDefault ();
		}
		
		Pixbuf GetIconForDocument (MonoDevelop.Ide.Gui.Document document, Gtk.IconSize iconSize)
		{
			if (!string.IsNullOrEmpty (document.Window.ViewContent.StockIconId))
				return ImageService.GetPixbuf (document.Window.ViewContent.StockIconId, iconSize);
			if (string.IsNullOrEmpty (document.FileName)) 
				return ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.MiscFiles, iconSize);
			
			return DesktopService.GetPixbufForFile (document.FileName, iconSize);
		}
		
		
		protected override bool OnFocusOutEvent (EventFocus evnt)
		{
			Destroy ();
			return base.OnFocusOutEvent (evnt);
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			IdeApp.CommandService.IsEnabled = true;
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			base.OnExposeEvent (evnt);
			
			int winWidth, winHeight;
			this.GetSize (out winWidth, out winHeight);
			
			this.GdkWindow.DrawRectangle (this.Style.ForegroundGC (StateType.Insensitive), false, 0, 0, winWidth - 1, winHeight - 1);
			return false;
		}
	}*/


	internal partial class DocumentSwitcher : Gtk.Window
	{
		Gtk.ListStore padListStore;
		Gtk.ListStore documentListStore;
		Gtk.TreeView  treeviewPads, treeviewDocuments;
		List<Document> documents;

		class MyTreeView : Gtk.TreeView
		{
			protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
			{
				return false;
			}
			protected override bool OnKeyReleaseEvent (Gdk.EventKey evnt)
			{
				return false;
			}
		}

		void ShowSelectedPad ()
		{
			Gtk.TreeIter iter;
			if (treeviewPads.Selection.GetSelected (out iter)) {
				MonoDevelop.Ide.Gui.Pad pad = padListStore.GetValue (iter, 2) as MonoDevelop.Ide.Gui.Pad;
				ShowType (ImageService.GetPixbuf (!pad.Icon.IsNull ? pad.Icon : MonoDevelop.Ide.Gui.Stock.MiscFiles, Gtk.IconSize.Dialog),
				          pad.Title,
				          "",
				          "");
			}
		}

		Pixbuf GetIconForDocument (MonoDevelop.Ide.Gui.Document document, Gtk.IconSize iconSize)
		{
			if (!string.IsNullOrEmpty (document.Window.ViewContent.StockIconId))
				return ImageService.GetPixbuf (document.Window.ViewContent.StockIconId, iconSize);
			if (string.IsNullOrEmpty (document.FileName)) 
				return ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.MiscFiles, iconSize);

			return DesktopService.GetPixbufForFile (document.FileName, iconSize);
		}

		void ShowSelectedDocument ()
		{
			MonoDevelop.Ide.Gui.Document document = SelectedDocument;
			if (document != null) {
				ShowType (GetIconForDocument (document, IconSize.Dialog),
				          System.IO.Path.GetFileName (document.Name),
				          document.Window.DocumentType,
				          document.FileName);
			}
		}

		public DocumentSwitcher (Gtk.Window parent, bool startWithNext) : base(Gtk.WindowType.Popup)
		{
			this.documents = new List<Document> (IdeApp.Workbench.Documents.OrderByDescending (d => d.LastTimeActive));
			this.TransientFor = parent;
			this.CanFocus = true;
			this.Decorated = false;
			this.DestroyWithParent = true;
			//the following are specified using stetic, but documenting them here too
			//this.Modal = true;
			//this.WindowPosition = Gtk.WindowPosition.CenterOnParent;
			//this.TypeHint = WindowTypeHint.Menu;

			this.Build ();

			treeviewPads = new MyTreeView ();
			scrolledwindow1.Child = treeviewPads;

			treeviewDocuments = new MyTreeView ();
			scrolledwindow2.Child = treeviewDocuments;

			padListStore = new Gtk.ListStore (typeof (Gdk.Pixbuf), typeof (string), typeof (Pad));
			treeviewPads.Model = padListStore;
			treeviewPads.AppendColumn ("icon", new Gtk.CellRendererPixbuf (), "pixbuf", 0);
			treeviewPads.AppendColumn ("text", new Gtk.CellRendererText (), "text", 1);
			treeviewPads.HeadersVisible = false;

			treeviewPads.Selection.Changed += TreeviewPadsSelectionChanged;
			documentListStore = new Gtk.ListStore (typeof (Gdk.Pixbuf), typeof (string), typeof (Document));
			treeviewDocuments.Model = documentListStore;
			treeviewDocuments.AppendColumn ("icon", new Gtk.CellRendererPixbuf (), "pixbuf", 0);
			treeviewDocuments.AppendColumn ("text", new Gtk.CellRendererText (), "text", 1);
			treeviewDocuments.HeadersVisible = false;
			treeviewDocuments.Selection.Changed += TreeviewDocumentsSelectionChanged;

			FillLists ();
			this.labelFileName.Ellipsize = Pango.EllipsizeMode.Start;
			if (IdeApp.Workbench.ActiveDocument != null) {
				SwitchToDocument ();
				SelectDocument (startWithNext ? GetNextDocument (IdeApp.Workbench.ActiveDocument) : GetPrevDocument (IdeApp.Workbench.ActiveDocument));
			} else {
				SwitchToPad ();
			}
		}

		void TreeviewPadsSelectionChanged (object sender, EventArgs e)
		{
			ShowSelectedPad ();
		}

		void TreeviewDocumentsSelectionChanged (object sender, EventArgs e)
		{
			ShowSelectedDocument ();
		}

		bool documentFocus = true;
		Gtk.TreeIter selectedPadIter, selectedDocumentIter;

		void SwitchToDocument ()
		{
			this.treeviewPads.Selection.GetSelected (out selectedPadIter);
			this.treeviewPads.Selection.UnselectAll ();
			if (documentListStore.IterIsValid (selectedDocumentIter))
				this.treeviewDocuments.Selection.SelectIter (selectedDocumentIter);
			else
				this.treeviewDocuments.Selection.SelectPath (new TreePath ("0"));

//			this.treeviewPads.Sensitive = false;
//			this.treeviewDocuments.Sensitive = true;
			documentFocus = true;
			treeviewDocuments.GrabFocus ();
			ShowSelectedDocument ();
		}

		void SwitchToPad ()
		{
			this.treeviewDocuments.Selection.GetSelected (out selectedDocumentIter);
			this.treeviewDocuments.Selection.UnselectAll ();
			if (padListStore.IterIsValid (selectedPadIter))
				this.treeviewPads.Selection.SelectIter (selectedPadIter);
			else
				this.treeviewPads.Selection.SelectPath (new TreePath ("0"));

//			this.treeviewPads.Sensitive = true;
//			this.treeviewDocuments.Sensitive = false;
			documentFocus = false;
			treeviewPads.GrabFocus ();
			ShowSelectedPad ();
		}

		Document GetNextDocument (Document doc)
		{
			if (documents.Count == 0)
				return null;
			int index = documents.IndexOf (doc);
			return documents [(index + 1) % documents.Count];
		}

		Document GetPrevDocument (Document doc)
		{
			if (documents.Count == 0)
				return null;
			int index = documents.IndexOf (doc);
			return documents [(index + documents.Count - 1) % documents.Count];
		}

		Document SelectedDocument {
			get {
				if (!documentFocus)
					return null;
				TreeIter iter;
				if (treeviewDocuments.Selection.GetSelected (out iter)) {
					return documentListStore.GetValue (iter, 2) as Document;
				}
				return null;
			}
		}

		Pad GetNextPad (Pad pad)
		{
			if (this.padListStore.NColumns == 0)
				return null;
			int index = IdeApp.Workbench.Pads.IndexOf (pad);
			Pad result = IdeApp.Workbench.Pads [(index + 1) % IdeApp.Workbench.Pads.Count];
			if (!result.Visible)
				return GetNextPad (result);
			return result;
		}

		Pad GetPrevPad (Pad pad)
		{
			if (this.padListStore.NColumns == 0)
				return null;
			int index = IdeApp.Workbench.Pads.IndexOf (pad);
			Pad result = IdeApp.Workbench.Pads [(index + IdeApp.Workbench.Pads.Count - 1) % IdeApp.Workbench.Pads.Count];
			if (!result.Visible)
				return GetPrevPad (result);
			return result;
		}

		Pad SelectedPad {
			get {
				if (documentFocus)
					return null;
				TreeIter iter;
				if (this.treeviewPads.Selection.GetSelected (out iter)) {
					return padListStore.GetValue (iter, 2) as Pad;
				}
				return null;
			}
		}

		void SelectDocument (Document doc)
		{
			Gtk.TreeIter iter;
			if (documentListStore.GetIterFirst (out iter)) {
				do {
					Document curDocument = documentListStore.GetValue (iter, 2) as Document;
					if (doc == curDocument) {
						treeviewDocuments.Selection.SelectIter (iter);
						return;
					}
				} while (documentListStore.IterNext (ref iter));
			}
		}

		void SelectPad (Pad pad)
		{
			Gtk.TreeIter iter;
			if (padListStore.GetIterFirst (out iter)) {
				do {
					Pad curPad = padListStore.GetValue (iter, 2) as Pad;
					if (pad == curPad) {
						treeviewPads.Selection.SelectIter (iter);
						return;
					}
				} while (padListStore.IterNext (ref iter));
			}
		}

		void ShowType (Gdk.Pixbuf image, string title, string type, string fileName)
		{
//			this.imageType.Pixbuf  = image;
			this.labelTitle.Markup = "<span size=\"xx-large\" weight=\"bold\">" +title + "</span>";
			this.labelType.Markup =  "<span size=\"small\">" +type + " </span>";
			this.labelFileName.Text = fileName;
		}

		void FillLists ()
		{
			foreach (Pad pad in IdeApp.Workbench.Pads) {
				if (!pad.Visible)
					continue;
				padListStore.AppendValues (ImageService.GetPixbuf (!String.IsNullOrEmpty (pad.Icon) ? pad.Icon : MonoDevelop.Ide.Gui.Stock.MiscFiles, IconSize.Menu),
				                           pad.Title,
				                           pad);
			}

			foreach (Document doc in documents) {
				documentListStore.AppendValues (GetIconForDocument (doc, IconSize.Menu),
				                                doc.Window.Title,
				                                doc);
			}
		}

		//FIXME: get ctrl(-shift)-tab keybindings from the Switch(Next|Previous)Document commands?
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			Gdk.Key key;
			Gdk.ModifierType mod;
			KeyBindingManager.MapRawKeys (evnt, out key, out mod);

			switch (key) {
			case Gdk.Key.Left:
				SwitchToPad ();
				break;
			case Gdk.Key.Right:
				SwitchToDocument ();
				break;
			case Gdk.Key.Up:
				Previous ();
				break;
			case Gdk.Key.Down:
				Next ();
				break;
			case Gdk.Key.Tab:
				if ((mod & ModifierType.ShiftMask) == 0)
					Next ();
				else
					Previous ();
				break;
			}
			return true;
		}

		protected override bool OnKeyReleaseEvent (Gdk.EventKey evnt)
		{
			bool ret;
			if (evnt.Key == Gdk.Key.Control_L || evnt.Key == Gdk.Key.Control_R) {
				Gdk.Window focusTarget = null;
				Document doc = SelectedDocument;
				if (doc != null) {
					doc.Select ();
					focusTarget = doc.ActiveView.Control.Toplevel.GdkWindow;
				} else {
					Pad pad = SelectedPad;
					if (pad != null) {
						pad.BringToFront (true);
						focusTarget = pad.Window.Content.Control.Toplevel.GdkWindow;
					}
				}
				ret = base.OnKeyReleaseEvent (evnt);
				Gtk.Window parent = this.TransientFor;
				this.Destroy ();

				(focusTarget ?? parent.GdkWindow).Focus (0);
			} else {
				ret = base.OnKeyReleaseEvent (evnt);
			}
			return ret;
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			base.OnExposeEvent (evnt);

			int winWidth, winHeight;
			this.GetSize (out winWidth, out winHeight);
			this.GdkWindow.DrawRectangle (this.Style.ForegroundGC (StateType.Insensitive), false, 0, 0, winWidth-1, winHeight-1);
			return false;
		}

		void Next ()
		{
			if (documentFocus) {
				SelectDocument (GetNextDocument (SelectedDocument));
			} else {
				SelectPad (GetNextPad (SelectedPad));
			}
		}

		void Previous ()
		{
			if (documentFocus) {
				SelectDocument (GetPrevDocument (SelectedDocument));
			} else {
				SelectPad (GetPrevPad (SelectedPad));
			}
		}
	}
	
	internal partial class DocumentSwitcher
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.HBox hbox2;

		private global::Gtk.VBox vbox4;

		private global::Gtk.Label labelTitle;

		private global::Gtk.Label labelType;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Table table1;

		private global::Gtk.VBox vbox2;

		private global::Gtk.Alignment alignment1;

		private global::Gtk.Label label8;

		private global::Gtk.ScrolledWindow scrolledwindow1;

		private global::Gtk.VBox vbox3;

		private global::Gtk.Label label10;

		private global::Gtk.ScrolledWindow scrolledwindow2;

		private global::Gtk.Image imagePreview;

		private global::Gtk.Label labelFileName;

		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget MonoDevelop.Ide.DocumentSwitcher
			this.Name = "MonoDevelop.Ide.DocumentSwitcher";
			this.Title = "";
			this.TypeHint = ((global::Gdk.WindowTypeHint)(2));
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.Modal = true;
			this.BorderWidth = ((uint)(6));
			// Container child MonoDevelop.Ide.DocumentSwitcher.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox ();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox ();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.vbox4 = new global::Gtk.VBox ();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.labelTitle = new global::Gtk.Label ();
			this.labelTitle.Name = "labelTitle";
			this.labelTitle.Xalign = 0f;
			this.vbox4.Add (this.labelTitle);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.labelTitle]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.labelType = new global::Gtk.Label ();
			this.labelType.Name = "labelType";
			this.labelType.Xalign = 0f;
			this.vbox4.Add (this.labelType);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.labelType]));
			w2.Position = 1;
			w2.Expand = false;
			w2.Fill = false;
			this.hbox2.Add (this.vbox4);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.vbox4]));
			w3.Position = 0;
			this.vbox1.Add (this.hbox2);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox2]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox ();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.table1 = new global::Gtk.Table (((uint)(1)), ((uint)(2)), true);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.vbox2 = new global::Gtk.VBox ();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.alignment1 = new global::Gtk.Alignment (0.5f, 0.5f, 1f, 1f);
			this.alignment1.Name = "alignment1";
			// Container child alignment1.Gtk.Container+ContainerChild
			this.label8 = new global::Gtk.Label ();
			this.label8.Name = "label8";
			this.label8.Xalign = 0f;
			this.label8.LabelProp = global::MonoDevelop.Core.GettextCatalog.GetString ("Pads");
			this.alignment1.Add (this.label8);
			this.vbox2.Add (this.alignment1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.alignment1]));
			w6.Position = 0;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow ();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.VscrollbarPolicy = ((global::Gtk.PolicyType)(2));
			this.scrolledwindow1.HscrollbarPolicy = ((global::Gtk.PolicyType)(2));
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			this.vbox2.Add (this.scrolledwindow1);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.scrolledwindow1]));
			w7.Position = 1;
			this.table1.Add (this.vbox2);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.table1[this.vbox2]));
			w8.XOptions = ((global::Gtk.AttachOptions)(4));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.vbox3 = new global::Gtk.VBox ();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.label10 = new global::Gtk.Label ();
			this.label10.Name = "label10";
			this.label10.Xalign = 0f;
			this.label10.LabelProp = global::MonoDevelop.Core.GettextCatalog.GetString ("Documents");
			this.vbox3.Add (this.label10);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.label10]));
			w9.Position = 0;
			w9.Expand = false;
			w9.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.scrolledwindow2 = new global::Gtk.ScrolledWindow ();
			this.scrolledwindow2.CanFocus = true;
			this.scrolledwindow2.Name = "scrolledwindow2";
			this.scrolledwindow2.VscrollbarPolicy = ((global::Gtk.PolicyType)(2));
			this.scrolledwindow2.HscrollbarPolicy = ((global::Gtk.PolicyType)(2));
			this.scrolledwindow2.ShadowType = ((global::Gtk.ShadowType)(1));
			this.vbox3.Add (this.scrolledwindow2);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.scrolledwindow2]));
			w10.Position = 1;
			this.table1.Add (this.vbox3);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.table1[this.vbox3]));
			w11.LeftAttach = ((uint)(1));
			w11.RightAttach = ((uint)(2));
			this.hbox1.Add (this.table1);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.table1]));
			w12.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.imagePreview = new global::Gtk.Image ();
			this.imagePreview.Name = "imagePreview";
			this.hbox1.Add (this.imagePreview);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.imagePreview]));
			w13.PackType = ((global::Gtk.PackType)(1));
			w13.Position = 1;
			w13.Expand = false;
			w13.Fill = false;
			this.vbox1.Add (this.hbox1);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox1]));
			w14.Position = 1;
			// Container child vbox1.Gtk.Box+BoxChild
			this.labelFileName = new global::Gtk.Label ();
			this.labelFileName.Name = "labelFileName";
			this.labelFileName.Ellipsize = ((global::Pango.EllipsizeMode)(1));
			this.vbox1.Add (this.labelFileName);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.labelFileName]));
			w15.PackType = ((global::Gtk.PackType)(1));
			w15.Position = 2;
			w15.Expand = false;
			w15.Fill = false;
			this.Add (this.vbox1);
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.DefaultWidth = 490;
			this.DefaultHeight = 350;
			this.Show ();
		}
	}
}