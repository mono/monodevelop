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
using System.Text;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Revwalk
{
	public abstract class AbstractRevQueue : Generator
	{
		internal static readonly AbstractRevQueue EMPTY_QUEUE = new AbstractRevQueue.AlwaysEmptyQueue
			();

		/// <summary>Current output flags set for this generator instance.</summary>
		/// <remarks>Current output flags set for this generator instance.</remarks>
		internal int outputType;

		/// <summary>Add a commit to the queue.</summary>
		/// <remarks>
		/// Add a commit to the queue.
		/// <p>
		/// This method always adds the commit, even if it is already in the queue or
		/// previously was in the queue but has already been removed. To control
		/// queue admission use
		/// <see cref="Add(RevCommit, RevFlag)">Add(RevCommit, RevFlag)</see>
		/// .
		/// </remarks>
		/// <param name="c">commit to add.</param>
		public abstract void Add(RevCommit c);

		/// <summary>Add a commit if it does not have a flag set yet, then set the flag.</summary>
		/// <remarks>
		/// Add a commit if it does not have a flag set yet, then set the flag.
		/// <p>
		/// This method permits the application to test if the commit has the given
		/// flag; if it does not already have the flag than the commit is added to
		/// the queue and the flag is set. This later will prevent the commit from
		/// being added twice.
		/// </remarks>
		/// <param name="c">commit to add.</param>
		/// <param name="queueControl">flag that controls admission to the queue.</param>
		public void Add(RevCommit c, RevFlag queueControl)
		{
			if (!c.Has(queueControl))
			{
				c.Add(queueControl);
				Add(c);
			}
		}

		/// <summary>Add a commit's parents if one does not have a flag set yet.</summary>
		/// <remarks>
		/// Add a commit's parents if one does not have a flag set yet.
		/// <p>
		/// This method permits the application to test if the commit has the given
		/// flag; if it does not already have the flag than the commit is added to
		/// the queue and the flag is set. This later will prevent the commit from
		/// being added twice.
		/// </remarks>
		/// <param name="c">commit whose parents should be added.</param>
		/// <param name="queueControl">flag that controls admission to the queue.</param>
		public void AddParents(RevCommit c, RevFlag queueControl)
		{
			RevCommit[] pList = c.parents;
			if (pList == null)
			{
				return;
			}
			foreach (RevCommit p in pList)
			{
				Add(p, queueControl);
			}
		}

		/// <summary>Remove the first commit from the queue.</summary>
		/// <remarks>Remove the first commit from the queue.</remarks>
		/// <returns>the first commit of this queue.</returns>
		internal abstract override RevCommit Next();

		/// <summary>Remove all entries from this queue.</summary>
		/// <remarks>Remove all entries from this queue.</remarks>
		public abstract void Clear();

		internal abstract bool EverbodyHasFlag(int f);

		internal abstract bool AnybodyHasFlag(int f);

		internal override int OutputType()
		{
			return outputType;
		}

		protected internal static void Describe(StringBuilder s, RevCommit c)
		{
			s.Append(c.ToString());
			s.Append('\n');
		}

		private class AlwaysEmptyQueue : AbstractRevQueue
		{
			public override void Add(RevCommit c)
			{
				throw new NotSupportedException();
			}

			internal override RevCommit Next()
			{
				return null;
			}

			internal override bool AnybodyHasFlag(int f)
			{
				return false;
			}

			internal override bool EverbodyHasFlag(int f)
			{
				return true;
			}

			public override void Clear()
			{
			}
			// Nothing to clear, we have no state.
		}
	}
}
