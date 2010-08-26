/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 * Copyright (C) 2010, Andrew Cooper <andymancooper@gmail.com>
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
 * - Neither the name of the TicGit project nor the
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.IO;
using System.Diagnostics;
using GitSharp.Commands;
using GitSharp.Core;
using CoreRepository = GitSharp.Core.Repository;
using System.Text.RegularExpressions;

namespace GitSharp
{
	/// <summary>
	/// Represents a git repository
	/// </summary>
	public class Repository : IDisposable
	{
		#region Constructors

		internal CoreRepository _internal_repo;

		internal Repository(CoreRepository repo)
		{
			//PreferredEncoding = Encoding.UTF8;
			_internal_repo = repo;
		}

		/// <summary>
		/// Initializes the Repository object.
		/// </summary>
		/// <param name="path">Path to the local git repository.</param>
		public Repository(string path)
			: this(GitSharp.Core.Repository.Open(path))
		{
		}


		#endregion

		#region Public properties


		/// <summary>
		/// Returns the path to the repository database (i.e. the .git directory).
		/// </summary>
		public string Directory
		{
			get
			{
				Debug.Assert(_internal_repo != null, "Repository not initialized correctly.");
				return _internal_repo.Directory.FullName;
			}
		}

		/// <summary>
		/// Get the repository's head which is a symbolic reference to the active branch.
		/// </summary>
		public Branch Head
		{
			get
			{
				Debug.Assert(_internal_repo != null, "Repository not initialized correctly.");
				return new Branch(this, "HEAD");
			}
			internal set // <-- Note that setting head does not automatically check out that branch into the repository's working directory. 
			{
				if (value == null)
					throw new ArgumentException("Value can not be null.");
				if (Head.Name != value.Name)
				{
					if (Branches.ContainsKey(value.Name))
					{
						var updateRef = _internal_repo.UpdateRef("HEAD");
						updateRef.NewObjectId = value.Target._id;
						updateRef.IsForceUpdate = true;
						updateRef.update();
						RefUpdate u = _internal_repo.UpdateRef(Constants.HEAD);
						u.link(value.Name);
					}
					else
						throw new ArgumentException("Trying to set HEAD to non existent branch: " + value.Name);
				}

			}
		}

		public Index Index
		{
			get
			{
				return new Index(this); // <--- this is just a wrapper around the internal repo's GitIndex instance so need not cache it here
			}
		}

		/// <summary>
		/// Returns true if this repository is a bare repository. Bare repositories don't have a working directory and thus do not support some operations.
		/// </summary>
		public bool IsBare
		{
			get
			{
				Debug.Assert(_internal_repo != null, "Repository not initialized correctly.");
				return _internal_repo.Config.getBoolean("core", "bare", false);
			}
		}

		/// <summary>
		/// Returns the path to the working directory (i.e. the parent of the .git directory of a non-bare repo). Returns null if it is a bare repository.
		/// </summary>
		public string WorkingDirectory
		{
			get
			{
				Debug.Assert(_internal_repo != null, "Repository not initialized correctly.");
				if (IsBare)
					return null;
				return _internal_repo.WorkingDirectory.FullName;
			}
		}

		#endregion

		/// <summary>
		/// Check out the branch with the given name into the working directory and make it the current branch.
		/// </summary>
		/// <param name="name"></param>
		public void SwitchToBranch(string name)
		{
			SwitchToBranch(new Branch(this, name));
		}

		/// <summary>
		/// Check out the given branch into the working directory and make it the current branch.
		/// </summary>
		/// <param name="branch"></param>
		public void SwitchToBranch(Branch branch)
		{
			branch.Checkout();
		}

		/// <summary>
		/// Commit staged changes and update HEAD. The default git author from the config is used.
		/// </summary>
		/// <param name="message">The commit message</param>
		/// <returns>Returns the newly created commit</returns>
		public Commit Commit(string message)
		{
			return Commit(message, new Author(Config["user.name"] ?? "unknown", Config["user.email"] ?? "unknown@(none)."));
		}

		/// <summary>
		/// Commit staged changes and update HEAD
		/// </summary>
		/// <param name="message">The commit message</param>
		/// <param name="author">The author of the content to be committed</param>
		/// <returns>Returns the newly created commit</returns>
		public Commit Commit(string message, Author author)
		{
			return Index.CommitChanges(message, author);
		}
		
