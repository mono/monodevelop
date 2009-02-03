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

using System;
using System.Threading;
using System.Collections.Generic;
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Designer;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Database.ConnectionManager
{
	public class UserNodeBuilder : TypeNodeBuilder
	{
		private EventHandler RefreshHandler;
		
		public UserNodeBuilder ()
			: base ()
		{
			RefreshHandler = new EventHandler (OnRefreshEvent);
		}
		
		public override Type NodeDataType {
			get { return typeof (UserNode); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Database/ContextMenu/ConnectionManagerPad/UserNode"; }
		}
		
		public override Type CommandHandlerType {
			get { return typeof (UserNodeCommandHandler); }
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			UserNode node = dataObject as UserNode;
			return node.User.Name;
		}
		
		public override void BuildNode (ITreeBuilder builder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			UserNode node = dataObject as UserNode;
			node.RefreshEvent += (EventHandler)DispatchService.GuiDispatch (RefreshHandler);

			label = node.User.Name;
			icon = Context.GetIcon ("md-db-user");
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return false;
		}
		
		private void OnRefreshEvent (object sender, EventArgs args)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (sender);
			builder.Update ();
		}
	}
	
	public class UserNodeCommandHandler : NodeCommandHandler
	{
		public override DragOperation CanDragNode ()
		{
			return DragOperation.None;
		}
		
		public override void RenameItem (string newName)
		{
			UserNode node = (UserNode)CurrentNode.DataItem;
			if (node.User.Name != newName)
				ThreadPool.QueueUserWorkItem (new WaitCallback (RenameItemThreaded), new object[]{ node, newName });
		}
		
		private void RenameItemThreaded (object state)
		{
			object[] objs = state as object[];
			
			UserNode node = objs[0] as UserNode;
			string newName = objs[1] as string;
			IEditSchemaProvider provider = (IEditSchemaProvider)node.User.SchemaProvider;
			
			if (provider.IsValidName (newName)) {
				provider.RenameUser (node.User, newName);
				node.Refresh ();
			} else {
				DispatchService.GuiDispatch (delegate () {
					MessageService.ShowError (String.Format (
						"Unable to rename user '{0}' to '{1}'!",
						node.User.Name, newName
					));
				});
			}
			node.Refresh ();
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
		
		[CommandHandler (ConnectionManagerCommands.AlterUser)]
		protected void OnAlterUser ()
		{
			UserNode node = CurrentNode.DataItem as UserNode;
			IDbFactory fac = node.ConnectionContext.DbFactory;
			IEditSchemaProvider schemaProvider = (IEditSchemaProvider)node.ConnectionContext.SchemaProvider;
			
			if (fac.GuiProvider.ShowUserEditorDialog (schemaProvider, node.User, false))
				ThreadPool.QueueUserWorkItem (new WaitCallback (OnAlterUserThreaded), CurrentNode.DataItem);
		}
		
		private void OnAlterUserThreaded (object state)
		{
//			UserNode node = (UserNode)state;
//			IEditSchemaProvider provider = (IEditSchemaProvider)node.ConnectionContext.SchemaProvider;
//			
//			provider.AlterUser (node.User);
		}
		
		[CommandHandler (ConnectionManagerCommands.DropUser)]
		protected void OnDropUser ()
		{
			UserNode node = (UserNode)CurrentNode.DataItem;
			AlertButton dropButton = new AlertButton (AddinCatalog.GetString ("Drop"), Gtk.Stock.Delete);
			if (MessageService.Confirm (
				AddinCatalog.GetString ("Are you sure you want to drop user '{0}'", node.User.Name),
				dropButton
			)) {
				ThreadPool.QueueUserWorkItem (new WaitCallback (OnDropUserThreaded), CurrentNode.DataItem);
			}
		}
			
		private void OnDropUserThreaded (object state)
		{
			UserNode node = (UserNode)state;
			IEditSchemaProvider provider = (IEditSchemaProvider)node.ConnectionContext.SchemaProvider;
			
			provider.DropUser (node.User);
			OnRefreshParent ();
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.Rename)]
		protected void OnRenameUser ()
		{
			Tree.StartLabelEdit ();
		}
		
		[CommandUpdateHandler (ConnectionManagerCommands.DropUser)]
		protected void OnUpdateDropUser (CommandInfo info)
		{
			BaseNode node = (BaseNode)CurrentNode.DataItem;
			info.Enabled = node.ConnectionContext.SchemaProvider.IsSchemaActionSupported (SchemaType.User, SchemaActions.Drop);
		}
		
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.EditCommands.Rename)]
		protected void OnUpdateRenameUser (CommandInfo info)
		{
			BaseNode node = (BaseNode)CurrentNode.DataItem;
			info.Enabled = node.ConnectionContext.SchemaProvider.IsSchemaActionSupported (SchemaType.User, SchemaActions.Rename);
		}
		
		[CommandUpdateHandler (ConnectionManagerCommands.AlterUser)]
		protected void OnUpdateAlterUser (CommandInfo info)
		{
			BaseNode node = (BaseNode)CurrentNode.DataItem;
			info.Enabled = node.ConnectionContext.SchemaProvider.IsSchemaActionSupported (SchemaType.User, SchemaActions.Alter);
		}
	}
}
