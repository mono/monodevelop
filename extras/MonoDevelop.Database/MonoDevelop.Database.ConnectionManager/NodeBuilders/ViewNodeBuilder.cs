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
	public class ViewNodeBuilder : TypeNodeBuilder
	{
		private EventHandler RefreshHandler;

		public ViewNodeBuilder ()
			: base ()
		{
			RefreshHandler = new EventHandler (OnRefreshEvent);
		}
		
		public override Type NodeDataType {
			get { return typeof (ViewNode); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Database/ContextMenu/ConnectionManagerPad/ViewNode"; }
		}
		
		public override Type CommandHandlerType {
			get { return typeof (ViewNodeCommandHandler); }
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			ViewNode node = dataObject as ViewNode;
			return node.View.Name;
		}
		
		public override void BuildNode (ITreeBuilder builder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			ViewNode node = dataObject as ViewNode;
			node.RefreshEvent += (EventHandler)DispatchService.GuiDispatch (RefreshHandler);

			label = node.View.Name;
			icon = Context.GetIcon ("md-db-view");
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			ThreadPool.QueueUserWorkItem (new WaitCallback (BuildChildNodesThreaded), dataObject);
		}
		
		private void BuildChildNodesThreaded (object state)
		{
			ViewNode node = state as ViewNode;
			ITreeBuilder builder = Context.GetTreeBuilder (state);
			ISchemaProvider provider = node.ConnectionContext.SchemaProvider;

			if (provider.IsSchemaActionSupported (SchemaType.ViewColumn, SchemaActions.Schema))
				DispatchService.GuiDispatch (delegate {
					builder.AddChild (new ColumnsNode (node.ConnectionContext, node.View));
					builder.Expanded = true;
				});
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			ViewNode node = dataObject as ViewNode;
			return node.ConnectionContext.SchemaProvider.IsSchemaActionSupported (SchemaType.ViewColumn, SchemaActions.Schema);
		}
		
		private void OnRefreshEvent (object sender, EventArgs args)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (sender);
			builder.Update ();
		}
	}
	
	public class ViewNodeCommandHandler : NodeCommandHandler
	{
		public override DragOperation CanDragNode ()
		{
			return DragOperation.None;
		}
		
		public override void RenameItem (string newName)
		{
			ViewNode node = (ViewNode)CurrentNode.DataItem;
			if (node.View.Name != newName)
				ThreadPool.QueueUserWorkItem (new WaitCallback (RenameItemThreaded), new object[]{ node, newName });
		}
		
		private void RenameItemThreaded (object state)
		{
			object[] objs = state as object[];
			
			ViewNode node = objs[0] as ViewNode;
			string newName = objs[1] as string;
			IEditSchemaProvider provider = (IEditSchemaProvider)node.View.SchemaProvider;
			
			if (provider.IsValidName (newName)) {
				provider.RenameView (node.View, newName);
				node.Refresh ();
			} else {
				DispatchService.GuiDispatch (delegate () {
					MessageService.ShowError (String.Format (
						"Unable to rename view '{0}' to '{1}'!",
						node.View.Name, newName
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
		
		[CommandHandler (ConnectionManagerCommands.AlterView)]
		protected void OnAlterView ()
		{
			ViewNode node = CurrentNode.DataItem as ViewNode;
			IDbFactory fac = node.ConnectionContext.DbFactory;
			IEditSchemaProvider schemaProvider = (IEditSchemaProvider)node.ConnectionContext.SchemaProvider;
			
			if (fac.GuiProvider.ShowViewEditorDialog (schemaProvider, node.View, false))
				ThreadPool.QueueUserWorkItem (new WaitCallback (OnAlterViewThreaded), CurrentNode.DataItem);
		}
		
		private void OnAlterViewThreaded (object state)
		{
//			ViewNode node = (ViewNode)state;
//			IEditSchemaProvider provider = (IEditSchemaProvider)node.ConnectionContext.SchemaProvider;
//			
//			provider.AlterView (node.View);
		}
		
		[CommandHandler (ConnectionManagerCommands.DropView)]
		protected void OnDropView ()
		{
			ViewNode node = (ViewNode)CurrentNode.DataItem;
			AlertButton dropButton = new AlertButton (AddinCatalog.GetString ("Drop"), Gtk.Stock.Delete);
			if (MessageService.Confirm (
				AddinCatalog.GetString ("Are you sure you want to drop view '{0}'", node.View.Name),
				dropButton
			)) {
				ThreadPool.QueueUserWorkItem (new WaitCallback (OnDropViewThreaded), CurrentNode.DataItem);
			}
		}
		
		private void OnDropViewThreaded (object state)
		{
			ViewNode node = (ViewNode)state;
			IEditSchemaProvider provider = (IEditSchemaProvider)node.ConnectionContext.SchemaProvider;
	
			provider.DropView (node.View);
			OnRefreshParent ();
		}
		
		[CommandHandler (ConnectionManagerCommands.Rename)]
		protected void OnRenameView ()
		{
			Tree.StartLabelEdit ();
		}
		
		[CommandUpdateHandler (ConnectionManagerCommands.DropView)]
		protected void OnUpdateDropView (CommandInfo info)
		{
			BaseNode node = (BaseNode)CurrentNode.DataItem;
			info.Enabled = node.ConnectionContext.SchemaProvider.IsSchemaActionSupported (SchemaType.View, SchemaActions.Drop);
		}
		
		[CommandUpdateHandler (ConnectionManagerCommands.Rename)]
		protected void OnUpdateRenameView (CommandInfo info)
		{
			BaseNode node = (BaseNode)CurrentNode.DataItem;
			info.Enabled = node.ConnectionContext.SchemaProvider.IsSchemaActionSupported (SchemaType.View, SchemaActions.Rename);
		}
		
		[CommandUpdateHandler (ConnectionManagerCommands.AlterView)]
		protected void OnUpdateAlterView (CommandInfo info)
		{
			BaseNode node = (BaseNode)CurrentNode.DataItem;
			info.Enabled = node.ConnectionContext.SchemaProvider.IsSchemaActionSupported (SchemaType.View, SchemaActions.Alter);
		}
	}
}