		/// <summary>
		/// Commit changes in the specified files and update HEAD
		/// </summary>
		/// <param name='message'>The commit message</param>
		/// <param name="paths">List of files to commit</param>
		/// <returns>Returns the newly created commit</returns>
		public Commit Commit(string message, params string[] paths)
		{
			return Commit(message, new Author(Config["user.name"] ?? "unknown", Config["user.email"] ?? "unknown@(none)."), paths);
		}
		
		/// <summary>
		/// Commit changes in the specified files and update HEAD
		/// </summary>
		/// <param name='message'>The commit message</param>
		/// <param name="author">The author of the content to be committed</param>
		/// <param name="paths">List of files to commit</param>
		/// <returns>Returns the newly created commit</returns>
		public Commit Commit(string message, Author author, params string[] paths)
		{
			if (string.IsNullOrEmpty(message))
				throw new ArgumentException("Commit message must not be null or empty!", "message");
			if (string.IsNullOrEmpty(author.Name))
				throw new ArgumentException("Author name must not be null or empty!", "author");
			
			int basePathLength = Path.GetFullPath (WorkingDirectory).TrimEnd ('/','\\').Length;
			
			// Expand directory paths into file paths. Convert paths to full paths.
			List<string> filePaths = new List<string> ();
			foreach (var path in paths) {
				string fullPath = path;
				if (!Path.IsPathRooted (fullPath))
					fullPath = Path.Combine (WorkingDirectory, fullPath);
				fullPath = Path.GetFullPath (fullPath).TrimEnd ('/','\\');
				DirectoryInfo dir = new DirectoryInfo (fullPath);
				if (dir.Exists)
					filePaths.AddRange (GetDirectoryFiles (dir));
				else
					filePaths.Add (fullPath);
			}
			
			// Read the tree of the last commit. We are going to update it.
			GitSharp.Core.Tree tree = _internal_repo.MapTree (CurrentBranch.CurrentCommit._id);

			// Keep a list of trees that have been modified, since they have to be written.
			HashSet<GitSharp.Core.Tree> modifiedTrees = new HashSet<GitSharp.Core.Tree> ();
			
			// Update the tree
			foreach (string fullPath in filePaths) {
				string relPath = fullPath.Substring (basePathLength + 1).Replace ('\\','/');				
				TreeEntry treeEntry = tree.FindBlobMember (relPath);
				
				if (File.Exists (fullPath)) {
					// Looks like an old directory is now a file. Delete the subtree and create a new entry for the file.
					if (!(treeEntry is FileTreeEntry))
						treeEntry.Delete ();

					FileTreeEntry fileEntry = treeEntry as FileTreeEntry;
					var writer = new ObjectWriter (_internal_repo);
					bool executable = GitSharp.Core.Util.FS.canExecute (new FileInfo (fullPath));
					ObjectId id = writer.WriteBlob (new FileInfo (fullPath));
					if (fileEntry == null) {
						// It's a new file. Add it.
						fileEntry = (FileTreeEntry) tree.AddFile (relPath);
						treeEntry = fileEntry;
					} else if (fileEntry.Id == id && executable == fileEntry.IsExecutable) {
						// Same file, ignore it
						continue;
					}
					
					fileEntry.Id = id;
					fileEntry.SetExecutable (executable);
				}
				else {
					// Deleted file or directory. Remove from the tree
					if (treeEntry != null) {
						GitSharp.Core.Tree ptree = treeEntry.Parent;
						treeEntry.Delete ();
						// Remove the subtree if it's now empty
						while (ptree != null && ptree.MemberCount == 0) {
							GitSharp.Core.Tree nextParent = ptree.Parent;
							ptree.Delete ();
							ptree = nextParent;
						}
					}
					else
						continue; // Already deleted.
				}
				modifiedTrees.Add (treeEntry.Parent);
			}

			// check if tree is different from current commit's tree
			if (modifiedTrees.Count == 0)
				throw new InvalidOperationException("There are no changes to commit");
			
			// Create new trees if there is any change
			ObjectId tree_id = SaveTree (tree, modifiedTrees);

			// Create the commit
			var parent = CurrentBranch.CurrentCommit;
			var commit = GitSharp.Commit.Create(message, parent, new Tree(this, tree_id), author);
			Ref.Update("HEAD", commit);
			
			// Unstage updated files
			Index.Unstage (paths);
			
			return commit;
		}
		
