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
using NGit.Errors;
using NGit.Revwalk;
using NGit.Treewalk;
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

		private static readonly byte[] EMPTY_PATH = new byte[] {  };

		private CanonicalTreeParser treeWalk;

		private IList<RevObject> rootObjects;

		private BlockObjQueue pendingObjects;

		private RevTree currentTree;

		private RevObject last;

		private RevCommit firstCommit;

		private RevCommit lastCommit;

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
			treeWalk = new CanonicalTreeParser();
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
				if (HasRevSort(RevSort.BOUNDARY))
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
			if (o.Type != Constants.OBJ_COMMIT && HasRevSort(RevSort.BOUNDARY))
			{
				AddObject(o);
			}
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
					return null;
				}
				if ((r.flags & UNINTERESTING) != 0)
				{
					MarkTreeUninteresting(r.Tree);
					if (HasRevSort(RevSort.BOUNDARY))
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
			if (last != null)
			{
				treeWalk = last is RevTree ? Enter(last) : treeWalk.Next();
			}
			while (!treeWalk.Eof)
			{
				FileMode mode = treeWalk.EntryFileMode;
				switch (mode.GetObjectType())
				{
					case Constants.OBJ_BLOB:
					{
						treeWalk.GetEntryObjectId(idBuffer);
						RevBlob o = LookupBlob(idBuffer);
						if ((o.flags & SEEN) != 0)
						{
							break;
						}
						o.flags |= SEEN;
						if (ShouldSkipObject(o))
						{
							break;
						}
						last = o;
						return o;
					}

					case Constants.OBJ_TREE:
					{
						treeWalk.GetEntryObjectId(idBuffer);
						RevTree o = LookupTree(idBuffer);
						if ((o.flags & SEEN) != 0)
						{
							break;
						}
						o.flags |= SEEN;
						if (ShouldSkipObject(o))
						{
							break;
						}
						last = o;
						return o;
					}

					default:
					{
						if (FileMode.GITLINK.Equals(mode))
						{
							break;
						}
						treeWalk.GetEntryObjectId(idBuffer);
						throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().corruptObjectInvalidMode3
							, mode, idBuffer.Name, treeWalk.EntryPathString, currentTree.Name));
					}
				}
				treeWalk = treeWalk.Next();
			}
			if (firstCommit != null)
			{
				reader.WalkAdviceBeginTrees(this, firstCommit, lastCommit);
				firstCommit = null;
				lastCommit = null;
			}
			last = null;
			for (; ; )
			{
				RevObject o = pendingObjects.Next();
				if (o == null)
				{
					reader.WalkAdviceEnd();
					return null;
				}
				if ((o.flags & SEEN) != 0)
				{
					continue;
				}
				o.flags |= SEEN;
				if (ShouldSkipObject(o))
				{
					continue;
				}
				if (o is RevTree)
				{
					currentTree = (RevTree)o;
					treeWalk = treeWalk.ResetRoot(reader, currentTree);
				}
				return o;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private CanonicalTreeParser Enter(RevObject tree)
		{
			CanonicalTreeParser p = treeWalk.CreateSubtreeIterator0(reader, tree);
			if (p.Eof)
			{
				// We can't tolerate the subtree being an empty tree, as
				// that will break us out early before we visit all names.
				// If it is, advance to the parent's next record.
				//
				return treeWalk.Next();
			}
			return p;
		}

		private bool ShouldSkipObject(RevObject o)
		{
			return (o.flags & UNINTERESTING) != 0 && !HasRevSort(RevSort.BOUNDARY);
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
					throw new MissingObjectException(o, Constants.TYPE_BLOB);
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
			return last != null ? treeWalk.EntryPathString : null;
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
			return last != null ? treeWalk.GetEntryPathHashCode() : 0;
		}

		/// <returns>the internal buffer holding the current path.</returns>
		public virtual byte[] GetPathBuffer()
		{
			return last != null ? treeWalk.GetEntryPathBuffer() : EMPTY_PATH;
		}

		/// <returns>
		/// length of the path in
		/// <see cref="GetPathBuffer()">GetPathBuffer()</see>
		/// .
		/// </returns>
		public virtual int GetPathLength()
		{
			return last != null ? treeWalk.GetEntryPathLength() : 0;
		}

		public override void Dispose()
		{
			base.Dispose();
			pendingObjects = new BlockObjQueue();
			treeWalk = new CanonicalTreeParser();
			currentTree = null;
			last = null;
			firstCommit = null;
			lastCommit = null;
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
			treeWalk = new CanonicalTreeParser();
			currentTree = null;
			last = null;
			firstCommit = null;
			lastCommit = null;
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
			treeWalk = treeWalk.ResetRoot(reader, tree);
			while (!treeWalk.Eof)
			{
				FileMode mode = treeWalk.EntryFileMode;
				int sType = mode.GetObjectType();
				switch (sType)
				{
					case Constants.OBJ_BLOB:
					{
						treeWalk.GetEntryObjectId(idBuffer);
						LookupBlob(idBuffer).flags |= UNINTERESTING;
						break;
					}

					case Constants.OBJ_TREE:
					{
						treeWalk.GetEntryObjectId(idBuffer);
						RevTree t = LookupTree(idBuffer);
						if ((t.flags & UNINTERESTING) == 0)
						{
							t.flags |= UNINTERESTING;
							treeWalk = treeWalk.CreateSubtreeIterator0(reader, t);
							continue;
						}
						break;
					}

					default:
					{
						if (FileMode.GITLINK.Equals(mode))
						{
							break;
						}
						treeWalk.GetEntryObjectId(idBuffer);
						throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().corruptObjectInvalidMode3
							, mode, idBuffer.Name, treeWalk.EntryPathString, tree));
					}
				}
				treeWalk = treeWalk.Next();
			}
		}
	}
}
