//
// NUnitAssemblyGroupConfigurationNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.NUnit.Commands;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;

namespace MonoDevelop.NUnit
{
	public class NUnitAssemblyGroupConfigurationNodeBuilder: TypeNodeBuilder
	{
		EventHandler assembliesChanged;
		
		public NUnitAssemblyGroupConfigurationNodeBuilder ()
		{
			assembliesChanged = (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnAssembliesChanged));
		}
		
		public override Type CommandHandlerType {
			get { return typeof(NUnitAssemblyGroupConfigurationNodeCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/NUnit/ContextMenu/ProjectPad/NUnitAssemblyGroupConfiguration"; }
		}

		public override Type NodeDataType {
			get { return typeof(NUnitAssemblyGroupProjectConfiguration); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((SolutionItemConfiguration)dataObject).Id;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var conf = (SolutionItemConfiguration) dataObject;
			nodeInfo.Label = conf.Id;
			nodeInfo.Icon = Context.GetIcon (Stock.ClosedFolder);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			var config = (NUnitAssemblyGroupProjectConfiguration) dataObject;
				
			foreach (TestAssembly ta in config.Assemblies)
				builder.AddChild (ta);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			var config = (NUnitAssemblyGroupProjectConfiguration) dataObject;
			return config.Assemblies.Count > 0;
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			var config = (NUnitAssemblyGroupProjectConfiguration) dataObject;
			config.AssembliesChanged += assembliesChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			var config = (NUnitAssemblyGroupProjectConfiguration) dataObject;
			config.AssembliesChanged -= assembliesChanged;
		}
		
		public void OnAssembliesChanged (object sender, EventArgs args)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (sender);
			if (tb != null) tb.UpdateAll ();
		}
	}
	
	class NUnitAssemblyGroupConfigurationNodeCommandHandler: NodeCommandHandler
	{
		[CommandHandler (NUnitProjectCommands.AddAssembly)]
		protected void OnAddAssembly ()
		{
			var config = (NUnitAssemblyGroupProjectConfiguration) CurrentNode.DataItem;
			
			var dlg = new SelectFileDialog (GettextCatalog.GetString ("Add files")) {
				TransientFor = IdeApp.Workbench.RootWindow,
				SelectMultiple = true,
			};
			if (!dlg.Run ())
				return;
			
			foreach (string file in dlg.SelectedFiles)
				config.Assemblies.Add (new TestAssembly (file));
			
			IdeApp.Workspace.Save();
		}
	}
}