		IEnumerable<string> GetDirectoryFiles (DirectoryInfo dir)
		{
			// Recursively get the list of files in the directory (which are not ignored)
			return dir.GetFiles ()
						.Where (f => !Index.IgnoreHandler.IsIgnored (f.FullName))
						.Select (f => f.FullName)
				   .Concat (dir.GetDirectories ()
						.Where (di => !Index.IgnoreHandler.IsIgnored (di.FullName) && di.Name != GitSharp.Core.Constants.DOT_GIT)
						.SelectMany (di => GetDirectoryFiles (di)));
		}
		
		ObjectId SaveTree (GitSharp.Core.Tree tree, HashSet<GitSharp.Core.Tree> modifiedTrees)
		{
			// Saves tree that have been modified (that is, which are in the provided list or
			// which have child trees that have been modified)
			
			bool childModified = false;
			foreach (var te in tree.Members) {
				GitSharp.Core.Tree childTree = te as GitSharp.Core.Tree;
				if (childTree != null) {
					ObjectId newId = SaveTree (childTree, modifiedTrees);
					if (newId != null) {
						childTree.Id = newId;
						childModified = true;
					}
				}
			}
			if (childModified || modifiedTrees.Contains (tree)) {
				ObjectWriter writer = new ObjectWriter (_internal_repo);
				return writer.WriteTree (tree);
			} else
				return null;
		}
		
		public IDictionary<string, Ref> Refs
		{
			get
			{
				var internal_refs = _internal_repo.getAllRefs();
				var dict = new Dictionary<string, Ref>(internal_refs.Count);
				foreach (var pair in internal_refs)
					dict[pair.Key] = new Ref(this, pair.Value);
				return dict;
			}
		}

		public IDictionary<string, Tag> Tags
		{
			get
			{
				var internal_tags = _internal_repo.getTags();
				var dict = new Dictionary<string, Tag>(internal_tags.Count);
				foreach (var pair in internal_tags)
					dict[pair.Key] = new Tag(this, pair.Value);
				return dict;
			}
		}

		public IDictionary<string, Branch> Branches
		{
			get
			{
				IDictionary<string, Core.Ref> internal_refs = _internal_repo._refDb.getRefs(Constants.R_HEADS);
				var dict = new Dictionary<string, Branch>(internal_refs.Count);
				foreach (var pair in internal_refs)
					dict[pair.Key.TrimStart('/')] = new Branch(this, pair.Value);
				return dict;
			}
		}

		public Branch CurrentBranch
		{
			get
			{
				return new Branch(this, _internal_repo.getBranch());
			}
		}

		public IDictionary<string, Branch> RemoteBranches
		{
			get
			{
				var internal_refs = _internal_repo._refDb.getRefs(Constants.R_REMOTES);
				var dict = new Dictionary<string, Branch>(internal_refs.Count);
				foreach (var pair in internal_refs)
				{
					var branch = new Branch(this, pair.Value);
					branch.IsRemote = true;
					dict[pair.Key] = branch;
				}
				return dict;
			}
		}

		/// <summary>
		/// Returns the git configuration containing repository-specific, user-specific and global 
		/// settings.
		/// </summary>
		public Config Config
		{
			get
			{
				return new Config(this);
			}
		}

		/// <summary>
		/// Get a report about the differences between the working directory, the index and the current commit.
		/// </summary>
		public RepositoryStatus Status
		{
			get
			{
				return new RepositoryStatus(this);
			}
		}

		public static implicit operator CoreRepository(Repository repo)
		{
			return repo._internal_repo;
		}

		public override string ToString()
		{
			return "Repository[" + Directory + "]";
		}

		public void Close()
		{
			_internal_repo.Dispose();
		}

		public void Dispose()
		{
			Close();
		}

		#region Repository initialization (git init)


		/// <summary>
		/// Initializes a non-bare repository. Use GitDirectory to specify location.
		/// </summary>
		/// <returns>The initialized repository</returns>
		public static Repository Init(string path)
		{
			return Init(path, false);
		}

