// 
// CustomPropertiesWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using Gtk;
using MonoDevelop.Core;
using System.Collections.Generic;
using MonoMac.Foundation;

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class CustomPropertiesWidget : Gtk.Bin
	{
		TreeStore treeStore = new TreeStore (typeof(string), typeof (PObject));
		PListSheme sheme;
		
		public PListSheme Sheme {
			get {
				return sheme;
			}
		}
		
		PDictionary nsDictionary;
		public PDictionary NSDictionary {
			get {
				return nsDictionary;
			}
			set {
				nsDictionary = value;
				sheme  = PListSheme.Sheme;
				RefreshTree ();
			}
		}
		
		public CustomPropertiesWidget ()
		{
			this.Build ();
			
			treeview1.AppendColumn (GettextCatalog.GetString ("Key"), new CellRendererText (), delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererText)cell;
				string id = (string)tree_model.GetValue (iter, 0) ?? "";
				var key = sheme.GetKey (id);
				renderer.Text = key != null ? GettextCatalog.GetString (key.Description) : id;
			});
			var comboRenderer = new CellRendererCombo ();
			
			Gtk.ListStore typeModel = new ListStore (typeof (string));
			typeModel.AppendValues ("Array");
			typeModel.AppendValues ("Dictionary");
			typeModel.AppendValues ("Boolean");
			typeModel.AppendValues ("Data");
			typeModel.AppendValues ("Date");
			typeModel.AppendValues ("Number");
			typeModel.AppendValues ("String");
			comboRenderer.Model = typeModel;
			comboRenderer.Mode = CellRendererMode.Editable;
			comboRenderer.TextColumn = 0;
			comboRenderer.Edited += delegate(object o, EditedArgs args) {
				TreeIter iter;
				if (!treeStore.GetIterFromString (out iter, args.Path)) 
					return;
				NSObject newObject;
				switch (args.NewText) {
				case "Array":
					newObject = new NSArray ();
					break;
				case "Dictionary":
					newObject = new NSDictionary ();
					break;
				case "Boolean":
					newObject = new NSString ();
					break;
				case "Data":
					newObject = new NSData ();
					break;
				case "Date":
					newObject = new NSDate ();
					break;
				case "Number":
					newObject = new NSNumber ();
					break;
				case "String":
					newObject = new NSString ();
					break;
				}
				treeStore.SetValue (iter, 1, newObject);
			};
			
			treeview1.AppendColumn (GettextCatalog.GetString ("Type"), new CellRendererCombo (), delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererCombo)cell;
				string id = (string)tree_model.GetValue (iter, 0) ?? "";
				var key   = sheme.GetKey (id);
				var obj   = (PObject)tree_model.GetValue (iter, 1);
				renderer.Editable = key == null;
				renderer.ForegroundGdk = Style.Text (key == null ? StateType.Normal : StateType.Insensitive);
				renderer.Text = obj.TypeString;
			});
			
			treeview1.AppendColumn (GettextCatalog.GetString ("Value"), new CellRendererProperty (), delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererProperty)cell;
				var obj      = (PObject)tree_model.GetValue (iter, 1);
				obj.RenderValue (this, renderer);
			});
			treeview1.EnableGridLines = TreeViewGridLines.Horizontal;
			treeview1.Model = treeStore;
		}
		
		static void AddToTree (Gtk.TreeStore treeStore, Gtk.TreeIter iter, PDictionary dict)
		{
			foreach (var item in dict.Value) {
				var key = item.Key.ToString ();
				var subIter = iter.Equals (TreeIter.Zero) ? treeStore.AppendValues (key, item.Value) : treeStore.AppendValues (iter, key, item.Value);
				if (item.Value is PArray)
					AddToTree (treeStore, subIter, (PArray)item.Value);
				if (item.Value is PDictionary)
					AddToTree (treeStore, subIter, (PDictionary)item.Value);
			}
		}
		
		static void AddToTree (Gtk.TreeStore treeStore, Gtk.TreeIter iter, PArray arr)
		{
			for (int i = 0; i < arr.Value.Count; i++) {
				var item = arr.Value [i];
				
				var txt = string.Format (GettextCatalog.GetString ("Item {0}"), i);
				var subIter = iter.Equals (TreeIter.Zero) ? treeStore.AppendValues (txt, item) : treeStore.AppendValues (iter, txt, item);
				
				if (item is PArray)
					AddToTree (treeStore, subIter, (PArray)item);
				if (item is PDictionary)
					AddToTree (treeStore, subIter, (PDictionary)item);
			}
		}
		
		void RefreshTree ()
		{
			treeStore.Clear ();
			if (nsDictionary != null)
				AddToTree (treeStore, Gtk.TreeIter.Zero, nsDictionary);
		}
		
		public class CellRendererProperty : CellRenderer
		{
			public object RenderValue {
				get;
				set;
			}
			
			public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
			{
				using (var layout = new Pango.Layout (widget.PangoContext)) {
					layout.SetMarkup (RenderValue.ToString ());
					layout.Width = -1;
					layout.GetPixelSize (out width, out height);
					width += (int)Xpad * 2;
					height += (int)Ypad * 2;
					
					x_offset = y_offset = 0;
				}
			}
			
			protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
			{
				using (var layout = new Pango.Layout (widget.PangoContext)) {
					layout.SetMarkup (RenderValue.ToString ());
					layout.Width = -1;
					int width, height;
					layout.GetPixelSize (out width, out height);
					
					int x = (int) (cell_area.X + Xpad);
					int y = cell_area.Y + (cell_area.Height - height) / 2;
					
					StateType state;
					if (flags.HasFlag (CellRendererState.Selected)) {
						state = StateType.Selected;
					} else {
						state = Sensitive ? StateType.Normal : StateType.Insensitive;
					}
					
					window.DrawLayout (widget.Style.TextGC (state), x, y, layout);
				}
			}
		}
	}
}

