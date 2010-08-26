/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CoreCommit = GitSharp.Core.Commit;

namespace GitSharp
{

	/// <summary>
	/// Represents the index of a git repository which keeps track of changes that are about to be committed.
	/// </summary>
	public class Index
	{
		private Repository _repo;

		public Index(Repository repo)
		{
			_repo = repo;
			//GitIndex.FilenameEncoding = repo.PreferredEncoding;
			//if (_repo.PreferredEncoding != Encoding.UTF8 && _repo.PreferredEncoding != Encoding.Default)
			//   GitIndex.FilenameEncoding = Encoding.Default;
		}

		static Index()
		{
			PathEncoding = Encoding.UTF8;
			ContentEncoding = Encoding.UTF8;
		}

		internal GitSharp.Core.GitIndex GitIndex
		{
			get
			{
				return _repo._internal_repo.Index;
			}
		}

		/// <summary>
		/// Add all untracked files to the index and stage all changes (like git add .)
		/// </summary>
		public void AddAll()
		{
			Add(_repo.WorkingDirectory);
		}

		/// <summary>
		/// Adds untracked files or directories to the index and writes the index to the disk (like "git add").
		/// For tracked files that were modified, it stages the modification. Is a no-op for tracked files that were
		/// not modified.
		/// 
		/// Note: Add as many files as possible by one call of this method for best performance.
		/// </summary>
		/// <param name="paths">Paths to add to the index</param>
		public void Add(params string[] paths)
		{
			GitIndex.RereadIfNecessary();
			foreach (var absolute_or_relative_path in paths)
			{
				string path = absolute_or_relative_path;
				if (!Path.IsPathRooted(absolute_or_relative_path))
					path = Path.Combine(_repo.WorkingDirectory, path);
				if (new FileInfo(path).Exists)
					AddFile(new FileInfo(path));
				else if (new DirectoryInfo(path).Exists)
					AddDirectory(new DirectoryInfo(path));
				else
					throw new ArgumentException("File or directory at <" + path + "> doesn't seem to exist.", "path");
			}
			GitIndex.write();
		}

		/// <summary>
		/// Add a file to index (without relying on the working directory) by specifying the file's content as string. 
		/// The added file doesn't need to exist in the working directory.
		/// </summary>
		/// <param name="path">Relative path in the working directory. Note: the path is encoded using PathEncoding</param>
		/// <param name="content">The content as string. Note: the content is encoded using ContentEncoding</param>
		public void AddContent(string path, string content)
		{
			AddContent(PathEncoding.GetBytes(path), ContentEncoding.GetBytes(content));
		}

		/// <summary>
		/// Add content to the index directly without the need for a file in the working directory.
		/// </summary>
		/// <param name="encoded_relative_filepath">encoded file path (relative to working directory)</param>
		/// <param name="encoded_content">encoded content</param>
		public void AddContent(byte[] encoded_relative_filepath, byte[] encoded_content)
		{
			GitIndex.RereadIfNecessary();
			GitIndex.add(encoded_relative_filepath, encoded_content);
			GitIndex.write();
		}

		private void AddFile(FileInfo path)
		{
			GitIndex.add(_repo._internal_repo.WorkingDirectory, path);
		}

		private GitSharp.Core.IgnoreHandler _ignoreHandler;
		public GitSharp.Core.IgnoreHandler IgnoreHandler
		{
			get
			{
				if (_ignoreHandler == null)
					_ignoreHandler = new Core.IgnoreHandler(_repo);
				return _ignoreHandler;
			}
		}

		private void AddDirectory(DirectoryInfo dir)
		{
			foreach (var file in dir.GetFiles())
				if (!IgnoreHandler.IsIgnored(file.FullName))
					AddFile(file);
			foreach (var subdir in dir.GetDirectories())
				if (subdir.Name != GitSharp.Core.Constants.DOT_GIT && !IgnoreHandler.IsIgnored(subdir.FullName))
					AddDirectory(subdir);
		}