		/// <summary>
		/// Initializes a repository. Use GitDirectory to specify the location. Default is the current directory.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="bare"></param>
		/// <returns></returns>
		public static Repository Init(string path, bool bare)
		{
			var cmd = new InitCommand() { GitDirectory = path, Bare = bare };
			return Init(cmd);
		}

		/// <summary>
		/// Initializes a repository in the current location using the provided git command's options.
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		public static Repository Init(InitCommand cmd)
		{
			cmd.Execute();
			return cmd.Repository;
		}

		#endregion

		/// <summary>
		/// Checks if the directory given by the path is a valid non-bare git repository. The given path may either point to 
		/// the working directory or the repository's .git directory.
		/// </summary>
		/// <param name="path"></param>
		/// <returns>Returns true if the given path is a valid git repository, false otherwise.</returns>
		public static bool IsValid(string path)
		{
			return IsValid(path, false);
		}

		/// <summary>
		/// Checks if the directory given by the path is a valid git repository.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="bare"></param>
		/// <returns>Returns true if the given path is a valid git repository, false otherwise.</returns>
		public static bool IsValid(string path, bool bare)
		{
			if (path == null)
				return false;
			if (!bare)
			{
				if (!Regex.IsMatch(path, "\\.git[/\\\\]?$"))
					path = Path.Combine(path, Constants.DOT_GIT);
			}

			if (!DirExists(path))
				return false;
			if (!FileExists(Path.Combine(path, "HEAD")))
				return false;
			if (!FileExists(Path.Combine(path, "config")))
				return false;
			//if (!DirExists(Path.Combine(path, "description")))
			//    return false;
			//if (!DirExists(Path.Combine(path, "hooks")))
			//   return false;
			//if (!DirExists(Path.Combine(path, "info")))
			//    return false;
			//if (!DirExists(Path.Combine(path, "info/exclude")))
			//    return false;
			if (!DirExists(Path.Combine(path, "objects")))
				return false;
			if (!DirExists(Path.Combine(path, "objects/info")))
				return false;
			if (!DirExists(Path.Combine(path, "objects/pack")))
				return false;
			if (!DirExists(Path.Combine(path, "refs")))
				return false;
			if (!DirExists(Path.Combine(path, "refs/heads")))
				return false;
			if (!DirExists(Path.Combine(path, "refs/tags")))
				return false;

			Repository repo = null;

			try
			{
				// let's see if it loads without throwing an exception
				repo = new Repository(path);
			}
			catch (Exception)
			{
				return false;
			}
			finally
			{
				if (repo != null)
				{
					repo.Dispose();
				}
			}
			return true;
		}

		/// <summary>
		/// Searches for a git repository starting at the given path.
		/// 
		/// Starting at the given path, searches up the file hierarchy for the next .git directory.
		/// </summary>
		/// <param name="starting_directory">The path where the search should start or null to start at the current directory</param>
		/// <returns>A path if a repository has been found or null otherwise</returns>
		public static string FindRepository(string starting_directory)
		{
			var directory = starting_directory;
			try
			{
				if (directory == null)
					directory = System.IO.Directory.GetCurrentDirectory();

				while (true)
				{
					var git = Path.Combine(directory, Constants.DOT_GIT);
					if (DirExists(git))
						return git;

					//Get parent directory
					var parent_directory = Path.Combine(Path.GetFullPath(directory), "..");
					parent_directory = Path.GetFullPath(parent_directory);
					if (parent_directory == directory) // <-- we have reached a toplevel directory which doesn't contain a .git dir.
						return null;
					directory = parent_directory;
				}
			}
			catch (ArgumentException) // --> invalid path form
			{
				return null;
			}
			catch (SecurityException) // --> access denied
			{
				return null;
			}
			catch (UnauthorizedAccessException) // --> access denied
			{
				return null;
			}
			catch (PathTooLongException)
			{
				return null;
			}
			catch (NotSupportedException) // --> hmm?
			{
				return null;
			}
		}

		private static bool DirExists(string path)
		{
			return new DirectoryInfo(path).Exists;
		}

		private static bool FileExists(string path)
		{
			return new FileInfo(path).Exists;
		}

		/// <summary>
		/// Converts a given absolute or relative path into a repository relative path. 
		/// </summary>
		protected string GetRelativePath(string path)
		{
			return Core.Util.PathUtil.RelativePath(WorkingDirectory, path);
		}

