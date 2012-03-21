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
using Mono.TextEditor;

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem(true)]
	public class CustomPropertiesWidget : VBox, IRawPListDisplayWidget
	{
		const string AddKeyNode = "Add new entry";
		const string DefaultNewObjectType = PString.Type;
		
		TreeStore treeStore = new TreeStore (typeof(string), typeof (PObject));
		Gtk.ListStore keyStore = new ListStore (typeof (string), typeof (PListScheme.Key));
		Gtk.ListStore valueStore = new ListStore (typeof (string), typeof (string));
		PopupTreeView treeview;
		PListScheme scheme;
		HashSet<PObject> expandedObjects = new HashSet<PObject> ();
		bool showDescriptions = true;
		
		public bool ShowDescriptions {
			get { return showDescriptions; }
		}
		
		void SetShowDescriptions (bool value)
		{
			showDescriptions = value;
			QueueDraw ();
		}
		
		public PListScheme Scheme {
			get {
				return scheme;
			}
		}
		
		PObject nsDictionary;
		public void SetPListContainer (PObjectContainer value)
		{
			nsDictionary = value;
			nsDictionary.Changed += delegate {
				QueueDraw ();
			};
			RefreshTree ();
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
		
		class PopupTreeView : MonoDevelop.Components.ContextMenuTreeView
		{
			CustomPropertiesWidget widget;
			
			public PopupTreeView (CustomPropertiesWidget widget)
			{
				this.widget = widget;
				this.DoPopupMenu += ShowPopup;
			}
			
			void ShowPopup (Gdk.EventButton evnt)
			{
				Gtk.TreeIter iter;
				bool hasSelection = Selection.GetSelected (out iter);
				PObject obj = null;
				if (hasSelection) {
					obj = (PObject)widget.treeStore.GetValue (iter, 1);
				} else {
					return;
				}
					
				var menu = new Gtk.Menu ();
				var newKey = new Gtk.MenuItem (GettextCatalog.GetString ("New key"));
				menu.Append (newKey);
				
				newKey.Activated += delegate(object sender, EventArgs e) {
					var newObj = new PString ("");
					
					PObject parent;
					if (obj != null) {
						parent = obj.Parent;
					} else {
						Gtk.TreeIter parentIter;
						if (widget.treeStore.IterParent (out parentIter, iter))
							parent = (PObject)widget.treeStore.GetValue (parentIter, 1);
						else
							parent = widget.nsDictionary;
					}
					
					if (parent is PArray) {
						var arr = (PArray)parent;
						arr.Add (newObj);
						return;
					}
					
					var dict = parent as PDictionary;
					if (dict == null)
						return;
					
					string name = "newNode";
					while (dict.ContainsKey (name))
						name += "_";
					dict[name] = newObj;
				};
				
				if (hasSelection && obj != null) {
					var removeKey = new Gtk.MenuItem (GettextCatalog.GetString ("Remove key"));
					menu.Append (removeKey);
					removeKey.Activated += delegate(object sender, EventArgs e) {
						//the change event handler removes it from the store
						obj.Remove ();
					};
				}
				
				if (widget.scheme != null) {
					menu.Append (new Gtk.SeparatorMenuItem ());
					var showDescItem = new Gtk.CheckMenuItem (GettextCatalog.GetString ("Show descriptions"));
					showDescItem.Active = widget.ShowDescriptions;
					showDescItem.Activated += delegate {
						widget.SetShowDescriptions (!widget.ShowDescriptions);
					};
					menu.Append (showDescItem);
				}
				menu.ShowAll ();
				IdeApp.CommandService.ShowContextMenu (this, evnt, menu, this);
			}

			
			protected override void OnSizeRequested (ref Requisition requisition)
			{
				base.OnSizeRequested (ref requisition);
				
				if (widget.treeStore.IterNChildren () == 0) {
				//Pad the bottom with empty space. This  prevents empty plists from having nowhere to click to adds rows
					requisition.Height += 20;
				}
			}
		}
		
		public CustomPropertiesWidget ()
			: this (null)
		{
		}
		
		class CellRendererButton : CellRenderer
		{
			Gdk.Pixbuf pixbuf;

			public CellRendererButton (Gdk.Pixbuf pixbuf)
			{
				this.pixbuf = pixbuf;
				base.Mode = CellRendererMode.Editable;
			}
			
			protected virtual void OnClicked (System.EventArgs e)
			{
				EventHandler handler = this.Clicked;
				if (handler != null)
					handler (this, e);
			}
			
			public event EventHandler Clicked;
			
			public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
			{
				x_offset = 0;
				y_offset = 0;
				width = 2;
				width += pixbuf.Width;
				height = pixbuf.Width + 2;
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
				int x = cell_area.X + 1;
				int y = cell_area.Y + 1;
				window.DrawPixbuf (widget.Style.BaseGC (StateType.Normal), pixbuf, 0, 0, x, y, pixbuf.Width, pixbuf.Height, Gdk.RgbDither.None, 0, 0);
			}
		}
		
		public CustomPropertiesWidget (PListScheme scheme)
		{
			this.scheme = scheme = scheme ?? PListScheme.Empty;
			treeview = new PopupTreeView  (this);
			treeview.HeadersClickable = true;
			this.PackStart (treeview, true, true, 0);
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
				
				var obj = (PObject)treeStore.GetValue (selIter, 1);
				
				var dict = obj.Parent as PDictionary;
				if (dict == null)
					return;
				
				var key = scheme.Keys.FirstOrDefault (k => k.Identifier == args.NewText || k.Description == args.NewText);
				var newKey = key != null ? key.Identifier : args.NewText;
				
				dict.ChangeKey (obj, newKey, key == null || obj.TypeString == key.Type ? null : CreateNewObject (key.Type));
			};
			var col = new TreeViewColumn ();
			col.Resizable = true;
			col.Title = GettextCatalog.GetString ("Property");
			col.PackStart (keyRenderer, true);
			col.SetCellDataFunc (keyRenderer, delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererCombo)cell;
				string id = (string)tree_model.GetValue (iter, 0) ?? "";
				var obj = (PObject)tree_model.GetValue (iter, 1);
				if (obj == null) {
					renderer.Text = id;
					renderer.Editable = false;
					renderer.Sensitive = false;
					return;
				}
				
				var key = scheme.GetKey (id);
				renderer.Editable = !(obj.Parent is PArray);
				renderer.Sensitive = true;
				renderer.Text = key != null && ShowDescriptions ? GettextCatalog.GetString (key.Description) : id;
			});
			treeview.AppendColumn (col);
			
			col = new TreeViewColumn { MinWidth = 25, Resizable = true, Sizing = Gtk.TreeViewColumnSizing.Autosize };
			
			var removeRenderer = new CellRendererButton (ImageService.GetPixbuf ("gtk-remove", IconSize.Menu));
			removeRenderer.Clicked += delegate {
				TreeIter iter;
				bool hasSelection = treeview.Selection.GetSelected (out iter);
				PObject obj = null;
				if (hasSelection) {
					obj = (PObject)treeStore.GetValue (iter, 1);
					obj.Remove ();
				}
			};
			col.PackEnd (removeRenderer, false);
			col.SetCellDataFunc (removeRenderer, delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				removeRenderer.Visible = treeview.Selection.IterIsSelected (iter) && !AddKeyNode.Equals (treeStore.GetValue (iter, 0));
			});
			
			var addRenderer = new CellRendererButton (ImageService.GetPixbuf ("gtk-add", IconSize.Menu));
			addRenderer.Clicked += delegate {
				// By default we assume we are adding something to the root dictionary/array
				var iter = TreeIter.Zero;
				var parent = nsDictionary;
				var parentKey = "";
				
				// Grab the selected row and find out what the parent is. If there is a parent, then we
				// will need it correlate against the schema to figure out what values are allowed to be
				// entered here
				if (treeview.Selection.GetSelected (out iter)) {
					if (treeStore.IterParent (out iter, iter)) {
						parentKey = (string) treeStore.GetValue (iter, 0);
						parent = (PObject) treeStore.GetValue (iter, 1);
					}

					if (parent is PArray)
						AddNewArrayElement ((PArray) parent, scheme.GetKey (parentKey));
					else if (parent is PDictionary)
						AddNewDictionaryElement ((PDictionary) parent, scheme.GetKey (parentKey));
				}
			};
			
			col.PackEnd (addRenderer, false);
			col.SetCellDataFunc (addRenderer, delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				addRenderer.Visible = treeview.Selection.IterIsSelected (iter) && AddKeyNode.Equals (treeStore.GetValue (iter, 0));
			});
			treeview.AppendColumn (col);
			
			treeview.RowExpanded += delegate(object o, RowExpandedArgs args) {
				var obj = (PObject)treeStore.GetValue (args.Iter, 1);
				expandedObjects.Add (obj);
			};
			treeview.RowCollapsed += delegate(object o, RowCollapsedArgs args) {
				var obj = (PObject)treeStore.GetValue (args.Iter, 1);
				expandedObjects.Remove (obj);
			};
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
			comboRenderer.HasEntry = false;
			comboRenderer.TextColumn = 0;
			comboRenderer.Edited += delegate(object o, EditedArgs args) {
				TreeIter selIter;
				if (!treeStore.GetIterFromString (out selIter, args.Path)) 
					return;
				
				PObject oldObj = (PObject)treeStore.GetValue (selIter, 1);
				if (oldObj != null && oldObj.TypeString != args.NewText)
					oldObj.Replace (CreateNewObject (args.NewText));
			};
			
			treeview.AppendColumn (GettextCatalog.GetString ("Type"), comboRenderer, delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererCombo)cell;
				string id = (string)tree_model.GetValue (iter, 0) ?? "";
				var key   = scheme.GetKey (id);
				var obj   = (PObject)tree_model.GetValue (iter, 1);
				if (obj == null) {
					renderer.Editable = false;
					renderer.Text = "";
					return; 
				}
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
				if (Scheme == null)
					return;
				TreeIter iter;
				if (!treeStore.GetIterFromString (out iter, args.Path)) 
					return;
				var pObject = (PObject)treeStore.GetValue (iter, 1);
				if (pObject == null)
					return;
				var key = Parent != null? Scheme.GetKey (pObject.Parent.Key) : null;
				if (key != null) {
					var descr = new List<string> (key.Values.Select (v => v.Description));
					descr.Sort ();
					foreach (var val in descr) {
						valueStore.AppendValues (val);
					}
				}
			};
			
			propRenderer.Edited += delegate(object o, EditedArgs args) {
				TreeIter iter;
				if (!treeStore.GetIterFromString (out iter, args.Path)) 
					return;
				var pObject = (PObject)treeStore.GetValue (iter, 1);
				if (pObject == null)
					return;
				string newText = args.NewText;
				var key = Parent != null? Scheme.GetKey (pObject.Parent.Key) : null;
				if (key != null) {
					foreach (var val in key.Values) {
						if (newText == val.Description) {
							newText = val.Identifier;
							break;
						}
					}
				}
				pObject.SetValue (newText);
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
			
			
			treeview.AppendColumn (GettextCatalog.GetString ("Value"), propRenderer, delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererCombo)cell;
				var obj      = (PObject)tree_model.GetValue (iter, 1);
				if (obj == null) {
					renderer.Editable = false;
					renderer.Text = "";
					return;
				}
				renderer.Editable = !(obj is PArray || obj is PDictionary || obj is PData);
				obj.RenderValue (this, renderer);
			});
			treeview.EnableGridLines = TreeViewGridLines.Horizontal;
			treeview.Model = treeStore;
		}
		
		void AddNewArrayElement (PArray array, PListScheme.Key key)
		{
			if (key == null) {
				array.Add (CreateNewObject (DefaultNewObjectType));
				return;
			}
			
			var usedValues = array;
			var allowedValues = new List<PListScheme.Value> (key.Values);
			foreach (var allowed in key.Values) {
				foreach (var used in usedValues) {
					if (used is PString && key.ArrayType == used.TypeString) {
						if (((PString)used).Value == allowed.Identifier)
							allowedValues.Remove (allowed);
					} else if (used is PNumber && key.ArrayType == used.TypeString) {
						if (((PNumber) used).Value.ToString () == allowed.Identifier) {
							allowedValues.Remove (allowed);
						}
					}
				}
			}
			
			if (allowedValues.Count > 0 && key.Values.Count > 0) {
				var newKey = allowedValues.First ();
				if (key.ArrayType == PString.Type) {
					array.Add (new PString (newKey.Identifier));
				} else if (key.ArrayType == PNumber.Type) {
					array.Add (new PNumber (int.Parse (newKey.Identifier)));
				} else {
					array.Add (CreateNewObject (key.ArrayType ?? DefaultNewObjectType));
				}
			} else {
				array.Add (CreateNewObject (key.ArrayType ?? DefaultNewObjectType));
			}
		}
		
		void AddNewDictionaryElement (PDictionary dict, PListScheme.Key key)
		{
			string name = "newNode";
			while (dict.ContainsKey (name))
				name += "_";
			dict.Add (name, new PString (""));
		}

		bool GetIsExpanded (Gtk.TreeIter iter, TreeStore treeStore)
		{
			if (TreeIter.Zero.Equals (iter))
				return  false;
			var path = treeStore.GetPath (iter);
			return path != null ? treeview.GetRowExpanded (path) : false;
		}
		
		Dictionary<PObject, Gtk.TreeIter> iterTable = new Dictionary<PObject, Gtk.TreeIter> ();

		void AddCreateNewEntry (Gtk.TreeIter iter)
		{
			if (iter.Equals (TreeIter.Zero)) {
				treeStore.AppendValues (GettextCatalog.GetString (AddKeyNode), null);
				return;
			}
			treeStore.AppendValues (iter, GettextCatalog.GetString (AddKeyNode), null);
		}
		
		void AddToTree (Gtk.TreeStore treeStore, Gtk.TreeIter iter, PDictionary dict)
		{
			iterTable[dict] = iter;
			foreach (var item in dict) {
				var key = item.Key.ToString ();
				var subIter = iter.Equals (TreeIter.Zero) ? treeStore.AppendValues (key, item.Value) : treeStore.AppendValues (iter, key, item.Value);
				if (item.Value is PArray)
					AddToTree (treeStore, subIter, (PArray)item.Value);
				if (item.Value is PDictionary)
					AddToTree (treeStore, subIter, (PDictionary)item.Value);
				if (expandedObjects.Contains (item.Value))
					treeview.ExpandRow (treeStore.GetPath (subIter), true);
			}
			AddCreateNewEntry (iter);
			
			if (!rebuildArrays.Contains (dict)) {
				rebuildArrays.Add (dict);
				dict.Changed += HandleDictRebuild;
			}
		}

		void HandleDictRebuild (object sender, EventArgs e)
		{
			var dict = (PDictionary)sender;
			var iter = iterTable[dict];
			bool isExpanded = GetIsExpanded (iter, treeStore);
			RemoveChildren (iter);
			AddToTree (treeStore, iter, dict);
			if (isExpanded)
				treeview.ExpandRow (treeStore.GetPath (iter), false);
		}
		
		HashSet<PObject> rebuildArrays = new HashSet<PObject> ();
		
		void AddToTree (Gtk.TreeStore treeStore, Gtk.TreeIter iter, PArray arr)
		{
			iterTable[arr] = iter;
			
			for (int i = 0; i < arr.Count; i++) {
				var item = arr[i];
				
				var txt = string.Format (GettextCatalog.GetString ("Item {0}"), i);
				var subIter = iter.Equals (TreeIter.Zero) ? treeStore.AppendValues (txt, item) : treeStore.AppendValues (iter, txt, item);
				
				if (item is PArray)
					AddToTree (treeStore, subIter, (PArray)item);
				if (item is PDictionary)
					AddToTree (treeStore, subIter, (PDictionary)item);
				if (expandedObjects.Contains (item))
					treeview.ExpandRow (treeStore.GetPath (subIter), true);
			}

			AddCreateNewEntry  (iter);
			
			if (!rebuildArrays.Contains (arr)) {
				rebuildArrays.Add (arr);
				arr.Changed += HandleArrRebuild;
			}
		}

		void HandleArrRebuild (object sender, EventArgs e)
		{
			var arr = (PArray)sender;
			var iter = iterTable[arr];
			bool isExpanded = GetIsExpanded (iter, treeStore);
			RemoveChildren (iter);
			AddToTree (treeStore, iter, arr);
			if (isExpanded)
				treeview.ExpandRow (treeStore.GetPath (iter), false);
		}

		void RemoveChildren (Gtk.TreeIter iter)
		{
			if (TreeIter.Zero.Equals (iter)) {
				treeStore.Clear ();
				return;
			}
			Gtk.TreeIter child;
			while (treeStore.IterChildren (out child, iter))
				treeStore.Remove (ref child);
		}
		
		void RefreshTree ()
		{
			keyStore.Clear ();
			if (scheme != null) {
				var sortedKeys = new List<PListScheme.Key> (scheme.Keys);
				sortedKeys.Sort ((x, y) => x.Description.CompareTo (y.Description));
				foreach (var key in sortedKeys)
					keyStore.AppendValues (key.Description, key);
			}
			
			treeStore.Clear ();
			if (nsDictionary is PDictionary) 
				AddToTree (treeStore, Gtk.TreeIter.Zero, (PDictionary)nsDictionary);
			if (nsDictionary is PArray) 
				AddToTree (treeStore, Gtk.TreeIter.Zero, (PArray)nsDictionary);
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

