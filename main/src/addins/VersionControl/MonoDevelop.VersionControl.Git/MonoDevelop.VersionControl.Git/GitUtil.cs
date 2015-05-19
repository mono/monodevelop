//
// GitCommands.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core;
using System;
using ProgressMonitor = MonoDevelop.Core.ProgressMonitor;
using LibGit2Sharp;

namespace MonoDevelop.VersionControl.Git
{
	static class GitUtil
	{
		public static string ToGitPath (this LibGit2Sharp.Repository repo, FilePath filePath)
		{
			return filePath.FullPath.ToRelative (repo.Info.WorkingDirectory).ToString ().Replace ('\\', '/');
		}

		public static IEnumerable<string> ToGitPath (this LibGit2Sharp.Repository repo, IEnumerable<FilePath> filePaths)
		{
			foreach (var p in filePaths)
				yield return ToGitPath (repo, p);
		}

		public static FilePath FromGitPath (this LibGit2Sharp.Repository repo, string filePath)
		{
			filePath = filePath.Replace ('/', Path.DirectorySeparatorChar);
			return Path.Combine (repo.Info.WorkingDirectory, filePath);
		}

		/// <summary>
		/// Compares two commits and returns a list of files that have changed
		/// </summary>
		public static TreeChanges CompareCommits (LibGit2Sharp.Repository repo, Commit reference, Commit compared)
		{
			return repo.Diff.Compare<TreeChanges> (reference != null ? reference.Tree : null, compared != null ? compared.Tree : null);
		}

		public static TreeChanges GetChangedFiles (LibGit2Sharp.Repository repo, string refRev)
		{
			return GitUtil.CompareCommits (repo, repo.Lookup<Commit> (refRev), repo.Head.Tip);
		}

		public static LibGit2Sharp.Repository Init (string targetLocalPath, string url)
		{
			var path = LibGit2Sharp.Repository.Init (targetLocalPath);
			var repo = new LibGit2Sharp.Repository (path);
			if (!string.IsNullOrEmpty (url))
				repo.Network.Remotes.Add ("origin", url);
			return repo;
		}

		internal static bool IsGitRepository (this FilePath path)
		{
			// Maybe check if it has a HEAD file? But this check should be enough.
			var newPath = path.Combine (".git");
			return Directory.Exists (newPath) && Directory.Exists (newPath.Combine ("objects")) && Directory.Exists (newPath.Combine ("refs"));
		}
	}
}
