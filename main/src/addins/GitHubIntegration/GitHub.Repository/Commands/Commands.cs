//
// Commands.cs
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using System.Linq;
using MonoDevelop.Ide.ProgressMonitoring;
using System.Threading;
using MonoDevelop.VersionControl.Git;
using MonoDevelop.VersionControl;
using GitHub.Repository.Core;

namespace GitHub.Repository.Commands
{
	class GitHubCommandHandler: CommandHandler
	{
		public GitRepository Repository {
			get {
				IWorkspaceObject wob = IdeApp.ProjectOperations.CurrentSelectedSolution;
				if (wob == null)
					wob = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem;
				if (wob != null)
					return VersionControlService.GetRepository (wob) as GitRepository;
				return null;
			}
		}

		public Octokit.Repository ORepository {

			get{
				Console.WriteLine ("Tesdsdsdsdsdsdsd: "+ this.Repository.Url);
				var obj = new OctokitHelper ();
				Octokit.Repository repo = obj.GetCurrentRepository (this.Repository.Url);
				return repo;
			}
		}

		protected override void Update (CommandInfo info)
		{
			info.Visible = Repository != null;
		}
	}


	class GitHubPropertyHandler: GitHubCommandHandler
	{
		protected override void Run ()
		{
			GitHubUtils.ViewProperties (ORepository);
		}
	}
}

