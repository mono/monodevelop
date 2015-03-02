//
// ProjectNodeBuilderExtension.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
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
using System.IO;
using System.Threading;

using Mono.Addins;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;

using MonoDevelop.ValaBinding;
using MonoDevelop.ValaBinding.Parser;
using MonoDevelop.ValaBinding.Parser.Afrodite;

namespace MonoDevelop.ValaBinding.Navigation
{
	public class ProjectNodeBuilderExtension : NodeBuilderExtension
	{
		public ClassPadEventHandler finishedBuildingTreeHandler;
		
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(ValaProject).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ProjectNodeBuilderExtensionHandler); }
		}
		
		protected override void Initialize ()
		{
			finishedBuildingTreeHandler = (ClassPadEventHandler)DispatchService.GuiDispatch (new ClassPadEventHandler (OnFinishedBuildingTree));
		}
		
		public override void Dispose ()
		{
		}
		
		public static void CreatePadTree (object o)
		{
			ValaProject p = o as ValaProject;
			if (o == null) return;
			ProjectInformation pi = ProjectInformationManager.Instance.Get (p);
			
			try {
				foreach (ProjectFile f in p.Files) {
					if (f.BuildAction == BuildAction.Compile)
						pi.AddFile (f.FilePath);
				}
				foreach (ProjectPackage package in p.Packages) {
					if(!package.IsProject){ pi.AddPackage (p.Name); }
				}
			} catch (IOException) {
				return;
			}
		}

        public override void BuildNode(ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
        }
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{			
			ValaProject p = dataObject as ValaProject;
			if (p == null) return;
			
			// bool nestedNamespaces = builder.Options["NestedNamespaces"];
			
			ProjectInformation info = ProjectInformationManager.Instance.Get (p);
			
			// Namespaces
			foreach (ProjectFile file in p.Files) {
				foreach (Symbol child in info.GetNamespacesForFile (file.FilePath.FullPath)) {
					builder.AddChild (child);
				}
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		private void OnFinishedBuildingTree (ClassPadEventArgs e)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (e.Project);
			if (null != builder)
				builder.UpdateChildren ();
		}
	}
	
	public class ProjectNodeBuilderExtensionHandler : NodeCommandHandler
	{
		[CommandHandler (ValaProjectCommands.UpdateClassPad)]
		public void UpdateClassPad ()
		{
			ProjectNodeBuilderExtension.CreatePadTree (CurrentNode.DataItem);
		}
	}
}
