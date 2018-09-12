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
using System.Collections.Generic;
using System.IO;

namespace MonoDevelop.VersionControl.Git
{
	abstract class GitVersionControl : VersionControlSystem
	{
		public override string Name {
			get { return "Git"; }
		}

		public override bool IsInstalled {
			get {
				return true;
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
			string repo = LibGit2Sharp.Repository.Discover (path.ResolveLinks ());
			if (!string.IsNullOrEmpty (repo)) {
				repo = repo.TrimEnd ('\\', '/');
				if (repo.EndsWith (".git", System.StringComparison.OrdinalIgnoreCase))
					repo = Path.GetDirectoryName (repo);
			}
			return repo;
		}
	}
}
