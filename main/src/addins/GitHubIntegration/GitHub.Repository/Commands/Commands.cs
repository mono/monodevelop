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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using System.Linq;
using MonoDevelop.Ide.ProgressMonitoring;
using System.Threading;
using MonoDevelop.VersionControl.Git;
using MonoDevelop.VersionControl;
using GitHub.Repository.Core;
using System.IO;
using System.Collections.Generic;
using Mono.TextEditor;
using Gtk;
using MonoDevelop.Core;

namespace GitHub.Repository.Commands
{
	public enum GitHubRepoCommands
	{
		ForkRepo,
		ViewGitHubRepoProperties
	}

	class GitHubCommandHandler: CommandHandler
	{
		public Document SelectedDocument{
			get{ 
				var doc = MonoDevelop.Ide.IdeApp.Workbench.ActiveDocument;
				return doc;
			}
		}

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
				string locationUri = this.Repository.LocationDescription;
				string Url = getRepositoryURL (locationUri);

				var obj = new OctokitHelper ();
				Octokit.Repository repo = obj.GetCurrentRepository (Url);
				return repo;
			}
		}

		private string getRepositoryURL(string locationDescription){

			string pathToConfig = Path.Combine (locationDescription, ".git", "config");
			using (StreamReader sr = File.OpenText(pathToConfig))
	
				{
				string s = String.Empty;
				while ((s = sr.ReadLine()) != null)
				{
					if (s.Trim ().StartsWith ("[remote")) {
						break;
					}
				}
				s = sr.ReadLine ();
				string[] words = s.Split ('=');
				return words [1].Trim ();
			}

		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = Repository != null;
		}

		protected string getGistFileName(string fileName)
		{
			return (fileName + "-" + DateTime.Now.ToString ()).Replace("/","-").Replace(" ","-").Trim();

		}
	}


	class GitHubPropertyHandler: GitHubCommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Loading properties"));
			GitHubUtils.ViewProperties (ORepository);
			IdeApp.Workbench.StatusBar.EndProgress ();
		}
	}

	class GistThisHandler : GitHubCommandHandler{

		protected override void Run ()
		{
			IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Started Gisting the selected document.."));
			IdeApp.Workbench.StatusBar.AutoPulse = true;
			Document doc = SelectedDocument;
			string mimeType = doc.Editor.MimeType;
			string content = doc.Editor.Text;
			string gistFileName = getGistFileName(doc.FileName.FileName);
			var obj = new OctokitHelper ();

			obj.GistThis (gistFileName, content, mimeType);
			IdeApp.Workbench.StatusBar.EndProgress ();
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);
			if (SelectedDocument.Editor.IsSomethingSelected) {
				info.Enabled = false;
			}
		}

	}

	class GistThisSelectedOnlyHandler : GitHubCommandHandler {
		protected override void Run ()
		{
			IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Started Gisting the selection in the document.."));
			IdeApp.Workbench.StatusBar.AutoPulse = true;
			Document doc = SelectedDocument;
			string content = doc.Editor.SelectedText;
			string mimeType = doc.Editor.MimeType;
			string gistFileName = getGistFileName(doc.FileName.FileName);
			var obj = new OctokitHelper ();
			obj.GistThis (gistFileName , content, mimeType);
			IdeApp.Workbench.StatusBar.EndProgress ();
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);
			if (!SelectedDocument.Editor.IsSomethingSelected) {
				info.Enabled = false;
			}

		}


	}


	class CopyURLInGitHubHandler : GitHubCommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Started copying the github location of the line in code"));

			string repositoryURL = this.Repository.GetCurrentRemote();
			string locationUri = this.Repository.LocationDescription;
			var wob = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem.BaseDirectory.FileName;

			Octokit.Repository repo = this.ORepository;

			string url = "https" + repo.GitUrl.Substring (3, (repo.GitUrl.Length- 7));


			Document document = this.SelectedDocument;
			string fileName = document.FileName;
			string relativeFilePath = document.PathRelativeToProject;
			IEnumerable<DocumentLine> selectedDocumentLines = document.Editor.SelectedLines;
			String githubURL = url + "/blob/" + relativeFilePath + "#L" + selectedDocumentLines.First ().LineNumber + "-" + selectedDocumentLines.Last ().LineNumber;
			
			Clipboard clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = githubURL;
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = githubURL;

			IdeApp.Workbench.StatusBar.EndProgress ();
		}
	}
}

