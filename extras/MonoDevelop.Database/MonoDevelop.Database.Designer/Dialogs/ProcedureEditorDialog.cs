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
using System.IO;
using System.Threading;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.Designer
{
	public partial class ProcedureEditorDialog : Gtk.Dialog
	{
		private SchemaActions action;
		
		private Notebook notebook;
		
		private IEditSchemaProvider schemaProvider;
		private ProcedureSchema procedure;
		
		private CommentEditorWidget commentEditor;
		private SqlEditorWidget sqlEditor;
		private ProcedureEditorSettings settings;
		
		public ProcedureEditorDialog (IEditSchemaProvider schemaProvider, bool create, ProcedureEditorSettings settings)
		{
			if (schemaProvider == null)
				throw new ArgumentNullException ("schemaProvider");
			
			this.settings = settings;
			this.schemaProvider = schemaProvider;
			this.action = create ? SchemaActions.Create : SchemaActions.Alter;
			
			this.Build();
			
			if (create)
				Title = AddinCatalog.GetString ("Create Procedure");
			else
				Title = AddinCatalog.GetString ("Alter Procedure");
			
			notebook = new Notebook ();

			sqlEditor = new SqlEditorWidget ();
			sqlEditor.TextChanged += new EventHandler (SqlChanged);
			notebook.AppendPage (sqlEditor, new Label (AddinCatalog.GetString ("Definition")));
			
			if (settings.ShowComment) {
				commentEditor = new CommentEditorWidget ();
				notebook.AppendPage (commentEditor, new Label (AddinCatalog.GetString ("Comment")));
			}
			
			if (!settings.ShowName) {
				nameLabel.Visible = false;
				entryName.Visible = false;
			}

			vboxContent.PackStart (notebook, true, true, 0);
			vboxContent.ShowAll ();
			SetWarning (null);
		}
		
		public void Initialize (ProcedureSchema procedure)
		{
			if (procedure == null)
				throw new ArgumentNullException ("procedure");
			
			this.procedure = procedure;
			entryName.Text = procedure.Name;

			if (action == SchemaActions.Alter) {
				sqlEditor.Text = schemaProvider.GetProcedureAlterStatement (procedure);
				if (commentEditor != null)
					commentEditor.Comment = procedure.Comment;
			} else 
				sqlEditor.Text = procedure.Definition;
		}

		protected virtual void OkClicked (object sender, EventArgs e)
		{
			if (settings.ShowName)
				procedure.Name = entryName.Text;
			procedure.Definition = sqlEditor.Text;
			
			if (settings.ShowComment)
				procedure.Comment = commentEditor.Comment;
			
			Respond (ResponseType.Ok);
			Hide ();
		}

		protected virtual void CancelClicked (object sender, EventArgs e)
		{
			Respond (ResponseType.Cancel);
			Hide ();
		}

		protected virtual void NameChanged (object sender, EventArgs e)
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
		protected virtual void OnButtonSaveClicked (object sender, System.EventArgs e)
		{
			FileChooserDialog dlg = new FileChooserDialog (
				AddinCatalog.GetString ("Save Script"), null, FileChooserAction.Save,
				"gtk-cancel", ResponseType.Cancel,
				"gtk-save", ResponseType.Accept
			);
			dlg.SelectMultiple = false;
			dlg.LocalOnly = true;
			dlg.Modal = true;
		
			FileFilter filter = new FileFilter ();
			filter.AddPattern ("*.sql");
			filter.Name = AddinCatalog.GetString ("SQL Scripts");
			FileFilter filterAll = new FileFilter ();
			filterAll.AddPattern ("*");
			filterAll.Name = AddinCatalog.GetString ("All files");
			dlg.AddFilter (filter);
			dlg.AddFilter (filterAll);

			try {
				if (dlg.Run () == (int)ResponseType.Accept) {
					if (File.Exists (dlg.Filename)) {
						if (!MessageService.Confirm (AddinCatalog.GetString (@"File {0} already exists. 
													Do you want to overwrite\nthe existing file?", dlg.Filename), 
						                             AlertButton.Yes))
							return;
						else
							File.Delete (dlg.Filename);
					}
				 	using (StreamWriter writer =  File.CreateText (dlg.Filename)) {
						writer.Write (sqlEditor.Text);
						writer.Close ();
					}
					
				}
			} finally {
				dlg.Destroy ();					
			}
		}
		
		protected virtual void OnButtonOpenClicked (object sender, System.EventArgs e)
		{
			FileChooserDialog dlg = new FileChooserDialog (
				AddinCatalog.GetString ("Open Script"), null, FileChooserAction.Open,
				"gtk-cancel", ResponseType.Cancel,
				"gtk-open", ResponseType.Accept
			);
			dlg.SelectMultiple = false;
			dlg.LocalOnly = true;
			dlg.Modal = true;
		
			FileFilter filter = new FileFilter ();
			filter.AddPattern ("*.sql");
			filter.Name = AddinCatalog.GetString ("SQL Scripts");
			FileFilter filterAll = new FileFilter ();
			filterAll.AddPattern ("*");
			filterAll.Name = AddinCatalog.GetString ("All files");
			dlg.AddFilter (filter);
			dlg.AddFilter (filterAll);

			try {
				if (dlg.Run () == (int)ResponseType.Accept) {
					
				 	using (StreamReader reader =  File.OpenText (dlg.Filename)) {
						sqlEditor.Text = reader.ReadToEnd ();
						reader.Close ();
					}
					
				}
			} finally {
				dlg.Destroy ();					
			}
		}
		
	}
	
	public class ProcedureEditorSettings
	{
		bool showComment = false;
		bool showName = true;
		
		public bool ShowComment {
			get { return showComment; }
			set { showComment = value; }
		}
		
		public bool ShowName {
			get { return showName; }
			set { showName = value; }
		}
	}
}
