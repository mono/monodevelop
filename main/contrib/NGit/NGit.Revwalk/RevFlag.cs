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
using NGit.Internal;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>
	/// Application level mark bit for
	/// <see cref="RevObject">RevObject</see>
	/// s.
	/// <p>
	/// To create a flag use
	/// <see cref="RevWalk.NewFlag(string)">RevWalk.NewFlag(string)</see>
	/// .
	/// </summary>
	public class RevFlag
	{
		/// <summary>
		/// Uninteresting by
		/// <see cref="RevWalk.MarkUninteresting(RevCommit)">RevWalk.MarkUninteresting(RevCommit)
		/// 	</see>
		/// .
		/// <p>
		/// We flag commits as uninteresting if the caller does not want commits
		/// reachable from a commit to
		/// <see cref="RevWalk.MarkUninteresting(RevCommit)">RevWalk.MarkUninteresting(RevCommit)
		/// 	</see>
		/// .
		/// This flag is always carried into the commit's parents and is a key part
		/// of the "rev-list B --not A" feature; A is marked UNINTERESTING.
		/// <p>
		/// This is a static flag. Its RevWalk is not available.
		/// </summary>
		public static readonly NGit.Revwalk.RevFlag UNINTERESTING = new RevFlag.StaticRevFlag
			("UNINTERESTING", RevWalk.UNINTERESTING);

		internal readonly RevWalk walker;

		internal readonly string name;

		internal readonly int mask;

		internal RevFlag(RevWalk w, string n, int m)
		{
			walker = w;
			name = n;
			mask = m;
		}

		/// <summary>Get the revision walk instance this flag was created from.</summary>
		/// <remarks>Get the revision walk instance this flag was created from.</remarks>
		/// <returns>the walker this flag was allocated out of, and belongs to.</returns>
		public virtual RevWalk GetRevWalk()
		{
			return walker;
		}

		public override string ToString()
		{
			return name;
		}

		internal class StaticRevFlag : RevFlag
		{
			internal StaticRevFlag(string n, int m) : base(null, n, m)
			{
			}

			public override RevWalk GetRevWalk()
			{
				throw new NotSupportedException(MessageFormat.Format(JGitText.Get().isAStaticFlagAndHasNorevWalkInstance
					, ToString()));
			}
		}
	}
}
