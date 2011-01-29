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
using System.Collections.Generic;

namespace MonoDevelop.VersionControl.Git
{
	[TestFixture()]
	public class GitUtilsTests : TestBase
	{
		private readonly string PROJECT_ROOT = "../../../";
		private RevCommit[] blame;

		[Test()]
		public void TestBlameLineCount ()
		{
			RevCommit[] blameCommits = GetBlameForFixedFile ();
			Assert.That (blameCommits.Length, Is.EqualTo (73));
		}

		[Test()]
		public void TestBlameRevisions ()
		{
			RevCommit[] blameCommits = GetBlameForFixedFile ();
			List<BlameFragment> blames = new List<BlameFragment> ();
			blames.Add (new BlameFragment (1, 27, "b6e41ee2"));
			blames.Add (new BlameFragment (28, 1, "15ed2793"));
			blames.Add (new BlameFragment (29, 1, "a78c32a5"));
			blames.Add (new BlameFragment (30, 5, "b6e41ee2"));
			
			CompareBlames (blameCommits, blames);
		}

		private RevCommit[] GetBlameForFixedFile ()
		{
			if (blame == null)
			{
				string path = PROJECT_ROOT + "main/src/addins/VersionControl/MonoDevelop.VersionControl.Git/MonoDevelop.VersionControl.Git/GitVersionControl.cs";
				string revision = "c5f4319ee3e077436e3950c8a764959d50bf57c0";
				DirectoryInfo gitDir = new DirectoryInfo (PROJECT_ROOT + ".git");
				FileRepository repo = new FileRepository (gitDir.FullName);
				RevWalk walker = new RevWalk (repo);
				ObjectId objectId = repo.Resolve (revision);
				RevCommit commit = walker.ParseCommit (objectId);
				blame = GitUtil.Blame (repo, commit, new FileInfo (path).FullName);
			}
			return blame;
		}

		void CompareBlames (RevCommit[] blameCommits,List<BlameFragment> blames)
		{
			foreach (BlameFragment blame in blames) {
				int zeroBasedStartLine = blame.startLine - 1;
				
				for (int i = 0; i < blame.lineCount; i++) {
					Assert.That (blameCommits [zeroBasedStartLine + i].Id.Name, Text.StartsWith(blame.revID), "Error at line {0}", blame.startLine + i);
				}
			}
		}

	}

	struct BlameFragment
	{
		public int startLine;
		public int lineCount;
		public string revID;

		public BlameFragment (int start,int count, string revision)
		{
			startLine = start;
			lineCount = count;
			revID = revision;
		}
	}
}

