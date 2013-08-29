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
using System.Threading;
using MonoDevelop.Database.Sql;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Database.Query;
using MonoDevelop.Database.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Database.ConnectionManager
{
	public class ConnectionContextNodeBuilder : TypeNodeBuilder
	{
		public ConnectionContextNodeBuilder ()
			: base ()
		{
			
		}
		
		public override Type NodeDataType {
			get { return typeof (DatabaseConnectionContext); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Database/ContextMenu/ConnectionManagerPad/ConnectionNode"; }
		}
		
		public override Type CommandHandlerType {
			get { return typeof (ConnectionContextCommandHandler); }
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			DatabaseConnectionContext context = dataObject as DatabaseConnectionContext;
			return context.ConnectionSettings.Name;
		}
		
		public override void BuildNode (ITreeBuilder builder, object dataObject, NodeInfo nodeInfo)
		{
			DatabaseConnectionContext context = dataObject as DatabaseConnectionContext;

			if (((bool)builder.Options["ShowDatabaseName"]))
				nodeInfo.Label = string.Format ("{0} - ({1})", 
				                       context.ConnectionSettings.Name, context.ConnectionSettings.Database);
			 else
				nodeInfo.Label = context.ConnectionSettings.Name;
			
			if (context.HasConnectionPool) {
				IConnectionPool pool = context.ConnectionPool;
				if (pool.IsInitialized) {
					nodeInfo.Icon = Context.GetIcon ("md-db-database-ok");
				} else if (pool.HasErrors) {
					nodeInfo.Icon = Context.GetIcon ("md-db-database-error");
					MessageService.ShowError (AddinCatalog.GetString ("Unable to connect:") + Environment.NewLine + pool.Error);
				} else {
					nodeInfo.Icon = Context.GetIcon ("md-db-database");
				}
			} else {
				nodeInfo.Icon = Context.GetIcon ("md-db-database");
			}
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			ThreadPool.QueueUserWorkItem (new WaitCallback (BuildChildNodesThreaded), dataObject);
		}
		
		private void BuildChildNodesThreaded (object state)
		{
			QueryService.EnsureConnection (state as DatabaseConnectionContext, new DatabaseConnectionContextCallback (BuildChildNodesGui), state);
		}
		
		private void BuildChildNodesGui (DatabaseConnectionContext context, bool connected, object state)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (state);
			
			builder.Update ();
			if (connected) {
				ISchemaProvider provider = context.SchemaProvider;

				if (provider.IsSchemaActionSupported (SchemaType.Table, SchemaActions.Schema))
					builder.AddChild (new TablesNode (context));

				if (provider.IsSchemaActionSupported (SchemaType.View, SchemaActions.Schema))
					builder.AddChild (new ViewsNode (context));
				
				if (provider.IsSchemaActionSupported (SchemaType.Procedure, SchemaActions.Schema))
					builder.AddChild (new ProceduresNode (context));

				if (provider.IsSchemaActionSupported (SchemaType.User, SchemaActions.Schema))
					builder.AddChild (new UsersNode (context));
				
				//TODO: custom datatypes, sequences, roles, operators, languages, groups and aggregates
				
				builder.Expanded = true;
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
	}
	
	public class ConnectionContextCommandHandler : NodeCommandHandler
	{
		public override void RenameItem (string newName)
		{
			DatabaseConnectionContext context = (DatabaseConnectionContext) CurrentNode.DataItem;
			if (context.ConnectionSettings.Name != newName) {
				if (ConnectionContextService.DatabaseConnections.Contains (newName)) {
					MessageService.ShowError (String.Format (
						"Unable to rename connection '{0}' to '{1}'! (duplicate name)",
						context.ConnectionSettings.Name, newName
					));
				} else {
					context.ConnectionSettings.Name = newName;
					context.Refresh ();
				}
			}
		}
		
		[CommandHandler (ConnectionManagerCommands.RemoveConnection)]
		protected void OnRemoveConnection ()
		{
			DatabaseConnectionContext context = (DatabaseConnectionContext) CurrentNode.DataItem;
			if (MessageService.Confirm (
				AddinCatalog.GetString ("Are you sure you want to remove connection '{0}'?", context.ConnectionSettings.Name),
				AlertButton.Remove)) {
				ConnectionContextService.RemoveDatabaseConnectionContext (context);
			}
		}
		
		[CommandHandler (ConnectionManagerCommands.EditConnection)]
		protected void OnEditConnection ()
		{
			DatabaseConnectionContext context = (DatabaseConnectionContext) CurrentNode.DataItem;
			DatabaseConnectionSettings newSettings;
			if (context.DbFactory.GuiProvider.ShowEditConnectionDialog (context.DbFactory,
			                                                            context.ConnectionSettings,
			                                                            out newSettings)) {
				DatabaseConnectionContext newContext = new DatabaseConnectionContext (newSettings);
				ConnectionContextService.RemoveDatabaseConnectionContext (context);
				ConnectionContextService.AddDatabaseConnectionContext (newContext);
				newContext.Refresh ();
			}
			
		}
		
		[CommandHandler (ConnectionManagerCommands.DisconnectConnection)]
		protected void OnDisconnectConnection ()
		{
			DatabaseConnectionContext context = (DatabaseConnectionContext) CurrentNode.DataItem;
			if (context.HasConnectionPool) {
				context.ConnectionPool.Close ();
				context.Refresh ();
				CurrentNode.Expanded = false;
			}
		}
		
		[CommandUpdateHandler (ConnectionManagerCommands.DisconnectConnection)]
		protected void OnUpdateDisconnectConnection (CommandInfo info)
		{
			DatabaseConnectionContext context = (DatabaseConnectionContext) CurrentNode.DataItem;
			info.Enabled = context.HasConnectionPool && context.ConnectionPool.IsInitialized;
		}
		
		[CommandHandler (ConnectionManagerCommands.ConnectConnection)]
		protected void OnConnectConnection ()
		{
			CurrentNode.Expanded = true;			
		}
		
		[CommandUpdateHandler (ConnectionManagerCommands.ConnectConnection)]
		protected void OnUpdateConnectConnection (CommandInfo info)
		{
			DatabaseConnectionContext context = (DatabaseConnectionContext) CurrentNode.DataItem;
			if (context.HasConnectionPool)
				info.Enabled = !context.ConnectionPool.IsInitialized;
			else
				info.Enabled = true;
		}
		
		public override void ActivateItem ()
		{
			OnQueryCommand ();
		}
		
		[CommandHandler (ConnectionManagerCommands.Query)]
		protected void OnQueryCommand ()
		{
			SqlQueryView view = new SqlQueryView ();
			view.SelectedConnectionContext = (DatabaseConnectionContext) CurrentNode.DataItem;

			IdeApp.Workbench.OpenDocument (view, true);
		}
			
		[CommandHandler (ConnectionManagerCommands.DropDatabase)]
		protected void OnDropDatabase ()
		{
			DatabaseConnectionContext context = (DatabaseConnectionContext) CurrentNode.DataItem;
			AlertButton dropButton = new AlertButton (AddinCatalog.GetString ("Drop"), Gtk.Stock.Delete);
			if (MessageService.Confirm (
				AddinCatalog.GetString ("Are you sure you want to drop database '{0}'", context.ConnectionSettings.Database),
				dropButton
			)) {
				ThreadPool.QueueUserWorkItem (new WaitCallback (OnDropDatabaseThreaded), CurrentNode.DataItem);
			}
		}
		
		private void OnDropDatabaseThreaded (object state)
		{
			DatabaseConnectionContext context = (DatabaseConnectionContext) CurrentNode.DataItem;
			try {
				context.ConnectionPool.Initialize ();
				ISchemaProvider provider = context.SchemaProvider;
				DatabaseSchema db = provider.CreateDatabaseSchema (context.ConnectionSettings.Database);
				IEditSchemaProvider schemaProvider = (IEditSchemaProvider)context.SchemaProvider;
				schemaProvider.DropDatabase (db);
				ConnectionContextService.RemoveDatabaseConnectionContext (context);
			} catch (Exception ex) {
				DispatchService.GuiDispatch (delegate {
					MessageService.ShowException (ex);
				});
			}
		}
		
		[CommandUpdateHandler (ConnectionManagerCommands.DropDatabase)]
		protected void OnUpdateDropDatabase (CommandInfo info)
		{
			DatabaseConnectionContext context = (DatabaseConnectionContext) CurrentNode.DataItem;
			//info.Enabled = context.DbFactory.IsActionSupported ("Database", SchemaActions.Drop);
		}

		[CommandUpdateHandler (ConnectionManagerCommands.AlterDatabase)]
		protected void OnUpdateAlterDatabase (CommandInfo info)
		{
			DatabaseConnectionContext context = (DatabaseConnectionContext) CurrentNode.DataItem;
			//info.Enabled = context.DbFactory.IsActionSupported ("Database", SchemaActions.Alter);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.Rename)]
		protected void OnRenameDatabase ()
		{
			//TODO: show a dialog, since inline tree renaming for this node renames the custom name
		}
		
		[CommandUpdateHandler (ConnectionManagerCommands.RenameDatabase)]
		protected void OnUpdateRenameDatabase (CommandInfo info)
		{
			DatabaseConnectionContext context = (DatabaseConnectionContext) CurrentNode.DataItem;
			//info.Enabled = context.DbFactory.IsActionSupported ("Database", SchemaActions.Rename);
		}
	
	}
}
