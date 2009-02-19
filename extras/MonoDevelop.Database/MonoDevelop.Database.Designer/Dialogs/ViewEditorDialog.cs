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
	public partial class ViewEditorDialog : Gtk.Dialog
	{
		private SchemaActions action;
		
		private Notebook notebook;
		
		private ISchemaProvider schemaProvider;
		private ViewSchema view;
		
		private CommentEditorWidget commentEditor;
		private SqlEditorWidget sqlEditor;
		
		public ViewEditorDialog (ISchemaProvider schemaProvider, bool create, ViewEditorSettings settings)
		{
			if (schemaProvider == null)
				throw new ArgumentNullException ("schemaProvider");
			
			
			this.schemaProvider = schemaProvider;
			this.action = create ? SchemaActions.Create : SchemaActions.Alter;
			
			this.Build();
			
			if (create)
				Title = AddinCatalog.GetString ("Create View");
			else
				Title = AddinCatalog.GetString ("Alter View");
			
			notebook = new Notebook ();

			sqlEditor = new SqlEditorWidget ();
			sqlEditor.TextChanged += new EventHandler (SqlChanged);
			notebook.AppendPage (sqlEditor, new Label (AddinCatalog.GetString ("Definition")));
			
			if (settings.ShowComment) {
				commentEditor = new CommentEditorWidget ();
				notebook.AppendPage (commentEditor, new Label (AddinCatalog.GetString ("Comment")));
			}

			notebook.Page = 0;

			entryName.Text = view.Name;

			vboxContent.PackStart (notebook, true, true, 0);
			vboxContent.ShowAll ();
			SetWarning (null);
		}
		
		public void Initialize (ViewSchema view)
		{
			if (view == null)
				throw new ArgumentNullException ("view");
			this.view = view;
			
			if (action == SchemaActions.Alter) {
//				sqlEditor.Text = schemaProvider.GetViewAlterStatement (view);
				commentEditor.Comment = view.Comment;
			}
		}

		protected virtual void CancelClicked (object sender, System.EventArgs e)
		{
			Respond (ResponseType.Cancel);
			Hide ();
		}

		protected virtual void OkClicked (object sender, System.EventArgs e)
		{
			view.Name = entryName.Text;
			view.Definition = sqlEditor.Text;
			view.Comment = commentEditor.Comment;
			
			Respond (ResponseType.Ok);
			Hide ();
		}

		protected virtual void NameChanged (object sender, System.EventArgs e)
		{
			CheckState ();
		}
		
		protected virtual void SqlChanged (object sender, EventArgs e)
		{
			CheckState ();
		}
		
		private void CheckState ()
		{
			buttonOk.Sensitive = entryName.Text.Length > 0 && sqlEditor.Text.Length > 0;
			//TODO: check for duplicate name
		}
		
		protected virtual void SetWarning (string msg)
		{
			if (msg == null) {
				hboxWarning.Hide ();
				labelWarning.Text = "";
			} else {
				hboxWarning.ShowAll ();
				labelWarning.Text = msg;
			}
		}
	}
	
	public class ViewEditorSettings
	{
		private bool showComment = false;

		public bool ShowComment {
			get { return showComment; }
			set { showComment = value; }
		}
	}
}
