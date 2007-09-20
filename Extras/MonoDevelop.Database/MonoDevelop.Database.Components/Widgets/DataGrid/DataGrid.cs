//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Database.Components
{
	public partial class DataGrid : Bin, ICommandDelegatorRouter
	{
		private bool limitPageSize = true;
		private int pageSize = 50;
		private int currentRecord = 0;
		private int numRecords = 0;
		private DataTable dataSource;
		
		private ListStore store;
		private DataGridColumn[] columns;
		private int columnCount = 0;
		
		private int sortColumn = 0;
		private IComparer sortComparer;
		
		private ObjectContentRenderer defaultContentRenderer;
		private Dictionary<Type, IDataGridContentRenderer> contentRenderers;
		private List<IDataGridVisualizer> visualizers;
		
		private Dictionary<int, ConvertObjectFunc> conversionLookup = new Dictionary<int, ConvertObjectFunc> ();
		private delegate string ConvertObjectFunc (object obj);
		
		private ConvertObjectFunc byteConvertFunc = new ConvertObjectFunc (ByteConvertFunc);
		private ConvertObjectFunc sbyteConvertFunc = new ConvertObjectFunc (SByteConvertFunc);
		
		public DataGrid ()
		{
			this.Build ();
			
			//Gtk# 2.10: grid.EnableGridLines = TreeViewGridLines.Both;
			grid.ButtonPressEvent += new ButtonPressEventHandler (ButtonPressed);

			contentRenderers = new Dictionary<Type, IDataGridContentRenderer> ();
			visualizers = new List<IDataGridVisualizer> ();
			
			AddContentRenderer (new BlobContentRenderer ());
			AddContentRenderer (new BooleanContentRenderer ());
			AddContentRenderer (new ByteContentRenderer ());
			AddContentRenderer (new DecimalContentRenderer ());
			AddContentRenderer (new DoubleContentRenderer ());
			AddContentRenderer (new FloatContentRenderer ());
			AddContentRenderer (new IntegerContentRenderer ());
			AddContentRenderer (new LongContentRenderer ());
			AddContentRenderer (new StringContentRenderer ());
			
			visualizers.Add (new ImageVisualizer ());
			visualizers.Add (new TextVisualizer ());
			visualizers.Add (new XmlTextVisualizer ());
			visualizers.Add (new XmlTreeVisualizer ());

			foreach (DataGridContentRendererCodon codon in AddinManager.GetExtensionNodes ("/MonoDevelop/Database/DataGrid/Renderers"))
				AddContentRenderer (codon.ContentRenderer);
		}
		
		public bool ShowNavigator {
			get { return hbox.Visible; }
			set { hbox.Visible = value; }
		}
		
		public bool EnableNavigator {
			get { return hbox.Sensitive; }
			set { hbox.Sensitive = value; }
		}
		
		public int PageSize {
			get { return pageSize; }
			set { pageSize = value; }
		}
		
		public bool LimitPageSize {
			get { return limitPageSize; }
			set {
				limitPageSize = value;
				ShowNavigator = LimitPageSize;
			}
		}
		
		public int CurrentRecord {
			get { return currentRecord; }
		}
		
		public int RecordCount {
			get { return numRecords; }
		}
		
		public void DataBind ()
		{
			if (dataSource == null) {
				Clear ();
				return;
			}
			
			int index = 0;
			Type[] storeTypes = new Type[dataSource.Columns.Count];
			columnCount = dataSource.Columns.Count;
			foreach (DataColumn col in dataSource.Columns) {
				DataGridColumn dgCol = new DataGridColumn (this, col, index);
				grid.AppendColumn (dgCol);

				if (col.DataType == typeof (byte)) {
					//special case for gchar (TODO: look up the bugzilla bug id)
					storeTypes[index] = typeof (string);
					conversionLookup.Add (index, byteConvertFunc);
				} else if (col.DataType == typeof (sbyte)) {
					storeTypes[index] = typeof (string);
					conversionLookup.Add (index, sbyteConvertFunc);
				} else if (col.DataType.IsPrimitive || col.DataType == typeof (string) || col.DataType.IsEnum) {
					storeTypes[index] = col.DataType;
				} else {
					//the ListStore doesn't allow types that can't be converted to a GType
					storeTypes[index] = typeof (object);
				}

				index++;
			}
			store = new ListStore (storeTypes);
			grid.Model = store;
			
			TreeIterCompareFunc sortFunc = new TreeIterCompareFunc (SortFunc);
			store.SetSortFunc (0, sortFunc);
			store.SetSortColumnId (0, SortType.Ascending); //TODO: is this needed ?
			
			NavigateToRecord (0);
		}
		
		public DataTable DataSource {
			get { return dataSource; }
			set {
				dataSource = value;
				if (value != null)
					numRecords = dataSource.Rows.Count;
				else
					numRecords = 0;
			}
		}
		
		public void Clear ()
		{
			currentRecord = 0;
			numRecords = 0;
			columnCount = 0;
			
			conversionLookup.Clear ();

			if (store != null) {
				store.Clear ();
				store = null;
			}
			
			if (columns != null) {
				for (int i=0; i<columns.Length; i++) {
					if (columns[i] != null)
						grid.RemoveColumn (columns[i]);
					columns[i] = null;
				}
				columns = null;
			}
		}

		public void NavigateToRecord (int record)
		{
			if (record < 0 || record > numRecords)
				throw new ArgumentException ("Invalid record index.");
			
			if (limitPageSize) {
				int count = numRecords - record;
				if (count > pageSize) count = pageSize;
				
				FillGrid (record, count);
			} else {
				int count = numRecords - record;
				FillGrid (record, count);
			}
			ShowNavigationState ();
		}
		
		object ICommandDelegatorRouter.GetNextCommandTarget ()
		{
			return null;
		}
		
		object ICommandDelegatorRouter.GetDelegatedCommandTarget ()
		{
//			if (dataObject != null) {
//				IDataGridVisualizer[] visualizers = GetItemVisualizers (dataObject);
//				if (visualizers.Length > 0) {
//					DataGridItemCommandHandler[] handlers = new DataGridItemCommandHandler [visualizers.Length];
//					for (int n=0; n<visualizers.Length; n++) {
//						handlers [n] = chain [n].CommandHandler;
//
//					for (int n=0; n<handlers.Length; n++) {
//						handlers [n].SetDataObject (dataObject);
//						if (n < chain.Length - 1)
//							handlers [n].SetNextTarget (handlers [n+1]);
//						else
//							handlers [n].SetNextTarget (null);
//					}
//					return handlers [0];
//				}
//			}
			return null;
		}
		
		private void ShowNavigationState ()
		{
			entryTotal.Text = numRecords.ToString ();
			int currentEnd = 0;
			if (limitPageSize) {
				currentEnd = currentRecord + pageSize;
				if (currentEnd > numRecords)
					currentEnd = numRecords;
			} else {
				currentEnd = numRecords;
			}
			entryCurrent.Text = String.Format ("{0}-{1}", currentRecord, currentEnd);
		}

		private void FillGrid (int start, int count)
		{
			grid.Model = null;
			
			int end = start + count;
			if (dataSource.Rows.Count < end)
				end = dataSource.Rows.Count;

			store.Clear ();
			for (int i=start; i<end; i++) {
				DataRow row = dataSource.Rows[i];
				
				TreeIter iter = store.Append ();
				for (int j=0;j<columnCount; j++) {
					//HACK: this is a hack for a bug that doesn't allow gchars in a liststore
					if (conversionLookup.ContainsKey (j)) {
						ConvertObjectFunc func = conversionLookup[j];
						store.SetValue (iter, j, func (row[j]));
					} else {
						store.SetValue (iter, j, row[j]);
					}
				}
			}
			
			grid.Model = store;
		}

		protected virtual void ButtonFirstClicked (object sender, System.EventArgs e)
		{
			currentRecord = 0;
			NavigateToRecord (currentRecord);
		}

		protected virtual void ButtonPreviousClicked (object sender, System.EventArgs e)
		{
			currentRecord -= pageSize;
			if (currentRecord < 0) currentRecord = 0;
			NavigateToRecord (currentRecord);
		}

		protected virtual void ButtonNextClicked (object sender, System.EventArgs e)
		{
			currentRecord += pageSize;
			if ((currentRecord + pageSize) > numRecords) {
				int modulus = numRecords % pageSize;
				currentRecord = numRecords - modulus;
			}
			NavigateToRecord (currentRecord);
		}

		protected virtual void ButtonLastClicked (object sender, System.EventArgs e)
		{
			int modulus = numRecords % pageSize;
			currentRecord = numRecords - modulus;
			NavigateToRecord (currentRecord);
		}

		protected virtual void EntryCurrentActivated (object sender, System.EventArgs e)
		{
			string[] split = entryCurrent.Text.Split (new char[]{'-'}, 2, StringSplitOptions.RemoveEmptyEntries);
			if (split == null || split.Length < 1) {
				ShowNavigationState ();
				return;
			}
			
			int curRec;
			int curEnd;
			
			if (int.TryParse (split[0], out curRec)) {
				if (curRec < 0)
					curRec = 0;
				else if (curRec >= numRecords)
					curRec = numRecords - 1;
				
				currentRecord = curRec;
			} else {
				return;
			}
			
			if (split.Length > 1 && int.TryParse (split[1], out curEnd)) {
				pageSize = curEnd - curRec;
				if (pageSize <= 0)
					pageSize = 50;
			}
			
			ShowNavigationState ();
		}
		
		internal IDataGridContentRenderer GetDataGridContentRenderer (Type type)
		{
			IDataGridContentRenderer renderer = null;
			if (contentRenderers.TryGetValue (type, out renderer))
				return renderer;
			
			if (defaultContentRenderer == null)
				defaultContentRenderer = new ObjectContentRenderer ();
			return defaultContentRenderer;
		}
		
		internal void Sort (DataGridColumn column)
		{
			sortColumn = column.ColumnIndex;
			sortComparer = column.ContentComparer;
			
			if (column.SortIndicator)
				column.SortOrder = ReverseSortOrder (column);
			
			//show indicator on current column, remove on all others
			foreach (TreeViewColumn col in grid.Columns)
				col.SortIndicator = col == column;
			
			store.SetSortFunc (column.ColumnIndex, new TreeIterCompareFunc (SortFunc));
			store.SetSortColumnId (column.ColumnIndex, column.SortOrder);
		}
		
		private SortType ReverseSortOrder (TreeViewColumn column)
		{
			if (column.SortIndicator)  {
				if (column.SortOrder == SortType.Ascending)
					return SortType.Descending;
				else
					return SortType.Ascending;
			} else {
				return SortType.Ascending;
			}
		}
		
		private int SortFunc (TreeModel model, TreeIter x, TreeIter y)
		{
			if (sortComparer == null)
				return 0;
			
			object ox = model.GetValue (x, sortColumn);
			object oy = model.GetValue (y, sortColumn);
		
			return sortComparer.Compare (ox, oy);
		}
		
		private void AddContentRenderer (IDataGridContentRenderer renderer)
		{
			foreach (Type type in renderer.DataTypes) {
				if (contentRenderers.ContainsKey (type))
					Runtime.LoggingService.ErrorFormat ("Duplicate IDataGridContentRenderer for type '{0}'", type.FullName);
				else
					contentRenderers.Add (type, renderer);
			}
		}
		
		[GLib.ConnectBefore]
		private void ButtonPressed (object sender, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3) {
				TreePath path = null;
				TreeViewColumn col = null;
				if (grid.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path, out col)) {
					//DataGridColumn dgCol = col as DataGridColumn;
					TreeIter iter;
					if (store.GetIter (out iter, path)) {
						//dataObject = store.GetValue (iter, dgCol.ColumnIndex);
							
						IdeApp.CommandService.ShowContextMenu ("/MonoDevelop/Database/ContextMenu/DataGrid");
					}
				}
			}
		}
		
		private static string ByteConvertFunc (object obj)
		{
			if (Convert.IsDBNull (obj) || obj == null)
				return null;
			
			byte b = (byte)obj;
			return "0x" + b.ToString ("X");
		}
		
		private static string SByteConvertFunc (object obj)
		{
			if (Convert.IsDBNull (obj) || obj == null)
				return null;			
			
			sbyte b = (sbyte)obj;
			return b.ToString ("N");
		}
	}
}
