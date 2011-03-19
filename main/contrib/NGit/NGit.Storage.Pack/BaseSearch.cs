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

using System.Collections.Generic;
using NGit;
using NGit.Revwalk;
using NGit.Storage.Pack;
using NGit.Treewalk;
using Sharpen;

namespace NGit.Storage.Pack
{
	internal class BaseSearch
	{
		private static readonly int M_BLOB = FileMode.REGULAR_FILE.GetBits();

		private static readonly int M_TREE = FileMode.TREE.GetBits();

		private readonly ProgressMonitor progress;

		private readonly ObjectReader reader;

		private readonly ObjectId[] baseTrees;

		private readonly ObjectIdOwnerMap<ObjectToPack> objectsMap;

		private readonly IList<ObjectToPack> edgeObjects;

		private readonly IntSet alreadyProcessed;

		private readonly ObjectIdOwnerMap<BaseSearch.TreeWithData> treeCache;

		private readonly CanonicalTreeParser parser;

		private readonly MutableObjectId idBuf;

		internal BaseSearch(ProgressMonitor countingMonitor, ICollection<RevTree> bases, 
			ObjectIdOwnerMap<ObjectToPack> objects, IList<ObjectToPack> edges, ObjectReader 
			or)
		{
			progress = countingMonitor;
			reader = or;
			baseTrees = Sharpen.Collections.ToArray(bases, new ObjectId[bases.Count]);
			objectsMap = objects;
			edgeObjects = edges;
			alreadyProcessed = new IntSet();
			treeCache = new ObjectIdOwnerMap<BaseSearch.TreeWithData>();
			parser = new CanonicalTreeParser();
			idBuf = new MutableObjectId();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void AddBase(int objectType, byte[] pathBuf, int pathLen, int pathHash
			)
		{
			int tailMode = ModeForType(objectType);
			if (tailMode == 0)
			{
				return;
			}
			if (!alreadyProcessed.Add(pathHash))
			{
				return;
			}
			if (pathLen == 0)
			{
				foreach (ObjectId root in baseTrees)
				{
					Add(root, Constants.OBJ_TREE, pathHash);
				}
				return;
			}
			int firstSlash = NextSlash(pathBuf, 0, pathLen);
			foreach (ObjectId root_1 in baseTrees)
			{
				int ptr = 0;
				int end = firstSlash;
				int mode = end != pathLen ? M_TREE : tailMode;
				parser.Reset(ReadTree(root_1));
				while (!parser.Eof)
				{
					int cmp = parser.PathCompare(pathBuf, ptr, end, mode);
					if (cmp < 0)
					{
						parser.Next();
						continue;
					}
					if (cmp > 0)
					{
						break;
					}
					if (end == pathLen)
					{
						if (parser.EntryFileMode.GetObjectType() == objectType)
						{
							idBuf.FromRaw(parser.IdBuffer, parser.IdOffset);
							Add(idBuf, objectType, pathHash);
						}
						break;
					}
					if (!FileMode.TREE.Equals(parser.EntryRawMode))
					{
						break;
					}
					ptr = end + 1;
					end = NextSlash(pathBuf, ptr, pathLen);
					mode = end != pathLen ? M_TREE : tailMode;
					idBuf.FromRaw(parser.IdBuffer, parser.IdOffset);
					parser.Reset(ReadTree(idBuf));
				}
			}
CHECK_BASE_break: ;
		}

		private static int ModeForType(int typeCode)
		{
			switch (typeCode)
			{
				case Constants.OBJ_TREE:
				{
					return M_TREE;
				}

				case Constants.OBJ_BLOB:
				{
					return M_BLOB;
				}

				default:
				{
					return 0;
					break;
				}
			}
		}

		private static int NextSlash(byte[] pathBuf, int ptr, int end)
		{
			while (ptr < end && pathBuf[ptr] != '/')
			{
				ptr++;
			}
			return ptr;
		}

		private void Add(AnyObjectId id, int objectType, int pathHash)
		{
			ObjectToPack obj = new ObjectToPack(id, objectType);
			obj.SetEdge();
			obj.SetPathHash(pathHash);
			if (objectsMap.AddIfAbsent(obj) == obj)
			{
				edgeObjects.AddItem(obj);
				progress.Update(1);
			}
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private byte[] ReadTree(AnyObjectId id)
		{
			BaseSearch.TreeWithData tree = treeCache.Get(id);
			if (tree != null)
			{
				return tree.buf;
			}
			ObjectLoader ldr = reader.Open(id, Constants.OBJ_TREE);
			byte[] buf = ldr.GetCachedBytes(int.MaxValue);
			treeCache.Add(new BaseSearch.TreeWithData(id, buf));
			return buf;
		}

		[System.Serializable]
		private class TreeWithData : ObjectIdOwnerMap.Entry
		{
			internal readonly byte[] buf;

			internal TreeWithData(AnyObjectId id, byte[] buf) : base(id)
			{
				this.buf = buf;
			}
		}
	}
}
