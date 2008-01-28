//
// Authors:
//    Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
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
//

using Gtk;
using System;
using System.Collections.Generic;
using MonoDevelop.Database.Sql;

namespace MonoDevelop.Database.Components
{
	public class SortedTableListStore
	{
		public event EventHandler TableToggled;		
		
		protected TableSchemaCollection tables;
		protected ListStore store;
		
		public const int ColSelectIndex = 0;
		public const int ColNameIndex = 1;
		public const int ColObjIndex = 2;
		
		protected bool singleCheck;
		
		public SortedTableListStore (TableSchemaCollection tables)
		{
			if (tables == null)
				throw new ArgumentNullException ("tables");
			
			this.tables = tables;
			
			store = new ListStore (typeof (bool), typeof (string), typeof (TableSchema));
			store.SetSortColumnId (ColNameIndex, SortType.Ascending);
			store.SetSortFunc (ColNameIndex, new TreeIterCompareFunc (SortName));
			
			foreach (TableSchema table in tables)
				AddTable (table);
			
			tables.ItemAdded += new SortedCollectionItemEventHandler<TableSchema> (OnTableAdded);
			tables.ItemRemoved += new SortedCollectionItemEventHandler<TableSchema> (OnTableRemoved);
		}
		
		public ListStore Store {
			get { return store; }
		}
		
		public virtual bool SingleCheck {
			get { return singleCheck; }
			set {
				if (value != singleCheck) {
					singleCheck = value;
					if (value)
						DeselectAll ();
				}
			}
		}
		
		public virtual TableSchema GetTableSchema (TreeIter iter)
		{
			if (!iter.Equals (TreeIter.Zero))
				return store.GetValue (iter, ColObjIndex) as TableSchema;
			return null;
		}
		
		public virtual IEnumerable<TableSchema> CheckedTables {
			get {
				TreeIter iter;
				if (store.GetIterFirst (out iter)) {
					do {
						bool chk = (bool)store.GetValue (iter, ColSelectIndex);
						if (chk)
							yield return store.GetValue (iter, ColObjIndex) as TableSchema;
					} while (store.IterNext (ref iter));
				}
			}
		}
		
		public virtual bool IsTableChecked {
			get {
				TreeIter iter;
				if (store.GetIterFirst (out iter)) {
					do {
						bool chk = (bool)store.GetValue (iter, ColSelectIndex);
						if (chk)
							return true;
					} while (store.IterNext (ref iter));
				}
				return false;
			}
		}
		
		public virtual void SelectAll ()
		{
			SetSelectState (true);
			OnTableToggled ();
		}
		
		public virtual void DeselectAll ()
		{
			SetSelectState (false);
			OnTableToggled ();
		}
		
		public virtual void Select (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			
			TableSchema table = tables.Search (name);
			if (table != null)
				Select (table);
		}
		
		public virtual void Select (TableSchema table)
		{
			if (table == null)
				throw new ArgumentNullException ("table");
			
			TreeIter iter = GetTreeIter (table);
			if (!iter.Equals (TreeIter.Zero)) {
				if (singleCheck)
					SetSelectState (false);
				
				store.SetValue (iter, ColSelectIndex, true);
				OnTableToggled ();
			}	
		}
		
		public virtual void ToggleSelect (TreeIter iter)
		{
			bool val = (bool) store.GetValue (iter, ColSelectIndex);
			bool newVal = !val;
			
			if (newVal && singleCheck)
				SetSelectState (false);
			
	 		store.SetValue (iter, ColSelectIndex, !val);
			OnTableToggled ();
		}

		protected virtual void SetSelectState (bool state)
		{
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					store.SetValue (iter, ColSelectIndex, state);
				} while (store.IterNext (ref iter));
			}
		}
		
		protected virtual TreeIter GetTreeIter (TableSchema table)
		{
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					object obj = store.GetValue (iter, ColObjIndex);
					
					if (obj == table)
						return iter;
				} while (store.IterNext (ref iter));
			}
			return TreeIter.Zero;
		}
		
		protected virtual void OnTableAdded (object sender, SortedCollectionItemEventArgs<TableSchema> args)
		{
			AddTable (args.Item);
		}
		
		protected virtual void AddTable (TableSchema table)
		{
			store.AppendValues (false, table.Name, table);
			
			table.Changed += delegate (object sender, EventArgs args) {
				TreeIter iter = GetTreeIter (sender as TableSchema);
				if (!iter.Equals (TreeIter.Zero))
					store.SetValue (iter, ColNameIndex, table.Name);
			};
		}
		
		protected virtual void OnTableRemoved (object sender, SortedCollectionItemEventArgs<TableSchema> args)
		{
			TreeIter iter = GetTreeIter (args.Item);
			if (!iter.Equals (TreeIter.Zero))
				store.Remove (ref iter);
		}
		
		protected virtual int SortName (TreeModel model, TreeIter iter1, TreeIter iter2)
		{
			string name1 = model.GetValue (iter1, ColNameIndex) as string;
			string name2 = model.GetValue (iter2, ColNameIndex) as string;
		
			if (name1 == null && name2 == null) return 0;
			else if (name1 == null) return -1;
			else if (name2 == null) return 1;

			return name1.CompareTo (name2);
		}
		
		protected virtual void OnTableToggled ()
		{
			if (TableToggled != null)
				TableToggled (this, EventArgs.Empty);
		}
	}
}