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
using GitSharp.Core.Util;
using ObjectId = GitSharp.Core.ObjectId;
using CoreRef = GitSharp.Core.Ref;
using CoreCommit = GitSharp.Core.Commit;
using CoreTree = GitSharp.Core.Tree;
using CoreTag = GitSharp.Core.Tag;
using System.IO;

namespace GitSharp
{
	/// <summary>
	/// Represents a specific version of the content of a file tracked by git. Using a Blob you can access the contents of 
	/// any git object as string or byte array. For tracked files (leaves of a git tree) it returns the content of the file. For git objects 
	/// such as Commit, Tag or Tree the Blob API may be used to inspect the the uncompressed internal representation.
	/// <para/>
	/// To open a git object instantiate a Blob with the object's Hash or another valid reference (see Ref).
	/// <code>var b=new Blob(repo, "e287f54");</code>
	/// <para/>
	/// Note, that new Blob( ...) does not create a new blob in the repository but rather constructs the object to manipulate an existing blob.
	/// <para/>
	/// Advanced: To create a new Blob inside the repository you can use the static <see cref="Create(GitSharp.Repository,byte[])"/> function, however, you are advised to use 
	/// higher level functionality to create new revisions of files, i.e. by using the <see cref="Commit"/> API, for instance <see cref="Commit.Create(string,GitSharp.Commit,GitSharp.Tree)"/>.
	/// </summary>
	public class Blob : AbstractObject
	{

		internal Blob(Repository repo, ObjectId id)
			: base(repo, id)
		{
		}

		internal Blob(Repository repo, ObjectId id, byte[] blob)
			: base(repo, id)
		{
			_blob = blob;
		}

		/// <summary>
		/// Create a Blob object which represents an existing blob in the git repository
		/// </summary>
		/// <param name="repo">The repository which owns the object to load</param>
		/// <param name="hash">The SHA1 Hash of the object to load</param>
		public Blob(Repository repo, string hash) : base(repo, hash) { }

		private byte[] _blob;

		/// <summary>
		/// Get the uncompressed contents of this Blob as string. This assumes that the contents are encoded in UTF8. 
		/// </summary>
		public string Data
		{
			get
			{
				if (RawData == null)
					return null;
				return RawParseUtils.decode(RawData);
			}
		}

		/// <summary>
		/// Get the uncompressed original encoded raw data of the Blob as byte array. This is useful if the contents of the blob are encoded in some legacy encoding instead of UTF8.
		/// </summary>
		public byte[] RawData
		{
			get
			{
				if (_blob == null)
				{
					var loader = _repo._internal_repo.OpenBlob(_id);
					if (loader == null)
						return null;
					_blob = loader.Bytes;
				}
				return _blob;
			}
		}

		public override string ToString()
		{
			return "Blob[" + ShortHash + "]";
		}

		/// <summary>
		/// Create a new Blob containing the given string data as content. The string will be encoded as UTF8
		/// </summary>
		/// <param name="repo"></param>
		/// <param name="content">string to be stored in the blob</param>
		/// <returns></returns>
		public static Blob Create(Repository repo, string content)
		{
			return Create(repo, content, Encoding.UTF8);
		}

		/// <summary>
		/// Create a new Blob containing the given string data as content. The string will be encoded by the submitted encoding
		/// </summary>
		/// <param name="repo"></param>
		/// <param name="content">string to be stored in the blob</param>
		/// <param name="encoding"></param>
		/// <returns></returns>
		public static Blob Create(Repository repo, string content, Encoding encoding)
		{
			return Create(repo, encoding.GetBytes(content));
		}

		/// <summary>
		/// Create a new Blob containing the contents of the given file.
		/// </summary>
		/// <param name="repo"></param>
		/// <param name="path">Path to the file that should be stored in the blob</param>
		/// <returns></returns>
		public static Blob CreateFromFile(Repository repo, string path)
		{
			if (new FileInfo(path).Exists == false)
				throw new ArgumentException("File does not exist", "path");
			return Create(repo, File.ReadAllBytes(path));
		}

		/// <summary>
		/// Create a new Blob containing exactly the raw bytes given (before compression).
		/// </summary>
		/// <param name="repo"></param>
		/// <param name="content">Uncompressed, encoded raw data to be stored in the blob</param>
		/// <returns></returns>
		public static Blob Create(Repository repo, byte[] content)
		{
			var db = repo._internal_repo;
			var id = new GitSharp.Core.ObjectWriter(db).WriteBlob(content);
			return new Blob(repo, id, content);
		}

	}
}