		#region Accessing git Objects

		/// <summary>
		/// Access a git object by name, id or path. Use the type parameter to tell what kind of object you like to get. Supported types are
		/// <ul>
		///   <il><see cref="Blob"/></il>
		///   <il><see cref="Branch"/></il>
		///   <il><see cref="GitSharp.Commit"/></il>
		///   <il><see cref="Leaf"/></il>
		///   <il><see cref="Tag"/></il>
		///   <il><see cref="Tree"/></il>
		///   <il><see cref="AbstractTreeNode"/> - use this if you are not sure about the type of a path (Tree or Leaf)</il>
		///   <il><see cref="AbstractObject"/> - use this if you are not sure about the type yourself. You will get back an object of the correct type (Blob, Commit, Tag or Tree).</il>
		/// </ul>
		///	<para />
		/// Branches, Commits or Tags may be accessed by name or reference expression. Currently supported are combinations of these:
		///	<ul>
		///	  <li>hash - a SHA-1 hash</li>
		///	  <li>refs/... - a ref name</li>
		///	  <li>ref^n - nth parent reference</li>
		///	  <li>ref~n - distance via parent reference</li>
		///	  <li>ref@{n} - nth version of ref</li>
		///	  <li>ref^{tree} - tree references by ref</li>
		///	  <li>ref^{commit} - commit references by ref</li>
		///	</ul>
		///	<para />
		///	Not supported is
		///	<ul>
		///    <li>abbreviated SHA-1</li>
		///	  <li>timestamps in reflogs, ref@{full or relative timestamp}</li>
		///	</ul>		
		/// <para/>
		/// Tree or Leaf objects can be addressed by long hash or by their absolute or relative repository path
		/// </summary>
		/// <returns></returns>
		public T Get<T>(string identifier) where T : class
		{
			if (typeof(T) == typeof(Blob))
				return GetBlob(identifier) as T;
			if (typeof(T) == typeof(Branch))
				return GetBranch(identifier) as T;
			if (typeof(T) == typeof(Commit))
				return GetCommit(identifier) as T;
			if (typeof(T) == typeof(Leaf))
				return GetLeaf(identifier) as T;
			if (typeof(T) == typeof(Tag))
				return GetTag(identifier) as T;
			if (typeof(T) == typeof(Tree))
				return GetTree(identifier) as T;
			if (typeof(T) == typeof(AbstractTreeNode))
				return GetTreeNode(identifier) as T;
			if (typeof(T) == typeof(AbstractObject))
				return Get(_internal_repo.Resolve(identifier)) as T;
			throw new ArgumentException("Type parameter " + typeof(T).Name + " is not supported by Get<T>!");
		}

		internal AbstractObject Get(ObjectId id)
		{
			return AbstractObject.Wrap(this, id);
		}

		internal AbstractTreeNode GetTreeNode(string path)
		{
			var obj = Head.CurrentCommit.Tree[GetRelativePath(path)];
			if (obj == null)
				return null;
			if (obj is AbstractTreeNode)
				return obj as AbstractTreeNode;
			return null;
		}

		internal Leaf GetLeaf(string path)
		{
			var obj = Head.CurrentCommit.Tree[GetRelativePath(path)];
			if (obj == null)
				return null;
			if (obj is Leaf)
				return obj as Leaf;
			return null;
		}

		internal Blob GetBlob(string path)
		{
			//if (path.Length==Constants.OBJECT_ID_LENGTH)
			var obj = Head.CurrentCommit.Tree[GetRelativePath(path)];
			if (obj == null)
				return null;
			if (obj is Leaf)
				return (obj as Leaf).Blob;
			if (obj.IsBlob)
				return (Blob)obj;
			return new Blob(this, obj._id);
		}

		internal Tag GetTag(string identifier)
		{
			return new Tag(this, identifier);
		}

		internal Branch GetBranch(string identifier)
		{
			return new Branch(this, identifier);
		}

		internal Commit GetCommit(string identifier)
		{
			return Get(_internal_repo.Resolve(identifier)) as Commit;
		}

		internal Tree GetTree(string path)
		{
			return Head.CurrentCommit.Tree[GetRelativePath(path)] as Tree;
		}


		#endregion
	}
}