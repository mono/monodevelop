//
// OctokitHelper.cs
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
using GitHub.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.VersionControl.Git;

namespace GitHub.Repository.Core
{
	public class OctokitHelper
	{

		public IReadOnlyList<Octokit.Repository> GetAllRepositories ()
		{
			Task<IReadOnlyList<Octokit.Repository>> repositories = GitHubService.Client.Repository.GetAllForCurrent ();
			return repositories.Result;
		}


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

		public bool SendPullRequest (Octokit.Repository reop, GitRepository gitRepo)
		{
			//	GitHubService.Client.Repository.PullRequest.Create(reop.Owner, reop.Name, new Octokit.NewPullRequest(
			return true;
		}

		public bool GistThis (String fileName, String FileContent, string mimeType)
		{

			Octokit.NewGist newGist = new Octokit.NewGist ();
			newGist.Description = "A Gist from MonoDevelop!";
			newGist.Files.Add (fileName, FileContent);

			try {
				GitHubService.Client.Gist.Create (newGist);
				return true;
			} catch (Exception ex) {
				return false;
			}
		}
			
	}
}

