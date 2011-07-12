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
using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class CustomPropertiesWidget : Gtk.Bin
	{
		TreeStore treeStore = new TreeStore (typeof(string), typeof (PObject));
		Gtk.ListStore keyStore = new ListStore (typeof (string), typeof (PListScheme.Key));
		Gtk.ListStore valueStore = new ListStore (typeof (string), typeof (object));
		Gtk.TreeView treeview1;
		PListScheme scheme;
		
		public PListScheme Scheme {
			get {
				return scheme;
			}
		}
		
		PDictionary nsDictionary;
		public PDictionary NSDictionary {
			get {
				return nsDictionary;
			}
			set {
				nsDictionary = value;
				scheme  = PListScheme.Scheme;
				RefreshTree ();
			}
		}

		public static PObject CreateNewObject (string type)
		{
			switch (type) {
			case "Array":
				return new PArray ();
			case "Dictionary":
				return new PDictionary ();
			case "Boolean":
				return new PBoolean (true);
			case "Data":
				return new PData (new byte[0]);
			case "Date":
				return new PDate (DateTime.Now);
			case "Number":
				return new PNumber (0);
			case "String":
				return new PString ("");
			}
			LoggingService.LogError ("Unknown pobject type:" + type);
			return new PString ("<error>");
		}
		
		
		class PopupTreeView : Gtk.TreeView
		{
			CustomPropertiesWidget widget;
			
			public PopupTreeView (CustomPropertiesWidget widget)
			{
				this.widget = widget;
			}
			
			protected override bool OnPopupMenu ()
			{
				ShowPoupMenu ();
				return base.OnPopupMenu ();
			}
			
			protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
			{
				if (evnt.Button == 3)
					ShowPoupMenu ();
				return base.OnButtonPressEvent (evnt);
			}
			
			void ShowPoupMenu ()
			{
				var menu = new Gtk.Menu ();
				var newKey = new Gtk.MenuItem (GettextCatalog.GetString ("New key"));
				newKey.Activated += delegate(object sender, EventArgs e) {
					Gtk.TreeIter iter;
					if (!Selection.GetSelected (out iter))
						return;
//					var obj = (PObject)widget.treeStore.GetValue (iter, 1);
					var newIter = widget.treeStore.InsertNodeAfter (iter);
					
					widget.treeStore.SetValues (newIter, "newNode", new PString (""));
				};
				menu.Append (newKey);
				
				var removeKey = new Gtk.MenuItem (GettextCatalog.GetString ("Remove key"));
				removeKey.Activated += delegate(object sender, EventArgs e) {
					Gtk.TreeIter iter;
					if (!Selection.GetSelected (out iter))
						return;
					var obj = (PObject)widget.treeStore.GetValue (iter, 1);
					obj.Remove ();
					widget.treeStore.Remove (ref iter);
				};
				menu.Append (removeKey);
				IdeApp.CommandService.ShowContextMenu (menu, this);
				menu.ShowAll ();
			}
		}
		
		public CustomPropertiesWidget ()
		{
			this.Build ();
			treeview1 = new PopupTreeView  (this);
			this.vbox1.PackStart (treeview1, true, true, 0);
			ShowAll ();
			
			var keyRenderer = new CellRendererCombo ();
			keyRenderer.Editable = true;
			keyRenderer.Model = keyStore;
			keyRenderer.Mode = CellRendererMode.Editable;
			keyRenderer.TextColumn = 0;
			keyRenderer.Edited += delegate(object o, EditedArgs args) {
				TreeIter selIter;
				if (!treeStore.GetIterFromString (out selIter, args.Path)) 
					return;
				if (args.NewText == (string)treeStore.GetValue (selIter, 0))
					return;
				var key = scheme.Keys.FirstOrDefault (k => k.Identifier == args.NewText || k.Description == args.NewText);
				if (key != null) {
					treeStore.SetValue (selIter, 0, key.Identifier);
					treeStore.SetValue (selIter, 1, CreateNewObject (key.Type));
				} else {
					treeStore.SetValue (selIter, 0, args.NewText);
					treeStore.SetValue (selIter, 1, CreateNewObject ("String"));
				}
			};
			
			treeview1.AppendColumn (GettextCatalog.GetString ("Key"), keyRenderer, delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererCombo)cell;
				string id = (string)tree_model.GetValue (iter, 0) ?? "";
				var obj = (PObject)tree_model.GetValue (iter, 1);
				var key = scheme.GetKey (id);
				renderer.Editable = !(obj.Parent is PArray);
				renderer.Text = key != null ? GettextCatalog.GetString (key.Description) : id;
			});
			
			var comboRenderer = new CellRendererCombo ();
			
			var typeModel = new ListStore (typeof (string));
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
				TreeIter selIter;
				if (!treeview1.Selection.GetSelected (out selIter))
					return;
				
				treeStore.SetValue (selIter, 1, CreateNewObject (args.NewText));
			};
			
			treeview1.AppendColumn (GettextCatalog.GetString ("Type"), comboRenderer, delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererCombo)cell;
				string id = (string)tree_model.GetValue (iter, 0) ?? "";
				var key   = scheme.GetKey (id);
				var obj   = (PObject)tree_model.GetValue (iter, 1);
				renderer.Editable = key == null;
				renderer.ForegroundGdk = Style.Text (renderer.Editable ? StateType.Normal : StateType.Insensitive);
				renderer.Text = obj.TypeString;
			});
			
			var propRenderer = new CellRendererCombo ();
			
			propRenderer.Model = valueStore;
			propRenderer.Mode = CellRendererMode.Editable;
			propRenderer.TextColumn = 0;
			propRenderer.EditingStarted += delegate(object o, EditingStartedArgs args) {
				valueStore.Clear ();
			};
			
	/*		propRenderer.EditingStarted += delegate(object o, EditingStartedArgs args) {
				TreeIter iter;
				if (!treeStore.GetIterFromString (out iter, args.Path)) 
					return;
				PObject obj = (PObject)treeStore.GetValue (iter, 1);
				if (obj is PBoolean) {
					((PBoolean)obj).Value = !((PBoolean)obj).Value;
					propRenderer.StopEditing (false);
				}
			};*/
			
			
			treeview1.AppendColumn (GettextCatalog.GetString ("Value"), propRenderer, delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererCombo)cell;
				var obj      = (PObject)tree_model.GetValue (iter, 1);
				renderer.Editable = !(obj is PArray || obj is PDictionary);
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
			keyStore.Clear ();
			if (scheme != null) {
				foreach (var key in scheme.Keys)
					keyStore.AppendValues (key.Description, key);
			}
			
			treeStore.Clear ();
			if (nsDictionary != null)
				AddToTree (treeStore, Gtk.TreeIter.Zero, nsDictionary);
		}
		
		/*public class CellRendererProperty : CellRenderer
		{
			public PObject Object {
				get;
				set;
			}
			
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
			
			public override CellEditable StartEditing (Gdk.Event ev, Widget widget, string path, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, CellRendererState flags)
			{
				if (Object is PBoolean) {
					((PBoolean)Object).Value = !((PBoolean)Object).Value;
					return null;
				}
				
				Gtk.Entry entry = new Gtk.Entry ();
				return entry;
			}
		}*/
	}
}

