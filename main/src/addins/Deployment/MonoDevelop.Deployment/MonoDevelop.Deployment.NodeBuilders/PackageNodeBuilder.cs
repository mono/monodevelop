//
// PackageNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Deployment.Gui;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Deployment.NodeBuilders
{
	internal class PackageNodeBuilder: TypeNodeBuilder
	{
		EventHandler configsChanged;
		
		public PackageNodeBuilder ()
		{
			configsChanged = (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnConfigurationsChanged));
		}
		
		public override Type CommandHandlerType {
			get { return typeof(PackageNodeCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Deployment/ContextMenu/ProjectPad/Package"; }
		}

		public override Type NodeDataType {
			get { return typeof(Package); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((Package)dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Package package = dataObject as Package;
			label = package.Name;
			if (package.Name != package.PackageBuilder.Description)
				label += " (" + package.PackageBuilder.Description + ")";
			
			if (package.PackageBuilder is UnknownPackageBuilder) {
				icon = Context.GetIcon (Stock.Error);
			}
			else {
				icon = Context.GetIcon (package.PackageBuilder.Icon);
			}
		}

		public override void OnNodeAdded (object dataObject)
		{
			Package package = dataObject as Package;
			package.Changed += configsChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			Package package = dataObject as Package;
			package.Changed -= configsChanged;
		}
		
		public void OnConfigurationsChanged (object sender, EventArgs args)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (sender);
			if (tb != null) tb.UpdateAll ();
		}
	}
	
	class PackageNodeCommandHandler: NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			Package package = CurrentNode.DataItem as Package;
			DeployOperations.ShowPackageSettings (package);
		}

		[CommandHandler (ProjectCommands.Options)]
		protected void OnShowOptions ()
		{
			ActivateItem ();
		}
		
		[CommandUpdateHandler (ProjectCommands.Build)]
		protected void OnUpdateShowOptions (CommandInfo info)
		{
			Package package = CurrentNode.DataItem as Package;
			info.Enabled = !(package.PackageBuilder is UnknownPackageBuilder);
		}
		
		public override void DeleteItem ()
		{
			Package package = CurrentNode.DataItem as Package;
			if (MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to delete the package '{0}'?", package.Name), AlertButton.Cancel, AlertButton.Delete) == AlertButton.Delete) {
				package.ParentProject.Packages.Remove (package);
				IdeApp.ProjectOperations.Save (package.ParentProject);
			}
		}
		
		[CommandHandler (ProjectCommands.Build)]
		protected void OnBuild ()
		{
			Package package = CurrentNode.DataItem as Package;
			DeployOperations.BuildPackage (package);
		}
		
		[CommandUpdateHandler (ProjectCommands.Build)]
		protected void OnBuild (CommandInfo info)
		{
			Package package = CurrentNode.DataItem as Package;
			info.Enabled = !(package.PackageBuilder is UnknownPackageBuilder);
		}
	}
}
