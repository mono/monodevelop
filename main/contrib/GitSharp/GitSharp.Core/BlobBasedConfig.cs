/*
 * Copyright (C) 2009, JetBrains s.r.o.
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

using System.IO;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core
{
	/// <summary>
	/// The configuration file based on the blobs stored in the repository.
	/// </summary>
	public class BlobBasedConfig : Config
	{
		///	<summary>
		/// The constructor from a byte array
		/// </summary>
		///	<param name="base">the base configuration file </param>
		///	<param name="blob">the byte array, should be UTF-8 encoded text. </param>
		///	<exception cref="ConfigInvalidException">
		///	The byte array is not a valid configuration format.
		/// </exception>

		public BlobBasedConfig(Config @base, byte[] blob)
			: base(@base)
		{
			fromText(RawParseUtils.decode(blob));
		}

		///	<summary> * The constructor from object identifier
		///	</summary>
		///	<param name="base">the base configuration file </param>
		///	<param name="repo">the repository</param>
		/// <param name="objectid">the object identifier</param>
		/// <exception cref="IOException">
		/// the blob cannot be read from the repository. </exception>
		/// <exception cref="ConfigInvalidException">
		/// the blob is not a valid configuration format.
		/// </exception> 
		public BlobBasedConfig(Config @base, Repository repo, ObjectId objectid)
			: base(@base)
		{
			if (repo == null)
			{
				throw new System.ArgumentNullException("repo");
			}
			
			ObjectLoader loader = repo.OpenBlob(objectid);
			if (loader == null)
			{
				throw new IOException("Blob not found: " + objectid);
			}
			fromText(RawParseUtils.decode(loader.Bytes));
		}

		///	<summary>
		/// The constructor from commit and path
		///	</summary>
		///	<param name="base">The base configuration file</param>
		///	<param name="commit">The commit that contains the object</param>
		///	<param name="path">The path within the tree of the commit</param>
		/// <exception cref="FileNotFoundException">
		/// the path does not exist in the commit's tree.
		/// </exception>
		/// <exception cref="IOException">
		/// the tree and/or blob cannot be accessed.
		/// </exception>
		/// <exception cref="ConfigInvalidException">
		/// the blob is not a valid configuration format.
		/// </exception>
		public BlobBasedConfig(Config @base, Commit commit, string path)
			: base(@base)
		{
			if (commit == null)
			{
				throw new System.ArgumentNullException("commit");
			}
			
			ObjectId treeId = commit.TreeId;
			Repository r = commit.Repository;
			TreeWalk.TreeWalk tree = TreeWalk.TreeWalk.ForPath(r, path, treeId);
			if (tree == null)
			{
				throw new FileNotFoundException("Entry not found by path: " + path);
			}
			ObjectId blobId = tree.getObjectId(0);
			ObjectLoader loader = tree.Repository.OpenBlob(blobId);
			
			if (loader == null)
			{
				throw new IOException("Blob not found: " + blobId + " for path: " + path);
			}

			fromText(RawParseUtils.decode(loader.Bytes));
		}
	}
}