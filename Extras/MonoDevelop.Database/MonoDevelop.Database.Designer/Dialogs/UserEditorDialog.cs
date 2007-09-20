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
	public partial class UserEditorDialog : Gtk.Dialog
	{
		private SchemaActions action;
		
		private Notebook notebook;
		
		private ISchemaProvider schemaProvider;
		private UserSchema user;
		
		public UserEditorDialog (ISchemaProvider schemaProvider, UserSchema user, bool create)
		{
			this.Build();
			
			if (schemaProvider == null)
				throw new ArgumentNullException ("schemaProvider");
			if (user == null)
				throw new ArgumentNullException ("user");
			
			this.schemaProvider = schemaProvider;
			this.user = user;
			this.action = create ? SchemaActions.Create : SchemaActions.Alter;
			
			this.Build();
			
			if (create)
				Title = GettextCatalog.GetString ("Create User");
			else
				Title = GettextCatalog.GetString ("Alter User");
			
			notebook = new Notebook ();
			vboxContent.PackStart (notebook, true, true, 0);
			vboxContent.ShowAll ();
		}

		protected virtual void OkClicked (object sender, System.EventArgs e)
		{
		}

		protected virtual void CancelClicked (object sender, System.EventArgs e)
		{
		}

		protected virtual void NameChanged (object sender, System.EventArgs e)
		{
		}
	}
}
