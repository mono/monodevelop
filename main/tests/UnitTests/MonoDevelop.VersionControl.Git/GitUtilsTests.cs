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
		private Dictionary<string, RevCommit[]> blames = new Dictionary<string, RevCommit[]>();
		private FileRepository repo;
		private RevWalk walker;
		
		[SetUp()]
		public override void Setup() {
			base.Setup();
			DirectoryInfo gitDir = new DirectoryInfo (PROJECT_ROOT + ".git");
			repo = new FileRepository (gitDir.FullName);
			walker = new RevWalk (repo);
		}

		[Test()]
		public void TestBlameLineCountWithMultipleCommits ()
		{
			RevCommit[] blameCommits = GetBlameForFixedFile ("c5f4319ee3e077436e3950c8a764959d50bf57c0");
			Assert.That (blameCommits.Length, Is.EqualTo (72));
		}

		[Test()]
		public void TestBlameRevisionsWithMultipleCommits ()
		{
			RevCommit[] blameCommits = GetBlameForFixedFile ("c5f4319ee3e077436e3950c8a764959d50bf57c0");
			List<BlameFragment> blames = new List<BlameFragment> ();
			blames.Add (new BlameFragment (1, 27, "b6e41ee2"));
			blames.Add (new BlameFragment (28, 1, "15ed2793"));
			blames.Add (new BlameFragment (29, 1, "a78c32a5"));
			blames.Add (new BlameFragment (30, 5, "b6e41ee2"));
			blames.Add(new BlameFragment(35, 2, "a78c32a5"));
			blames.Add(new BlameFragment(37, 2, "927ca9cd"));
			//The following two are correct according to "git blame", but according to a "git diff" of the two lines
			//then the uncommented lines are correct (and pass the test)
			//blames.Add(new BlameFragment(39, 2, "a78c32a5"));
			//blames.Add(new BlameFragment(41, 6, "b6e41ee2"));
			blames.Add(new BlameFragment(39, 1, "a78c32a5"));
			blames.Add(new BlameFragment(40, 7, "b6e41ee2"));
			blames.Add(new BlameFragment(47, 1, "927ca9cd"));
			blames.Add(new BlameFragment(48, 3, "b6e41ee2"));
			blames.Add(new BlameFragment(51, 2, "15ed2793"));
			blames.Add(new BlameFragment(53, 1, "39f9d8e3"));
			blames.Add(new BlameFragment(54, 7, "15ed2793"));
			blames.Add(new BlameFragment(61, 2, "b6e41ee2"));
			blames.Add(new BlameFragment(63, 1, "15ed2793"));
			blames.Add(new BlameFragment(64, 2, "b6e41ee2"));
			blames.Add(new BlameFragment(66, 1, "c5f4319e"));
			blames.Add(new BlameFragment(67, 1, "b6e41ee2"));
			blames.Add(new BlameFragment(68, 1, "c5f4319e"));
			blames.Add(new BlameFragment(69, 4, "b6e41ee2"));
			
			CompareBlames (blameCommits, blames);
		}
		
		[Test()]
		public void TestBlameRevisionsWithTwoCommits ()
		{
			string commit1 = "b6e41ee2dd00e8744abc4835567e06667891b2cf";
			string commit2 = "15ed279";
			RevCommit[] blameCommits = GetBlameForFixedFile (commit2);
			List<BlameFragment> blames = new List<BlameFragment> ();
			blames.Add (new BlameFragment (1, 27, commit1));
			blames.Add (new BlameFragment (28, 1, commit2));
			blames.Add (new BlameFragment (29, 5, commit1));
			blames.Add (new BlameFragment (34, 1, commit2));
			blames.Add (new BlameFragment (35, 7, commit1));
			blames.Add (new BlameFragment (42, 1, commit2));
			blames.Add (new BlameFragment (43, 3, commit1));
			blames.Add (new BlameFragment (46, 10, commit2));
			blames.Add (new BlameFragment (56, 2, commit1));
			blames.Add (new BlameFragment (58, 1, commit2));
			blames.Add (new BlameFragment (59, 4, commit1));
			blames.Add (new BlameFragment (63, 1, commit2));
			blames.Add (new BlameFragment (64, 4, commit1));
			CompareBlames (blameCommits, blames);
		}
		
		[Test()]
		public void TestBlameLineCountWithTwoCommits ()
		{
			RevCommit[] blameCommits = GetBlameForFixedFile ("15ed279");
			Assert.That (blameCommits.Length, Is.EqualTo (67));
		}
		
		[Test()]
		public void TestBlameRevisionsWithOneCommit ()
		{
			string commit = "b6e41ee2dd00e8744abc4835567e06667891b2cf";
			RevCommit[] blameCommits = GetBlameForFixedFile (commit);
			List<BlameFragment> blames = new List<BlameFragment> ();
			blames.Add (new BlameFragment (1, 56, commit));			
			CompareBlames (blameCommits, blames);
		}
		
		[Test()]
		public void TestBlameLineCountWithOneCommit ()
		{
			RevCommit[] blameCommits = GetBlameForFixedFile ("b6e41ee2dd00e8744abc4835567e06667891b2cf");
			Assert.That (blameCommits.Length, Is.EqualTo (56));
		}
		
		[Test()]
		public void TestBlameLineCountWithNoCommits ()
		{
			RevCommit[] blameCommits = GetBlameForFixedFile ("39fe1158de8da8b82822e299958d35c51d493298");
			Assert.That (blameCommits.Length, Is.EqualTo (0));
		}
		
		private RevCommit[] GetBlameForFixedFile (string revision)
		{
			RevCommit[] blame = null;
			string path = PROJECT_ROOT + "main/src/addins/VersionControl/MonoDevelop.VersionControl.Git/MonoDevelop.VersionControl.Git/GitVersionControl.cs";
			string key = path + revision;
			blames.TryGetValue(key, out blame);
			
			if (blame == null)
			{
				ObjectId objectId = repo.Resolve (revision);
				RevCommit commit = walker.ParseCommit (objectId);
				blame = GitUtil.Blame (repo, commit, new FileInfo (path).FullName);
				blames.Add(key, blame);
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

