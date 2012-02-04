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

using NGit;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>Interface for revision walkers that perform depth filtering.</summary>
	/// <remarks>Interface for revision walkers that perform depth filtering.</remarks>
	public interface DepthWalk
	{
		/// <returns>Depth to filter to.</returns>
		int GetDepth();

		/// <returns>flag marking commits that should become unshallow.</returns>
		RevFlag GetUnshallowFlag();

		/// <returns>flag marking commits that are interesting again.</returns>
		RevFlag GetReinterestingFlag();
	}
}
namespace NGit.Revwalk.Depthwalk {
		/// <summary>RevCommit with a depth (in commits) from a root.</summary>
		/// <remarks>RevCommit with a depth (in commits) from a root.</remarks>
		[System.Serializable]
		public class Commit : RevCommit
		{
			/// <summary>Depth of this commit in the graph, via shortest path.</summary>
			/// <remarks>Depth of this commit in the graph, via shortest path.</remarks>
			internal int depth;

			/// <returns>depth of this commit, as found by the shortest path.</returns>
			public virtual int GetDepth()
			{
				return depth;
			}

			/// <summary>Initialize a new commit.</summary>
			/// <remarks>Initialize a new commit.</remarks>
			/// <param name="id">object name for the commit.</param>
			protected internal Commit(AnyObjectId id) : base(id)
			{
				depth = -1;
			}
		}

		/// <summary>Subclass of RevWalk that performs depth filtering.</summary>
		/// <remarks>Subclass of RevWalk that performs depth filtering.</remarks>
		public class RevWalk : NGit.Revwalk.RevWalk, DepthWalk
		{
			private readonly int depth;

			private readonly RevFlag UNSHALLOW;

			private readonly RevFlag REINTERESTING;

			/// <param name="repo">Repository to walk</param>
			/// <param name="depth">Maximum depth to return</param>
			public RevWalk(Repository repo, int depth) : base(repo)
			{
				this.depth = depth;
				this.UNSHALLOW = NewFlag("UNSHALLOW");
				this.REINTERESTING = NewFlag("REINTERESTING");
			}

			/// <param name="or">ObjectReader to use</param>
			/// <param name="depth">Maximum depth to return</param>
			public RevWalk(ObjectReader or, int depth) : base(or)
			{
				this.depth = depth;
				this.UNSHALLOW = NewFlag("UNSHALLOW");
				this.REINTERESTING = NewFlag("REINTERESTING");
			}

			/// <summary>Mark a root commit (i.e., one whose depth should be considered 0.)</summary>
			/// <param name="c">Commit to mark</param>
			/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
			/// <exception cref="NGit.Errors.IncorrectObjectTypeException">NGit.Errors.IncorrectObjectTypeException
			/// 	</exception>
			/// <exception cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
			/// 	</exception>
			public virtual void MarkRoot(RevCommit c)
			{
				if (c is NGit.Revwalk.Depthwalk.Commit)
				{
					((NGit.Revwalk.Depthwalk.Commit)c).depth = 0;
				}
				base.MarkStart(c);
			}

			protected internal override RevCommit CreateCommit(AnyObjectId id)
			{
				return new NGit.Revwalk.Depthwalk.Commit(id);
			}

			public int GetDepth()
			{
				return depth;
			}

			public RevFlag GetUnshallowFlag()
			{
				return UNSHALLOW;
			}

			public RevFlag GetReinterestingFlag()
			{
				return REINTERESTING;
			}
		}

		/// <summary>Subclass of ObjectWalk that performs depth filtering.</summary>
		/// <remarks>Subclass of ObjectWalk that performs depth filtering.</remarks>
		public class ObjectWalk : NGit.Revwalk.ObjectWalk, DepthWalk
		{
			private readonly int depth;

			private readonly RevFlag UNSHALLOW;

			private readonly RevFlag REINTERESTING;

			/// <param name="repo">Repository to walk</param>
			/// <param name="depth">Maximum depth to return</param>
			public ObjectWalk(Repository repo, int depth) : base(repo)
			{
				this.depth = depth;
				this.UNSHALLOW = NewFlag("UNSHALLOW");
				this.REINTERESTING = NewFlag("REINTERESTING");
			}

			/// <param name="or">Object Reader</param>
			/// <param name="depth">Maximum depth to return</param>
			public ObjectWalk(ObjectReader or, int depth) : base(or)
			{
				this.depth = depth;
				this.UNSHALLOW = NewFlag("UNSHALLOW");
				this.REINTERESTING = NewFlag("REINTERESTING");
			}

			/// <summary>Mark a root commit (i.e., one whose depth should be considered 0.)</summary>
			/// <param name="o">Commit to mark</param>
			/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
			/// <exception cref="NGit.Errors.IncorrectObjectTypeException">NGit.Errors.IncorrectObjectTypeException
			/// 	</exception>
			/// <exception cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
			/// 	</exception>
			public virtual void MarkRoot(RevObject o)
			{
				RevObject c = o;
				while (c is RevTag)
				{
					c = ((RevTag)c).GetObject();
					ParseHeaders(c);
				}
				if (c is NGit.Revwalk.Depthwalk.Commit)
				{
					((NGit.Revwalk.Depthwalk.Commit)c).depth = 0;
				}
				base.MarkStart(o);
			}

			/// <summary>
			/// Mark an element which used to be shallow in the client, but which
			/// should now be considered a full commit.
			/// </summary>
			/// <remarks>
			/// Mark an element which used to be shallow in the client, but which
			/// should now be considered a full commit. Any ancestors of this commit
			/// should be included in the walk, even if they are the ancestor of an
			/// uninteresting commit.
			/// </remarks>
			/// <param name="c">Commit to mark</param>
			/// <exception cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
			/// 	</exception>
			/// <exception cref="NGit.Errors.IncorrectObjectTypeException">NGit.Errors.IncorrectObjectTypeException
			/// 	</exception>
			/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
			public virtual void MarkUnshallow(RevObject c)
			{
				if (c is RevCommit)
				{
					c.Add(UNSHALLOW);
				}
				base.MarkStart(c);
			}

			protected internal override RevCommit CreateCommit(AnyObjectId id)
			{
				return new NGit.Revwalk.Depthwalk.Commit(id);
			}

			public int GetDepth()
			{
				return depth;
			}

			public RevFlag GetUnshallowFlag()
			{
				return UNSHALLOW;
			}

			public RevFlag GetReinterestingFlag()
			{
				return REINTERESTING;
			}
		}

}
