//
// Authors:
//   Christian Hergert <chris@mosaix.net>
//   Daniel Morgan <danielmorgan@verizon.net>
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2005 Christian Hergert
// Copyright (C) 2005 Daniel Morgan
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
using System.Threading;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Database.Sql;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.Query
{
	public class SqlQueryView : AbstractViewContent
	{
		private VBox vbox;
		private SqlEditorWidget sqlEditor;
		private ToolButton buttonExecute;
		private ToolButton buttonStop;
		private ToolButton buttonClear;
		private DatabaseConnectionContextComboBox comboConnections;
		private Notebook notebook;
		private VPaned pane;
		private TextView status;
		
		private object currentQueryState;
		private List<object> stoppedQueries;
		private DateTime queryStart;
		
		private DatabaseConnectionContext selectedConnection;
		
		public SqlQueryView ()
		{
			stoppedQueries = new List<object> ();
			
			vbox = new VBox (false, 6);
			vbox.BorderWidth = 6;

			sqlEditor = new SqlEditorWidget ();
			sqlEditor.TextChanged += new EventHandler (SqlChanged);
			
			Toolbar toolbar = new Toolbar ();
			toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
			
			buttonExecute = new ToolButton (
				ImageService.GetImage ("md-db-execute", IconSize.SmallToolbar),
					AddinCatalog.GetString ("Execute")
			);
			buttonStop = new ToolButton ("gtk-stop");
			buttonClear = new ToolButton (ImageService.GetImage ("gtk-clear", IconSize.Button), AddinCatalog.GetString ("Clear Results"));
			buttonStop.Sensitive = false;
			buttonExecute.Sensitive = false;
			
			buttonExecute.Clicked += new EventHandler (ExecuteClicked);
			buttonStop.Clicked += new EventHandler (StopClicked);
			buttonClear.Clicked += new EventHandler (ClearClicked);
			
			comboConnections = new DatabaseConnectionContextComboBox ();
			selectedConnection = comboConnections.DatabaseConnection;
			comboConnections.Changed += new EventHandler (ConnectionChanged);

			buttonExecute.IsImportant = true;
			
			ToolItem comboItem = new ToolItem ();
			comboItem.Child = comboConnections;
			
			toolbar.Add (buttonExecute);
			toolbar.Add (buttonStop);
			toolbar.Add (buttonClear);
			toolbar.Add (new SeparatorToolItem ());
			toolbar.Add (comboItem);
			
			pane = new VPaned ();

			ScrolledWindow windowStatus = new ScrolledWindow ();
			status = new TextView ();
			windowStatus.Add (status);
			
			notebook = new Notebook ();
			notebook.AppendPage (windowStatus, new Label (AddinCatalog.GetString ("Status")));
			
			pane.Pack1 (sqlEditor, true, true);
			pane.Pack2 (notebook, true, true);
			
			vbox.PackStart (toolbar, false, true, 0);
			vbox.PackStart (pane, true, true, 0);
			
			vbox.ShowAll ();
			notebook.Hide ();
		}
		
		public string Text {
			get { return sqlEditor.Text; }
			set { sqlEditor.Text = value; }
		}
		
		public DatabaseConnectionContext SelectedConnectionContext {
			get { return selectedConnection; }
			set {
				if (selectedConnection != value) {
					selectedConnection = value;
					buttonExecute.Sensitive = value != null;
					comboConnections.DatabaseConnection = value;
				}
			}
		}
		
		public override string UntitledName {
			get { return "SQL Query"; }
		}
		
		public override void Dispose ()
		{
			Control.Destroy ();
		}
		
		public override void Load (string filename)
		{
			throw new NotImplementedException ();
		}
		
		public override Widget Control {
			get { return vbox; }
		}
		
		public void ExecuteQuery ()
		{
			if (selectedConnection == null || Text.Length < 0) {
				SetQueryState (false, "No connection selected.");
				return;
			}
			
			QueryService.EnsureConnection (selectedConnection, new DatabaseConnectionContextCallback (ExecuteQueryCallback), null);
		}
		
		private void ExecuteQueryCallback (DatabaseConnectionContext context, bool connected, object state)
		{
			if (!connected) {
				MessageService.ShowError (
					AddinCatalog.GetString ("Unable to connect to database '{0}'"), context.ConnectionSettings.Name);
				return;
			}
			
			currentQueryState = new object ();
			IPooledDbConnection conn = context.ConnectionPool.Request ();
			IDbCommand command = conn.CreateCommand (Text);
			queryStart = DateTime.Now;
			conn.ExecuteSetAsync (command, new ExecuteCallback<DataSet> (ExecuteQueryThreaded), currentQueryState);
		}
		
		private void ExecuteQueryThreaded (IPooledDbConnection connection, DataSet result, object state)
		{
			connection.Release ();
			TimeSpan duration = DateTime.Now.Subtract (queryStart);

			DispatchService.GuiDispatch (delegate () {
				notebook.ShowAll ();
				string msg = String.Concat (
					AddinCatalog.GetPluralString ("Query executed ({0} result table)",
						"Query executed ({0} result tables)", result.Tables.Count),
					Environment.NewLine,
				        AddinCatalog.GetString ("Query duration: {0}", duration.ToString ())
				);
				SetQueryState (false, String.Format (msg, result.Tables.Count));
			});
			
			if (stoppedQueries.Contains (state)) {
				stoppedQueries.Remove (state);
				return;
			}

			if (result != null) {
				foreach (DataTable table in result.Tables) {
					DispatchService.GuiDispatch (delegate () {
						MonoDevelop.Database.Components.DataGrid grid = new MonoDevelop.Database.Components.DataGrid ();
						grid.DataSource = table;
						grid.DataBind ();
	
						string msg = String.Concat (Environment.NewLine, AddinCatalog.GetString ("Table"), ": ",table.TableName,
							Environment.NewLine, "\t", AddinCatalog.GetString ("Affected Rows"), ": ", table.Rows.Count);
						status.Buffer.Text += msg;
							
						TabLabel label = new TabLabel (new Label (table.TableName), ImageService.GetImage ("md-db-table", IconSize.Menu));
						label.CloseClicked += new EventHandler (OnResultTabClose);
						notebook.AppendPage (grid, label);
						notebook.ShowAll ();
					});
				}
			}
			
			if (result == null || result.Tables.Count == 0) {
				DispatchService.GuiDispatch (delegate () {
					status.Buffer.Text += AddinCatalog.GetString ("No Results");
				});
			}
		}
			
		private void OnResultTabClose (object sender, EventArgs args)
		{
			Widget tabLabel = (Widget)sender;
			foreach (Widget child in notebook.Children) {
				if (notebook.GetTabLabel (child) == tabLabel) {
					notebook.Remove (child);
					break;
				}
			}
		}
		
		private void ExecuteClicked (object sender, EventArgs e)
		{
			SetQueryState (true, AddinCatalog.GetString ("Executing query"));
			ExecuteQuery ();
		}
		
		private void ClearClicked (object sender, EventArgs e)
		{
			for (int i=1; i<notebook.NPages; i++)
				notebook.RemovePage (i);
			status.Buffer.Text = String.Empty;
			notebook.Hide ();
		}
		
		private void StopClicked (object sender, EventArgs e)
		{
			SetQueryState (false, AddinCatalog.GetString ("Query execute cancelled"));
			
			//since we can't abort a threadpool task, each task is assigned a unique state
			//when stop is pressed, the state is added to the list of results that need
			//to be discarded when they get in
			if (!stoppedQueries.Contains (currentQueryState))
				stoppedQueries.Add (currentQueryState);
		}
		
		private void ConnectionChanged (object sender, EventArgs args)
		{
			selectedConnection = comboConnections.DatabaseConnection;
			buttonExecute.Sensitive = sqlEditor.Text.Length > 0;
		}
		
		private void SqlChanged (object sender, EventArgs args)
		{
			buttonExecute.Sensitive = sqlEditor.Text.Length > 0;
		}
		
		private void SetQueryState (bool exec, string msg)
		{
			buttonExecute.Sensitive = !exec && sqlEditor.Text.Length > 0;
			buttonStop.Sensitive = exec;
			buttonClear.Sensitive = !exec;
			sqlEditor.Editable = !exec;
			notebook.Show ();
			
			status.Buffer.Text = msg + Environment.NewLine;
		}
	}
	
	internal class ConnectionContextMenuItem : RadioMenuItem
	{
		private DatabaseConnectionContext context;
		
		public ConnectionContextMenuItem (DatabaseConnectionContext context)
			: base (context.ConnectionSettings.Name)
		{
			this.context = context;
		}
		
		public DatabaseConnectionContext ConnectionContext {
			get { return context; }
		}
		
		public void Update ()
		{
			(Child as Label).Text = context.ConnectionSettings.Name;
		}
	}
}
