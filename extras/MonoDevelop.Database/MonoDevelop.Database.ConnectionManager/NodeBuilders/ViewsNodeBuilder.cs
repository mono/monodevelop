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
using System.Collections.Generic;
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Designer;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Database.ConnectionManager
{
	public class ViewsNodeBuilder : TypeNodeBuilder
	{
		private EventHandler RefreshHandler;
		
		public ViewsNodeBuilder ()
			: base ()
		{
			RefreshHandler = new EventHandler (OnRefreshEvent);
		}
		
		public override Type NodeDataType {
			get { return typeof (ViewsNode); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Database/ContextMenu/ConnectionManagerPad/ViewsNode"; }
		}
		
		public override Type CommandHandlerType {
			get { return typeof (ViewsNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return GettextCatalog.GetString ("Views");
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = GettextCatalog.GetString ("Views");
			icon = Context.GetIcon ("md-db-views");
			
			BaseNode node = (BaseNode) dataObject;
			node.RefreshEvent += (EventHandler)(DispatchService.GuiDispatch (RefreshHandler));
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			ThreadPool.QueueUserWorkItem (new WaitCallback (BuildChildNodesThreaded), dataObject);
		}
		
		private void BuildChildNodesThreaded (object state)
		{
			BaseNode node = state as BaseNode;
			ITreeBuilder builder = Context.GetTreeBuilder (state);

			bool showSystemObjects = (bool)builder.Options["ShowSystemObjects"];
			ViewSchemaCollection views = node.ConnectionContext.SchemaProvider.GetViews ();
			DispatchService.GuiDispatch (delegate {
				foreach (ViewSchema view in views) {
					if (view.IsSystemView && !showSystemObjects)
						continue;

					builder.AddChild (new ViewNode (node.ConnectionContext, view));
				}
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
			
			builder.UpdateChildren ();			
			builder.ExpandToNode ();
		}
	}
	
	public class ViewsNodeCommandHandler : NodeCommandHandler
	{
		public override DragOperation CanDragNode ()
		{
			return DragOperation.None;
		}
		
		[CommandHandler (ConnectionManagerCommands.Refresh)]
		protected void OnRefresh ()
		{
			BaseNode node = CurrentNode.DataItem as BaseNode;
			node.Refresh ();
		}
		
		[CommandHandler (ConnectionManagerCommands.CreateView)]
		protected void OnCreateView ()
		{
			BaseNode node = CurrentNode.DataItem as BaseNode;
			IDbFactory fac = node.ConnectionContext.DbFactory;
			ISchemaProvider schemaProvider = node.ConnectionContext.SchemaProvider;
			ViewSchema view = schemaProvider.GetNewViewSchema ("NewView");

			if (fac.GuiProvider.ShowViewEditorDialog (schemaProvider, view, true))
				ThreadPool.QueueUserWorkItem (new WaitCallback (OnCreateViewThreaded), new object[] {schemaProvider, view, node} as object);
		}
		
		private void OnCreateViewThreaded (object state)
		{
			object[] objs = state as object[];
			
			ISchemaProvider provider = objs[0] as ISchemaProvider;
			ViewSchema view = objs[1] as ViewSchema;
			BaseNode node = objs[2] as BaseNode;
			
			provider.CreateView (view);
			node.Refresh ();
		}
		
		[CommandUpdateHandler (ConnectionManagerCommands.CreateView)]
		protected void OnUpdateCreateView (CommandInfo info)
		{
			BaseNode node = (BaseNode)CurrentNode.DataItem;
			info.Enabled = node.ConnectionContext.DbFactory.IsActionSupported ("View", SchemaActions.Create);
		}
	}
}
