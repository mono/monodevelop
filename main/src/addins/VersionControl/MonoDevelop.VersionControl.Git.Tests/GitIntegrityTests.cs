//
// GitUtilsTests.cs
//
// Author:
//       IBBoard <dev@ibboard.co.uk>
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
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
/*using System;
using System.Linq;
using NUnit.Framework;
using System.IO;
using System.Collections.Generic;
using LibGit2Sharp;

namespace MonoDevelop.VersionControl.Git.Tests
{
	[TestFixture]
	public class GitIntegrityTests
	{
		readonly string PROJECT_ROOT = "../../../";
		readonly Dictionary<string, Commit[]> blames = new Dictionary<string, Commit[]>();
		LibGit2Sharp.Repository repo;

		[SetUp]
		public void Setup() {
			var gitDir = new DirectoryInfo (PROJECT_ROOT + ".git");
			repo = new LibGit2Sharp.Repository (gitDir.FullName);
		}

		[Test]
		public void TestBlameLineCountWithMultipleCommits ()
		{
			Commit[] blameCommits = GetBlameForFixedFile ("c5f4319ee3e077436e3950c8a764959d50bf57c0");
			Assert.That (blameCommits.Length, Is.EqualTo (72));
		}

		[Test]
		[Ignore ("This fails with NGit, probably because the diff algorithm is different")]
		public void TestBlameRevisionsWithMultipleCommits ()
		{
			Commit[] blameCommits = GetBlameForFixedFile ("c5f4319ee3e077436e3950c8a764959d50bf57c0");
			var blames = new List<BlameFragment> ();
			blames.Add (new BlameFragment (1, 27, "b6e41ee2"));
			blames.Add (new BlameFragment (28, 1, "15ed2793"));
			blames.Add (new BlameFragment (29, 1, "a78c32a5"));
			blames.Add (new BlameFragment (30, 5, "b6e41ee2"));
			blames.Add(new BlameFragment(35, 2, "a78c32a5"));
			blames.Add(new BlameFragment(37, 2, "927ca9cd"));
			blames.Add(new BlameFragment(39, 2, "a78c32a5"));
			blames.Add(new BlameFragment(41, 6, "b6e41ee2"));
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

		[Test]
		[Ignore ("This fails with NGit, probably because the diff algorithm is different")]
		public void TestBlameRevisionsWithTwoCommits ()
		{
			string commit1 = "b6e41ee2dd00e8744abc4835567e06667891b2cf";
			string commit2 = "15ed279";
			Commit[] blameCommits = GetBlameForFixedFile (commit2);
			var blames = new List<BlameFragment> ();
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

		[Test]
		public void TestBlameLineCountWithTwoCommits ()
		{
			Commit[] blameCommits = GetBlameForFixedFile ("15ed279");
			Assert.That (blameCommits.Length, Is.EqualTo (67));
		}

		[Test]
		public void TestBlameRevisionsWithOneCommit ()
		{
			string commit = "b6e41ee2dd00e8744abc4835567e06667891b2cf";
			Commit[] blameCommits = GetBlameForFixedFile (commit);
			var blames = new List<BlameFragment> ();
			blames.Add (new BlameFragment (1, 56, commit));
			CompareBlames (blameCommits, blames);
		}

		[Test]
		public void TestBlameLineCountWithOneCommit ()
		{
			Commit[] blameCommits = GetBlameForFixedFile ("b6e41ee2dd00e8744abc4835567e06667891b2cf");
			Assert.That (blameCommits.Length, Is.EqualTo (56));
		}

		[Test]
		public void TestBlameLineCountWithNoCommits ()
		{
			Commit[] blameCommits = GetBlameForFixedFile ("39fe1158de8da8b82822e299958d35c51d493298");
			Assert.That (blameCommits, Is.Null);
		}

		[Test]
		[Ignore]
		public void TestBlameForProjectDom ()
		{
			Commit[] blameCommits = GetBlameForFile ("6469602e3c0ba6953fd3ef0ae01d77abe1d9ab70", "main/src/core/MonoDevelop.Core/MonoDevelop.Projects.Dom.Parser/ProjectDom.cs");
			var blames = new List<BlameFragment> ();
			blames.Add(new BlameFragment(1, 59, "3352c438"));
			blames.Add(new BlameFragment(60, 5, "85dfe8a5"));
			blames.Add(new BlameFragment(65, 3, "3352c438"));
			blames.Add(new BlameFragment(68, 3, "c45c8708"));
			blames.Add(new BlameFragment(71, 112, "3352c438"));
			blames.Add(new BlameFragment(183, 1, "c7da699"));
			blames.Add(new BlameFragment(184, 1, "3352c438"));
			blames.Add(new BlameFragment(185, 1, "c7da699"));
			blames.Add(new BlameFragment(186, 1, "3352c438"));
			blames.Add(new BlameFragment(187, 9, "e2ddc3e3"));
			blames.Add(new BlameFragment(196, 14, "3352c438"));
			blames.Add(new BlameFragment(210, 1, "3352c438"));
			blames.Add(new BlameFragment(211, 2, "4d06ef70"));
			blames.Add(new BlameFragment(213, 18, "3352c438"));
			blames.Add(new BlameFragment(231, 1, "d802c4d2"));
			blames.Add(new BlameFragment(232, 5, "3352c438"));
			blames.Add(new BlameFragment(237, 1, "d802c4d2"));
			blames.Add(new BlameFragment(238, 9, "3352c438"));
			blames.Add(new BlameFragment(247, 1, "d802c4d2"));
			blames.Add(new BlameFragment(248, 5, "3352c438"));
			blames.Add(new BlameFragment(253, 1, "d802c4d2"));
			blames.Add(new BlameFragment(254, 10, "3352c438"));
			blames.Add(new BlameFragment(264, 1, "0095b4ad"));
			blames.Add(new BlameFragment(265, 22, "3352c438"));
			blames.Add(new BlameFragment(287, 2, "0095b4ad"));
			blames.Add(new BlameFragment(289, 4, "3352c438"));
			blames.Add(new BlameFragment(293, 1, "0095b4ad"));
			blames.Add(new BlameFragment(294, 19, "3352c438"));
			blames.Add(new BlameFragment(313, 11, "0095b4ad"));
			blames.Add(new BlameFragment(324, 13, "3352c438"));
			blames.Add(new BlameFragment(337, 2, "7c9a428e"));
			blames.Add(new BlameFragment(339, 119, "3352c438"));
			blames.Add(new BlameFragment(458, 1, "165e9be7"));
			blames.Add(new BlameFragment(459, 1, "3352c438"));
			blames.Add(new BlameFragment(460, 1, "165e9be7"));
			blames.Add(new BlameFragment(461, 43, "3352c438"));
			blames.Add(new BlameFragment(504, 1, "37041bcf"));
			blames.Add(new BlameFragment(505, 3, "1ee2429c"));
			blames.Add(new BlameFragment(508, 11, "37041bcf"));
			blames.Add(new BlameFragment(519, 1, "1ee2429c"));
			blames.Add(new BlameFragment(520, 5, "37041bcf"));
			blames.Add(new BlameFragment(525, 1, "1ee2429c"));
			blames.Add(new BlameFragment(526, 1, "37041bcf"));
			blames.Add(new BlameFragment(527, 1, "1ee2429c"));
			blames.Add(new BlameFragment(528, 1, "37041bcf"));
			blames.Add(new BlameFragment(529, 1, "1ee2429c"));
			blames.Add(new BlameFragment(530, 1, "37041bcf"));
			blames.Add(new BlameFragment(531, 11, "1ee2429c"));
			blames.Add(new BlameFragment(542, 1, "37041bcf"));
			//Another minor discrepancy from "git blame" on a blank line that matches what is found by "git diff"
			blames.Add(new BlameFragment(543, 1, "1ee2429c"));
			blames.Add(new BlameFragment(544, 2, "3352c438"));
			blames.Add(new BlameFragment(546, 3, "37041bcf"));
			blames.Add(new BlameFragment(549, 59, "3352c438"));
			blames.Add(new BlameFragment(608, 1, "08a25d26"));
			blames.Add(new BlameFragment(609, 162, "3352c438"));
			blames.Add(new BlameFragment(771, 1, "c7da699f"));
			blames.Add(new BlameFragment(772, 16, "3352c438"));
			blames.Add(new BlameFragment(788, 13, "4d06ef70"));
			blames.Add(new BlameFragment(801, 37, "c3609340"));
			blames.Add(new BlameFragment(838, 2, "4d06ef70"));
			blames.Add(new BlameFragment(840, 1, "0f6822a9"));
			blames.Add(new BlameFragment(841, 1, "4d06ef70"));
			blames.Add(new BlameFragment(842, 1, "e262ac50"));
			blames.Add(new BlameFragment(843, 1, "4d06ef70"));
			blames.Add(new BlameFragment(844, 2, "daabc6e1"));
			blames.Add(new BlameFragment(846, 7, "e262ac50"));
			blames.Add(new BlameFragment(853, 3, "4d06ef70"));
			blames.Add(new BlameFragment(856, 37, "3352c438"));
			blames.Add(new BlameFragment(893, 1, "cc279afd"));
			blames.Add(new BlameFragment(894, 121, "3352c438"));
			blames.Add(new BlameFragment(1015, 10, "85dfe8a5"));
			blames.Add(new BlameFragment(1025, 43, "3352c438"));
			CompareBlames(blameCommits, blames);
		}

		[Test]
		public void GetCommitChangesAddedRemoved ()
		{
			var commit = "9ed729ee";
			var com = repo.Lookup<Commit> (commit);
			var changes = GitUtil.CompareCommits (repo, com.Parents.First (), com).ToArray ();

			var add = changes.First (c => c.Path.EndsWith ("DocumentLine.cs", StringComparison.Ordinal));
			var remove = changes.First (c => c.OldPath.EndsWith ("LineSegment.cs", StringComparison.Ordinal));

			Assert.AreEqual (ChangeKind.Renamed, add.Status, "#1");
			Assert.AreEqual ("main/src/core/Mono.Texteditor/Mono.TextEditor/Document/LineSegment.cs".Replace ('/', Path.DirectorySeparatorChar), add.OldPath, "#2");
			Assert.AreEqual (ChangeKind.Renamed, remove.Status, "#3");
			Assert.AreEqual ("main/src/core/Mono.Texteditor/Mono.TextEditor/Document/DocumentLine.cs".Replace ('/', Path.DirectorySeparatorChar), remove.Path, "#4");
		}

		[Test]
		public void GetCommitChangesModifications ()
		{
			var commit = "c6798c34577";
			var changedFiles = new [] {
				"EditActions.cs",
				"SourceEditorView.cs",
				"SourceEditorWidget.cs",
				"DeleteActions.cs",
				"DocumentUpdateRequest.cs",
				"FoldMarkerMargin.cs",
				"HeightTree.cs",
				"LineSplitter.cs",
				"TextDocument.cs",
				"TextEditor.cs",
				"TextViewMargin.cs",
			};

			var c = repo.Lookup<Commit> (commit);
			var changes = GitUtil.CompareCommits (repo, c.Parents.First (), c).ToArray ();
			Assert.AreEqual (11, changes.Length, "#1");

			foreach (var file in changedFiles)
				Assert.IsTrue (changes.Any (f => f.Path.EndsWith (".cs", StringComparison.Ordinal)), "#2." + file);
		}

		Commit[] GetBlameForFixedFile (string revision)
		{
			string filePath = "main/src/addins/VersionControl/MonoDevelop.VersionControl.Git/MonoDevelop.VersionControl.Git/GitVersionControl.cs";
			return GetBlameForFile (revision, filePath);
		}

		Commit[] GetBlameForFile (string revision, string filePath)
		{
			Commit[] blame;
			string path = PROJECT_ROOT + filePath;
			string key = path + revision;
			blames.TryGetValue(key, out blame);

			if (blame == null)
			{
				var result = repo.Blame (filePath);
				if (result == null)
					return null;

				blame = new Commit [result.Count ()];
				for (int i = 0; i < result.Count (); i ++)
					blame [i] = result[i].FinalCommit;
				blames.Add(key, blame);
			}

			return blame;
		}

		static void CompareBlames (Commit[] blameCommits,List<BlameFragment> blames)
		{
			foreach (BlameFragment blame in blames) {
				int zeroBasedStartLine = blame.StartLine - 1;

				for (int i = 0; i < blame.LineCount; i++) {
					Assert.That (blameCommits [zeroBasedStartLine + i].Id.Sha, Is.StringStarting (blame.RevID), "Error at line {0}", blame.StartLine + i);
				}
			}
		}
	}

	struct BlameFragment
	{
		public int StartLine;
		public int LineCount;
		public string RevID;

		public BlameFragment (int start,int count, string revision)
		{
			StartLine = start;
			LineCount = count;
			RevID = revision;
		}
	}
}
*/
