// 
// ExternalFrameworkNodeBuilder.cs
//  
// Author:
//       Duane Wandless
// 
// Copyright (c) 2009-2010 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.MonoMac
{
	public class ExternalFrameworkFolderNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { 
				return typeof (ExternalFrameworksFolder); 
			}
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return GettextCatalog.GetString ("External frameworks");
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = GettextCatalog.GetString ("External frameworks");
			icon = Context.GetIcon (Stock.OpenReferenceFolder);
			closedIcon = Context.GetIcon (Stock.ClosedReferenceFolder);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return((ExternalFrameworksFolder)dataObject).Project.Items.GetAll<MonoMacFrameworkItem> ().Any ();
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var frameworkFolder = (ExternalFrameworksFolder)dataObject;
			
			foreach (var node in frameworkFolder.Project.Items.GetAll<MonoMacFrameworkItem> ())
				treeBuilder.AddChild (node);
		}
	}
	
	public class ExternalFrameworkNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { 
				return typeof (MonoMacFrameworkItem); 
			}
		}
		
		public override Type CommandHandlerType {
			get {
				return typeof (ExternalFolderNodeCommandHandler);
			}
		}
		
		public override string ContextMenuAddinPath {
			get {
				return base.ContextMenuAddinPath;
			}
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((MonoMacFrameworkItem)dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = ((MonoMacFrameworkItem)dataObject).Name;
			icon = Context.GetIcon (Stock.MiscFiles);
		}
	}
	
	public class ExternalFolderNodeCommandHandler : NodeCommandHandler
	{
		public override void DeleteItem ()
		{
			var node = (MonoMacFrameworkItem)CurrentNode.DataItem;
			var proj = (MonoMacProject)CurrentNode.GetParentDataItem (typeof (MonoMacProject), false);
			proj.Items.Remove (node);
			IdeApp.ProjectOperations.Save (proj);
			IdeApp.Workbench.StatusBar.ShowMessage (GettextCatalog.GetString ("Removed framework reference"));	
			
			MonoMacAddFrameworksHandler.NotifyFrameworksChanged (proj);
		}
	}
}

