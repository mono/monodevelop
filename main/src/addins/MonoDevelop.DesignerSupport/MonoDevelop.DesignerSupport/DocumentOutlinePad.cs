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

namespace MonoDevelop.DesignerSupport
{


	public class DocumentOutlinePad : AbstractPadContent
	{
		Gtk.Alignment          box;
		IOutlinedDocument      currentOutlineDoc;
		Document               currentDoc;

		// Sorting related widgets

		DockItemToolbar        toolbar;

		ToggleButton           groupToggleButton;
		ToggleButton           sortAlphabeticallyToggleButton;
		DockToolButton         preferencesButton;

		VSeparator             separator;


		public DocumentOutlinePad ()
		{
			box = new Gtk.Alignment (0, 0, 1, 1);
			box.BorderWidth = 0;
			SetEmptyWidget ();
			box.ShowAll ();
		}

		public override void Initialize (IPadWindow window)
		{
			base.Initialize (window);
			IdeApp.Workbench.ActiveDocumentChanged += DocumentChangedHandler;
			CurrentDoc = IdeApp.Workbench.ActiveDocument;

			InitializeSortingWidgets (window);

			Update ();
		}

		public override void Dispose ()
		{
			IdeApp.Workbench.ActiveDocumentChanged -= DocumentChangedHandler;
			CurrentDoc = null;

			DisposeSortingWidgets ();

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

		public override Gtk.Widget Control {
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

			Gtk.Widget newWidget = null;
			if (outlineDoc != null)
				newWidget = outlineDoc.GetOutlineWidget ();
			if (newWidget == null)
				SetEmptyWidget ();
			else
				SetWidget (newWidget);
			currentOutlineDoc = outlineDoc;

			UpdateSorting ();
		}

		void ReleaseDoc ()
		{
			ReleaseSortableDocumentHandler ();

			RemoveBoxChild ();
			if (currentOutlineDoc != null)
				currentOutlineDoc.ReleaseOutlineWidget ();

			currentOutlineDoc = null;
		}

		void SetEmptyWidget ()
		{
			WrappedCentreLabel label = new WrappedCentreLabel (MonoDevelop.Core.GettextCatalog.GetString (
			    "An outline is not available for the current document."));
			label.Show ();
			SetWidget (label);
		}

		void SetWidget (Gtk.Widget widget)
		{
			RemoveBoxChild ();
			box.Add (widget);
			widget.Show ();
			box.Show ();
		}

		void RemoveBoxChild ()
		{
			Gtk.Widget curChild = box.Child;
			if (curChild != null)
				box.Remove (curChild);
		}

		void InitializeSortingWidgets (IPadWindow window)
		{
			groupToggleButton = new ToggleButton ();
			groupToggleButton.Image = new Image (ImageService.GetPixbuf ("md-design-categorise", IconSize.Menu));
			groupToggleButton.Toggled += new EventHandler (OnButtonGroupClicked);
			groupToggleButton.TooltipText = GettextCatalog.GetString ("Group entries by type");

			sortAlphabeticallyToggleButton = new ToggleButton ();
			sortAlphabeticallyToggleButton.Image = new Image (Gtk.Stock.SortAscending, IconSize.Menu);
			sortAlphabeticallyToggleButton.Toggled += new EventHandler (OnButtonSortAlphabeticallyClicked);
			sortAlphabeticallyToggleButton.TooltipText = GettextCatalog.GetString ("Sort entries alphabetically");

			preferencesButton = new DockToolButton (Gtk.Stock.Preferences);
			preferencesButton.Image = new Image (Gtk.Stock.Preferences, IconSize.Menu);
			preferencesButton.Clicked += new EventHandler (OnButtonPreferencesClicked);
			preferencesButton.TooltipText = GettextCatalog.GetString ("Open preferences dialog");

			separator = new VSeparator ();

			toolbar = window.GetToolbar (PositionType.Top);
			toolbar.Add (groupToggleButton);
			toolbar.Add (sortAlphabeticallyToggleButton);
			toolbar.Add (separator);
			toolbar.Add (preferencesButton);
			toolbar.ShowAll ();

			toolbar.Visible = false;
		}

		void DisposeSortingWidgets ()
		{
			groupToggleButton.Dispose ();
			groupToggleButton = null;

			sortAlphabeticallyToggleButton.Dispose ();
			sortAlphabeticallyToggleButton = null;

			preferencesButton.Dispose ();
			preferencesButton = null;

			separator.Dispose ();
			separator = null;

			toolbar = null;
		}

		void UpdateSorting ()
		{
			bool isSortableOutline = currentOutlineDoc is ISortableOutline;

			// Only show the toolbar if it is a sortable outline

			toolbar.Visible = isSortableOutline;

			if (isSortableOutline)
			{
				// Register for sorting properties change events

				var properties = ((ISortableOutline) currentOutlineDoc).GetSortingProperties ();
				properties.EventSortingPropertiesChanged += OnSortingPropertiesChanged;

				// Update button state to properties

				OnSortingPropertiesChanged (this, EventArgs.Empty);
			}
		}

		void ReleaseSortableDocumentHandler ()
		{
			if (currentOutlineDoc is ISortableOutline)
			{
				// De-register from sorting properties change events

				var properties = ((ISortableOutline) currentOutlineDoc).GetSortingProperties ();
				properties.EventSortingPropertiesChanged -= OnSortingPropertiesChanged;
			}
		}

		void OnButtonGroupClicked (object sender, EventArgs e)
		{
			// Set properties to button state

			var properties = ((ISortableOutline) currentOutlineDoc).GetSortingProperties ();

			properties.IsGrouping = groupToggleButton.Active;

			// Notify listeners on properties changes

			properties.SortingPropertiesChanged (this, EventArgs.Empty);
		}

		void OnButtonSortAlphabeticallyClicked (object sender, EventArgs e)
		{
			// Set properties to button state

			var properties = ((ISortableOutline) currentOutlineDoc).GetSortingProperties ();

			properties.IsSortingAlphabetically = sortAlphabeticallyToggleButton.Active;

			// Notify listeners on properties changes

			properties.SortingPropertiesChanged (this, EventArgs.Empty);
		}

		void OnButtonPreferencesClicked (object sender, EventArgs e)
		{
			var properties = ((ISortableOutline) currentOutlineDoc).GetSortingProperties ();

			SortingPreferencesDialog dialog = new SortingPreferencesDialog (properties);

			dialog.Run ();

			dialog.Destroy ();
		}

		void OnSortingPropertiesChanged (object sender, EventArgs e)
		{
			// Synchronize property state with buttons

			var properties = ((ISortableOutline) currentOutlineDoc).GetSortingProperties ();

			groupToggleButton.Active              = properties.IsGrouping;
			sortAlphabeticallyToggleButton.Active = properties.IsSortingAlphabetically;
		}

		private class WrappedCentreLabel : Gtk.Widget
		{
			string text;
			Pango.Layout layout;

			public WrappedCentreLabel ()
			{
				WidgetFlags |= Gtk.WidgetFlags.NoWindow;
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

			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				if (evnt.Window != GdkWindow || layout == null) {
					return base.OnExposeEvent (evnt);
				}
				layout.Width = (int)(Allocation.Width * 2 / 3 * Pango.Scale.PangoScale);
				Gtk.Style.PaintLayout (Style, GdkWindow, State, false, evnt.Area,
				    this, null, Allocation.Width * 1 / 6 + Allocation.X , 12 + Allocation.Y, layout);
				return true;
			}

			protected override void OnStyleSet (Gtk.Style previous_style)
			{
				CreateLayout ();
				UpdateLayout ();
				base.OnStyleSet (previous_style);
			}

			public override void Dispose ()
			{
				if (layout != null) {
					layout.Dispose ();
					layout = null;
				}
				base.Dispose ();
			}


		}
	}
}
