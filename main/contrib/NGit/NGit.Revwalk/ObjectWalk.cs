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

using System;
using System.Collections.Generic;
using NGit;
using NGit.Errors;
using NGit.Revwalk;
using NGit.Util;
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>Specialized subclass of RevWalk to include trees, blobs and tags.</summary>
	/// <remarks>
	/// Specialized subclass of RevWalk to include trees, blobs and tags.
	/// <p>
	/// Unlike RevWalk this subclass is able to remember starting roots that include
	/// annotated tags, or arbitrary trees or blobs. Once commit generation is
	/// complete and all commits have been popped by the application, individual
	/// annotated tag, tree and blob objects can be popped through the additional
	/// method
	/// <see cref="NextObject()">NextObject()</see>
	/// .
	/// <p>
	/// Tree and blob objects reachable from interesting commits are automatically
	/// scheduled for inclusion in the results of
	/// <see cref="NextObject()">NextObject()</see>
	/// , returning
	/// each object exactly once. Objects are sorted and returned according to the
	/// the commits that reference them and the order they appear within a tree.
	/// Ordering can be affected by changing the
	/// <see cref="RevSort">RevSort</see>
	/// used to order the
	/// commits that are returned first.
	/// </remarks>
	public class ObjectWalk : RevWalk
	{
		private const int ID_SZ = 20;

		private const int TYPE_SHIFT = 12;

		private const int TYPE_TREE = (int)(((uint)0x4000) >> TYPE_SHIFT);

		private const int TYPE_SYMLINK = (int)(((uint)0xa000) >> TYPE_SHIFT);

		private const int TYPE_FILE = (int)(((uint)0x8000) >> TYPE_SHIFT);

		private const int TYPE_GITLINK = (int)(((uint)0xe000) >> TYPE_SHIFT);

		/// <summary>
		/// Indicates a non-RevCommit is in
		/// <see cref="pendingObjects">pendingObjects</see>
		/// .
		/// <p>
		/// We can safely reuse
		/// <see cref="RevWalk.REWRITE">RevWalk.REWRITE</see>
		/// here for the same value as it
		/// is only set on RevCommit and
		/// <see cref="pendingObjects">pendingObjects</see>
		/// never has RevCommit
		/// instances inserted into it.
		/// </summary>
		private const int IN_PENDING = RevWalk.REWRITE;

		private IList<RevObject> rootObjects;

		private BlockObjQueue pendingObjects;

		private RevCommit firstCommit;

		private RevCommit lastCommit;

		private ObjectWalk.TreeVisit freeVisit;

		private ObjectWalk.TreeVisit currVisit;

		private byte[] pathBuf;

		private int pathLen;

		private bool boundary;

		/// <summary>Create a new revision and object walker for a given repository.</summary>
		/// <remarks>Create a new revision and object walker for a given repository.</remarks>
		/// <param name="repo">the repository the walker will obtain data from.</param>
		public ObjectWalk(Repository repo) : this(repo.NewObjectReader())
		{
		}

		/// <summary>Create a new revision and object walker for a given repository.</summary>
		/// <remarks>Create a new revision and object walker for a given repository.</remarks>
		/// <param name="or">
		/// the reader the walker will obtain data from. The reader should
		/// be released by the caller when the walker is no longer
		/// required.
		/// </param>
		public ObjectWalk(ObjectReader or) : base(or)
		{
			rootObjects = new AList<RevObject>();
			pendingObjects = new BlockObjQueue();
			pathBuf = new byte[256];
		}

		/// <summary>Mark an object or commit to start graph traversal from.</summary>
		/// <remarks>
		/// Mark an object or commit to start graph traversal from.
		/// <p>
		/// Callers are encouraged to use
		/// <see cref="RevWalk.ParseAny(NGit.AnyObjectId)">RevWalk.ParseAny(NGit.AnyObjectId)
		/// 	</see>
		/// instead of
		/// <see cref="RevWalk.LookupAny(NGit.AnyObjectId, int)">RevWalk.LookupAny(NGit.AnyObjectId, int)
		/// 	</see>
		/// , as this method
		/// requires the object to be parsed before it can be added as a root for the
		/// traversal.
		/// <p>
		/// The method will automatically parse an unparsed object, but error
		/// handling may be more difficult for the application to explain why a
		/// RevObject is not actually valid. The object pool of this walker would
		/// also be 'poisoned' by the invalid RevObject.
		/// <p>
		/// This method will automatically call
		/// <see cref="RevWalk.MarkStart(RevCommit)">RevWalk.MarkStart(RevCommit)</see>
		/// if passed RevCommit instance, or a RevTag that directly (or indirectly)
		/// references a RevCommit.
		/// </remarks>
		/// <param name="o">
		/// the object to start traversing from. The object passed must be
		/// from this same revision walker.
		/// </param>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// the object supplied is not available from the object
		/// database. This usually indicates the supplied object is
		/// invalid, but the reference was constructed during an earlier
		/// invocation to
		/// <see cref="RevWalk.LookupAny(NGit.AnyObjectId, int)">RevWalk.LookupAny(NGit.AnyObjectId, int)
		/// 	</see>
		/// .
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the object was not parsed yet and it was discovered during
		/// parsing that it is not actually the type of the instance
		/// passed in. This usually indicates the caller used the wrong
		/// type in a
		/// <see cref="RevWalk.LookupAny(NGit.AnyObjectId, int)">RevWalk.LookupAny(NGit.AnyObjectId, int)
		/// 	</see>
		/// call.
		/// </exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual void MarkStart(RevObject o)
		{
			while (o is RevTag)
			{
				AddObject(o);
				o = ((RevTag)o).GetObject();
				ParseHeaders(o);
			}
			if (o is RevCommit)
			{
				base.MarkStart((RevCommit)o);
			}
			else
			{
				AddObject(o);
			}
		}

		/// <summary>Mark an object to not produce in the output.</summary>
		/// <remarks>
		/// Mark an object to not produce in the output.
		/// <p>
		/// Uninteresting objects denote not just themselves but also their entire
		/// reachable chain, back until the merge base of an uninteresting commit and
		/// an otherwise interesting commit.
		/// <p>
		/// Callers are encouraged to use
		/// <see cref="RevWalk.ParseAny(NGit.AnyObjectId)">RevWalk.ParseAny(NGit.AnyObjectId)
		/// 	</see>
		/// instead of
		/// <see cref="RevWalk.LookupAny(NGit.AnyObjectId, int)">RevWalk.LookupAny(NGit.AnyObjectId, int)
		/// 	</see>
		/// , as this method
		/// requires the object to be parsed before it can be added as a root for the
		/// traversal.
		/// <p>
		/// The method will automatically parse an unparsed object, but error
		/// handling may be more difficult for the application to explain why a
		/// RevObject is not actually valid. The object pool of this walker would
		/// also be 'poisoned' by the invalid RevObject.
		/// <p>
		/// This method will automatically call
		/// <see cref="RevWalk.MarkStart(RevCommit)">RevWalk.MarkStart(RevCommit)</see>
		/// if passed RevCommit instance, or a RevTag that directly (or indirectly)
		/// references a RevCommit.
		/// </remarks>
		/// <param name="o">the object to start traversing from. The object passed must be</param>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// the object supplied is not available from the object
		/// database. This usually indicates the supplied object is
		/// invalid, but the reference was constructed during an earlier
		/// invocation to
		/// <see cref="RevWalk.LookupAny(NGit.AnyObjectId, int)">RevWalk.LookupAny(NGit.AnyObjectId, int)
		/// 	</see>
		/// .
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the object was not parsed yet and it was discovered during
		/// parsing that it is not actually the type of the instance
		/// passed in. This usually indicates the caller used the wrong
		/// type in a
		/// <see cref="RevWalk.LookupAny(NGit.AnyObjectId, int)">RevWalk.LookupAny(NGit.AnyObjectId, int)
		/// 	</see>
		/// call.
		/// </exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual void MarkUninteresting(RevObject o)
		{
			while (o is RevTag)
			{
				o.flags |= UNINTERESTING;
				if (boundary)
				{
					AddObject(o);
				}
				o = ((RevTag)o).GetObject();
				ParseHeaders(o);
			}
			if (o is RevCommit)
			{
				base.MarkUninteresting((RevCommit)o);
			}
			else
			{
				if (o is RevTree)
				{
					MarkTreeUninteresting((RevTree)o);
				}
				else
				{
					o.flags |= UNINTERESTING;
				}
			}
			if (o.Type != Constants.OBJ_COMMIT && boundary)
			{
				AddObject(o);
			}
		}

		public override void Sort(RevSort s)
		{
			base.Sort(s);
			boundary = HasRevSort(RevSort.BOUNDARY);
		}

		public override void Sort(RevSort s, bool use)
		{
			base.Sort(s, use);
			boundary = HasRevSort(RevSort.BOUNDARY);
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override RevCommit Next()
		{
			for (; ; )
			{
				RevCommit r = base.Next();
				if (r == null)
				{
					if (firstCommit != null)
					{
						reader.WalkAdviceBeginTrees(this, firstCommit, lastCommit);
					}
					return null;
				}
				if ((r.flags & UNINTERESTING) != 0)
				{
					MarkTreeUninteresting(r.Tree);
					if (boundary)
					{
						return r;
					}
					continue;
				}
				if (firstCommit == null)
				{
					firstCommit = r;
				}
				lastCommit = r;
				pendingObjects.Add(r.Tree);
				return r;
			}
		}

		/// <summary>Pop the next most recent object.</summary>
		/// <remarks>Pop the next most recent object.</remarks>
		/// <returns>next most recent object; null if traversal is over.</returns>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// one or or more of the next objects are not available from the
		/// object database, but were thought to be candidates for
		/// traversal. This usually indicates a broken link.
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// one or or more of the objects in a tree do not match the type
		/// indicated.
		/// </exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual RevObject NextObject()
		{
			pathLen = 0;
			ObjectWalk.TreeVisit tv = currVisit;
			while (tv != null)
			{
				byte[] buf = tv.buf;
				for (int ptr = tv.ptr; ptr < buf.Length; )
				{
					int startPtr = ptr;
					ptr = FindObjectId(buf, ptr);
					idBuffer.FromRaw(buf, ptr);
					ptr += ID_SZ;
					RevObject obj = objects.Get(idBuffer);
					if (obj != null && (obj.flags & SEEN) != 0)
					{
						continue;
					}
					int mode = ParseMode(buf, startPtr, ptr, tv);
					int flags;
					switch ((int)(((uint)mode) >> TYPE_SHIFT))
					{
						case TYPE_FILE:
						case TYPE_SYMLINK:
						{
							if (obj == null)
							{
								obj = new RevBlob(idBuffer);
								obj.flags = SEEN;
								objects.Add(obj);
								return obj;
							}
							if (!(obj is RevBlob))
							{
								throw new IncorrectObjectTypeException(obj, Constants.OBJ_BLOB);
							}
							obj.flags = flags = obj.flags | SEEN;
							if ((flags & UNINTERESTING) == 0)
							{
								return obj;
							}
							if (boundary)
							{
								return obj;
							}
							continue;
							goto case TYPE_TREE;
						}

						case TYPE_TREE:
						{
							if (obj == null)
							{
								obj = new RevTree(idBuffer);
								obj.flags = SEEN;
								objects.Add(obj);
								return EnterTree(obj);
							}
							if (!(obj is RevTree))
							{
								throw new IncorrectObjectTypeException(obj, Constants.OBJ_TREE);
							}
							obj.flags = flags = obj.flags | SEEN;
							if ((flags & UNINTERESTING) == 0)
							{
								return EnterTree(obj);
							}
							if (boundary)
							{
								return EnterTree(obj);
							}
							continue;
							goto case TYPE_GITLINK;
						}

						case TYPE_GITLINK:
						{
							continue;
							goto default;
						}

						default:
						{
							throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().corruptObjectInvalidMode3
								, string.Format("%o", mode), idBuffer.Name, RawParseUtils.Decode(buf, tv.namePtr
								, tv.nameEnd), tv.obj));
						}
					}
				}
				currVisit = tv.parent;
				ReleaseTreeVisit(tv);
				tv = currVisit;
			}
			for (; ; )
			{
				RevObject o = pendingObjects.Next();
				if (o == null)
				{
					reader.WalkAdviceEnd();
					return null;
				}
				int flags = o.flags;
				if ((flags & SEEN) != 0)
				{
					continue;
				}
				flags |= SEEN;
				o.flags = flags;
				if ((flags & UNINTERESTING) == 0 | boundary)
				{
					if (o is RevTree)
					{
						tv = NewTreeVisit(o);
						tv.parent = null;
						currVisit = tv;
					}
					return o;
				}
			}
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private RevObject EnterTree(RevObject obj)
		{
			ObjectWalk.TreeVisit tv = NewTreeVisit(obj);
			tv.parent = currVisit;
			currVisit = tv;
			return obj;
		}

		private static int FindObjectId(byte[] buf, int ptr)
		{
			// Skip over the mode and name until the NUL before the ObjectId
			// can be located. Skip the NUL as the function returns.
			for (; ; )
			{
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
				if (buf[++ptr] == 0)
				{
					return ++ptr;
				}
			}
		}

		private static int ParseMode(byte[] buf, int startPtr, int recEndPtr, ObjectWalk.TreeVisit
			 tv)
		{
			int mode = buf[startPtr] - '0';
			for (; ; )
			{
				byte c = buf[++startPtr];
				if (' ' == c)
				{
					break;
				}
				mode <<= 3;
				mode += c - '0';
				c = buf[++startPtr];
				if (' ' == c)
				{
					break;
				}
				mode <<= 3;
				mode += c - '0';
				c = buf[++startPtr];
				if (' ' == c)
				{
					break;
				}
				mode <<= 3;
				mode += c - '0';
				c = buf[++startPtr];
				if (' ' == c)
				{
					break;
				}
				mode <<= 3;
				mode += c - '0';
				c = buf[++startPtr];
				if (' ' == c)
				{
					break;
				}
				mode <<= 3;
				mode += c - '0';
				c = buf[++startPtr];
				if (' ' == c)
				{
					break;
				}
				mode <<= 3;
				mode += c - '0';
				c = buf[++startPtr];
				if (' ' == c)
				{
					break;
				}
				mode <<= 3;
				mode += c - '0';
			}
			tv.ptr = recEndPtr;
			tv.namePtr = startPtr + 1;
			tv.nameEnd = recEndPtr - (ID_SZ + 1);
			return mode;
		}

		/// <summary>Verify all interesting objects are available, and reachable.</summary>
		/// <remarks>
		/// Verify all interesting objects are available, and reachable.
		/// <p>
		/// Callers should populate starting points and ending points with
		/// <see cref="MarkStart(RevObject)">MarkStart(RevObject)</see>
		/// and
		/// <see cref="MarkUninteresting(RevObject)">MarkUninteresting(RevObject)</see>
		/// and then use this method to verify all objects between those two points
		/// exist in the repository and are readable.
		/// <p>
		/// This method returns successfully if everything is connected; it throws an
		/// exception if there is a connectivity problem. The exception message
		/// provides some detail about the connectivity failure.
		/// </remarks>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// one or or more of the next objects are not available from the
		/// object database, but were thought to be candidates for
		/// traversal. This usually indicates a broken link.
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// one or or more of the objects in a tree do not match the type
		/// indicated.
		/// </exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual void CheckConnectivity()
		{
			for (; ; )
			{
				RevCommit c = Next();
				if (c == null)
				{
					break;
				}
			}
			for (; ; )
			{
				RevObject o = NextObject();
				if (o == null)
				{
					break;
				}
				if (o is RevBlob && !reader.Has(o))
				{
					throw new MissingObjectException(o, Constants.OBJ_BLOB);
				}
			}
		}

		/// <summary>Get the current object's complete path.</summary>
		/// <remarks>
		/// Get the current object's complete path.
		/// <p>
		/// This method is not very efficient and is primarily meant for debugging
		/// and final output generation. Applications should try to avoid calling it,
		/// and if invoked do so only once per interesting entry, where the name is
		/// absolutely required for correct function.
		/// </remarks>
		/// <returns>
		/// complete path of the current entry, from the root of the
		/// repository. If the current entry is in a subtree there will be at
		/// least one '/' in the returned string. Null if the current entry
		/// has no path, such as for annotated tags or root level trees.
		/// </returns>
		public virtual string GetPathString()
		{
			if (pathLen == 0)
			{
				pathLen = UpdatePathBuf(currVisit);
				if (pathLen == 0)
				{
					return null;
				}
			}
			return RawParseUtils.Decode(pathBuf, 0, pathLen);
		}

		/// <summary>Get the current object's path hash code.</summary>
		/// <remarks>
		/// Get the current object's path hash code.
		/// <p>
		/// This method computes a hash code on the fly for this path, the hash is
		/// suitable to cluster objects that may have similar paths together.
		/// </remarks>
		/// <returns>path hash code; any integer may be returned.</returns>
		public virtual int GetPathHashCode()
		{
			ObjectWalk.TreeVisit tv = currVisit;
			if (tv == null)
			{
				return 0;
			}
			int nameEnd = tv.nameEnd;
			if (nameEnd == 0)
			{
				// When nameEnd == 0 the subtree is itself the current path
				// being visited. The name hash must be obtained from its
				// parent tree. If there is no parent, this is a root tree with
				// a hash code of 0.
				tv = tv.parent;
				if (tv == null)
				{
					return 0;
				}
				nameEnd = tv.nameEnd;
			}
			byte[] buf;
			int ptr;
			if (16 <= (nameEnd - tv.namePtr))
			{
				buf = tv.buf;
				ptr = nameEnd - 16;
			}
			else
			{
				nameEnd = pathLen;
				if (nameEnd == 0)
				{
					nameEnd = UpdatePathBuf(currVisit);
					pathLen = nameEnd;
				}
				buf = pathBuf;
				ptr = Math.Max(0, nameEnd - 16);
			}
			int hash = 0;
			for (; ptr < nameEnd; ptr++)
			{
				byte c = buf[ptr];
				if (c != ' ')
				{
					hash = ((int)(((uint)hash) >> 2)) + (c << 24);
				}
			}
			return hash;
		}

		/// <returns>the internal buffer holding the current path.</returns>
		public virtual byte[] GetPathBuffer()
		{
			if (pathLen == 0)
			{
				pathLen = UpdatePathBuf(currVisit);
			}
			return pathBuf;
		}

		/// <returns>
		/// length of the path in
		/// <see cref="GetPathBuffer()">GetPathBuffer()</see>
		/// .
		/// </returns>
		public virtual int GetPathLength()
		{
			if (pathLen == 0)
			{
				pathLen = UpdatePathBuf(currVisit);
			}
			return pathLen;
		}

		private int UpdatePathBuf(ObjectWalk.TreeVisit tv)
		{
			if (tv == null)
			{
				return 0;
			}
			// If nameEnd == 0 this tree has not yet contributed an entry.
			// Update only for the parent, which if null will be empty.
			int nameEnd = tv.nameEnd;
			if (nameEnd == 0)
			{
				return UpdatePathBuf(tv.parent);
			}
			int ptr = tv.pathLen;
			if (ptr == 0)
			{
				ptr = UpdatePathBuf(tv.parent);
				if (ptr == pathBuf.Length)
				{
					GrowPathBuf(ptr);
				}
				if (ptr != 0)
				{
					pathBuf[ptr++] = (byte)('/');
				}
				tv.pathLen = ptr;
			}
			int namePtr = tv.namePtr;
			int nameLen = nameEnd - namePtr;
			int end = ptr + nameLen;
			while (pathBuf.Length < end)
			{
				GrowPathBuf(ptr);
			}
			System.Array.Copy(tv.buf, namePtr, pathBuf, ptr, nameLen);
			return end;
		}

		private void GrowPathBuf(int ptr)
		{
			byte[] newBuf = new byte[pathBuf.Length << 1];
			System.Array.Copy(pathBuf, 0, newBuf, 0, ptr);
			pathBuf = newBuf;
		}

		public override void Dispose()
		{
			base.Dispose();
			pendingObjects = new BlockObjQueue();
			firstCommit = null;
			lastCommit = null;
			currVisit = null;
			freeVisit = null;
		}

		protected internal override void Reset(int retainFlags)
		{
			base.Reset(retainFlags);
			foreach (RevObject obj in rootObjects)
			{
				obj.flags &= ~IN_PENDING;
			}
			rootObjects = new AList<RevObject>();
			pendingObjects = new BlockObjQueue();
			firstCommit = null;
			lastCommit = null;
			currVisit = null;
			freeVisit = null;
		}

		private void AddObject(RevObject o)
		{
			if ((o.flags & IN_PENDING) == 0)
			{
				o.flags |= IN_PENDING;
				rootObjects.AddItem(o);
				pendingObjects.Add(o);
			}
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private void MarkTreeUninteresting(RevTree tree)
		{
			if ((tree.flags & UNINTERESTING) != 0)
			{
				return;
			}
			tree.flags |= UNINTERESTING;
			byte[] raw = reader.Open(tree, Constants.OBJ_TREE).GetCachedBytes();
			for (int ptr = 0; ptr < raw.Length; )
			{
				byte c = raw[ptr];
				int mode = c - '0';
				for (; ; )
				{
					c = raw[++ptr];
					if (' ' == c)
					{
						break;
					}
					mode <<= 3;
					mode += c - '0';
				}
				while (raw[++ptr] != 0)
				{
				}
				// Skip entry name.
				ptr++;
				switch ((int)(((uint)mode) >> TYPE_SHIFT))
				{
					case TYPE_FILE:
					case TYPE_SYMLINK:
					{
						// Skip NUL after entry name.
						idBuffer.FromRaw(raw, ptr);
						LookupBlob(idBuffer).flags |= UNINTERESTING;
						break;
					}

					case TYPE_TREE:
					{
						idBuffer.FromRaw(raw, ptr);
						MarkTreeUninteresting(LookupTree(idBuffer));
						break;
					}

					case TYPE_GITLINK:
					{
						break;
					}

					default:
					{
						idBuffer.FromRaw(raw, ptr);
						throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().corruptObjectInvalidMode3
							, string.Format("%o", mode), idBuffer.Name, string.Empty, tree));
					}
				}
				ptr += ID_SZ;
			}
		}

		/// <exception cref="NGit.Errors.LargeObjectException"></exception>
		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private ObjectWalk.TreeVisit NewTreeVisit(RevObject obj)
		{
			ObjectWalk.TreeVisit tv = freeVisit;
			if (tv != null)
			{
				freeVisit = tv.parent;
				tv.ptr = 0;
				tv.namePtr = 0;
				tv.nameEnd = 0;
				tv.pathLen = 0;
			}
			else
			{
				tv = new ObjectWalk.TreeVisit();
			}
			tv.obj = obj;
			tv.buf = reader.Open(obj, Constants.OBJ_TREE).GetCachedBytes();
			return tv;
		}

		private void ReleaseTreeVisit(ObjectWalk.TreeVisit tv)
		{
			tv.buf = null;
			tv.parent = freeVisit;
			freeVisit = tv;
		}

		private class TreeVisit
		{
			/// <summary>Parent tree visit that entered this tree, null if root tree.</summary>
			/// <remarks>Parent tree visit that entered this tree, null if root tree.</remarks>
			internal ObjectWalk.TreeVisit parent;

			/// <summary>The RevTree currently being iterated through.</summary>
			/// <remarks>The RevTree currently being iterated through.</remarks>
			internal RevObject obj;

			/// <summary>
			/// Canonical encoding of the tree named by
			/// <see cref="obj">obj</see>
			/// .
			/// </summary>
			internal byte[] buf;

			/// <summary>
			/// Index of next entry to parse in
			/// <see cref="buf">buf</see>
			/// .
			/// </summary>
			internal int ptr;

			/// <summary>
			/// Start of the current name entry in
			/// <see cref="buf">buf</see>
			/// .
			/// </summary>
			internal int namePtr;

			/// <summary>
			/// One past end of name,
			/// <code>nameEnd - namePtr</code>
			/// is the length.
			/// </summary>
			internal int nameEnd;

			/// <summary>Number of bytes in the path leading up to this tree.</summary>
			/// <remarks>Number of bytes in the path leading up to this tree.</remarks>
			internal int pathLen;
		}
	}
}
