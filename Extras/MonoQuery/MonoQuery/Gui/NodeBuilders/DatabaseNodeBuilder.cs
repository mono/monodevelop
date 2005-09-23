//
// DatabaseNodeBuilder.cs
//
// Authors:
// Christian Hergert <chris@mosaix.net>
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
using System.Runtime.Remoting.Messaging;

using Mono.Data.Sql;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui.Pads;
using MonoDevelop.Commands;
using MonoQuery.Commands;

namespace MonoQuery
{
	public class DatabaseNodeBuilder : TypeNodeBuilder
	{
		DbProviderChangedEventHandler providerNameChanged;
		DbProviderChangedEventHandler connectionStateChanged;
		DbProviderChangedEventHandler providerRefreshed;
		
		object ThreadSync = new Object ();
		ITreeBuilder threadedBuilder;
		DbProviderBase threadedProvider;
		
		private delegate void ExpandToNodeHandler (ITreeBuilder builder);
		private event ExpandToNodeHandler ExpandToNode;
		private delegate void AddChildHandler (ITreeBuilder builder, object child);
		private event AddChildHandler AddChildEvent;
		
		public DatabaseNodeBuilder ()
		{
			providerNameChanged = (DbProviderChangedEventHandler) Runtime.DispatchService.GuiDispatch (new DbProviderChangedEventHandler (OnProviderNameChanged));
			connectionStateChanged = (DbProviderChangedEventHandler) Runtime.DispatchService.GuiDispatch (new DbProviderChangedEventHandler (OnProviderStateChanged));
			providerRefreshed = (DbProviderChangedEventHandler) Runtime.DispatchService.GuiDispatch (new DbProviderChangedEventHandler (OnProviderRefreshed));
			
			ExpandToNode += (ExpandToNodeHandler) Runtime.DispatchService.GuiDispatch (new ExpandToNodeHandler (OnExpandToNode));
			AddChildEvent += (AddChildHandler) Runtime.DispatchService.GuiDispatch (new AddChildHandler (OnAddChild));
		}
		
		public override Type NodeDataType {
			get {
				return typeof (DbProviderBase);
			}
		}
		
		public override string ContextMenuAddinPath {
			get {
				return "/SharpDevelop/Views/DatabasePad/ContextMenu/ConnectionBrowserNode";
			}
		}
		
		public override Type CommandHandlerType {
			get {
				return typeof (DatabaseNodeCommandHandler);
			}
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			DbProviderBase provider = (DbProviderBase) dataObject;
			provider.NameChanged += providerNameChanged;
			provider.StateChanged += connectionStateChanged;
			provider.Refreshed += providerRefreshed;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			DbProviderBase provider = (DbProviderBase) dataObject;
			provider.NameChanged -= providerNameChanged;
			provider.StateChanged -= connectionStateChanged;
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ( (DbProviderBase)dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			DbProviderBase p = dataObject as DbProviderBase;
			label = p.Name;
			
			string iconName = "md-mono-query-database";
			
			if (p.IsConnectionStringWrong && p.IsOpen == false)
				iconName = "md-mono-query-disconnected";
			else if (p.IsOpen)
				iconName = "md-mono-query-connected";
			
			icon = Context.GetIcon (iconName);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			DbProviderBase provider = (DbProviderBase) dataObject;
			BuildChildNodes (builder, provider);
		}
		
		public void BuildChildNodes (ITreeBuilder builder, DbProviderBase provider)
		{
			lock (ThreadSync) {
				threadedBuilder = builder;
				threadedProvider = provider;
				Thread thread = new Thread (new ThreadStart (BuildChildNodesThreadStart));
				thread.Start ();
			}
		}
		
		private void BuildChildNodesThreadStart ()
		{
			ITreeBuilder builder = threadedBuilder;
			DbProviderBase provider = threadedProvider;
			
			if (provider.IsOpen == false && provider.Open () == false)
				return;
			
			if (provider.SupportsSchemaType (typeof (TableSchema)))
				AddChildEvent (builder, new TablesNode (provider));
			
			if (provider.SupportsSchemaType (typeof (ViewSchema)))
				AddChildEvent (builder, new ViewsNode (provider));
			
			if (provider.SupportsSchemaType (typeof (ProcedureSchema)))
				AddChildEvent (builder, new ProceduresNode (provider));
			
			if (provider.SupportsSchemaType (typeof (AggregateSchema)))
				AddChildEvent (builder, new AggregatesNode (provider));
			
			if (provider.SupportsSchemaType (typeof (GroupSchema)))
				AddChildEvent (builder, new GroupsNode (provider));
			
			if (provider.SupportsSchemaType (typeof (LanguageSchema)))
				AddChildEvent (builder, new LanguagesNode (provider));
			
			if (provider.SupportsSchemaType (typeof (OperatorSchema)))
				AddChildEvent (builder, new OperatorsNode (provider));
			
			if (provider.SupportsSchemaType (typeof (RoleSchema)))
				AddChildEvent (builder, new RolesNode (provider));
			
			if (provider.SupportsSchemaType (typeof (SequenceSchema)))
				AddChildEvent (builder, new SequencesNode (provider));
			
			if (provider.SupportsSchemaType (typeof (UserSchema)))
				AddChildEvent (builder, new UsersNode (provider));
			
			if (provider.SupportsSchemaType (typeof (DataTypeSchema)))
				AddChildEvent (builder, new TypesNode (provider));
			
			ExpandToNode (builder);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		protected void OnProviderNameChanged (object sender, DbProviderChangedArgs args)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (args.Provider);
			if (tb != null)
				tb.Update ();
		}
		
		protected void OnProviderStateChanged (object sender, DbProviderChangedArgs args)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (args.Provider);
			if (tb != null)
				tb.Update ();
		}
		
		protected void OnProviderRefreshed (object sender, DbProviderChangedArgs args)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (args.Provider);
			if (tb != null)
				tb.UpdateAll ();
		}
		
		protected void OnExpandToNode (ITreeBuilder builder)
		{
			builder.Expanded = true;
		}
		
		protected void OnAddChild (ITreeBuilder builder, object child)
		{
			builder.AddChild (child);
		}
	}
	
	public class DatabaseNodeCommandHandler: NodeCommandHandler
	{
		[CommandHandler (MonoQueryCommands.RemoveConnection)]
		protected void OnRemoveConnection ()
		{
			MonoQueryService service = (MonoQueryService) ServiceManager.GetService (typeof (MonoQueryService));
			DbProviderBase provider = (DbProviderBase) CurrentNode.DataItem;
			service.Providers.Remove (provider);
		}
		
		[CommandHandler (MonoQueryCommands.RefreshConnection)]
		protected void OnRefreshConnection ()
		{
			DbProviderBase provider = (DbProviderBase) CurrentNode.DataItem;
			provider.Refresh ();
		}
		
		[CommandHandler (MonoQueryCommands.DisconnectConnection)]
		protected void OnDisconnectConnection ()
		{
			DbProviderBase provider = (DbProviderBase) CurrentNode.DataItem;
			provider.Close ();
		}
		
		public override void ActivateItem ()
		{
			OnQueryCommand ();
		}
		
		[CommandHandler (MonoQueryCommands.QueryCommand)]
		protected void OnQueryCommand ()
		{
			SqlQueryView sql = new SqlQueryView ();
			sql.Connection = (DbProviderBase) CurrentNode.DataItem;
			Runtime.Gui.Workbench.ShowView (sql, true);
			
			CurrentNode.MoveToParent ();
		}
	}
}
