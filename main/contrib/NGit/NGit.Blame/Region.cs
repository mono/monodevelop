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

using System.Text;
using Sharpen;

namespace NGit.Blame
{
	/// <summary>Region of the result that still needs to be computed.</summary>
	/// <remarks>
	/// Region of the result that still needs to be computed.
	/// <p>
	/// Regions are held in a singly-linked-list by
	/// <see cref="Candidate">Candidate</see>
	/// using the
	/// <see cref="Candidate.regionList">Candidate.regionList</see>
	/// field. The list is kept in sorted order by
	/// <see cref="resultStart">resultStart</see>
	/// .
	/// </remarks>
	internal class Region
	{
		/// <summary>Next entry in the region linked list.</summary>
		/// <remarks>Next entry in the region linked list.</remarks>
		internal NGit.Blame.Region next;

		/// <summary>First position of this region in the result file blame is computing.</summary>
		/// <remarks>First position of this region in the result file blame is computing.</remarks>
		internal int resultStart;

		/// <summary>
		/// First position in the
		/// <see cref="Candidate">Candidate</see>
		/// that owns this Region.
		/// </summary>
		internal int sourceStart;

		/// <summary>Length of the region, always &gt;= 1.</summary>
		/// <remarks>Length of the region, always &gt;= 1.</remarks>
		internal int length;

		internal Region(int rs, int ss, int len)
		{
			resultStart = rs;
			sourceStart = ss;
			length = len;
		}

		/// <summary>Copy the entire result region, but at a new source position.</summary>
		/// <remarks>Copy the entire result region, but at a new source position.</remarks>
		/// <param name="newSource">the new source position.</param>
		/// <returns>the same result region, but offset for a new source.</returns>
		internal virtual NGit.Blame.Region Copy(int newSource)
		{
			return new NGit.Blame.Region(resultStart, newSource, length);
		}

		/// <summary>Split the region, assigning a new source position to the first half.</summary>
		/// <remarks>Split the region, assigning a new source position to the first half.</remarks>
		/// <param name="newSource">the new source position.</param>
		/// <param name="newLen">length of the new region.</param>
		/// <returns>the first half of the region, at the new source.</returns>
		internal virtual NGit.Blame.Region SplitFirst(int newSource, int newLen)
		{
			return new NGit.Blame.Region(resultStart, newSource, newLen);
		}

		/// <summary>
		/// Edit this region to remove the first
		/// <code>d</code>
		/// elements.
		/// </summary>
		/// <param name="d">number of elements to remove from the start of this region.</param>
		internal virtual void SlideAndShrink(int d)
		{
			resultStart += d;
			sourceStart += d;
			length -= d;
		}

		internal virtual NGit.Blame.Region DeepCopy()
		{
			NGit.Blame.Region head = new NGit.Blame.Region(resultStart, sourceStart, length);
			NGit.Blame.Region tail = head;
			for (NGit.Blame.Region n = next; n != null; n = n.next)
			{
				NGit.Blame.Region q = new NGit.Blame.Region(n.resultStart, n.sourceStart, n.length
					);
				tail.next = q;
				tail = q;
			}
			return head;
		}

		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			NGit.Blame.Region r = this;
			do
			{
				if (r != this)
				{
					buf.Append(',');
				}
				buf.Append(r.resultStart);
				buf.Append('-');
				buf.Append(r.resultStart + r.length);
				r = r.next;
			}
			while (r != null);
			return buf.ToString();
		}
	}
}
