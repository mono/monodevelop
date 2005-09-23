//
// TablesNodeBuilder.cs
//
// Authors:
//   Christian Hergert <chris@mosaix.net>
//
// Copyright (c) 2005 Christian Hergert
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

using Mono.Data.Sql;
using MonoDevelop.Services;
using MonoDevelop.Gui.Pads;
using MonoDevelop.Commands;

using MonoQuery.Commands;

namespace MonoQuery
{
	public class TablesNodeBuilder : TypeNodeBuilder
	{
		private delegate void AddTableHandler (ITreeBuilder builder, TableSchema schema);
		private event AddTableHandler AddTable;
		
		private object ThreadSync = new Object ();
		private ITreeBuilder threadedBuilder;
		private TablesNode threadedNode;
		
		private EventHandler RefreshHandler;
		
		public TablesNodeBuilder()
		{
			AddTable += (AddTableHandler) Runtime.DispatchService.GuiDispatch (new AddTableHandler (OnTableAdd));
			RefreshHandler = (EventHandler) Runtime.DispatchService.GuiDispatch (new EventHandler (OnRefreshEvent));
		}
		
		public override Type NodeDataType {
			get {
				return typeof(TablesNode);
			}
		}
		
		public override Type CommandHandlerType {
			get {
				return typeof (TablesNodeCommandHandler);
			}
		}
		
		public override string ContextMenuAddinPath {
			get {
				return "/SharpDevelop/Views/DatabasePad/ContextMenu/TablesBrowserNode";
			}
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return GettextCatalog.GetString ("Tables");
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = GettextCatalog.GetString ("Tables");
			string iconName = "md-mono-query-tables";
			icon = Context.GetIcon (iconName);
			
			TablesNode node = (TablesNode) dataObject;
			node.RefreshEvent += RefreshHandler;
		}
		
		void OnRefreshEvent (object sender, EventArgs args)
		{
			ITreeBuilder builder = Context.GetTreeBuilder ();
			
			if (builder != null)
				builder.UpdateChildren ();
			
			builder.ExpandToNode ();
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			TablesNode node = (TablesNode) dataObject;
			BuildChildNodes (builder, node);
		}
		
		public void BuildChildNodes (ITreeBuilder builder, TablesNode node)
		{
			if (node.Provider.IsOpen == false && node.Provider.Open () == false)
				return;
			
			lock (ThreadSync) {
				threadedBuilder = builder;
				threadedNode = node;
				Thread thread = new Thread (new ThreadStart (BuildChildNodesThreadStart));
				thread.Start ();
			}
		}
		
		private void BuildChildNodesThreadStart ()
		{
			ITreeBuilder builder = threadedBuilder;
			foreach (TableSchema schema in threadedNode.Provider.GetTables ()) {
				AddTable (builder, schema);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		private void OnTableAdd (ITreeBuilder builder, TableSchema schema)
		{
			if (((bool)builder.Options["ShowSystemObjects"]) == true || schema.IsSystemTable == false)
				builder.AddChild (schema);
			builder.Expanded = true;
		}
	}
	
	public class TablesNodeCommandHandler : NodeCommandHandler
	{
		public override DragOperation CanDragNode ()
		{
			return DragOperation.None;
		}
		
		[CommandHandler (MonoQueryCommands.QueryCommand)]
		protected void OnQueryCommand ()
		{
			SqlQueryView sql = new SqlQueryView ();
			TablesNode node = (TablesNode) CurrentNode.DataItem;
			sql.Connection = node.Provider;
			Runtime.Gui.Workbench.ShowView (sql, true);
		}
		
		[CommandHandler (MonoQueryCommands.Refresh)]
		protected void OnRefresh ()
		{
			TablesNode node = (TablesNode) CurrentNode.DataItem;
			node.Refresh ();
		}
	}
}