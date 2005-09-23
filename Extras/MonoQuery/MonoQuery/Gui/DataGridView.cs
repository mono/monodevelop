//
// DataGridView.cs: View information in a data table.
//
// Author:
//   Christian Hergert <chris@mosaix.net>
//
// Copyright (C) 2005 Christian Hergert
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Data;

using Gtk;

using Mono.Data.Sql;

using MonoDevelop.Gui;
using MonoDevelop.Gui.Widgets;

namespace MonoQuery
{
	public class DataGridView : AbstractViewContent
	{
		protected Frame frame;
		protected DataGrid grid;
		
		public DataGridView () : base ()
		{
			frame = new Gtk.Frame ();
			grid = new DataGrid ();
			frame.Add (grid);
			frame.ShowAll ();
		}
		
		public DataGridView (DataTable table) : this ()
		{
			LoadDataTable (table);
		}
		
		public override string UntitledName {
			get {
				return "UntitledResult";
			}
		}
		
		public override void Dispose ()
		{
			Control.Dispose ();
		}
		
		public override void Load (string filename)
		{
		}
		
		public void LoadDataTable (DataTable table)
		{
			grid.DataSource = table;
			grid.DataBind ();
		}
		
		public override Gtk.Widget Control {
			get {
				return frame;
			}
		}
	}
}