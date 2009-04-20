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
using MonoDevelop.Core.Gui;


namespace MonoDevelop.Ide
{
	public partial class DocumentSwitcher : Gtk.Window
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
				ShowType (ImageService.GetPixbuf (!string.IsNullOrEmpty (pad.Icon) ? pad.Icon : MonoDevelop.Core.Gui.Stock.MiscFiles, Gtk.IconSize.Dialog),
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
				return ImageService.GetPixbuf (MonoDevelop.Core.Gui.Stock.MiscFiles, iconSize);
			
			return IdeApp.Services.PlatformService.GetPixbufForFile (document.FileName, iconSize);
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
		
		public DocumentSwitcher (Gtk.Window parent, bool startWithNext) : base(Gtk.WindowType.Toplevel)
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
			
			treeviewPads.Selection.Changed += delegate {
				ShowSelectedPad ();
			};
			documentListStore = new Gtk.ListStore (typeof (Gdk.Pixbuf), typeof (string), typeof (Document));
			treeviewDocuments.Model = documentListStore;
			treeviewDocuments.AppendColumn ("icon", new Gtk.CellRendererPixbuf (), "pixbuf", 0);
			treeviewDocuments.AppendColumn ("text", new Gtk.CellRendererText (), "text", 1);
			treeviewDocuments.HeadersVisible = false;
			treeviewDocuments.Selection.Changed += delegate {
				ShowSelectedDocument ();
			};
			
			FillLists ();
			this.labelFileName.Ellipsize = Pango.EllipsizeMode.Start;
			if (IdeApp.Workbench.ActiveDocument != null) {
				SwitchToDocument ();
				SelectDocument (startWithNext ? GetNextDocument (IdeApp.Workbench.ActiveDocument) : GetPrevDocument (IdeApp.Workbench.ActiveDocument));
			} else {
				SwitchToPad ();
			}
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
				padListStore.AppendValues (ImageService.GetPixbuf (!String.IsNullOrEmpty (pad.Icon) ? pad.Icon : MonoDevelop.Core.Gui.Stock.MiscFiles, IconSize.Menu),
				                           pad.Title,
				                           pad);
			}
			
			foreach (Document doc in documents) {
				documentListStore.AppendValues (GetIconForDocument (doc, IconSize.Menu),
				                                doc.Window.Title,
				                                doc);
			}
		}
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			bool next = (evnt.State & Gdk.ModifierType.ShiftMask) != ModifierType.ShiftMask;
//			System.Console.WriteLine (evnt.Key + " -- " + evnt.State);
			switch (evnt.Key) {
			case Gdk.Key.Left:
				SwitchToPad ();
				break;
			case Gdk.Key.Right:
				SwitchToDocument ();
				break;
			case Gdk.Key.Up:
				if (documentFocus) {
					SelectDocument (GetPrevDocument (SelectedDocument));
				} else {
					SelectPad (GetPrevPad (SelectedPad));
				}
				break;
			case Gdk.Key.Down:
				if (documentFocus) {
					SelectDocument (GetNextDocument (SelectedDocument));
				} else {
					SelectPad (GetNextPad (SelectedPad));
				}
				break;
			case Gdk.Key.ISO_Left_Tab:
			case Gdk.Key.Tab:
				if (documentFocus) {
					SelectDocument (next ? GetNextDocument (SelectedDocument) : GetPrevDocument (SelectedDocument));
				} else  {
					SelectPad (next ? GetNextPad (SelectedPad) : GetPrevPad (SelectedPad));
				}
				break;
			}
			return true;
		}
		
		protected override bool OnKeyReleaseEvent (Gdk.EventKey evnt)
		{
			bool ret;
			if (evnt.Key == Gdk.Key.Control_L || evnt.Key == Gdk.Key.Control_R) {
				Document doc = SelectedDocument;
				if (doc != null) {
					doc.Select ();
				} else {
					Pad pad = SelectedPad;
					if (pad != null)
						pad.BringToFront ();
				}
				ret = base.OnKeyReleaseEvent (evnt);
				Gtk.Window parent = this.TransientFor;
				this.Destroy ();
				parent.Present ();
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

	}
}
