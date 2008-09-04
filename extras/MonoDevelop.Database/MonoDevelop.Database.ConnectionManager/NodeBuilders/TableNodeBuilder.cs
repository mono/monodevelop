//
// Authors:
//   Christian Hergert	<chris@mosaix.net>
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
// Copyright (c) 2007 Ben Motmans
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
using System.Data;
using System.Threading;
using System.Collections.Generic;
using MonoDevelop.Database.Sql;
using SQL = MonoDevelop.Database.Sql;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Database.Query;
using MonoDevelop.Database.Components;
using MonoDevelop.Database.Designer;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Database.ConnectionManager
{
	public class TableNodeBuilder : TypeNodeBuilder
	{
		private EventHandler RefreshHandler;

		public TableNodeBuilder ()
			: base ()
		{
			RefreshHandler = new EventHandler (OnRefreshEvent);
		}
		
		public override Type NodeDataType {
			get { return typeof (TableNode); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Database/ContextMenu/ConnectionManagerPad/TableNode"; }
		}
		
		public override Type CommandHandlerType {
			get { return typeof (TableNodeCommandHandler); }
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			TableNode node = dataObject as TableNode;
			return node.Table.Name;
		}
		
		public override void BuildNode (ITreeBuilder builder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			TableNode node = dataObject as TableNode;
			node.RefreshEvent += (EventHandler)DispatchService.GuiDispatch (RefreshHandler);

			label = node.Table.Name;
			icon = Context.GetIcon ("md-db-table");
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			ThreadPool.QueueUserWorkItem (new WaitCallback (BuildChildNodesThreaded), dataObject);
		}
		
		private void BuildChildNodesThreaded (object state)
		{
			TableNode node = state as TableNode;
			ITreeBuilder builder = Context.GetTreeBuilder (state);
			ISchemaProvider provider = node.ConnectionContext.SchemaProvider;
		
			if (provider.IsSchemaActionSupported (SQL.SchemaType.TableColumn, SchemaActions.Schema))
				DispatchService.GuiDispatch (delegate {
					builder.AddChild (new ColumnsNode (node.ConnectionContext, node.Table));
				});
			
			if (provider.IsSchemaActionSupported (SQL.SchemaType.Constraint, SchemaActions.Schema))
				DispatchService.GuiDispatch (delegate {
					builder.AddChild (new ConstraintsNode (node.ConnectionContext, node.Table));
				});
			
			if (provider.IsSchemaActionSupported (SQL.SchemaType.Trigger, SchemaActions.Schema))
				DispatchService.GuiDispatch (delegate {
					builder.AddChild (new TriggersNode (node.ConnectionContext));
				});
			
			//TODO: rules
			
			DispatchService.GuiDispatch (delegate {
				builder.Expanded = true;
			});
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		private void OnRefreshEvent (object sender, EventArgs args)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (sender);
			builder.Update ();
		}
	}
	
	public class TableNodeCommandHandler : NodeCommandHandler
	{
		public override DragOperation CanDragNode ()
		{
			return DragOperation.None;
		}
		
		public override void ActivateItem ()
		{
			OnQueryCommand ();
		}
		
		public override void RenameItem (string newName)
		{
			TableNode node = (TableNode)CurrentNode.DataItem;
			if (node.Table.Name != newName)
				ThreadPool.QueueUserWorkItem (new WaitCallback (RenameItemThreaded), new object[]{ node, newName });
		}
		
		private void RenameItemThreaded (object state)
		{
			object[] objs = state as object[];
			
			TableNode node = objs[0] as TableNode;
			string newName = objs[1] as string;
			IEditSchemaProvider provider = (IEditSchemaProvider)node.Table.SchemaProvider;
			
			if (provider.IsValidName (newName)) {
				provider.RenameTable (node.Table, newName);
				node.Refresh ();
			} else {
				DispatchService.GuiDispatch (delegate () {
					MessageService.ShowError (String.Format (
						"Unable to rename table '{0}' to '{1}'!",
						node.Table.Name, newName
					));
				});
			}
			node.Refresh ();
		}
		
		[CommandHandler (ConnectionManagerCommands.Query)]
		protected void OnQueryCommand ()
		{
			TableNode node = (TableNode)CurrentNode.DataItem;
			
			IdentifierExpression tableId = new IdentifierExpression (node.Table.Name);
			SelectStatement sel = new SelectStatement (new FromTableClause (tableId));
			
			SqlQueryView view = new SqlQueryView ();
			view.SelectedConnectionContext = node.ConnectionContext;
			
			IDbFactory fac = DbFactoryService.GetDbFactory (node.ConnectionContext.ConnectionSettings);
			view.Text = fac.Dialect.GetSql (sel);

			IdeApp.Workbench.OpenDocument (view, true);
		}
		
		[CommandHandler (ConnectionManagerCommands.SelectColumns)]
		protected void OnSelectColumnsCommand ()
		{
			ThreadPool.QueueUserWorkItem (new WaitCallback (OnSelectColumnsCommandGetColumns), CurrentNode.DataItem);
		}
		
		private void OnSelectColumnsCommandGetColumns (object state)
		{
			TableNode node = (TableNode)state;
			
			ColumnSchemaCollection columns = node.Table.Columns; //this can invoke the schema provider, so it must be in bg thread
			
			DispatchService.GuiDispatch (delegate () {
				SelectColumnDialog dlg = new SelectColumnDialog (true, columns);
				if (dlg.Run () == (int)Gtk.ResponseType.Ok) {
					IdentifierExpression tableId = new IdentifierExpression (node.Table.Name);
					List<IdentifierExpression> cols = new List<IdentifierExpression> ();
					foreach (ColumnSchema schema in dlg.CheckedColumns)
						cols.Add (new IdentifierExpression (schema.Name));
					
					SelectStatement sel = new SelectStatement (new FromTableClause (tableId), cols);

					IPooledDbConnection conn = node.ConnectionContext.ConnectionPool.Request ();
					IDbCommand command = conn.CreateCommand (sel);
					
					conn.ExecuteTableAsync (command, new ExecuteCallback<DataTable> (OnSelectCommandThreaded), null);
				}
				dlg.Destroy ();
			});
		}
		
		[CommandHandler (ConnectionManagerCommands.SelectAll)]
		protected void OnSelectAllCommand ()
		{
			TableNode node = (TableNode)CurrentNode.DataItem;
			
			IdentifierExpression tableId = new IdentifierExpression (node.Table.Name);
			SelectStatement sel = new SelectStatement (new FromTableClause (tableId));

			IPooledDbConnection conn = node.ConnectionContext.ConnectionPool.Request ();
			IDbCommand command = conn.CreateCommand (sel);
			conn.ExecuteTableAsync (command, new ExecuteCallback<DataTable> (OnSelectCommandThreaded), null);
		}
		
		private void OnSelectCommandThreaded (IPooledDbConnection connection, DataTable table, object state)
		{
			connection.Release ();
				
			DispatchService.GuiDispatch (delegate () {
				QueryResultView view = new QueryResultView (table);
				IdeApp.Workbench.OpenDocument (view, true);
			});
		}
		
		[CommandHandler (ConnectionManagerCommands.EmptyTable)]
		protected void OnEmptyTable ()
		{
			TableNode node = (TableNode)CurrentNode.DataItem;

			AlertButton emptyButton = new AlertButton (AddinCatalog.GetString ("Empty Table"));
			if (MessageService.Confirm (
				AddinCatalog.GetString ("Are you sure you want to empty table '{0}'", node.Table.Name),
				emptyButton
			)) {
				IdentifierExpression tableId = new IdentifierExpression (node.Table.Name);
				DeleteStatement del = new DeleteStatement (new FromTableClause (tableId));
				
				IPooledDbConnection conn = node.ConnectionContext.ConnectionPool.Request ();
				IDbCommand command = conn.CreateCommand (del);
				conn.ExecuteNonQueryAsync (command, new ExecuteCallback<int> (OnEmptyTableCallback), null);
			}
		}
		
		private void OnEmptyTableCallback (IPooledDbConnection connection, int result, object state)
		{
			connection.Release ();

			DispatchService.GuiDispatch (delegate () {
				IdeApp.Workbench.StatusBar.ShowMessage (AddinCatalog.GetString ("Table emptied"));
			});
		}
		
		[CommandHandler (ConnectionManagerCommands.DropTable)]
		protected void OnDropTable ()
		{
			TableNode node = (TableNode)CurrentNode.DataItem;
			AlertButton dropButton = new AlertButton (AddinCatalog.GetString ("Drop"), Gtk.Stock.Delete);
			if (MessageService.Confirm (
				AddinCatalog.GetString ("Are you sure you want to drop table '{0}'", node.Table.Name),
				dropButton
			)) {
				ThreadPool.QueueUserWorkItem (new WaitCallback (OnDropTableThreaded), CurrentNode.DataItem);
			}
		}
		
		private void OnDropTableThreaded (object state)
		{
			TableNode node = (TableNode)state;
			IEditSchemaProvider provider = (IEditSchemaProvider)node.ConnectionContext.SchemaProvider;
			
			provider.DropTable (node.Table);
			OnRefreshParent ();
		}
		
		[CommandHandler (ConnectionManagerCommands.Refresh)]
		protected void OnRefresh ()
		{
			BaseNode node = CurrentNode.DataItem as BaseNode;
			node.Refresh ();
		}
		
		protected void OnRefreshParent ()
		{
			if (CurrentNode.MoveToParent ()) {
				BaseNode node = CurrentNode.DataItem as BaseNode;
				node.Refresh ();
			}
		}
		
		[CommandHandler (ConnectionManagerCommands.AlterTable)]
		protected void OnAlterTable ()
		{
			TableNode node = CurrentNode.DataItem as TableNode;
			IDbFactory fac = node.ConnectionContext.DbFactory;
			IEditSchemaProvider schemaProvider = (IEditSchemaProvider)node.ConnectionContext.SchemaProvider;
			
			if (fac.GuiProvider.ShowTableEditorDialog (schemaProvider, node.Table, false))
				ThreadPool.QueueUserWorkItem (new WaitCallback (OnAlterTableThreaded), CurrentNode.DataItem);
		}
		
		private void OnAlterTableThreaded (object state)
		{
//			TableNode node = (TableNode)state;
//			IEditSchemaProvider provider = (IEditSchemaProvider)node.ConnectionContext.SchemaProvider;
//			
//			provider.AlterTable (node.Table);
		}
		
		[CommandHandler (ConnectionManagerCommands.Rename)]
		protected void OnRenameTable ()
		{
			Tree.StartLabelEdit ();
		}
		
		[CommandUpdateHandler (ConnectionManagerCommands.DropTable)]
		protected void OnUpdateDropTable (CommandInfo info)
		{
			BaseNode node = (BaseNode)CurrentNode.DataItem;
			info.Enabled = node.ConnectionContext.SchemaProvider.IsSchemaActionSupported (SQL.SchemaType.Table, SchemaActions.Drop);
		}
		
		[CommandUpdateHandler (ConnectionManagerCommands.Rename)]
		protected void OnUpdateRenameTable (CommandInfo info)
		{
			BaseNode node = (BaseNode)CurrentNode.DataItem;
			info.Enabled = node.ConnectionContext.SchemaProvider.IsSchemaActionSupported (SQL.SchemaType.Table, SchemaActions.Rename);
		}
		
		[CommandUpdateHandler (ConnectionManagerCommands.AlterTable)]
		protected void OnUpdateAlterTable (CommandInfo info)
		{
			BaseNode node = (BaseNode)CurrentNode.DataItem;
			info.Enabled = node.ConnectionContext.SchemaProvider.IsSchemaActionSupported (SQL.SchemaType.Table, SchemaActions.Alter);
		}
	}
}
