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
using GtkSourceView;
using System;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Database.Sql;

namespace MonoDevelop.Database.Components
{
	public partial class SqlEditorWidget : Bin
	{
		public event EventHandler TextChanged;
		
		private SourceView sourceView;
		
		public SqlEditorWidget()
		{
			this.Build();
			
			SourceLanguagesManager lm = new SourceLanguagesManager ();
			SourceLanguage lang = lm.GetLanguageFromMimeType ("text/x-sql");
			SourceBuffer buf = new SourceBuffer (lang);
			buf.Highlight = true;
			sourceView = new SourceView (buf);
			sourceView.ShowLineNumbers = true;
			
			sourceView.Buffer.Changed += new EventHandler (BufferChanged);
			sourceView.PopulatePopup += new PopulatePopupHandler (OnPopulatePopup);
			
			scrolledwindow.Add (sourceView);
			ShowAll ();
		}

		protected virtual void OnPopulatePopup (object sender, PopulatePopupArgs args)
		{
			CommandEntrySet cset = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/Database/ContextMenu/SqlEditor");
			if (cset.Count > 0) {
				cset.AddItem (Command.Separator);
				IdeApp.CommandService.InsertOptions (args.Menu, cset, 0);
			}
		}
		
		private void BufferChanged (object sender, EventArgs args)
		{
			if (TextChanged != null)
				TextChanged (this, EventArgs.Empty);
		}
		
		public string Text {
			get { return sourceView.Buffer.Text; }
			set {
				if (value == null)
					sourceView.Buffer.Text = String.Empty;
				else
					sourceView.Buffer.Text = value;
			}
		}
		
		public bool Editable {
			get { return sourceView.Editable; }
			set { sourceView.Editable = value; }
		}
		
		[CommandHandler (SqlEditorCommands.ImportFromFile)]
		protected void OnImportFromFile ()
		{
			FileChooserDialog dlg = new FileChooserDialog (
				GettextCatalog.GetString ("Import From File"), null, FileChooserAction.Open,
				"gtk-cancel", ResponseType.Cancel,
				"gtk-open", ResponseType.Accept
			);
			dlg.SelectMultiple = false;
			dlg.LocalOnly = true;
			dlg.Modal = true;
			
			FileFilter filter = new FileFilter ();
			filter.AddPattern ("*.[sS][qQ][lL]");
			filter.Name = GettextCatalog.GetString ("SQL files");
			FileFilter filterAll = new FileFilter ();
			filterAll.AddPattern ("*");
			filterAll.Name = GettextCatalog.GetString ("All files");
			dlg.AddFilter (filter);
			dlg.AddFilter (filterAll);

			if (dlg.Run () == (int)ResponseType.Accept) {
				using (FileStream stream = File.Open (dlg.Filename, FileMode.Open)) {
					using (StreamReader reader = new StreamReader (stream)) {
						Text = reader.ReadToEnd ();
					}
				}
			}
			dlg.Destroy ();
		}
		
		[CommandHandler (SqlEditorCommands.ExportToFile)]
		protected void OnExportToFile ()
		{
			FileChooserDialog dlg = new FileChooserDialog (
				GettextCatalog.GetString ("Export To File"), null, FileChooserAction.Save,
				"gtk-cancel", ResponseType.Cancel,
				"gtk-save", ResponseType.Accept
			);
			
			dlg.SelectMultiple = false;
			dlg.LocalOnly = true;
			dlg.Modal = true;
			
			FileFilter filter = new FileFilter ();
			filter.AddPattern ("*.[sS][qQ][lL]");
			filter.Name = GettextCatalog.GetString ("SQL files");
			dlg.AddFilter (filter);

			if (dlg.Run () == (int)ResponseType.Accept) {
				if (File.Exists (dlg.Filename)) {
					bool overwrite = Services.MessageService.AskQuestion (
						GettextCatalog.GetString ("Are you sure you want to overwrite the file '{0}'?", dlg.Filename), 
						GettextCatalog.GetString ("Overwrite?"));
					if (overwrite) {
						using (FileStream stream = File.Open (dlg.Filename, FileMode.Create)) {
							using (StreamWriter writer = new StreamWriter (stream)) {
								writer.Write (Text);
								writer.Flush ();
							}
						}
					}
				}
			}
			dlg.Destroy ();
		}
		
		[CommandUpdateHandler (SqlEditorCommands.ExportToFile)]
		protected void OnUpdateExportToFile (CommandInfo info)
		{
			info.Enabled = Text.Length > 0;
		}
	}
}
