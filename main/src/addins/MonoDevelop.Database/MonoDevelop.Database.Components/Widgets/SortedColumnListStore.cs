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
	public class SortedColumnListStore
	{
		public event EventHandler ColumnToggled;		
		
		protected ColumnSchemaCollection columns;
		protected ListStore store;
		
		public const int ColSelectIndex = 0;
		public const int ColNameIndex = 1;
		public const int ColObjIndex = 2;
		
		protected bool singleCheck;
		
		public SortedColumnListStore (ColumnSchemaCollection columns)
		{
			if (columns == null)
				throw new ArgumentNullException ("columns");
			
			this.columns = columns;
			
			store = new ListStore (typeof (bool), typeof (string), typeof (ColumnSchema));
			store.SetSortColumnId (ColNameIndex, SortType.Ascending);
			store.SetSortFunc (ColNameIndex, new TreeIterCompareFunc (SortName));
			
			foreach (ColumnSchema col in columns)
				AddColumn (col);
			
			columns.ItemAdded += new SortedCollectionItemEventHandler<ColumnSchema> (OnColumnAdded);
			columns.ItemRemoved += new SortedCollectionItemEventHandler<ColumnSchema> (OnColumnRemoved);
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
		
		public virtual ColumnSchema GetColumnSchema (TreeIter iter)
		{
			if (!iter.Equals (TreeIter.Zero))
				return store.GetValue (iter, ColObjIndex) as ColumnSchema;
			return null;
		}
		
		public virtual IEnumerable<ColumnSchema> CheckedColumns {
			get {
				TreeIter iter;
				if (store.GetIterFirst (out iter)) {
					do {
						bool chk = (bool)store.GetValue (iter, ColSelectIndex);
						if (chk)
							yield return store.GetValue (iter, ColObjIndex) as ColumnSchema;
					} while (store.IterNext (ref iter));
				}
			}
		}
		
		public virtual bool IsColumnChecked {
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
			OnColumnToggled ();
		}
		
		public virtual void DeselectAll ()
		{
			SetSelectState (false);
			OnColumnToggled ();
		}
		
		public virtual void Select (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			
			ColumnSchema col = columns.Search (name);
			if (col != null)
				Select (col);
		}
		
		public virtual void Select (ColumnSchema column)
		{
			if (column == null)
				throw new ArgumentNullException ("column");
			
			TreeIter iter = GetTreeIter (column);
			if (!iter.Equals (TreeIter.Zero)) {
				if (singleCheck)
					SetSelectState (false);
				
				store.SetValue (iter, ColSelectIndex, true);
				OnColumnToggled ();
			}	
		}
		
		public virtual void ToggleSelect (TreeIter iter)
		{
			bool val = (bool) store.GetValue (iter, ColSelectIndex);
			bool newVal = !val;
			
			if (newVal && singleCheck)
				SetSelectState (false);
			
	 		store.SetValue (iter, ColSelectIndex, !val);
			OnColumnToggled ();
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
		
		protected virtual TreeIter GetTreeIter (ColumnSchema column)
		{
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					object obj = store.GetValue (iter, ColObjIndex);
					
					if (obj == column)
						return iter;
				} while (store.IterNext (ref iter));
			}
			return TreeIter.Zero;
		}
		
		protected virtual void OnColumnAdded (object sender, SortedCollectionItemEventArgs<ColumnSchema> args)
		{
			AddColumn (args.Item);
		}
		
		protected virtual void AddColumn (ColumnSchema column)
		{
			store.AppendValues (false, column.Name, column);
			
			column.Changed += delegate (object sender, EventArgs args) {
				TreeIter iter = GetTreeIter (sender as ColumnSchema);
				if (!iter.Equals (TreeIter.Zero))
					store.SetValue (iter, ColNameIndex, column.Name);
			};
		}
		
		protected virtual void OnColumnRemoved (object sender, SortedCollectionItemEventArgs<ColumnSchema> args)
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
		
		protected virtual void OnColumnToggled ()
		{
			if (ColumnToggled != null)
				ColumnToggled (this, EventArgs.Empty);
		}
	}
}