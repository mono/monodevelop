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
		static readonly string AddKeyNode = GettextCatalog.GetString ("Add new entry");
		const string DefaultNewObjectType = PString.Type;
		
		TreeStore treeStore = new TreeStore (typeof(string), typeof (PObject), typeof (PListScheme.SchemaItem));
		Gtk.ListStore keyStore = new ListStore (typeof (string), typeof (PListScheme.Key));
		Gtk.ListStore valueStore = new ListStore (typeof (string), typeof (string));
		PopupTreeView treeview;
		bool showDescriptions = true;
		
		public bool ShowDescriptions {
			get { return showDescriptions; }
			private set {
				if (showDescriptions != value) {
					showDescriptions = value;
					QueueDraw ();
				}
			}
		}
		
		public PListScheme Scheme {
			get; private set;
		}
		
		PObject nsDictionary;
		public void SetPListContainer (PObjectContainer value)
		{
			nsDictionary = value;
			nsDictionary.Changed += delegate {
				QueueDraw ();
				RefreshTree ();
			};
			RefreshTree ();
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
				
				if (widget.Scheme != null) {
					menu.Append (new Gtk.SeparatorMenuItem ());
					var showDescItem = new Gtk.CheckMenuItem (GettextCatalog.GetString ("Show descriptions"));
					showDescItem.Active = widget.ShowDescriptions;
					showDescItem.Activated += delegate {
						widget.ShowDescriptions = !widget.ShowDescriptions;
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
			Scheme = scheme ?? PListScheme.Empty;
			treeview = new PopupTreeView  (this) { DoubleBuffered = true };
			treeview.HeadersClickable = true;
			
			RefreshKeyStore ();
			PackStart (treeview, true, true, 0);
			FindOrAddNewEntry (TreeIter.Zero);
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
				
				var key = Scheme.Keys.FirstOrDefault (k => k.Identifier == args.NewText || k.Description == args.NewText);
				var newKey = key != null ? key.Identifier : args.NewText;
				
				dict.ChangeKey (obj, newKey, key == null ? null : key.Create ());
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
				
				var key = Scheme.GetKey (id);
				renderer.Editable = !(obj.Parent is PArray);
				renderer.Sensitive = true;
				renderer.Text = key != null && ShowDescriptions ? GettextCatalog.GetString (key.Description) : id;
			});
			treeview.AppendColumn (col);
			
			col = new TreeViewColumn { MinWidth = 25, Resizable = false, Sizing = Gtk.TreeViewColumnSizing.Fixed };
			
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
						AddNewArrayElement ((PArray) parent, Scheme.GetKey (parentKey));
					else if (parent is PDictionary)
						AddNewDictionaryElement ((PDictionary) parent, Scheme.GetKey (parentKey));
				}
			};
			
			col.PackEnd (addRenderer, false);
			col.SetCellDataFunc (addRenderer, delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				addRenderer.Visible = treeview.Selection.IterIsSelected (iter) && AddKeyNode.Equals (treeStore.GetValue (iter, 0));
			});
			treeview.AppendColumn (col);

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
					oldObj.Replace (PObject.Create (args.NewText));
			};
			
			treeview.AppendColumn (GettextCatalog.GetString ("Type"), comboRenderer, delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererCombo)cell;
				string id = (string)tree_model.GetValue (iter, 0) ?? "";
				var key   = Scheme.GetKey (id);
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
				TreeIter iter;
				if (!treeStore.GetIterFromString (out iter, args.Path)) 
					return;
				
				var item = (PListScheme.SchemaItem)treeStore.GetValue (iter, 2);
				if (item != null) {
					var descr = new List<string> (item.Values.Select (v => ShowDescriptions ? v.Description : v.Identifier));
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
				array.Add (PObject.Create (DefaultNewObjectType));
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
			
			if (key.Values.Count == 0) {
				array.Add (PObject.Create (key.ArrayType ?? DefaultNewObjectType));
			} else if (allowedValues.Count > 0) {
				var newKey = allowedValues.First ();
				if (key.ArrayType == PString.Type) {
					array.Add (new PString (newKey.Identifier));
				} else if (key.ArrayType == PNumber.Type) {
					array.Add (new PNumber (int.Parse (newKey.Identifier)));
				} else {
					array.Add (PObject.Create (key.ArrayType ?? DefaultNewObjectType));
				}
			}
		}
		
		void AddNewDictionaryElement (PDictionary dict, PListScheme.Key key)
		{
			string name = "newNode";
			while (dict.ContainsKey (name))
				name += "_";
			dict.Add (name, new PString (""));
		}

		Dictionary<PObject, Gtk.TreeIter> iterTable = new Dictionary<PObject, Gtk.TreeIter> ();

		void AddToTree (Gtk.TreeStore treeStore, Gtk.TreeIter iter, PDictionary dict, Dictionary<PObject, PListScheme.SchemaItem> tree)
		{
			iterTable[dict] = iter;
			foreach (var item in dict) {
				TreeIter subIter;
				if (currentTree.ContainsKey (item.Value)) {
					if (iter.Equals (TreeIter.Zero) ? treeStore.IterChildren (out subIter) : treeStore.IterChildren (out subIter, iter)) {
						do {
							if (treeStore.GetValue (subIter, 1) == item.Value)
								break;
						} while (treeStore.IterNext (ref subIter));
					}
				} else {
					var key = tree [item.Value] ?? PListScheme.Key.Empty;
					subIter = treeStore.InsertNodeBefore (FindOrAddNewEntry (iter));
					treeStore.SetValues (subIter, key.Identifier ?? item.Key, item.Value, key);
					
					if (item.Value is PArray || item.Value is PDictionary)
						FindOrAddNewEntry (subIter);
				}
					
				if (item.Value is PArray)
					AddToTree (treeStore, subIter, (PArray)item.Value, tree);
				if (item.Value is PDictionary)
					AddToTree (treeStore, subIter, (PDictionary)item.Value, tree);
			}
		}
		
		void AddToTree (Gtk.TreeStore treeStore, Gtk.TreeIter iter, PArray arr, Dictionary<PObject, PListScheme.SchemaItem> tree)
		{
			iterTable[arr] = iter;
			
			for (int i = 0; i < arr.Count; i++) {
				var item = arr[i];
				TreeIter subIter;
				if (currentTree.ContainsKey (item)) {
					if (iter.Equals (TreeIter.Zero) ? treeStore.IterChildren (out subIter) : treeStore.IterChildren (out subIter, iter)) {
						do {
							if (treeStore.GetValue (subIter, 1) == item)
								break;
						} while (treeStore.IterNext (ref subIter));
					}
				} else {
					subIter = treeStore.InsertNodeBefore (FindOrAddNewEntry (iter));
					treeStore.SetValues (subIter, "", item, tree [item]);
				}
				
				if (item is PArray)
					AddToTree (treeStore, subIter, (PArray)item, tree);
				if (item is PDictionary)
					AddToTree (treeStore, subIter, (PDictionary)item, tree);
			}
		}
		
		TreeIter FindOrAddNewEntry (TreeIter iter)
		{
			TreeIter subIter;
			if (iter.Equals (TreeIter.Zero) ? treeStore.IterChildren (out subIter) : treeStore.IterChildren (out subIter, iter)) {
				do {
					if (AddKeyNode.Equals (treeStore.GetValue (subIter, 0)))
						return subIter;
				} while (treeStore.IterNext (ref subIter));
			}
			
			if (iter.Equals (TreeIter.Zero))
				return treeStore.AppendValues (AddKeyNode, null);
			else
				return treeStore.AppendValues (iter, AddKeyNode, null);
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
		
		void RefreshKeyStore ()
		{
			var sortedKeys = new List<PListScheme.Key> (Scheme.Keys);
			sortedKeys.Sort ((x, y) => x.Description.CompareTo (y.Description));
			
			foreach (var key in sortedKeys)
				keyStore.AppendValues (key.Description, key);
		}
		
		Dictionary<PObject, PListScheme.SchemaItem> currentTree = new Dictionary<PObject, PListScheme.SchemaItem> ();
		void RefreshTree ()
		{
			var dict = nsDictionary as PDictionary;
			if (dict != null) {
				var tree = PListScheme.Match (dict, Scheme);
				RemoveOldEntries (currentTree, tree);
				AddToTree (treeStore, Gtk.TreeIter.Zero, dict, tree);
				currentTree = tree;
			} else if (nsDictionary is PArray) {
				AddToTree (treeStore, Gtk.TreeIter.Zero, (PArray)nsDictionary, null);
			}
		}
		
		void RemoveOldEntries (Dictionary<PObject, PListScheme.SchemaItem> oldTree, Dictionary<PObject, PListScheme.SchemaItem> newTree)
		{
			var toRemove = new List<PObject> ();
			foreach (var key in oldTree.Keys)
				if (!newTree.ContainsKey (key))
					toRemove.Add (key);
			
			TreeIter iter;
			if (treeStore.GetIterFirst (out iter))
				RemovePObjects (iter, toRemove);
		}
		
		void RemovePObjects (TreeIter iter, List<PObject> toRemove)
		{
			do {
				if (toRemove.Contains (treeStore.GetValue (iter, 1))) {
					if (treeview.Selection.IterIsSelected (iter)) {
						TreeIter parent;
						if (treeStore.IterParent (out parent, iter))
							treeview.Selection.SelectIter (parent);
					}
					if (treeStore.Remove (ref iter))
						continue;
				} else if (treeStore.IterHasChild (iter)) {
					TreeIter subIter;
					if (treeStore.IterChildren (out subIter, iter))
						RemovePObjects (subIter, toRemove);
				}
			} while (!iter.Equals (TreeIter.Zero) && treeStore.IterNext (ref iter));
		}
	}
}

