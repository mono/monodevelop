using System;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.VersionControl;
using MonoDevelop.VersionControl.Git;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitHub.Auth;

namespace GitHub.Issues
{
	public class GitHubUtilities
	{
		public GitHubUtilities ()
		{
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

				Octokit.Repository repo = this.GetCurrentRepository (Url);
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

		public Octokit.Repository GetCurrentRepository(string gitHubUrl)
		{
			Octokit.Repository repo = null;
			Task<IReadOnlyList<Octokit.Repository>> repositories = GitHubService.Client.Repository.GetAllForCurrent();
			foreach (var item in repositories.Result) {
				if (item.CloneUrl == gitHubUrl) {
					repo = item ;
					break;
				} 
			} 
			return repo;
		}
	}
}

