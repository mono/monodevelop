//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2007 Ben Motmans
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
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Profiling
{
	public class ProfilingSnapshotCollectionNodeBuilder : TypeNodeBuilder
	{
		private ITreeBuilder builder;
		
		public ProfilingSnapshotCollectionNodeBuilder ()
			: base ()
		{
			ProfilingService.ProfilingSnapshots.SnapshotAdded += (ProfilingSnapshotEventHandler)DispatchService.GuiDispatch (new ProfilingSnapshotEventHandler (OnSnapshotAdded));
			ProfilingService.ProfilingSnapshots.SnapshotRemoved += (ProfilingSnapshotEventHandler)DispatchService.GuiDispatch (new ProfilingSnapshotEventHandler (OnSnapshotRemoved));
		}
		
		public override Type NodeDataType {
			get { return typeof (ProfilingSnapshotCollection); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Profiling/ContextMenu/ProfilingPad/ProfilingSnapshotNodes"; }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return GettextCatalog.GetString ("Profiling Summaries");
		}		
		
		public override void BuildNode (ITreeBuilder builder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = GettextCatalog.GetString ("Profiling Snapshots");
			icon = Context.GetIcon ("md-prof-snapshot");
			this.builder = builder;
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			ProfilingSnapshotCollection collection = (ProfilingSnapshotCollection) dataObject;

			foreach (IProfilingSnapshot snapshot in collection)
				builder.AddChild (snapshot);
			builder.Expanded = true;
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			ProfilingSnapshotCollection collection = (ProfilingSnapshotCollection) dataObject;
			return collection.Count > 0;
		}
		
		private void OnSnapshotAdded (object sender, ProfilingSnapshotEventArgs args)
		{
			builder.AddChild (args.Snapshot);
			builder.Expanded = true;
		}
		
		private void OnSnapshotRemoved (object sender, ProfilingSnapshotEventArgs args)
		{
			builder.UpdateChildren ();
			builder.Expanded = true;
		}
	}
}