		/// <summary>
		/// Removes files or directories from the index which are no longer to be tracked. 
		/// Does not delete files from the working directory. Use <seealso cref="Delete"/> to remove and delete files.
		/// </summary>
		/// <param name="paths"></param>
		public void Remove(params string[] paths)
		{
			GitIndex.RereadIfNecessary();
			foreach (var absolute_or_relative_path in paths)
			{
				string path = absolute_or_relative_path;
				string relative_path = absolute_or_relative_path;
				if (!Path.IsPathRooted(absolute_or_relative_path))
					path = Path.Combine(_repo.WorkingDirectory, absolute_or_relative_path);
				else
					relative_path = Core.Util.PathUtil.RelativePath(_repo.WorkingDirectory, absolute_or_relative_path);
				if (new FileInfo(path).Exists)
					RemoveFile(new FileInfo(path), false);
				else if (new DirectoryInfo(path).Exists)
					RemoveDirectory(new DirectoryInfo(path), false);
				else
					GitIndex.Remove(relative_path);
			}
			GitIndex.write();
		}

		/// <summary>
		/// Removes files or directories from the index and delete them from the working directory.
		/// 
		/// </summary>
		/// <param name="paths"></param>
		public void Delete(params string[] paths)
		{
			GitIndex.RereadIfNecessary();
			foreach (var absolute_or_relative_path in paths)
			{
				string path = absolute_or_relative_path;
				if (!Path.IsPathRooted(absolute_or_relative_path))
					path = Path.Combine(_repo.WorkingDirectory, path);
				if (new FileInfo(path).Exists)
					RemoveFile(new FileInfo(path), true);
				else if (new DirectoryInfo(path).Exists)
					RemoveDirectory(new DirectoryInfo(path), true);
				else
					throw new ArgumentException("File or directory at <" + path + "> doesn't seem to exist.", "path");
			}
			GitIndex.write();
		}

		private void RemoveFile(FileInfo path, bool delete_file)
		{
			GitIndex.remove(_repo._internal_repo.WorkingDirectory, path); // Todo: change GitIndex.Remove to remove(DirectoryInfo , FileInfo) ??
			if (delete_file)
				path.Delete();
		}

		private void RemoveDirectory(DirectoryInfo dir, bool delete_dir)
		{
			foreach (var file in dir.GetFiles())
				RemoveFile(file, delete_dir);
			foreach (var subdir in dir.GetDirectories())
				RemoveDirectory(subdir, delete_dir);
			if (delete_dir)
				dir.Delete(true);
		}

		/// <summary>
		/// Stages the given files. Untracked files are added. This is an alias for Add.
		/// </summary>
		/// <param name="paths"></param>
		public void Stage(params string[] paths)
		{
			Add(paths);
		}

		/// <summary>
		/// This is an alias for AddContent.
		/// </summary>
		public void StageContent(string path, string content)
		{
			AddContent(path, content);
		}

		/// <summary>
		/// Unstage overwrites staged files in the index with their current version in HEAD. In case of newly added files they are removed from the index.
		/// </summary>
		/// <param name="paths">Relative paths to files you want to unstage.</param>
		public void Unstage(params string[] paths)
		{
			GitIndex.RereadIfNecessary();
			foreach (var absolute_or_relative_path in paths)
			{
				string path = absolute_or_relative_path;
				if (Path.IsPathRooted(absolute_or_relative_path))
					path = Core.Util.PathUtil.RelativePath(_repo.WorkingDirectory, absolute_or_relative_path);
				if (this[path] == null)
					return;
				var blob = _repo.Get<Leaf>(path); // <--- we wouldn't want to stage something that is not representing a file
				if (blob == null)
					GitIndex.Remove(path);
				else
					GitIndex.add(Core.Repository.GitInternalSlash(PathEncoding.GetBytes(path)), blob.RawData);
			}
			GitIndex.write();
		}

		/// <summary>
		/// Check out the index into the working directory. Any modified files will be overwritten.
		/// <para/>
		/// <seealso cref="Branch.Checkout"/> to checkout from a commit.
		/// </summary>
		public void Checkout()
		{
			Checkout(_repo.WorkingDirectory);
		}

