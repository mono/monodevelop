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
using System;
using System.Linq;
using NUnit.Framework;
using System.IO;
using System.Collections.Generic;
using LibGit2Sharp;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Git.Tests
{
	[TestFixture]
	public class GitIntegrityTests
	{
		readonly Dictionary<string, Commit[]> blames = new Dictionary<string, Commit[]>();
		LibGit2Sharp.Repository repo;

		protected static void DeleteDirectory (string path)
		{
			string[] files = Directory.GetFiles (path);
			string[] dirs = Directory.GetDirectories (path);

			foreach (var file in files) {
				File.SetAttributes (file, FileAttributes.Normal);
				File.Delete (file);
			}

			foreach (var dir in dirs) {
				DeleteDirectory (dir);
			}

			Directory.Delete (path, true);
		}

		[SetUp]
		public void Setup() {
			var path = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);

			var repoPath = LibGit2Sharp.Repository.Init (path.FullPath + "repo.git", false);
			repo = new LibGit2Sharp.Repository (repoPath);

			CreateData ();
		}

		[TearDown]
		public void TearDown ()
		{
			var workDir = repo.Info.WorkingDirectory;
			repo.Dispose ();
			DeleteDirectory (workDir);
		}

		class BlameData {
			public string File { get; }
			public string [] Lines { get; }
			public int ChunkCount { get; }
			public int [] ChunkLengths { get; }

			public BlameData (string file, string[] lines, int count, int[] lengths)
			{
				File = file;
				Lines = lines;
				ChunkCount = count;
				ChunkLengths = lengths;
			}

			public string AsText ()
			{
				return string.Join (Environment.NewLine, Lines) + Environment.NewLine;
			}
		}

		class CommitData {
			public List<BlameData> Datas = new List<BlameData> ();
			public Commit Commit { get; set; }
		}

		static CommitData [] commits = {
			// Initial
			new CommitData {
				Datas = {
					// File content text to write to file and commit.
					new BlameData ("a.cs", new [] { "a", "a", "b", "c" }, 1, new int [] { 4, }),
					new BlameData ("b.cs", new [] { "a", }, 1, new int [] { 1, }),
					new BlameData ("insert_lines.cs", new [] { "Line1", "Line5" }, 1, new int[] { 2 } ),
				},
			},
			// Second
			new CommitData {
				Datas = {
					new BlameData ("b.cs", new [] { "b", }, 1, new int [] { 1, }),
					new BlameData ("c.cs", Enumerable.Repeat ("a", 10).ToArray (), 1, new int [] { 10, }),
					new BlameData ("insert_lines.cs", new [] { "Line1", "Line2", "Line 3", "Line 4", "Line5" }, 3, new int[] { 1, 3, 1 } ),
				},
			},
			new CommitData { Datas = { new BlameData ("a.cs", new [] { "b", "a", "b", "c" }, 2, new int [] { 1, 3, }), } },
			new CommitData { Datas = { new BlameData ("a.cs", new [] { "b", "a", "b", "d" }, 3, new int [] { 1, 2, 1, }), } },
			new CommitData { Datas = { new BlameData ("a.cs", new [] { "b", "a", "c", "d" }, 4, new int [] { 1, 1, 1, 1, }), } },
			new CommitData { Datas = { new BlameData ("a.cs", new [] { "e", "f", "g", "h" }, 1, new int [] { 4, }), } },
			new CommitData { Datas = { new BlameData ("a.cs", new [] { "e", "f", "g", }, 1, new int [] { 3, }), } },
		};

		static Signature signature = new Signature ("author", "email@domain.com", DateTimeOffset.UtcNow);
		// Created on repo init.
		string GetPath (string relativePath)
		{
			return Path.Combine (repo.Info.WorkingDirectory, relativePath);
		}

		void CreateData ()
		{
			int index = 0;
			foreach (var commit in commits) {
				foreach (var data in commit.Datas) {
					var path = GetPath (data.File);
					File.WriteAllText (path, data.AsText ());
				}

				LibGit2Sharp.Commands.Stage (repo, "*");

				var gitCommit = repo.Commit ($"commit - {index}", signature, signature);
				commit.Commit = gitCommit;
			}
		}

		void AssertBlame (BlameData data, Commit compareCommit)
		{
			var blame = GetBlameForFile (compareCommit.Sha, data.File);

			AssertBlame (data, blame);
		}

		void AssertBlame (BlameData data, Commit[] blame)
		{
			var allLines = data.ChunkLengths.Sum ();
			Assert.AreEqual (allLines, blame.Length);

			int lastIndex = 0;
			for (int i = 0; i < data.ChunkCount; ++i) {
				var blameCommit = blame [lastIndex];

				for (int j = 0; j < data.ChunkLengths [i]; ++j) {
					Assert.AreEqual (blameCommit, blame [lastIndex]);
				}
			}
		}

		[Test]
		public void TestFinalData ()
		{
			Dictionary<string, BlameData> headBlames = new Dictionary<string, BlameData> ();
			// Look for the last blame data, it has the required hunks.
			foreach (var commit in commits) {
				foreach (var data in commit.Datas) {
					headBlames [data.File] = data;
				}
			}

			foreach (var data in headBlames) {
				AssertBlame (data.Value, repo.Head.Tip);
			}
		}

		[Test]
		public void TestCommittedData()
		{
			foreach (var commit in commits) {
				foreach (var data in commit.Datas) {
					AssertBlame (data, commit.Commit);
				}
			}
		}

		[Test]
		public void BlameWithNoCommits()
		{
			Assert.Throws<NotFoundException> (() => GetBlameForFile (commits[0].Commit.Sha, ".cs"));
		}

		[Test]
		public void BlameWithRename ()
		{
			LibGit2Sharp.Commands.Move (repo, "a.cs", "d.cs");
			repo.Commit ("Remove commit", signature, signature);

			var dData = commits [commits.Length - 1].Datas.Single (x => x.File == "a.cs");

			var commit = repo.Head.Tip;
			var blame = GetBlameForFile (commit.Sha, "d.cs");

			AssertBlame (dData, blame);

			// Also assert the changekind
			var changes = GitUtil.CompareCommits (repo, commit.Parents.Single (), commit).ToArray ();

			Assert.AreEqual (1, changes.Length);
			Assert.AreEqual (ChangeKind.Renamed, changes [0].Status);
			Assert.AreEqual ("a.cs", changes [0].OldPath);
			Assert.AreEqual ("d.cs", changes [0].Path);
		}

		[Test]
		public void GetCommitChangesIgnoresUnchangedFiles ()
		{
			var initial = commits [0];
			var second = commits [1];

			var changes = GitUtil.CompareCommits (repo, initial.Commit, second.Commit).ToArray ();

			Assert.AreEqual (3, changes.Length);

			var changeForB = changes.Single (x => x.Path == "b.cs");
			var changeForC = changes.Single (x => x.Path == "c.cs");

			Assert.AreEqual (ChangeKind.Modified, changeForB.Status);
			Assert.AreEqual (ChangeKind.Added, changeForC.Status);
		}

		[Test]
		public void GetCommitChangesFromInitial ()
		{
			var initial = commits [0];
			var second = commits [commits.Length - 1];

			var changes = GitUtil.CompareCommits (repo, initial.Commit, second.Commit).ToArray ();

			Assert.AreEqual (4, changes.Length);

			var changeForA = changes.Single (x => x.Path == "a.cs");
			var changeForB = changes.Single (x => x.Path == "b.cs");
			var changeForC = changes.Single (x => x.Path == "c.cs");

			Assert.AreEqual (ChangeKind.Modified, changeForA.Status);
			Assert.AreEqual (ChangeKind.Modified, changeForB.Status);
			Assert.AreEqual (ChangeKind.Added, changeForC.Status);
		}

		[Test]
		public void GetCommitChangesFullHistory ()
		{
			Commit initial = null;
			Commit second = commits [commits.Length - 1].Commit;

			var changes = GitUtil.CompareCommits (repo, initial, second).ToArray ();

			Assert.AreEqual (4, changes.Length);
			Assert.That (changes.Select (x => x.Status), Is.All.EqualTo (ChangeKind.Added));
		}

		Commit[] GetBlameForFile (string revision, string filePath)
		{
			Commit[] blame;
			string key = filePath + revision;
			blames.TryGetValue(key, out blame);

			if (blame == null)
			{
				var result = repo.Blame (filePath, new BlameOptions {
					StartingAt = revision,
				});
				if (!result.Any ())
					return null;

				var count = result.Sum (hunk => hunk.LineCount);
				blame = new Commit [count];
				int x = 0;
				foreach (var res in result) {
					for (int i = 0; i < res.LineCount; ++i)
						blame [x++] = res.FinalCommit;
				}
				
				blames.Add(key, blame);
			}

			return blame;
		}
	}
}

