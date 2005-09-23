//
// ProceduresNodeBuilder.cs
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

namespace MonoQuery
{
	public class ProceduresNodeBuilder : TypeNodeBuilder
	{
		private delegate void AddProcedureHandler (ITreeBuilder builder, ProcedureSchema schema);
		private event AddProcedureHandler AddProcedure;
		
		private object ThreadSync = new Object ();
		private ITreeBuilder threadedBuilder;
		private ProceduresNode threadedNode;
		
		public ProceduresNodeBuilder()
		{
			AddProcedure += (AddProcedureHandler) Runtime.DispatchService.GuiDispatch (new AddProcedureHandler (OnProcedureAdd));
		}
		
		public override Type NodeDataType {
			get {
				return typeof(ProceduresNode);
			}
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return GettextCatalog.GetString ("Procedures");
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = GettextCatalog.GetString ("Procedures");
			string iconName = "md-mono-query-tables";
			icon = Context.GetIcon (iconName);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			ProceduresNode node = (ProceduresNode) dataObject;
			BuildChildNodes (builder, node);
		}
		
		public void BuildChildNodes (ITreeBuilder builder, ProceduresNode node)
		{
			lock (ThreadSync) {
				threadedBuilder = builder;
				threadedNode = node;
				Thread thread = new Thread (new ThreadStart (BuildChildNodesThreadStart));
				thread.Start ();
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		private void OnProcedureAdd (ITreeBuilder builder, ProcedureSchema schema)
		{
			if (((bool)builder.Options["ShowSystemObjects"]) == true || schema.IsSystemProcedure == false)
				builder.AddChild (schema);
			builder.Expanded = true;
		}
		
		private void BuildChildNodesThreadStart ()
		{
			ITreeBuilder builder = threadedBuilder;
			foreach (ProcedureSchema proc in threadedNode.Provider.GetProcedures ()) {
				AddProcedure (builder, proc);
			}
		}
	}
}