		// [henon] we do not publicly expose checking out into a custom directory, as this is an unrealistic use case and conflicts with checking out paths. 
		// it is possible anyway by iterating over the Entries and writing the contents of each entry into a custom directory!
		private void Checkout(string directory)
		{
			GitIndex.RereadIfNecessary();
			GitIndex.checkout(new FileInfo(directory));
		}

		/// <summary>
		/// Check out given paths from the index overwriting files in the working directory. Modified files might be overwritten.
		/// </summary>
		/// <param name="paths"></param>
		public void Checkout(params string[] paths)
		{
			GitIndex.RereadIfNecessary();
			foreach (var absolute_or_relative_path in paths)
			{
				string path = absolute_or_relative_path;
				if (Path.IsPathRooted(absolute_or_relative_path))
					path = Core.Util.PathUtil.RelativePath(_repo.WorkingDirectory, absolute_or_relative_path);
				var e = GitIndex.GetEntry(path);
				if (e == null)
					continue;
				GitIndex.checkoutEntry(new FileInfo(_repo.WorkingDirectory), e);
			}
		}

		/// <summary>
		/// Writes the index to the disk.
		/// </summary>
		public void Write()
		{
			GitIndex.write();
		}

		/// <summary>
		/// Reads the index from the disk
		/// </summary>
		public void Read()
		{
			GitIndex.Read();
		}

		//public RepositoryStatus CompareAgainstWorkingDirectory(bool honor_ignore_rules)

		public RepositoryStatus Status
		{
			get
			{
				return _repo.Status;
			}
		}

		/// <summary>
		/// Returns true if the index has been changed, which means there are changes to be committed. This
		/// is not to be confused with the status of the working directory. If changes in the working directory have not been
		/// staged then IsChanged is false.
		/// </summary>
		public bool IsChanged
		{
			get
			{
				return GitIndex.IsChanged;
			}
		}

		public Commit CommitChanges(string message, Author author)
		{
			if (string.IsNullOrEmpty(message))
				throw new ArgumentException("Commit message must not be null or empty!", "message");
			if (string.IsNullOrEmpty(author.Name))
				throw new ArgumentException("Author name must not be null or empty!", "author");
			GitIndex.RereadIfNecessary();
			var tree_id = GitIndex.writeTree();
			// check if tree is different from current commit's tree
			var parent = _repo.CurrentBranch.CurrentCommit;
			if ((parent == null && GitIndex.Members.Count == 0) || (parent != null && parent.Tree._id == tree_id))
				throw new InvalidOperationException("There are no changes to commit");
			var commit = Commit.Create(message, parent, new Tree(_repo, tree_id), author);
			Ref.Update("HEAD", commit);
			return commit;
		}

		public override string ToString()
		{
			return "Index[" + Path.Combine(_repo.Directory, "index") + "]";
		}

		/// <summary>
		/// The encoding to be used to convert file paths from string to byte arrays.
		/// </summary>
		public static Encoding PathEncoding
		{
			get;
			set;
		}

		/// <summary>
		/// The encoding to be used to convert file contents from string to byte arrays.
		/// </summary>
		public static Encoding ContentEncoding
		{
			get;
			set;
		}


		public string GetContent(string path)
		{
			var blob = this[path];
			if (blob == null)
				return null;
			return ContentEncoding.GetString(blob.RawData);
		}

		public Blob this[string path]
		{
			get
			{
				var e = GitIndex.GetEntry(path);
				if (e == null)
					return null;
				return new Blob(_repo, e.ObjectId);
			}
			set
			{
				//todo
			}
		}

		public IEnumerable<string> Entries
		{
			get
			{
				GitIndex.RereadIfNecessary();
				return GitIndex.Members.Select(e => e.Name).ToArray();
			}
		}

		/// <summary>
		/// The number of files tracked by the repository 
		/// </summary>
		public int Size
		{
			get { return GitIndex.Members.Count; }
		}
	}
}
