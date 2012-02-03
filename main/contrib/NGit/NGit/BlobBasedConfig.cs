/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System.IO;
using NGit;
using NGit.Revwalk;
using NGit.Treewalk;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>Configuration file based on the blobs stored in the repository.</summary>
	/// <remarks>
	/// Configuration file based on the blobs stored in the repository.
	/// This implementation currently only provides reading support, and is primarily
	/// useful for supporting the
	/// <code>.gitmodules</code>
	/// file.
	/// </remarks>
	public class BlobBasedConfig : Config
	{
		/// <summary>Parse a configuration from a byte array.</summary>
		/// <remarks>Parse a configuration from a byte array.</remarks>
		/// <param name="base">the base configuration file</param>
		/// <param name="blob">the byte array, should be UTF-8 encoded text.</param>
		/// <exception cref="NGit.Errors.ConfigInvalidException">the byte array is not a valid configuration format.
		/// 	</exception>
		public BlobBasedConfig(Config @base, byte[] blob) : base(@base)
		{
			FromText(RawParseUtils.Decode(blob));
		}

		/// <summary>Load a configuration file from a blob.</summary>
		/// <remarks>Load a configuration file from a blob.</remarks>
		/// <param name="base">the base configuration file</param>
		/// <param name="db">the repository</param>
		/// <param name="objectId">the object identifier</param>
		/// <exception cref="System.IO.IOException">the blob cannot be read from the repository.
		/// 	</exception>
		/// <exception cref="NGit.Errors.ConfigInvalidException">the blob is not a valid configuration format.
		/// 	</exception>
		public BlobBasedConfig(Config @base, Repository db, AnyObjectId objectId) : this(
			@base, Read(db, objectId))
		{
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private static byte[] Read(Repository db, AnyObjectId blobId)
		{
			ObjectReader or = db.NewObjectReader();
			try
			{
				return Read(or, blobId);
			}
			finally
			{
				or.Release();
			}
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private static byte[] Read(ObjectReader or, AnyObjectId blobId)
		{
			ObjectLoader loader = or.Open(blobId, Constants.OBJ_BLOB);
			return loader.GetCachedBytes(int.MaxValue);
		}

		/// <summary>Load a configuration file from a blob stored in a specific commit.</summary>
		/// <remarks>Load a configuration file from a blob stored in a specific commit.</remarks>
		/// <param name="base">the base configuration file</param>
		/// <param name="db">the repository containing the objects.</param>
		/// <param name="treeish">the tree (or commit) that contains the object</param>
		/// <param name="path">the path within the tree</param>
		/// <exception cref="System.IO.FileNotFoundException">the path does not exist in the commit's tree.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">the tree and/or blob cannot be accessed.</exception>
		/// <exception cref="NGit.Errors.ConfigInvalidException">the blob is not a valid configuration format.
		/// 	</exception>
		public BlobBasedConfig(Config @base, Repository db, AnyObjectId treeish, string path
			) : this(@base, Read(db, treeish, path))
		{
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private static byte[] Read(Repository db, AnyObjectId treeish, string path)
		{
			ObjectReader or = db.NewObjectReader();
			try
			{
				TreeWalk tree = TreeWalk.ForPath(or, path, AsTree(or, treeish));
				if (tree == null)
				{
					throw new FileNotFoundException(MessageFormat.Format(JGitText.Get().entryNotFoundByPath
						, path));
				}
				return Read(or, tree.GetObjectId(0));
			}
			finally
			{
				or.Release();
			}
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private static AnyObjectId AsTree(ObjectReader or, AnyObjectId treeish)
		{
			if (treeish is RevTree)
			{
				return treeish;
			}
			if (treeish is RevCommit && ((RevCommit)treeish).Tree != null)
			{
				return ((RevCommit)treeish).Tree;
			}
			return new RevWalk(or).ParseTree(treeish).Id;
		}
	}
}
