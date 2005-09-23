//
// TableNodeBuilder.cs
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
using System.Data;

using Mono.Data.Sql;

using MonoDevelop.Core.Services;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Pads;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Services;
using MonoDevelop.Commands;

using MonoQuery.Commands;

namespace MonoQuery
{
	public class TableNodeBuilder : TypeNodeBuilder
	{
		public TableNodeBuilder ()
		{
		}
		
		public override Type NodeDataType {
			get {
				return typeof (TableSchema);
			}
		}
		
		public override Type CommandHandlerType {
			get {
				return typeof (TableNodeCommandHandler);
			}
		}
		
		public override string ContextMenuAddinPath {
			get {
				return "/SharpDevelop/Views/DatabasePad/ContextMenu/TableBrowserNode";
			}
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return GettextCatalog.GetString ("Table");
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = (dataObject as TableSchema).Name;
			string iconName = "md-mono-query-table";
			icon = Context.GetIcon (iconName);
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			TableSchema node = (TableSchema) dataObject;
			BuildChildNodes (builder, node);
		}
		
		public static void BuildChildNodes (ITreeBuilder builder, TableSchema node)
		{
			if (node.Provider.SupportsSchemaType (typeof (ColumnSchema)))
				builder.AddChild (new ColumnsNode (node.Provider, node));

			if (node.Provider.SupportsSchemaType (typeof (RuleSchema)))
				builder.AddChild (new RulesNode (node.Provider));

			if (node.Provider.SupportsSchemaType (typeof (ConstraintSchema)))
				builder.AddChild (new ConstraintsNode (node.Provider, node));

			if (node.Provider.SupportsSchemaType (typeof (TriggerSchema)))
				builder.AddChild (new TriggersNode (node.Provider));
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
	}
	
	public class TableNodeCommandHandler : NodeCommandHandler
	{
		public override DragOperation CanDragNode ()
		{
			return DragOperation.None;
		}
		
		public override void OnItemSelected ()
		{
			TableSchema table = CurrentNode.DataItem as TableSchema;
			MonoQueryService service = (MonoQueryService) ServiceManager.GetService (typeof (MonoQueryService));
			
			if (service.SqlDefinitionPad != null)
				service.SqlDefinitionPad.SetText (table.Definition);
		}
		
		public override void ActivateItem ()
		{
			TableSchema table = CurrentNode.DataItem as TableSchema;
			string query = String.Format ("SELECT * FROM {0}", table.Name);
			table.Provider.ExecuteSQL (query, (SQLCallback) Runtime.DispatchService.GuiDispatch (new SQLCallback (ActivateSQLCallback)));
		}
		
		protected void ActivateSQLCallback (object sender, object res)
		{
			DataTable results = (res as DataTable);
			
			DataGridView dataView = new DataGridView (results);
			Runtime.Gui.Workbench.ShowView (dataView, true);
		}
		
		[CommandHandler (MonoQueryCommands.QueryCommand)]
		protected void OnQueryCommand ()
		{
			SqlQueryView sql = new SqlQueryView ();
			TableSchema table = (TableSchema) CurrentNode.DataItem;
			sql.Connection = table.Provider;
			sql.Text = String.Format ("SELECT * FROM {0}", table.Name);
			Runtime.Gui.Workbench.ShowView (sql, true);
		}
		
		[CommandHandler (MonoQueryCommands.EmptyTable)]
		protected void OnEmptyTable ()
		{
			TableSchema table = (TableSchema) CurrentNode.DataItem;
			table.Provider.ExecuteSQL (String.Format (
				"DELETE FROM {0}", table.Name));
			Runtime.Gui.StatusBar.SetMessage (
				GettextCatalog.GetString ("Table emptied"));
		}
		
		[CommandHandler (MonoQueryCommands.DropTable)]
		protected void OnDropTable ()
		{
			TableSchema table = (TableSchema) CurrentNode.DataItem;
			table.Provider.ExecuteSQL (String.Format (
				"DROP TABLE {0}", table.Name));
			Runtime.Gui.StatusBar.SetMessage (
				GettextCatalog.GetString ("Table dropped"));
			
			OnRefresh ();
		}
		
		[CommandHandler (MonoQueryCommands.Refresh)]
		protected void OnRefresh ()
		{
			CurrentNode.MoveToParent ();
			if (CurrentNode.DataItem as TablesNode != null)
				(CurrentNode.DataItem as TablesNode).Refresh ();
			
			CurrentNode.ExpandToNode ();
		}
	}
}
