//
// Authors:
//  	tomasz kubacki <tomasz.kubacki@gmail.com>
//
// Copyright (c) 2011 Tomasz Kubacki
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

using Gtk;
using System;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Database.Components;
using MonoDevelop.Database.Sql;
using System.Data;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;


namespace MonoDevelop.Database.Query
{
	public class QueryResultPad : TreeViewPad
	{
		VBox vbox;
		DatabaseConnectionContextComboBox comboConnections;
		Notebook notebook;
		VPaned pane;
		SqlEditorWidget history;
		
		public QueryResultPad ()
		{
			vbox = new VBox (false, 6);
			vbox.BorderWidth = 6;
				
			pane = new VPaned ();

			// Sql History Window
			ScrolledWindow windowHistory = new ScrolledWindow ();
			history = new SqlEditorWidget ();
			history.Editable = false;
			windowHistory.AddWithViewport (history);

			notebook = new Notebook ();
			notebook.AppendPage (windowHistory, new Label (AddinCatalog.GetString ("Query History")));
			
			pane.Pack2 (notebook, true, true);			
			vbox.PackStart (pane, true, true, 0);	
			vbox.ShowAll ();
		}
		
		public override void Initialize (NodeBuilder[] builders, TreePadOption[] options, string contextMenuPath)
		{
			base.Initialize (builders, options, contextMenuPath);						
			 
		}
		
		public void ShowResults() {
			notebook.ShowAll ();
		}
		
		
		public void AppendeResultTab(DataTable table) {
			
			MonoDevelop.Database.Components.DataGrid grid = new MonoDevelop.Database.Components.DataGrid ();
						grid.DataSource = table;
						grid.DataBind ();
			
			string msg = String.Concat (AddinCatalog.GetString ("Affected Rows"), ": ", table.Rows.Count);
			IdeApp.Workbench.StatusBar.ShowMessage (msg);
			AppendHistory(msg);
			TabLabel label = new TabLabel (new Label (table.TableName), ImageService.GetImage ("md-db-table", IconSize.Menu));
			
			label.CloseClicked += new EventHandler (OnResultTabClose);
			notebook.AppendPage (grid, label);
			notebook.ShowAll ();
			notebook.Page = notebook.NPages -1;
		}
		
		void OnResultTabClose (object sender, EventArgs args)
		{
			Widget tabLabel = (Widget)sender;
			foreach (Widget child in notebook.Children) {
				if (notebook.GetTabLabel (child) == tabLabel) {
					notebook.Remove (child);
					break;
				}
			}
		}
			
		public void AppendHistory (string entry) {
			if (history.Text == String.Empty)
				history.Text = string.Concat (history.Text, entry);
			else
				history.Text = string.Concat (history.Text, Environment.NewLine, entry);
		}
				
		public void Clear () {
			while (notebook.NPages > 1)
				notebook.RemovePage (1);
			history.Text = String.Empty;
		}
		
		public override Widget Control {
			get { return vbox; }
		}
	}
}