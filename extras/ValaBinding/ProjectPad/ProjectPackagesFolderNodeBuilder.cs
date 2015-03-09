//
// ProjectPackagesFolderNodeBuilder.cs: Node to control the packages in the project
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
using System.Collections;

using Mono.Addins;

using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.ValaBinding.ProjectPad
{
    public class ProjectPackagesFolderNodeBuilder : TypeNodeBuilder
    {
        ProjectPackageEventHandler addedHandler;
        ProjectPackageEventHandler removedHandler;

        public override Type NodeDataType
        {
            get { return typeof(ProjectPackageCollection); }
        }

        public override void OnNodeAdded(object dataObject)
        {
            ValaProject project = ((ProjectPackageCollection)dataObject).Project;
            if (project == null) return;
            project.PackageAddedToProject += addedHandler;
            project.PackageRemovedFromProject += removedHandler;
        }

        public override void OnNodeRemoved(object dataObject)
        {
            ValaProject project = ((ProjectPackageCollection)dataObject).Project;
            if (project == null) return;
            project.PackageAddedToProject -= addedHandler;
            project.PackageRemovedFromProject -= removedHandler;
        }

        public override Type CommandHandlerType
        {
            get { return typeof(ProjectPackagesFolderNodeCommandHandler); }
        }

        protected override void Initialize()
        {
            addedHandler = (ProjectPackageEventHandler)DispatchService.GuiDispatch(new ProjectPackageEventHandler(OnAddPackage));
            removedHandler = (ProjectPackageEventHandler)DispatchService.GuiDispatch(new ProjectPackageEventHandler(OnRemovePackage));
        }

        public override string GetNodeName(ITreeNavigator thisNode, object dataObject)
        {
            return "Packages";
        }

        public override void BuildNode(ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
            nodeInfo.Label = "Packages";
            nodeInfo.Icon = Context.GetIcon(Stock.OpenReferenceFolder);
            nodeInfo.ClosedIcon = Context.GetIcon(Stock.ClosedReferenceFolder);
        }

        public override bool HasChildNodes(ITreeBuilder builder, object dataObject)
        {
            return ((ProjectPackageCollection)dataObject).Count > 0;
        }

        public override void BuildChildNodes(ITreeBuilder treeBuilder, object dataObject)
        {
            ProjectPackageCollection packages = (ProjectPackageCollection)dataObject;

            foreach (ProjectPackage p in packages)
                treeBuilder.AddChild(p);
        }

        public override string ContextMenuAddinPath
        {
            get { return "/MonoDevelop/ValaBinding/Views/ProjectBrowser/ContextMenu/PackagesFolderNode"; }
        }

        public override int CompareObjects(ITreeNavigator thisNode, ITreeNavigator otherNode)
        {
            return -1;
        }

        private void OnAddPackage(object sender, ProjectPackageEventArgs e)
        {
            ITreeBuilder builder = Context.GetTreeBuilder(e.Project.Packages);
            if (builder != null)
                builder.UpdateAll();
        }

        private void OnRemovePackage(object sender, ProjectPackageEventArgs e)
        {
            ITreeBuilder builder = Context.GetTreeBuilder(e.Project.Packages);
            if (builder != null)
                builder.UpdateAll();
        }
    }

    public class ProjectPackagesFolderNodeCommandHandler : NodeCommandHandler
    {
        [CommandHandler(MonoDevelop.ValaBinding.ValaProjectCommands.AddPackage)]
        public void AddPackageToProject()
        {
            ValaProject project = (ValaProject)CurrentNode.GetParentDataItem(
                typeof(ValaProject), false);

            EditPackagesDialog dialog = new EditPackagesDialog(project);
            dialog.Run();

            IdeApp.ProjectOperations.Save(project);
            CurrentNode.Expanded = true;
        }

        // Currently only accepts packages and projects that compile into a static library
        public override bool CanDropNode(object dataObject, DragOperation operation)
        {
            if (dataObject is ProjectPackage)
                return true;

            if (dataObject is ValaProject)
            {
                ValaProject project = (ValaProject)dataObject;

                if (((ProjectPackageCollection)CurrentNode.DataItem).Project.Equals(project))
                    return false;

                ValaProjectConfiguration config = (ValaProjectConfiguration)project.GetConfiguration(IdeApp.Workspace.ActiveConfiguration);

                if (config.CompileTarget != ValaBinding.CompileTarget.Bin)
                    return true;
            }

            return false;
        }

        public override DragOperation CanDragNode()
        {
            return DragOperation.Copy | DragOperation.Move;
        }

        public override void OnNodeDrop(object dataObject, DragOperation operation)
        {
            if (dataObject is ProjectPackage)
            {
                ProjectPackage package = (ProjectPackage)dataObject;
                ITreeNavigator nav = CurrentNode;

                ValaProject dest = nav.GetParentDataItem(typeof(ValaProject), true) as ValaProject;
                nav.MoveToObject(dataObject);
                ValaProject source = nav.GetParentDataItem(typeof(ValaProject), true) as ValaProject;

                dest.Packages.Add(package);
                IdeApp.ProjectOperations.Save(dest);

                if (operation == DragOperation.Move)
                {
                    source.Packages.Remove(package);
                    IdeApp.ProjectOperations.Save(source);
                }
            }
            else if (dataObject is ValaProject)
            {
                ValaProject draggedProject = (ValaProject)dataObject;
                ValaProject destProject = (CurrentNode.DataItem as ProjectPackageCollection).Project;

                ProjectPackage package = ProjectPackage.CreateBetween2Projects(destProject, draggedProject);

                if (!destProject.Packages.Contains(package))
                {
                    destProject.Packages.Add(package);
                    IdeApp.ProjectOperations.Save(destProject);
                }
            }
        }
    }
}
