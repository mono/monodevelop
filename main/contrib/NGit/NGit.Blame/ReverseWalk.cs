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
using NGit.Blame;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Blame
{
	internal sealed class ReverseWalk : RevWalk
	{
		public ReverseWalk(Repository repo) : base(repo)
		{
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override RevCommit Next()
		{
			ReverseWalk.ReverseCommit c = (ReverseWalk.ReverseCommit)base.Next();
			if (c == null)
			{
				return null;
			}
			for (int pIdx = 0; pIdx < c.ParentCount; pIdx++)
			{
				((ReverseWalk.ReverseCommit)c.GetParent(pIdx)).AddChild(c);
			}
			return c;
		}

		protected internal override RevCommit CreateCommit(AnyObjectId id)
		{
			return new ReverseWalk.ReverseCommit(id);
		}

		[System.Serializable]
		internal sealed class ReverseCommit : RevCommit
		{
			private static readonly ReverseWalk.ReverseCommit[] NO_CHILDREN = new ReverseWalk.ReverseCommit
				[] {  };

			private ReverseWalk.ReverseCommit[] children = NO_CHILDREN;

			protected internal ReverseCommit(AnyObjectId id) : base(id)
			{
			}

			internal void AddChild(ReverseWalk.ReverseCommit c)
			{
				// Always put the most recent child onto the front of the list.
				// This works correctly because our ReverseWalk parent (above)
				// runs in COMMIT_TIME_DESC order. Older commits will be popped
				// later and should go in front of the children list so they are
				// visited first by BlameGenerator when considering candidates.
				int cnt = children.Length;
				if (cnt == 0)
				{
					children = new ReverseWalk.ReverseCommit[] { c };
				}
				else
				{
					if (cnt == 1)
					{
						children = new ReverseWalk.ReverseCommit[] { c, children[0] };
					}
					else
					{
						ReverseWalk.ReverseCommit[] n = new ReverseWalk.ReverseCommit[1 + cnt];
						n[0] = c;
						System.Array.Copy(children, 0, n, 1, cnt);
						children = n;
					}
				}
			}

			internal int GetChildCount()
			{
				return children.Length;
			}

			internal ReverseWalk.ReverseCommit GetChild(int nth)
			{
				return children[nth];
			}
		}
	}
}
