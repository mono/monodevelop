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

		/// <summary>
		/// Gets the repository (GitRepository) that we are currently working in if one exists or is set up
		/// </summary>
		/// <value>The repository.</value>
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

		/// <summary>
		/// Gets the repository (Octokit.Repository) that we are currently working in if one exists or is set up
		/// </summary>
		/// <value>The O repository.</value>
		public Octokit.Repository ORepository {

			get {
				string locationUri = this.Repository.LocationDescription;
				string Url = getRepositoryURL (locationUri);

				Octokit.Repository repo = this.GetCurrentRepository (Url);
				return repo;
			}
		}

		/// <summary>
		/// Gets the repository URL.
		/// </summary>
		/// <returns>The repository URL.</returns>
		/// <param name="locationDescription">Location description.</param>
		private string getRepositoryURL (string locationDescription)
		{

			string pathToConfig = Path.Combine (locationDescription, ".git", "config");
			using (StreamReader sr = File.OpenText (pathToConfig)) {
				string s = String.Empty;
				while ((s = sr.ReadLine ()) != null) {
					if (s.Trim ().StartsWith ("[remote")) {
						break;
					}
				}
				s = sr.ReadLine ();
				string[] words = s.Split ('=');
				return words [1].Trim ();
			}
		}

		/// <summary>
		/// Gets the current repository.
		/// </summary>
		/// <returns>The current repository.</returns>
		/// <param name="gitHubUrl">Git hub URL.</param>
		public Octokit.Repository GetCurrentRepository (string gitHubUrl)
		{
			Octokit.Repository repo = null;
			Task<IReadOnlyList<Octokit.Repository>> repositories = GitHubService.Client.Repository.GetAllForCurrent ();
			foreach (var item in repositories.Result) {
				if (item.CloneUrl == gitHubUrl) {
					repo = item;
					break;
				} 
			} 
			return repo;
		}
	}
}

