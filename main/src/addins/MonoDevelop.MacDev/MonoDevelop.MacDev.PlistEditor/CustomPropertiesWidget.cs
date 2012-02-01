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
using System.Globalization;

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem(true)]
	public class CustomPropertiesWidget : VBox, IRawPListDisplayWidget
	{
		static readonly string AddKeyNode = GettextCatalog.GetString ("Add new entry");
		const PObjectType DefaultNewObjectType = PObjectType.String;
		
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
					RefreshKeyStore ();
					QueueDraw ();
				}
			}
		}
		
		/// <summary>
		/// The Dictionary representing all the PObjects in the GtkTreeView and the corresponding SchemaItem they
		/// were instantiated from.
		/// </summary>
		/// <value>
		/// The current tree.
		/// </value>
		Dictionary<PObject, PListScheme.SchemaItem> CurrentTree {
			get; set;
		}
		
		PObject RootPObject {
			get; set;
		}

		public PListScheme Scheme {
			get; private set;
		}
		
		public void SetPListContainer (PObjectContainer value)
		{
			RootPObject = value;
			RootPObject.Changed += delegate {
				RefreshTree ();
				QueueDraw ();
			};
			RefreshTree ();
		}
		
		void RenderValue (PObject obj, CellRendererCombo renderer)
		{
			switch (obj.Type) {
			case PObjectType.Dictionary:
			case PObjectType.Array:
				int count = ((PObjectContainer)obj).Count;
				renderer.Sensitive = false;
				renderer.Text = string.Format (GettextCatalog.GetPluralString ("({0} item)", "({0} items)", count), count);
				return;
			case PObjectType.Number:
				renderer.Sensitive = true;
				renderer.Text = ((PNumber)obj).Value.ToString (CultureInfo.CurrentCulture);
				return;
			case PObjectType.Boolean:
				renderer.Sensitive = true;
				renderer.Text = ((PBoolean)obj).Value
					? GettextCatalog.GetString ("Yes")
					: GettextCatalog.GetString ("No");
				return;
			case PObjectType.Data:
				renderer.Sensitive = false;
				renderer.Text = string.Format ("byte[{0}]", ((PData)obj).Value.Length);
				return;
			case PObjectType.String:
				string str = ((PString)obj).Value;
				var key = obj.Parent != null? this.Scheme.GetKey (obj.Parent.Key) : null;
				if (key != null) {
					var val = key.Values.FirstOrDefault (v => v.Identifier == str);
					if (val != null && this.ShowDescriptions) {
						str = GettextCatalog.GetString (val.Description);
					}
				}
				renderer.Sensitive = true;
				renderer.Text = str;
				return;
			case PObjectType.Date:
				renderer.Sensitive = true;
				renderer.Text = ((PDate)obj).Value.ToString (CultureInfo.CurrentCulture);
				return;
			}
			throw new InvalidOperationException ();
		}

		static string GetTranslatedStringFromPObjectType (PObjectType type)
		{
			switch (type) {
			case PObjectType.Dictionary:
				return GettextCatalog.GetString ("Dictionary");
			case PObjectType.Array:
				return GettextCatalog.GetString ("Array");
			case PObjectType.Number:
				return GettextCatalog.GetString ("Number");
			case PObjectType.Boolean:
				return GettextCatalog.GetString ("Boolean");
			case PObjectType.Data:
				return GettextCatalog.GetString ("Data");
			case PObjectType.String:
				return GettextCatalog.GetString ("String");
			case PObjectType.Date:
				return GettextCatalog.GetString ("Date");
			}
			throw new ArgumentOutOfRangeException ();
		}

		static PObjectType GetPObjectTypeFromTranslatedString (string str)
		{
			if (str == GettextCatalog.GetString ("Dictionary"))
				return PObjectType.Dictionary;
			if (str == GettextCatalog.GetString ("Array"))
				return PObjectType.Array;
			if (str == GettextCatalog.GetString ("Number"))
				return PObjectType.Number;
			if (str == GettextCatalog.GetString ("Boolean"))
				return PObjectType.Boolean;
			if (str == GettextCatalog.GetString ("Data"))
				return PObjectType.Data;
			if (str == GettextCatalog.GetString ("String"))
				return PObjectType.String;
			if (str == GettextCatalog.GetString ("Date"))
				return PObjectType.Date;
			throw new ArgumentOutOfRangeException ();
		}
		
		static bool TrySetValueFromString (PObject obj, string text)
		{
			//special case boolean since it's localized
			if (obj is PBoolean) {
				const StringComparison ic = StringComparison.OrdinalIgnoreCase;
				if (GettextCatalog.GetString ("Yes").Equals (text, ic)) {
					((PBoolean)obj).Value = true;
					return true;
				} else if (GettextCatalog.GetString ("No").Equals (text, ic)) {
					((PBoolean)obj).Value = false;
					return true;
				}
			}

			if (obj is IPValueObject) {
				var pv = (IPValueObject) obj;
				return pv.TrySetValueFromString (text, CultureInfo.CurrentCulture)
					|| pv.TrySetValueFromString (text, CultureInfo.InvariantCulture);
			}

			throw new NotSupportedException ();
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
				if (!Selection.GetSelected (out iter))
					return;

				var menu = new Gtk.Menu ();
				var newKey = new Gtk.MenuItem (GettextCatalog.GetString ("New key"));
				newKey.Activated += widget.AddElement;
				menu.Append (newKey);

				if (widget.treeStore.GetValue (iter, 1) != null) {
					var removeKey = new Gtk.MenuItem (GettextCatalog.GetString ("Remove key"));
					menu.Append (removeKey);
					removeKey.Activated += widget.RemoveElement;
				}
				
				if (widget.Scheme != PListScheme.Empty) {
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
				try {
					if (evnt.Type == Gdk.EventType.ButtonPress)
						OnClicked (EventArgs.Empty);
				} catch (Exception ex) {
					GLib.ExceptionManager.RaiseUnhandledException (ex, false);
				}
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
		
		public CustomPropertiesWidget ()
			: this (null)
		{
		}
		
		public CustomPropertiesWidget (PListScheme scheme)
		{
			Scheme = scheme ?? PListScheme.Empty;
			CurrentTree = new Dictionary<PObject, PListScheme.SchemaItem> ();
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
			keyRenderer.EditingStarted += KeyRendererEditingStarted;
			keyRenderer.Edited += KeyRendererEditingFinished;
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
				
				var key = (PListScheme.SchemaItem) tree_model.GetValue (iter, 2);
				var parentPArray = obj.Parent is PArray;
				renderer.Editable = !parentPArray;
				renderer.Sensitive = !parentPArray;
				if (parentPArray)
					renderer.Text = "";
				else
					renderer.Text = key != null && ShowDescriptions ? key.Description : id;
			});
			treeview.AppendColumn (col);
			
			col = new TreeViewColumn { MinWidth = 25, Resizable = false, Sizing = Gtk.TreeViewColumnSizing.Fixed };
				
			var removeRenderer = new CellRendererButton (ImageService.GetPixbuf ("gtk-remove", IconSize.Menu));
			removeRenderer.Clicked += RemoveElement;
			col.PackEnd (removeRenderer, false);
			col.SetCellDataFunc (removeRenderer, delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				removeRenderer.Visible = treeview.Selection.IterIsSelected (iter) && !AddKeyNode.Equals (treeStore.GetValue (iter, 0));
			});
			
			var addRenderer = new CellRendererButton (ImageService.GetPixbuf ("gtk-add", IconSize.Menu));
			addRenderer.Clicked += AddElement;
			col.PackEnd (addRenderer, false);
			col.SetCellDataFunc (addRenderer, delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				addRenderer.Visible = treeview.Selection.IterIsSelected (iter) && AddKeyNode.Equals (treeStore.GetValue (iter, 0));
			});
			treeview.AppendColumn (col);
			
			treeview.RowActivated += delegate(object o, RowActivatedArgs args) {
				TreeIter iter;
				if (treeStore.GetIter (out iter, args.Path) && AddKeyNode.Equals (treeStore.GetValue (iter, 0)))
					AddElement (o, EventArgs.Empty);
			};

			var comboRenderer = new CellRendererCombo ();

			var typeModel = new ListStore (typeof (string));
			typeModel.AppendValues (GetTranslatedStringFromPObjectType (PObjectType.Array));
			typeModel.AppendValues (GetTranslatedStringFromPObjectType (PObjectType.Dictionary));
			typeModel.AppendValues (GetTranslatedStringFromPObjectType (PObjectType.Boolean));
			typeModel.AppendValues (GetTranslatedStringFromPObjectType (PObjectType.Data));
			typeModel.AppendValues (GetTranslatedStringFromPObjectType (PObjectType.Date));
			typeModel.AppendValues (GetTranslatedStringFromPObjectType (PObjectType.Number));
			typeModel.AppendValues (GetTranslatedStringFromPObjectType (PObjectType.String));

			comboRenderer.Model = typeModel;
			comboRenderer.Mode = CellRendererMode.Editable;
			comboRenderer.HasEntry = false;
			comboRenderer.TextColumn = 0;
			comboRenderer.Edited += delegate(object o, EditedArgs args) {
				TreeIter selIter;
				if (!treeStore.GetIterFromString (out selIter, args.Path)) 
					return;

				var type = GetPObjectTypeFromTranslatedString (args.NewText);
				
				PObject oldObj = (PObject)treeStore.GetValue (selIter, 1);
				if (oldObj != null && oldObj.Type != type) {
					oldObj.Replace (PObject.Create (type));
				}
			};
			
			treeview.AppendColumn (GettextCatalog.GetString ("Type"), comboRenderer, delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererCombo)cell;
				var value = (string) tree_model.GetValue (iter, 0);
				var obj   = (PObject)tree_model.GetValue (iter, 1);
				var key   = (PListScheme.SchemaItem)tree_model.GetValue (iter, 2);
				renderer.Editable = key == null && !AddKeyNode.Equals (value);
				renderer.ForegroundGdk = Style.Text (renderer.Editable ? StateType.Normal : StateType.Insensitive);
				renderer.Text = obj == null ? "" : GetTranslatedStringFromPObjectType (obj.Type);
			});
			
			var propRenderer = new CellRendererCombo ();
			
			propRenderer.Model = valueStore;
			propRenderer.Mode = CellRendererMode.Editable;
			propRenderer.TextColumn = 0;
			propRenderer.EditingStarted += PropRendererEditingStarted;
			propRenderer.Edited += PropRendererEditingFinished;

			treeview.AppendColumn (GettextCatalog.GetString ("Value"), propRenderer, delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererCombo)cell;
				var obj      = (PObject)tree_model.GetValue (iter, 1);

				RenderValue (obj, renderer);
			});
			treeview.EnableGridLines = TreeViewGridLines.Horizontal;
			treeview.Model = treeStore;
		}

		void KeyRendererEditingStarted (object o, EditingStartedArgs args)
		{
			try {
				RefreshKeyStore ();
			} catch (Exception ex) {
				GLib.ExceptionManager.RaiseUnhandledException (ex, false);
			}
		}

		void KeyRendererEditingFinished (object o, EditedArgs args)
		{
			try {
				TreeIter iter;
				if (!treeStore.GetIterFromString (out iter, args.Path)) 
					return;

				var obj = (PObject) treeStore.GetValue (iter, 1);
				var dict = obj.Parent as PDictionary;
				if (dict != null) {
					var keys = Scheme.Keys.Cast<PListScheme.SchemaItem> ();
					if (treeStore.IterParent (out iter, iter)) {
						var subkey = (PListScheme.SchemaItem) treeStore.GetValue (iter, 2) ?? PListScheme.Key.Empty;
						keys = subkey.Values;
					}
					
					var key = keys.FirstOrDefault (k => k.Identifier == args.NewText || k.Description == args.NewText);
					var newKey = key == null ? args.NewText : key.Identifier;
					dict.ChangeKey (obj, newKey, key == null ? null : key.Create ());
				}
			} catch (Exception ex) {
				GLib.ExceptionManager.RaiseUnhandledException (ex, false);
			}
		}

		void PropRendererEditingStarted (object o, EditingStartedArgs args)
		{
			try {
				valueStore.Clear ();
	
				TreeIter iter;
				if (!treeStore.GetIterFromString (out iter, args.Path)) 
					return;
				
				var obj = (PObject) treeStore.GetValue (iter, 1);
				var values = PListScheme.AvailableValues (obj, CurrentTree);
				if (values != null) {
					var descr = new List<string> (values.Select (v => ShowDescriptions ? v.Description : v.Identifier));
					descr.Sort ();
					foreach (var val in descr) {
						valueStore.AppendValues (val);
					}
				}
			} catch (Exception ex) {
				GLib.ExceptionManager.RaiseUnhandledException (ex, false);
			}
		}
		
		void PropRendererEditingFinished (object o, EditedArgs args)
		{
			try {
				TreeIter iter;
				string newText = args.NewText;
				if (!treeStore.GetIterFromString (out iter, args.Path)) 
					return;
	
				var obj = (PObject)treeStore.GetValue (iter, 1);
				var values = PListScheme.AvailableValues (obj, CurrentTree);
				if (values != null) {
					var value = values.FirstOrDefault (v => v.Description == newText || v.Identifier == newText);
					if (value != null)
						newText = value.Identifier;
				}
				TrySetValueFromString (obj, newText);
			} catch (Exception ex) {
				GLib.ExceptionManager.RaiseUnhandledException (ex, false);
			}
		}

		public void AddElement (object o, EventArgs e)
		{
			// By default we assume we are adding something to the root dictionary/array
			TreeIter iter;
			var parent = RootPObject;
			
			// Grab the selected row and find out what the parent is. If there is a parent, then we
			// will need it correlate against the schema to figure out what values are allowed to be
			// entered here
			if (treeview.Selection.GetSelected (out iter)) {
				if (treeStore.IterParent (out iter, iter)) {
					parent = (PObject) treeStore.GetValue (iter, 1);
				}

				if (parent is PArray)
					AddNewArrayElement ((PArray) parent);
				else if (parent is PDictionary)
					AddNewDictionaryElement ((PDictionary) parent);
			}
		}
		
		public void RemoveElement (object o, EventArgs e)
		{
			TreeIter iter;
			if (treeview.Selection.GetSelected (out iter)) {
				var obj = (PObject)treeStore.GetValue (iter, 1);
				obj.Remove ();
			}
		}
		
		void AddNewArrayElement (PArray array)
		{
			var values = PListScheme.AvailableKeys (array, CurrentTree);
			if (values == null) {
				array.Add (PObject.Create (DefaultNewObjectType));
			} else if (values.Any ()) {
				array.Add (values.First ().Create ());
			}
		}
		
		void AddNewDictionaryElement (PDictionary dict)
		{
			var values = PListScheme.AvailableKeys (dict, CurrentTree);
			if (values == null) {
				string name = "newProperty";
				while (dict.ContainsKey (name))
					name += "_";
				dict.Add (name, PObject.Create (DefaultNewObjectType));
			} else if (values.Any ()) {
				var value = values.FirstOrDefault ();
				dict.Add (value.Identifier, value.Create ());
			}
		}

		void AddToTree (Gtk.TreeStore treeStore, Gtk.TreeIter iter, PObject current, Dictionary<PObject, PListScheme.SchemaItem> tree)
		{
			foreach (var item in PObject.ToEnumerable (current)) {
				var subIter = FindOrAddPObject (iter, item.Key, item.Value, tree);
				AddToTree (treeStore, subIter, item.Value, tree);
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
		
		TreeIter FindOrAddPObject (TreeIter iter, string id, PObject item, Dictionary<PObject, PListScheme.SchemaItem> tree)
		{
			TreeIter subIter;
			if (CurrentTree.ContainsKey (item)) {
				if (iter.Equals (TreeIter.Zero) ? treeStore.IterChildren (out subIter) : treeStore.IterChildren (out subIter, iter)) {
					do {
						if (treeStore.GetValue (subIter, 1) == item) {
							// If we have modified the value of an existing PObject, such
							// as changing the value of a PNumber from '1' to '2', we will
							// need to refresh the value in the treestore.
							treeStore.SetValue (subIter, 0, id);
							break;
						}
					} while (treeStore.IterNext (ref subIter));
				}
			} else {
				subIter = treeStore.InsertNodeBefore (FindOrAddNewEntry (iter));
				treeStore.SetValues (subIter, id, item, tree [item]);
				if (CurrentTree.Count > 0) {
					treeview.ExpandToPath (treeStore.GetPath (subIter));
					this.treeview.Selection.SelectIter (subIter);
				}
				
				if (item is PArray || item is PDictionary) {
					var newEntryIter = FindOrAddNewEntry (subIter);
					if (CurrentTree.Count > 0)
						treeview.ExpandToPath (treeStore.GetPath (newEntryIter));
				}
			}
			
			return subIter;
		}
		
		void RefreshKeyStore ()
		{
			TreeIter iter;
			var keys = Scheme.Keys.Cast <PListScheme.SchemaItem> ();
			if (treeview.Selection.GetSelected (out iter)) {
				var currentObj = (PObject) treeStore.GetValue (iter, 1);
				if (treeStore.IterParent (out iter, iter)) {
					var obj = (PObject) treeStore.GetValue (iter, 1);
					keys = PListScheme.AvailableKeys (obj, CurrentTree);
				}
			}
			
			keyStore.Clear ();
			if (keys != null) {
				var sortedKeys = new List<PListScheme.SchemaItem> (keys);
				if (ShowDescriptions)
					sortedKeys.Sort ((x, y) => StringComparer.CurrentCulture.Compare (x.Description, y.Description));
				else
					sortedKeys.Sort ((x, y) => StringComparer.CurrentCulture.Compare (x.Identifier, y.Identifier));
				
				foreach (var key in sortedKeys)
					keyStore.AppendValues (ShowDescriptions ? key.Description : key.Identifier, key);
			}
		}

		void RefreshTree ()
		{
			var dict = RootPObject as PDictionary;
			if (dict != null) {
				var tree = PListScheme.Match (dict, Scheme);
				RemoveOldEntries (CurrentTree, tree);
				AddToTree (treeStore, Gtk.TreeIter.Zero, dict, tree);
				CurrentTree = tree;
			} else if (RootPObject is PArray) {
				AddToTree (treeStore, Gtk.TreeIter.Zero, (PArray)RootPObject, null);
			}
		}
		
		void RemoveOldEntries (Dictionary<PObject, PListScheme.SchemaItem> oldTree, Dictionary<PObject, PListScheme.SchemaItem> newTree)
		{
			var toRemove = oldTree.Keys.Where (k => !newTree.ContainsKey (k)).ToArray ();
			
			TreeIter iter;
			if (treeStore.GetIterFirst (out iter))
				RemovePObjects (iter, toRemove);
		}
		
		void RemovePObjects (TreeIter iter, IList<PObject> toRemove)
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

