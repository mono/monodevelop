//
// Authors:
//   Christian Hergert <chris@mosaix.net>
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2005 Christian Hergert
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
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.Query
{
	public class QueryResultView : AbstractViewContent
	{
		protected DataGrid grid;
		
		public QueryResultView ()
			: base ()
		{
			grid = new DataGrid ();
			grid.ShowAll ();
		}
		
		public QueryResultView (DataTable table)
			: this ()
		{
			LoadDataTable (table);
		}
		
		public override string UntitledName {
			get { return "UntitledResult"; }
		}
		
		public override void Dispose ()
		{
			Control.Dispose ();
		}
		
		public override void Load (string filename)
		{
			throw new NotImplementedException ();
		}

		public void LoadDataTable (DataTable table)
		{
			grid.Clear ();
			grid.DataSource = table;
			grid.DataBind ();
		}
		
		public override Widget Control {
			get { return grid; }
		}
	}
}