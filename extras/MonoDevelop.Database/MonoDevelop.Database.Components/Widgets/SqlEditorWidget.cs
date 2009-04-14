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
using Mono.TextEditor;
using System;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Database.Sql;
using MonoDevelop.SourceEditor;

namespace MonoDevelop.Database.Components
{
	//TODO: use the abstracted MD source editor widget
	//TODO: remove gtksourceview-sharp as dependency + from configure.in
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class SqlEditorWidget : Bin
	{
		public event EventHandler TextChanged;
		
		private Mono.TextEditor.TextEditor sourceView;
		
		public SqlEditorWidget()
		{
			this.Build();
			
			sourceView = new Mono.TextEditor.TextEditor ();
			// The SourceEditor Addin should be initialized before opening any Project.
			// Database Addin works with or without project opened.
			MonoDevelop.SourceEditor.Extension.TemplateExtensionNodeLoader.Init ();
			sourceView.Document.MimeType = "text/x-sql";
			
			// TODO: Set styling ?
			//	sourceView.Options = new MonoDevelop.SourceEditor.StyledSourceEditorOptions (null);
			//	sourceView.ShowLineNumbers = true;
			
			sourceView.Document.TextReplaced += BufferChanged;
			sourceView.TextViewMargin.ButtonPressed += delegate (object s, MarginMouseEventArgs args) {
				if (args.Button == 3) {
					IdeApp.CommandService.ShowContextMenu ("/MonoDevelop/Database/ContextMenu/SqlEditor");
				}
			};
			
			scrolledwindow.Add (sourceView);
			ShowAll ();
		}
		
		private void BufferChanged (object sender, ReplaceEventArgs args)
		{
			if (TextChanged != null)
				TextChanged (this, EventArgs.Empty);
		}
		
		public string Text {
			get { return sourceView.Document.Text; }
			set {
				if (value == null)
					sourceView.Document.Text = String.Empty;
				else
					sourceView.Document.Text = value;
			}
		}
		
		public bool Editable {
			get { return !sourceView.Document.ReadOnly; }
			set { sourceView.Document.ReadOnly = !value; }
		}
		
		[CommandHandler (SqlEditorCommands.ImportFromFile)]
		protected void OnImportFromFile ()
		{
			FileChooserDialog dlg = new FileChooserDialog (
				AddinCatalog.GetString ("Import From File"), null, FileChooserAction.Open,
				"gtk-cancel", ResponseType.Cancel,
				"gtk-open", ResponseType.Accept
			);
			dlg.SelectMultiple = false;
			dlg.LocalOnly = true;
			dlg.Modal = true;
			
			FileFilter filter = new FileFilter ();
			filter.AddPattern ("*.[sS][qQ][lL]");
			filter.Name = AddinCatalog.GetString ("SQL files");
			FileFilter filterAll = new FileFilter ();
			filterAll.AddPattern ("*");
			filterAll.Name = AddinCatalog.GetString ("All files");
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
				AddinCatalog.GetString ("Export To File"), null, FileChooserAction.Save,
				"gtk-cancel", ResponseType.Cancel,
				"gtk-save", ResponseType.Accept
			);
			
			dlg.SelectMultiple = false;
			dlg.LocalOnly = true;
			dlg.Modal = true;
			
			FileFilter filter = new FileFilter ();
			filter.AddPattern ("*.[sS][qQ][lL]");
			filter.Name = AddinCatalog.GetString ("SQL files");
			dlg.AddFilter (filter);

			if (dlg.Run () == (int)ResponseType.Accept) {
				if (File.Exists (dlg.Filename)) {
					bool overwrite = MessageService.Confirm (
						AddinCatalog.GetString ("Are you sure you want to overwrite the file '{0}'?", dlg.Filename), AlertButton.OverwriteFile);
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
