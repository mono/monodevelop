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

namespace GitHub.Repository.Core
{
	public class OctokitHelper
	{

		public IReadOnlyList<Octokit.Repository> GetAllRepositories()
		{
			Task<IReadOnlyList<Octokit.Repository>> repositories = GitHubService.Client.Repository.GetAllForCurrent();
			return repositories.Result;
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

//		public bool GistThis(List<Octokit.GistFile> gistFiles){
//			Octokit.Gist gist = new Octokit.Gist ();
//			gist.Files.Add (gistFiles);
//			Octokit.GistFile file = new Octokit.GistFile ();
//			try {
//				GitHubService.Client.Gist.Create (gist).Result;
//				return true;
//			} catch (Exception ex) {
//				return false;
//			}
//		}


		//in progress
//		public bool GistAFile()
//		{
//			Octokit.Gist gist = new Octokit.Gist ();
//			Octokit.GistFile file = new Octokit.GistFile ();
//			//gist.Files.Add (file);
//			//GitHubService.Client.Gist.Create (gist).Result;
//		}
			
	}
}

