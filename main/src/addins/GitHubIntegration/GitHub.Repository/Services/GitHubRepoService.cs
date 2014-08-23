//
// GitHubRepoService.cs
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
using System.Collections;
using System.Collections.Generic;
using GitHub.Repository.Core;
using GitHub.Repository.Services;

namespace GitHub.Repository.Services
{
	public class GitHubRepoService
	{
		static GitHubRepoService instance;

		ArrayList providers = new ArrayList ();
		GitHubRepo[] repoList;

		public GitHubRepoService ()
		{
		}

		public static GitHubRepoService Instance {
			get {
				if (instance == null) {
					instance = new GitHubRepoService ();
					instance.getAllReposFromGitHub ();
				}
				return instance;
			}
		}

		void getAllReposFromGitHub(){
			List<GitHubRepo> repos = new List<GitHubRepo>();
			foreach (Octokit.Repository item in new OctokitHelper().GetAllRepositories()) {
				repos.Add (new GitHubRepo (item){ Name = item.FullName });
			} 
			repoList = repos.ToArray ();
		}

		public GitHubRepo[] RepoList {
			get { return repoList; }
		}

	}
}

