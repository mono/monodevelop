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

using NGit.Revwalk;
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>
	/// Utility methods for
	/// <see cref="RevWalk">RevWalk</see>
	/// .
	/// </summary>
	public sealed class RevWalkUtils
	{
		public RevWalkUtils()
		{
		}

		// Utility class
		/// <summary>
		/// Count the number of commits that are reachable from <code>start</code>
		/// until a commit that is reachable from <code>end</code> is encountered.
		/// </summary>
		/// <remarks>
		/// Count the number of commits that are reachable from <code>start</code>
		/// until a commit that is reachable from <code>end</code> is encountered. In
		/// other words, count the number of commits that are in <code>start</code>,
		/// but not in <code>end</code>.
		/// <p>
		/// Note that this method calls
		/// <see cref="RevWalk.Reset()">RevWalk.Reset()</see>
		/// at the beginning.
		/// Also note that the existing rev filter on the walk is left as-is, so be
		/// sure to set the right rev filter before calling this method.
		/// </remarks>
		/// <param name="walk">the rev walk to use</param>
		/// <param name="start">the commit to start counting from</param>
		/// <param name="end">
		/// the commit where counting should end, or null if counting
		/// should be done until there are no more commits
		/// </param>
		/// <returns>the number of commits</returns>
		/// <exception cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">NGit.Errors.IncorrectObjectTypeException
		/// 	</exception>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public static int Count(RevWalk walk, RevCommit start, RevCommit end)
		{
			walk.Reset();
			walk.MarkStart(start);
			if (end != null)
			{
				walk.MarkUninteresting(end);
			}
			int count = 0;
			for (RevCommit c = walk.Next(); c != null; c = walk.Next())
			{
				count++;
			}
			return count;
		}
	}
}
