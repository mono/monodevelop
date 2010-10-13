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
using Sharpen;

namespace NGit
{
	/// <summary>Util for sorting (or comparing) Ref instances by name.</summary>
	/// <remarks>
	/// Util for sorting (or comparing) Ref instances by name.
	/// <p>
	/// Useful for command line tools or writing out refs to file.
	/// </remarks>
	public class RefComparator : IComparer<Ref>
	{
		/// <summary>Singleton instance of RefComparator</summary>
		public static readonly RefComparator INSTANCE = new RefComparator();

		public virtual int Compare(Ref o1, Ref o2)
		{
			return CompareTo(o1, o2);
		}

		/// <summary>Sorts the collection of refs, returning a new collection.</summary>
		/// <remarks>Sorts the collection of refs, returning a new collection.</remarks>
		/// <param name="refs">collection to be sorted</param>
		/// <returns>sorted collection of refs</returns>
		public static ICollection<Ref> Sort(ICollection<Ref> refs)
		{
			IList<Ref> r = new AList<Ref>(refs);
			r.Sort(INSTANCE);
			return r;
		}

		/// <summary>Compare a reference to a name.</summary>
		/// <remarks>Compare a reference to a name.</remarks>
		/// <param name="o1">the reference instance.</param>
		/// <param name="o2">the name to compare to.</param>
		/// <returns>standard Comparator result of &lt; 0, 0, &gt; 0.</returns>
		public static int CompareTo(Ref o1, string o2)
		{
			return o1.GetName().CompareTo(o2);
		}

		/// <summary>Compare two references by name.</summary>
		/// <remarks>Compare two references by name.</remarks>
		/// <param name="o1">the reference instance.</param>
		/// <param name="o2">the other reference instance.</param>
		/// <returns>standard Comparator result of &lt; 0, 0, &gt; 0.</returns>
		public static int CompareTo(Ref o1, Ref o2)
		{
			return o1.GetName().CompareTo(o2.GetName());
		}
	}
}
