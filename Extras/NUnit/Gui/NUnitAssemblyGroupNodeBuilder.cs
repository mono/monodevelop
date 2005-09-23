//
// NUnitAssemblyGroupNodeBuilder.cs
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
using System.Collections;

using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using MonoDevelop.Gui.Pads;
using MonoDevelop.Commands;
using MonoDevelop.Gui;

namespace MonoDevelop.NUnit
{
	public class NUnitAssemblyGroupNodeBuilder: TypeNodeBuilder
	{
		ConfigurationEventHandler configsChanged;
		
		public NUnitAssemblyGroupNodeBuilder ()
		{
			configsChanged = (ConfigurationEventHandler) Runtime.DispatchService.GuiDispatch (new ConfigurationEventHandler (OnConfigurationsChanged));
		}
		
		public override Type CommandHandlerType {
			get { return typeof(NUnitAssemblyGroupNodeCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/NUnitAssemblyGroup"; }
		}

		public override Type NodeDataType {
			get { return typeof(NUnitAssemblyGroupProject); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((NUnitAssemblyGroupProject)dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			NUnitAssemblyGroupProject project = dataObject as NUnitAssemblyGroupProject;
			label = project.Name;
			icon = Context.GetIcon (Stock.EmptyProjectIcon);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			NUnitAssemblyGroupProject project = dataObject as NUnitAssemblyGroupProject;
				
			foreach (NUnitAssemblyGroupProjectConfiguration c in project.Configurations)
				builder.AddChild (c);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			NUnitAssemblyGroupProject project = dataObject as NUnitAssemblyGroupProject;
			return project.Configurations.Count > 0;
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			NUnitAssemblyGroupProject project = dataObject as NUnitAssemblyGroupProject;
			project.ConfigurationAdded += configsChanged;
			project.ConfigurationRemoved += configsChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			NUnitAssemblyGroupProject project = dataObject as NUnitAssemblyGroupProject;
			project.ConfigurationAdded -= configsChanged;
			project.ConfigurationRemoved -= configsChanged;
		}
		
		public void OnConfigurationsChanged (object sender, ConfigurationEventArgs args)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (sender);
			if (tb != null) tb.UpdateAll ();
		}
	}
	
	class NUnitAssemblyGroupNodeCommandHandler: NodeCommandHandler
	{
		[CommandHandler (NUnitProjectCommands.AddAssembly)]
		protected void OnShowTest ()
		{
		}
	}
}
