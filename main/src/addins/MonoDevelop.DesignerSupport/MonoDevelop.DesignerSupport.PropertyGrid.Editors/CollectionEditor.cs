/* 
 * CollectionEditor.cs - The base class for the visual type editors
 * 
 * Part of PropertyGrid - A Gtk# widget that displays and allows 
 * editing of all of an object's public properties 
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using Gtk;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;

namespace MonoDevelop.DesignerSupport.PropertyGrid.PropertyEditors
{
	class CollectionEditor : PropertyEditorCell
	{
		//TODO: Support for multiple object types
		private Type[] types;

		private ListStore itemStore;
		private TreeView itemTree;
		private PropertyGrid grid;
		private TreeIter previousIter = TreeIter.Zero;

		protected override void Initialize ()
		{
			base.Initialize ();
			this.types = new Type[] { EditorManager.GetCollectionItemType (Property.PropertyType) };
		}

		protected virtual Type[] NewItemTypes ()
		{
			return types;
		}

		public override bool DialogueEdit
		{
			get { return true; }
		}

		public override bool EditsReadOnlyObject {
			get { return true; }
		}
		
		protected override string GetValueText ()
		{
			return "(Collection)";
		}


		public override void LaunchDialogue ()
		{
			//the Type in the collection
			IList collection = (IList) Value;
			string displayName = Property.DisplayName;

			//populate list with existing items
			itemStore = new ListStore (typeof (object), typeof (int), typeof (string));
			for (int i=0; i<collection.Count; i++)
			{
				itemStore.AppendValues(collection [i], i, collection [i].ToString ());
			}

			#region Building Dialogue

			//dialogue and buttons
			Dialog dialog = new Dialog ();
			dialog.Title = displayName + " Editor";
			dialog.Modal = true;
			dialog.AllowGrow = true;
			dialog.AllowShrink = true;
			dialog.Modal = true;
			dialog.AddActionWidget (new Button (Stock.Cancel), ResponseType.Cancel);
			dialog.AddActionWidget (new Button (Stock.Ok), ResponseType.Ok);
			
			//three columns for items, sorting, PropGrid
			HBox hBox = new HBox ();
			dialog.VBox.PackStart (hBox, true, true, 5);

			//propGrid at end
			grid = new PropertyGrid (base.EditorManager);
			grid.CurrentObject = null;
			grid.WidthRequest = 200;
			grid.ShowHelp = false;
			hBox.PackEnd (grid, true, true, 5);

			//followed by item sorting buttons in ButtonBox
			VButtonBox sortButtonBox = new VButtonBox ();
			sortButtonBox.LayoutStyle = ButtonBoxStyle.Start;
			Button upButton = new Button ();
			Image upImage = new Image (Stock.GoUp, IconSize.Button);
			upImage.Show ();
			upButton.Add (upImage);
			upButton.Show ();
			sortButtonBox.Add (upButton);
			Button downButton = new Button ();
			Image downImage = new Image (Stock.GoDown, IconSize.Button);
			downImage.Show ();
			downButton.Add (downImage);
			downButton.Show ();
			sortButtonBox.Add (downButton);
			hBox.PackEnd (sortButtonBox, false, false, 5);

			//Third column is a VBox
			VBox itemsBox = new VBox ();
			hBox.PackStart (itemsBox, false, false, 5);

			//which at bottom has add/remove buttons
			HButtonBox addRemoveButtons = new HButtonBox();
			addRemoveButtons.LayoutStyle = ButtonBoxStyle.End;
			Button addButton = new Button (Stock.Add);
			addRemoveButtons.Add (addButton);
			if (types [0].IsAbstract)
				addButton.Sensitive = false;
			Button removeButton = new Button (Stock.Remove);
			addRemoveButtons.Add (removeButton);
			itemsBox.PackEnd (addRemoveButtons, false, false, 10);
		
			//and at top has list (TreeView) in a ScrolledWindow
			ScrolledWindow listScroll = new ScrolledWindow ();
			listScroll.WidthRequest = 200;
			listScroll.HeightRequest = 320;
			itemsBox.PackStart (listScroll, true, true, 0);
			
			itemTree = new TreeView (itemStore);
			itemTree.Selection.Mode = SelectionMode.Single;
			itemTree.HeadersVisible = false;
			listScroll.AddWithViewport (itemTree);

			//renderers and attribs for TreeView
			CellRenderer rdr = new CellRendererText ();
			itemTree.AppendColumn (new TreeViewColumn ("Index", rdr, "text", 1));
			rdr = new CellRendererText ();
			itemTree.AppendColumn (new TreeViewColumn ("Object", rdr, "text", 2));

			#endregion

			#region Events

			addButton.Clicked += new EventHandler (addButton_Clicked);
			removeButton.Clicked += new EventHandler (removeButton_Clicked);
			itemTree.Selection.Changed += new EventHandler (Selection_Changed);
			upButton.Clicked += new EventHandler (upButton_Clicked);
			downButton.Clicked += new EventHandler (downButton_Clicked);


			#endregion

			//show and get response			
			dialog.ShowAll ();
			ResponseType response = (ResponseType) dialog.Run();
			dialog.Destroy ();
			
			//if 'OK' put items back in collection
			if (response == ResponseType.Ok)
			{
				DesignerTransaction tran = CreateTransaction (Instance);
				object old = collection;
			
				try {
					collection.Clear();
					foreach (object[] o in itemStore)
						collection.Add (o[0]);
					EndTransaction (Instance, tran, old, collection, true);
				}
				catch {
					EndTransaction (Instance, tran, old, collection, false);
					throw;
				}
			}
			
			//clean up so we start fresh if launched again
			
			itemTree = null;
			itemStore = null;
			grid = null;
			previousIter = TreeIter.Zero;
		}
		
		//This and EndTransaction are from Mono internal class System.ComponentModel.ReflectionPropertyDescriptor
		// Lluis Sanchez Gual (lluis@ximian.com), (C) Novell, Inc, MIT X11 license
		DesignerTransaction CreateTransaction (object obj)
		{
			IComponent com = obj as IComponent;
			if (com == null || com.Site == null) return null;
			
			IDesignerHost dh = (IDesignerHost) com.Site.GetService (typeof(IDesignerHost));
			if (dh == null) return null;
			
			DesignerTransaction tran = dh.CreateTransaction ();
			IComponentChangeService ccs = (IComponentChangeService) com.Site.GetService (typeof(IComponentChangeService));
			if (ccs != null)
				ccs.OnComponentChanging (com, Property);
			return tran;
		}
		
		void EndTransaction (object obj, DesignerTransaction tran, object oldValue, object newValue, bool commit)
		{
			if (tran == null) return;
			
			if (commit) {
				IComponent com = obj as IComponent;
				IComponentChangeService ccs = (IComponentChangeService) com.Site.GetService (typeof(IComponentChangeService));
				if (ccs != null)
					ccs.OnComponentChanged (com, Property, oldValue, newValue);
				tran.Commit ();
			}
			else
				tran.Cancel ();
		}

		void downButton_Clicked (object sender, EventArgs e)
		{
			TreeIter iter, next;
			TreeModel model;
			if (!itemTree.Selection.GetSelected(out model, out iter))
				return;

			//get next iter
			next = iter;
			if (!itemStore.IterNext (ref next))
				return;
			
			//swap the two
			itemStore.Swap (iter, next);

			//swap indices too
			object nextVal = itemStore.GetValue (next, 1);
			object iterVal = itemStore.GetValue (iter, 1);
			itemStore.SetValue (next, 1, iterVal);
			itemStore.SetValue (iter, 1, nextVal);
		}

		void upButton_Clicked (object sender, EventArgs e)
		{
			TreeIter iter, prev;
			TreeModel model;
			if (!itemTree.Selection.GetSelected (out model, out iter))
				return;

			//get previous iter
			prev = iter;
			if (!IterPrev (model, ref prev))
				return;

			//swap the two
			itemStore.Swap (iter, prev);

			//swap indices too
			object prevVal = itemStore.GetValue (prev, 1);
			object iterVal = itemStore.GetValue (iter, 1);
			itemStore.SetValue (prev, 1, iterVal);
			itemStore.SetValue (iter, 1, prevVal);
		}

		void removeButton_Clicked (object sender, EventArgs e)
		{
			//get selected iter and the replacement selection
			TreeIter iter, newSelection;
			TreeModel model;
			if (!itemTree.Selection.GetSelected (out model, out iter))
				return;
			
			newSelection = iter;
			if (!IterPrev (model, ref newSelection)) {
				newSelection = iter;
				if (!itemStore.IterNext (ref newSelection))
					newSelection = TreeIter.Zero;
			}
			
			//new selection. Zeroing previousIter prevents trying to update name of deleted iter.
			previousIter = TreeIter.Zero;
			if (itemStore.IterIsValid (newSelection))
				itemTree.Selection.SelectIter (newSelection);
			
			//and the removal and index update
			((ListStore) model).Remove (ref iter);
			UpdateIndices ();
		}

		void Selection_Changed (object sender, EventArgs e)
		{
			//get selection
			TreeIter iter;
			TreeModel model;
			if (!itemTree.Selection.GetSelected (out model, out iter))
				return;

			//update grid
			object obj = model.GetValue (iter, 0);
			grid.CurrentObject = obj;
			
			//update previously selected iter's name
			UpdateName (previousIter);
			
			//update current selection so we can update
			//name next selection change
			previousIter = iter;
			
		}

		void addButton_Clicked (object sender, EventArgs e)
		{

			//create the object
			object instance = System.Activator.CreateInstance (types[0]);
			
			//get existing selection and insert after it
			TreeIter oldIter, newIter;
			TreeModel model;
			if (itemTree.Selection.GetSelected (out model, out oldIter))
				newIter = itemStore.InsertAfter (oldIter);
			//or append if no previous selection 
			else
				newIter = itemStore.Append ();
			itemStore.SetValue (newIter, 0, instance);
			
			//select, set name and update all the indices
			itemTree.Selection.SelectIter (newIter);
			UpdateName (newIter);
			UpdateIndices ();
		}

		private void UpdateIndices ()
		{
			TreeIter iter;
			int i = 0;
			if (!itemStore.GetIterFirst (out iter))
				return;
			
			do {
				itemStore.SetValue (iter, 1, i);
				i++;
			} while (itemStore.IterNext (ref iter));
		}

		private void UpdateName (TreeIter iter)
		{
			if (iter.Equals (TreeIter.Zero))
				return;

			object item = itemStore.GetValue (iter, 0);
			string name = item.ToString ();
			if (name.Length == 0)
				name = item.GetType ().Name;

			itemStore.SetValue(iter, 2, name);
		}

		//generally useful function... why not in model already?
		private bool IterPrev (TreeModel model, ref TreeIter iter)
		{
			TreePath tp = model.GetPath (iter);
			return tp.Prev() && model.GetIter (out iter, tp);
		}
	}
}
