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
	/// Delays commits to be at least
	/// <see cref="PendingGenerator.OVER_SCAN">PendingGenerator.OVER_SCAN</see>
	/// late.
	/// <p>
	/// This helps to "fix up" weird corner cases resulting from clock skew, by
	/// slowing down what we produce to the caller we get a better chance to ensure
	/// PendingGenerator reached back far enough in the graph to correctly mark
	/// commits
	/// <see cref="RevWalk.UNINTERESTING">RevWalk.UNINTERESTING</see>
	/// if necessary.
	/// <p>
	/// This generator should appear before
	/// <see cref="FixUninterestingGenerator">FixUninterestingGenerator</see>
	/// if the
	/// lower level
	/// <see cref="pending">pending</see>
	/// isn't already fully buffered.
	/// </summary>
	internal sealed class DelayRevQueue : Generator
	{
		private const int OVER_SCAN = PendingGenerator.OVER_SCAN;

		private readonly Generator pending;

		private readonly FIFORevQueue delay;

		private int size;

		internal DelayRevQueue(Generator g)
		{
			pending = g;
			delay = new FIFORevQueue();
		}

		internal override int OutputType()
		{
			return pending.OutputType();
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		internal override RevCommit Next()
		{
			while (size < OVER_SCAN)
			{
				RevCommit c = pending.Next();
				if (c == null)
				{
					break;
				}
				delay.Add(c);
				size++;
			}
			RevCommit c_1 = delay.Next();
			if (c_1 == null)
			{
				return null;
			}
			size--;
			return c_1;
		}
	}
}
