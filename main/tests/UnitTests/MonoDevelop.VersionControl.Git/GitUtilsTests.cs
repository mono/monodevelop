// 
// GitUtilsTests.cs
//  
// Author:
//       IBBoard <dev@ibboard.co.uk>
// 
// Copyright (c) 2011 IBBoard
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
using NUnit.Framework;
using NGit.Storage.File;
using NGit.Revwalk;
using NGit;
using UnitTests;
using NUnit.Framework.SyntaxHelpers;
using System.IO;

namespace MonoDevelop.VersionControl.Git
{
	[TestFixture()]
	public class GitUtilsTests : TestBase
	{
		private readonly string PROJECT_ROOT = "../../../";
		[Test()]
		public void TestBlame ()
		{
			string path = PROJECT_ROOT + "main/src/addins/VersionControl/MonoDevelop.VersionControl.Git/MonoDevelop.VersionControl.Git/GitVersionControl.cs";
			string revision = "c5f4319ee3e077436e3950c8a764959d50bf57c0";
			DirectoryInfo gitDir = new DirectoryInfo(PROJECT_ROOT + ".git");
			FileRepository repo = new FileRepository(gitDir.FullName);
			RevWalk walker = new RevWalk(repo);
			ObjectId objectId = repo.Resolve (revision);
			RevCommit commit = walker.ParseCommit (objectId);
			RevCommit[] blameCommits = GitUtil.Blame (repo, commit, new FileInfo(path).FullName);
			Assert.That(blameCommits.Length, Is.EqualTo(73));
		}
	}
}

