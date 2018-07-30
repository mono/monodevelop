//
// DocumentOutlinePad.cs
//
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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
using Gtk;

using MonoDevelop.Components.Docking;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Collections.Generic;
using MonoDevelop.Components;

namespace MonoDevelop.DesignerSupport
{


	public class DocumentOutlinePad : PadContent
	{
		Alignment box;
		IOutlinedDocument currentOutlineDoc;
		Document currentDoc;
		DockItemToolbar toolbar;

		public DocumentOutlinePad ()
		{
			box = new Gtk.Alignment (0, 0, 1, 1);
			box.BorderWidth = 0;
			SetWidget (null);
			box.ShowAll ();
		}

		protected override void Initialize (IPadWindow window)
		{
			base.Initialize (window);
			IdeApp.Workbench.ActiveDocumentChanged += DocumentChangedHandler;
			CurrentDoc = IdeApp.Workbench.ActiveDocument;
			toolbar = window.GetToolbar (DockPositionType.Top);
			toolbar.Visible = false;
			Update ();
		}

		public override void Dispose ()
		{
			IdeApp.Workbench.ActiveDocumentChanged -= DocumentChangedHandler;
			CurrentDoc = null;
			ReleaseDoc ();
			base.Dispose ();
		}

		Document CurrentDoc {
			get { return currentDoc; }
			set {
				if (value == currentDoc)
					return;
				if (currentDoc != null)
					currentDoc.ViewChanged -= ViewChangedHandler;
				currentDoc = value;
				if (currentDoc != null)
					currentDoc.ViewChanged += ViewChangedHandler;
			}
		}

		public override Control Control {
			get { return box; }
		}

		void ViewChangedHandler (object sender, EventArgs args)
		{
			Update ();
		}

		void DocumentChangedHandler (object sender, EventArgs args)
		{
			CurrentDoc = IdeApp.Workbench.ActiveDocument;
			Update ();
		}

		void Update ()
		{
			IOutlinedDocument outlineDoc = null;
			if (CurrentDoc != null)
				outlineDoc = CurrentDoc.GetContent<IOutlinedDocument> ();

			if (currentOutlineDoc == outlineDoc)
				return;
			ReleaseDoc ();
			currentOutlineDoc = outlineDoc;

			Widget newWidget = null;
			IEnumerable<Widget> toolbarWidgets = null;
			if (outlineDoc != null) {
				newWidget = outlineDoc.GetOutlineWidget ();
				if (newWidget != null)
					toolbarWidgets = outlineDoc.GetToolbarWidgets ();
			}
			SetWidget (newWidget);
			SetToolbarWidgets (toolbarWidgets);
		}

		void ReleaseDoc ()
		{
			RemoveBoxChild ();
			if (currentOutlineDoc != null)
				currentOutlineDoc.ReleaseOutlineWidget ();
			currentOutlineDoc = null;
		}

		void SetWidget (Gtk.Widget widget)
		{
			if (widget == null)
				widget = new WrappedCentreLabel (MonoDevelop.Core.GettextCatalog.GetString (
			    	"An outline is not available for the current document."));
			RemoveBoxChild ();
			box.Add (widget);
			widget.Show ();
			box.Show ();
		}
		
		void SetToolbarWidgets (IEnumerable<Widget> toolbarWidgets)
		{
			foreach (var old in toolbar.Children)
				toolbar.Remove (old);
			bool any = false;
			if (toolbarWidgets != null) {
				foreach (var w in toolbarWidgets) {
					w.Show ();
					toolbar.Add (w);
					any = true;
				}
			}
			toolbar.Visible = any;
		}

		void RemoveBoxChild ()
		{
			Gtk.Widget curChild = box.Child;
			if (curChild != null)
				box.Remove (curChild);
		}

		private class WrappedCentreLabel : Gtk.Widget
		{
			string text;
			Pango.Layout layout;

			public WrappedCentreLabel ()
			{
				this.HasWindow = false;
			}

			public WrappedCentreLabel (string text)
				: this ()
			{
				this.Text = text;
			}

			public string Text {
				set {
					text = value;
					UpdateLayout ();
				}
				get { return text; }
			}

			private void CreateLayout ()
			{
				if (layout != null) {
					layout.Dispose ();
				}

				layout = new Pango.Layout (PangoContext);
				layout.Wrap = Pango.WrapMode.Word;
			}


			void UpdateLayout ()
			{
				 if (layout == null)
					CreateLayout ();
				layout.Alignment = Pango.Alignment.Center;
				layout.SetText (text);
			}

//			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
//			{
//				if (evnt.Window != GdkWindow || layout == null) {
//					return base.OnExposeEvent (evnt);
//				}
//				layout.Width = (int)(Allocation.Width * 2 / 3 * Pango.Scale.PangoScale);
//				Gtk.Style.PaintLayout (Style, GdkWindow, State, false, evnt.Area,
//				    this, null, Allocation.Width * 1 / 6 + Allocation.X , 12 + Allocation.Y, layout);
//				return true;
//			}

			protected override void OnStyleSet (Gtk.Style previous_style)
			{
				CreateLayout ();
				UpdateLayout ();
				base.OnStyleSet (previous_style);
			}

//			public override void Dispose ()
//			{
//				if (layout != null) {
//					layout.Dispose ();
//					layout = null;
//				}
//				base.Dispose ();
//			}
		}
	}
}