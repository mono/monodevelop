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
using System.Data;
using System.Collections;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Database.Components
{
	public class DataGridColumn : TreeViewColumn
	{
		private DataGrid grid;
		private DataColumn column;
		private int columnIndex;
		private IDataGridContentRenderer contentRenderer;
		private static IDataGridContentRenderer nullRenderer;
		
		static DataGridColumn ()
		{
			nullRenderer = new NullContentRenderer ();
		}
		
		public DataGridColumn (DataGrid grid, DataColumn column, int columnIndex)
		{
			this.grid = grid;
			this.column = column;
			this.columnIndex = columnIndex;
			
			contentRenderer = grid.GetDataGridContentRenderer (column.DataType);

			Title = column.ColumnName.Replace ("_", "__"); //underscores are normally used for underlining, so needs escape char
			Clickable = true;
			
			CellRendererText textRenderer = new CellRendererText ();
			PackStart (textRenderer, true);
			SetCellDataFunc (textRenderer, new CellLayoutDataFunc (ContentDataFunc));
		}
		
		public int ColumnIndex {
			get { return columnIndex; }
		}
		
		public IComparer ContentComparer {
			get { return contentRenderer; }
		}
		
		public Type DataType {
			get { return column.DataType; }
		}

		private void ContentDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			object dataObject = model.GetValue (iter, columnIndex);
			if (dataObject == null)
				nullRenderer.SetContent (cell as CellRendererText, dataObject);
			else
				contentRenderer.SetContent (cell as CellRendererText, dataObject);
		}
		
		protected override void OnClicked ()
		{
			base.OnClicked ();
			grid.Sort (this);
		}

	}
}