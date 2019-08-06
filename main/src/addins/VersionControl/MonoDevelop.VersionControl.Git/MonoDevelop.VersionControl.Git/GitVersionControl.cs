//
// GitVersionControl.cs
//
// Author:
//       Dale Ragan <dale.ragan@sinesignal.com>
//
// Copyright (c) 2010 SineSignal, LLC
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

using MonoDevelop.Core;
using MonoDevelop.Ide;
using System.IO;

namespace MonoDevelop.VersionControl.Git
{
	abstract class GitVersionControl : VersionControlSystem
	{
		string version = null;
		bool failedToInitialize;

		internal const string GitExtension = ".git";

		public override string Name {
			get { return "Git"; }
		}

		public override string Version {
			get {
				if (version == null)
					version = LibGit2Sharp.GlobalSettings.Version.InformationalVersion;
				return version;
			}
		}

		public override bool IsInstalled {
			get {
				return true;
			}
		}

		static GitVersionControl ()
		{
			// soft settings migration for 8.0 without slowing down Ide initialization
			// should work fine for most users using git on regular basis
			if (Ide.IdeApp.IsInitialRunAfterUpgrade && Ide.IdeApp.UpgradedFromVersion < new System.Version (8, 0, 1, 2800)) {
				GitService.StashUnstashWhenSwitchingBranches.Set (false);
				GitService.StashUnstashWhenUpdating.Set (false);
				PropertyService.SaveProperties ();
			}
		}

		public override Repository GetRepositoryReference (FilePath path, string id)
		{
			return new GitRepository (this, path, null);
		}

		protected override Repository OnCreateRepositoryInstance ()
		{
			return new GitRepository ();
		}

		public override IRepositoryEditor CreateRepositoryEditor (Repository repo)
		{
			return new UrlBasedRepositoryEditor ((GitRepository)repo);
		}

		protected override FilePath OnGetRepositoryPath (FilePath path, string id)
		{
			if (failedToInitialize)
				return null;
			string repo = string.Empty;
			try {
				repo = LibGit2Sharp.Repository.Discover (path.ResolveLinks ());
			} catch (System.DllNotFoundException ex) {
				failedToInitialize = true;
				LoggingService.LogInternalError ("Error when loading the libgit libraries", ex);
				MessageService.ShowError (GettextCatalog.GetString ("Error initializing Version Control"), ex);
			}
			if (!string.IsNullOrEmpty (repo)) {
				repo = repo.TrimEnd ('\\', '/');
				if (repo.EndsWith (GitExtension, System.StringComparison.OrdinalIgnoreCase))
					repo = Path.GetDirectoryName (repo);
			}
			return repo;
		}

		public override string GetRelativeCheckoutPathForRemote (string remoteRelativePath)
		{
			remoteRelativePath = base.GetRelativeCheckoutPathForRemote (remoteRelativePath);
			if (remoteRelativePath.EndsWith (GitExtension, System.StringComparison.CurrentCultureIgnoreCase)) {
				remoteRelativePath = remoteRelativePath.Substring (0, remoteRelativePath.Length - GitExtension.Length);
			} 
			return remoteRelativePath;
		}
	}
}
