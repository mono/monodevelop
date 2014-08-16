//
// EmptyClass.cs
//
// Author:
//       Praveena <>
//
// Copyright (c) 2014 Praveena
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
using MonoDevelop.Components.Commands;
using GitHub.Repository.Commands;
using GitHub.Repository.Services;
using GitHub.Repository.Core;
using MonoDevelop.VersionControl.Dialogs;
using MonoDevelop.Ide;
using MonoDevelop.Core;

namespace GitHub.Repository.Gui
{
	public class GitHubRepoNodeBuilder : TypeNodeBuilder
	{
		public GitHubRepoNodeBuilder ()
		{
		}

		public override Type CommandHandlerType {
			get { return typeof(GitHubRepoNodeCommandHandler); }
		}


		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/GitHubRepository/ContextMenu/GitHubPad"; }
		}

		public override Type NodeDataType {
			get { return typeof(GitHubRepo); }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((GitHubRepo)dataObject).Name;
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			GitHubRepo repo = dataObject as GitHubRepo;
			nodeInfo.Icon = repo.StatusIcon;
			nodeInfo.Label = repo.Name;

		}
	}


	class GitHubRepoNodeCommandHandler: NodeCommandHandler
	{

		[CommandHandler (GitHubRepoCommands.ViewGitHubRepoProperties)]
		protected void OnViewGitHubRepoProperties ()
		{
			IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Loading properties"));

			GitHubRepo repo = CurrentNode.DataItem as GitHubRepo;
			GitHubUtils.ViewProperties (repo.ORepository);

			IdeApp.Workbench.StatusBar.EndProgress ();

		}

		[CommandHandler (GitHubRepoCommands.CloneRepo)]
		protected void OnCloneRepo ()
		{
			GitHubRepo repo = CurrentNode.DataItem as GitHubRepo;


		}


	}
}

