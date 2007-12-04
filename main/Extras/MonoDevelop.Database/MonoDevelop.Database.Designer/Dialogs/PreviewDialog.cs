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
using System.Threading;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.Designer
{
	public partial class PreviewDialog : Gtk.Dialog
	{
		public PreviewDialog (string sql)
		{
			this.Build();

			sqlEditor.TextChanged += new EventHandler (SqlChanged);
			sqlEditor.Text = sql;
		}
		
		public string Text {
			get { return sqlEditor.Text; }
			set { sqlEditor.Text = value; }
		}

		protected virtual void CancelClicked (object sender, EventArgs e)
		{
			Respond (ResponseType.Cancel);
			Hide ();
		}

		protected virtual void OkClicked (object sender, EventArgs e)
		{
			Respond (ResponseType.Ok);
			Hide ();
		}
		
		protected virtual void SqlChanged (object sender, EventArgs e)
		{
			buttonOk.Sensitive = sqlEditor.Text.Length > 0;
		}
	}
}
