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

namespace MonoDevelop.Components.PropertyGrid.PropertyEditors
{
	class CollectionEditor : PropertyEditorCell
	{
		//TODO: Support for multiple object types
		private Type[] types;

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
			return MonoDevelop.Core.GettextCatalog.GetString ("(Collection)");
		}

		public override void LaunchDialogue ()
		{
			//the Type in the collection
			IList collection = (IList) Value;
			string displayName = Property.DisplayName;

			//populate list with existing items
			ListStore itemStore = new ListStore (typeof (object), typeof (int), typeof (string));
			for (int i=0; i<collection.Count; i++)
				itemStore.AppendValues(collection [i], i, collection [i].ToString ());

			#region Building Dialogue
			
			TreeView itemTree;
			PropertyGrid grid;
			TreeIter previousIter = TreeIter.Zero;

			//dialogue and buttons
			Dialog dialog = new Dialog () {
				Title = displayName + " Editor",
				Modal = true,
				AllowGrow = true,
				AllowShrink = true,
			};
			var toplevel = this.Container.Toplevel as Window;
			if (toplevel != null)
				dialog.TransientFor = toplevel;
			
			dialog.AddActionWidget (new Button (Stock.Cancel), ResponseType.Cancel);
			dialog.AddActionWidget (new Button (Stock.Ok), ResponseType.Ok);
			
			//three columns for items, sorting, PropGrid
			HBox hBox = new HBox ();
			dialog.VBox.PackStart (hBox, true, true, 5);

			//propGrid at end
			grid = new PropertyGrid (base.EditorManager) {
				CurrentObject = null,
				WidthRequest = 200,
				ShowHelp = false
			};
			hBox.PackEnd (grid, true, true, 5);

			//followed by a ButtonBox
			VBox buttonBox = new VBox ();
			buttonBox.Spacing = 6;
			hBox.PackEnd (buttonBox, false, false, 5);
			
			//add/remove buttons
			Button addButton = new Button (new Image (Stock.Add, IconSize.Button));
			buttonBox.PackStart (addButton, false, false, 0);
			if (types [0].IsAbstract)
				addButton.Sensitive = false;
			Button removeButton = new Button (new Gtk.Image (Stock.Remove, IconSize.Button));
			buttonBox.PackStart (removeButton, false, false, 0);
			
			//sorting buttons
			Button upButton = new Button (new Image (Stock.GoUp, IconSize.Button));
			buttonBox.PackStart (upButton, false, false, 0);
			Button downButton = new Button (new Image (Stock.GoDown, IconSize.Button));
			buttonBox.PackStart (downButton, false, false, 0);

			//Third column has list (TreeView) in a ScrolledWindow
			ScrolledWindow listScroll = new ScrolledWindow ();
			listScroll.WidthRequest = 200;
			listScroll.HeightRequest = 320;
			hBox.PackStart (listScroll, false, false, 5);
			
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

			addButton.Clicked += delegate {
				//create the object
				object instance = System.Activator.CreateInstance (types[0]);
				
				//get existing selection and insert after it
				TreeIter oldIter, newIter;
				if (itemTree.Selection.GetSelected (out oldIter))
					newIter = itemStore.InsertAfter (oldIter);
				//or append if no previous selection 
				else
					newIter = itemStore.Append ();
				itemStore.SetValue (newIter, 0, instance);
				
				//select, set name and update all the indices
				itemTree.Selection.SelectIter (newIter);
				UpdateName (itemStore, newIter);
				UpdateIndices (itemStore);
			};
			
			removeButton.Clicked += delegate {
				//get selected iter and the replacement selection
				TreeIter iter, newSelection;
				if (!itemTree.Selection.GetSelected (out iter))
					return;
				
				newSelection = iter;
				if (!IterPrev (itemStore, ref newSelection)) {
					newSelection = iter;
					if (!itemStore.IterNext (ref newSelection))
						newSelection = TreeIter.Zero;
				}
				
				//new selection. Zeroing previousIter prevents trying to update name of deleted iter.
				previousIter = TreeIter.Zero;
				if (itemStore.IterIsValid (newSelection))
					itemTree.Selection.SelectIter (newSelection);
				
				//and the removal and index update
				itemStore.Remove (ref iter);
				UpdateIndices (itemStore);
			};
			
			upButton.Clicked += delegate {
				TreeIter iter, prev;
				if (!itemTree.Selection.GetSelected (out iter))
					return;
	
				//get previous iter
				prev = iter;
				if (!IterPrev (itemStore, ref prev))
					return;
	
				//swap the two
				itemStore.Swap (iter, prev);
	
				//swap indices too
				object prevVal = itemStore.GetValue (prev, 1);
				object iterVal = itemStore.GetValue (iter, 1);
				itemStore.SetValue (prev, 1, iterVal);
				itemStore.SetValue (iter, 1, prevVal);
			};
			
			downButton.Clicked += delegate {
				TreeIter iter, next;
				if (!itemTree.Selection.GetSelected (out iter))
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
			};
			
			itemTree.Selection.Changed += delegate {
				TreeIter iter;
				if (!itemTree.Selection.GetSelected (out iter)) {
					removeButton.Sensitive = false;
					return;
				}
				removeButton.Sensitive = true;
	
				//update grid
				object obj = itemStore.GetValue (iter, 0);
				grid.CurrentObject = obj;
				
				//update previously selected iter's name
				UpdateName (itemStore, previousIter);
				
				//update current selection so we can update
				//name next selection change
				previousIter = iter;
			};
			
			grid.Changed += delegate {
				TreeIter iter;
				if (itemTree.Selection.GetSelected (out iter))
					UpdateName (itemStore, iter);
			};
			
			TreeIter selectionIter;
			removeButton.Sensitive = itemTree.Selection.GetSelected (out selectionIter);
			
			dialog.ShowAll ();
			grid.ShowToolbar = false;
			
			#endregion
			
			//if 'OK' put items back in collection
			using (dialog) {
				if (MonoDevelop.Ide.MessageService.ShowCustomDialog (dialog, toplevel) == (int)ResponseType.Ok) {
					DesignerTransaction tran = CreateTransaction (Instance);
					object old = collection;
			
					try {
						collection.Clear ();
						foreach (object[] o in itemStore)
							collection.Add (o [0]);
						EndTransaction (Instance, tran, old, collection, true);
					} catch {
						EndTransaction (Instance, tran, old, collection, false);
						throw;
					}
				}
			}
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
		
		static void UpdateIndices (ListStore itemStore)
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

		static void UpdateName (ListStore itemStore, TreeIter iter)
		{
			if (iter.Equals (TreeIter.Zero))
				return;

			object item = itemStore.GetValue (iter, 0);
			string name = item.ToString ();
			if (string.IsNullOrEmpty (name))
				name = MonoDevelop.Core.GettextCatalog.GetString ("(Empty)");;

			itemStore.SetValue(iter, 2, name);
		}

		//generally useful function... why not in model already?
		static bool IterPrev (TreeModel model, ref TreeIter iter)
		{
			TreePath tp = model.GetPath (iter);
			return tp.Prev() && model.GetIter (out iter, tp);
		}
	}
}
