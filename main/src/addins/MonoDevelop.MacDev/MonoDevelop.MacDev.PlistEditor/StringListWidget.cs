// 
// StringListWidget.cs
//  
// Author: Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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
using System.Linq;
using System.Collections.Generic;

using Gtk;
using MonoMac.Foundation;

using Mono.TextEditor;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem(true)]
	public class StringListWidget : VBox, IRawPListDisplayWidget
	{
		const string AddNewEntry = "Add new entry";
		
		enum ListColumn {
			DisplayValue,
			Object,
		}
		
		TreeStore treeStore = new TreeStore (typeof (string), typeof (PString));
		bool editing = false;
		TreeView treeview;
		PArray array;
		
		class CellRendererAddRemove : CellRenderer
		{
			static Gdk.Pixbuf RemoveIcon = ImageService.GetPixbuf ("gtk-remove", IconSize.Menu);
			static Gdk.Pixbuf AddIcon = ImageService.GetPixbuf ("gtk-add", IconSize.Menu);
			public static int MinWidth = Math.Max (AddIcon.Width, RemoveIcon.Width);
			
			public enum AddRemoveAction {
				Add,
				Remove,
			}
			
			public AddRemoveAction Action {
				get; set;
			}
			
			public CellRendererAddRemove ()
			{
				Mode = CellRendererMode.Editable;
			}
			
			protected virtual void OnClicked (EventArgs e)
			{
				EventHandler handler = Action == AddRemoveAction.Add ? AddClicked : RemoveClicked;
				
				if (handler != null)
					handler (this, e);
			}
			
			public event EventHandler RemoveClicked;
			public event EventHandler AddClicked;
			
			public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
			{
				var pixbuf = Action == AddRemoveAction.Add ? AddIcon : RemoveIcon;
				
				x_offset = 0;
				y_offset = 0;
				
				width = pixbuf.Width;
				height = pixbuf.Width;
			}
			
			public override CellEditable StartEditing (Gdk.Event evnt, Widget widget, string path, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, CellRendererState flags)
			{
				if (evnt.Type == Gdk.EventType.ButtonPress)
					OnClicked (EventArgs.Empty);
				
				return base.StartEditing (evnt, widget, path, background_area, cell_area, flags);
			}
			
			protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
			{
				if (!Visible)
					return;
				
				var pixbuf = Action == AddRemoveAction.Add ? AddIcon : RemoveIcon;
				int x = cell_area.X;
				int y = cell_area.Y;
				
				window.DrawPixbuf (widget.Style.BaseGC (StateType.Normal), pixbuf, 0, 0, x, y, pixbuf.Width, pixbuf.Height, Gdk.RgbDither.None, 0, 0);
			}
		}
		
		public StringListWidget ()
		{
			treeview = new Gtk.TreeView ();
			treeview.HeadersVisible = false;
			PackStart (treeview, true, true, 0);
			ShowAll ();
			
			var col = new TreeViewColumn { MinWidth = CellRendererAddRemove.MinWidth, Resizable = true, Sizing = Gtk.TreeViewColumnSizing.Autosize };
			var addRemoveRenderer = new CellRendererAddRemove ();
			col.PackStart (addRemoveRenderer, false);
			col.SetCellDataFunc (addRemoveRenderer, delegate (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				if (treeStore.GetValue (iter, (int) ListColumn.Object) != null)
					addRemoveRenderer.Action = CellRendererAddRemove.AddRemoveAction.Remove;
				else
					addRemoveRenderer.Action = CellRendererAddRemove.AddRemoveAction.Add;
			});
			addRemoveRenderer.RemoveClicked += delegate {
				TreeIter iter;
				bool hasSelection = treeview.Selection.GetSelected (out iter);
				PObject obj = null;
				if (hasSelection) {
					obj = (PObject) treeStore.GetValue (iter, (int) ListColumn.Object);
					treeStore.Remove (ref iter);

					editing = true;
					obj.Remove ();
					editing = false;
				}
			};
			addRemoveRenderer.AddClicked += delegate {
				PObject obj = new PString (string.Empty);
				TreeIter iter;

				bool hasSelection = treeview.Selection.GetSelected (out iter);
				if (hasSelection) {
					treeStore.SetValues (iter, string.Empty, obj);
					AppendCreateNewEntry ();

					editing = true;
					array.Add (obj);
					editing = false;
				}
			};
			treeview.AppendColumn (col);
			
			var valueRenderer = new CellRendererCombo ();
			valueRenderer.Mode = CellRendererMode.Editable;
			valueRenderer.TextColumn = (int) ListColumn.DisplayValue;
			
			valueRenderer.Edited += delegate (object o, EditedArgs args) {
				TreeIter iter;
				
				if (!treeStore.GetIterFromString (out iter, args.Path))
					return;
				
				var pObject = (PObject) treeStore.GetValue (iter, (int) ListColumn.Object);
				if (pObject == null)
					return;
				
				string newValue = !string.IsNullOrEmpty (ValuePrefix) ? ValuePrefix + args.NewText : args.NewText;
				
				pObject.SetValue (newValue);
			};
			
			treeview.AppendColumn (GettextCatalog.GetString ("Value"), valueRenderer, delegate (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var text     = (string) tree_model.GetValue (iter, (int) ListColumn.DisplayValue);
				var obj      = (PObject) tree_model.GetValue (iter, (int) ListColumn.Object);
				var renderer = (CellRendererCombo) cell;
				
				renderer.Sensitive = obj != null;
				renderer.Editable = obj != null;
				renderer.Text = text;
			});
			
			treeview.EnableGridLines = TreeViewGridLines.Horizontal;
			treeview.Model = treeStore;
		}
		
		public string ValuePrefix {
			get; set;
		}
		
		public bool ShowDescriptions {
			get { return false; }
		}
		
		public PListScheme Scheme {
			get { return null; }
		}
		
		public void SetPListContainer (PObjectContainer container)
		{
			if (!(container is PArray))
				throw new ArgumentException ("The PList container must be a PArray.", "container");
			
			array = (PArray) container;
			array.Changed += OnArrayChanged;
			RefreshList ();
		}

		void OnArrayChanged (object sender, EventArgs e)
		{
			if (editing)
				return;

			RefreshList ();
			QueueDraw ();
		}

		void AppendCreateNewEntry ()
		{
			treeStore.AppendValues (GettextCatalog.GetString (AddNewEntry), null);
		}

		void Rebuild (object sender, EventArgs e)
		{
			RefreshList ();
		}
		
		void RefreshList ()
		{
			treeStore.Clear ();
			
			for (int i = 0; i < array.Count; i++) {
				var item = array[i];
				var str = item as PString;
				
				if (str == null)
					continue;
				
				string value = str.Value;
				if (!string.IsNullOrEmpty (ValuePrefix) && value.StartsWith (ValuePrefix))
					value = value.Substring (ValuePrefix.Length);
				
				treeStore.AppendValues (value, item);
			}
			
			AppendCreateNewEntry ();
		}
	}
}
