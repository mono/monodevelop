/*
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

/// This classes are used to check if the example code for the 
/// site http://www.eqqon.com/index.php/GitSharp/Examples compile


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using GitSharp.Commands;

namespace GitSharp
{
	internal class Manipulating_git_objects
	{
		Repository repo;

		public void Repository()
		{

			//Opening an existing git repository
			repo = new Repository("path/to/repo");

			// Now suppose you have created some new files and want to commit them
			repo.Index.Add("README", "License.txt");
			Commit commit = repo.Commit("My first commit with gitsharp", new Author("henon", "meinrad.recheis@gmail.com"));

			// Easy, isn't it? Now let's have a look at the changes of this commit:
			foreach (Change change in commit.Changes)
				Console.WriteLine(change.Name + " " + change.ChangeType);

			//Get the staged changes from the index
			repo.Status.Added.Contains("README");

			//Access and manipulate the configuration
			repo.Config["core.autocrlf"] = "false";
		}

		public void Commit()
		{

			// Get the message of the previous commit
			string msg = new Commit(repo, "HEAD^").Message;
			Debug.Assert(msg == repo.CurrentBranch.CurrentCommit.Parent.Message);

			//Print a list of changes between two commits c1 and c2:
			Commit c1 = repo.Get<Commit>( "979829389f136bfabb5956c68d909e7bf3092a4e"); // <-- note: short hashes are not yet supported
			Commit c2 = new Commit(repo, "4a7455c2f23e0f7356877d1813594041c56e2db9");
			foreach (Change change in c1.CompareAgainst(c2))
				Console.WriteLine(change.ChangeType + ": " + change.Path);

			//Print all previous commits of HEAD of the repository repo
			foreach (Commit commit in repo.Head.CurrentCommit.Ancestors)
				Console.WriteLine(commit.ShortHash + ": " + commit.Message + ", " + commit.Author.Name + ", " + commit.AuthorDate);
		}

		public void Tree_and_Leaf()
		{

			//Get the root tree of the most recent commit
			var tree = repo.Head.CurrentCommit.Tree;

			//It has no Parent so IsRoot should be true
			Debug.Assert(tree.Parent == null);
			Debug.Assert(tree.IsRoot);

			//Now you can browse throught that tree by iterating over its child trees
			foreach (Tree subtree in tree.Trees)
				Console.WriteLine(subtree.Path);

			//Or printing the names of the files it contains
			foreach (Leaf leaf in tree.Leaves)
				Console.WriteLine(leaf.Path);
		}

		public void Blob()
		{

			//A Leaf is a Blob and inherits from it a method to retrieve the data as a UTF8 encoded string:
			string string_data = new Blob(repo, "49322bb17d3acc9146f98c97d078513228bbf3c0").Data;

			// Blob also let's you access the raw data as byte array
			byte[] byte_data = new Blob(repo, "49322bb17d3acc9146f98c97d078513228bbf3c0").RawData;
		}

		public void Branch()
		{

			//Get the current branch
			var branch = repo.CurrentBranch;
			Console.WriteLine("Current branch is " + branch.Name);

			//Another way to get the current branch
			Branch head = repo.Head;

			// check if head == master
			Debug.Assert(head.Name == "master");

			//Get master branch
			var master = new Branch(repo, "master");
			Debug.Assert(master == repo.Get<Branch>("master"));

			//Get the abbreviated hash of the last commit on master
			Console.WriteLine(master.CurrentCommit.ShortHash);

			//Create a new branch
			var b = GitSharp.Branch.Create(repo, "foo");

			// Switching to our new branch
			b.Checkout();

			//Check if foo is current branch
			Debug.Assert(b.IsCurrent);

			//Reset the branch to a previous commit (hard or soft or mixed)
			master.Reset("HEAD^", ResetBehavior.Hard);
			master.Reset("49322bb17d3acc9146f98c97d078513228bbf3c0", ResetBehavior.Soft);
			master.Reset("master", ResetBehavior.Mixed);
		}
	}

	internal class Using_git_commands
	{
		public void Init()
		{

			//Initializing a new repository in the current directory (if GID_DIR environment variable is not set)
			Git.Init(".");

			//Initializing a new repository in the specified location
			Git.Init("path/to/repo");

			//Initializing a new repository with options
			var cmd = new InitCommand { GitDirectory ="path/to/repo", Quiet = false, Bare = true };
			cmd.Execute();
		}

		public void Clone()
		{

			//Clone a repository from a public repository via http
			Git.Clone("git://github.com/henon/GitSharp.git", "path/to/local/copy");

			// Or using options
			Git.Clone(new CloneCommand { Source = "git://github.com/henon/GitSharp.git", GitDirectory = "path/to/local/copy", Quiet = false, Bare = true });

		}
	}
}
