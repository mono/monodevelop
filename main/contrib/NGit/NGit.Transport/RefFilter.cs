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
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Filters the list of refs that are advertised to the client.</summary>
	/// <remarks>
	/// Filters the list of refs that are advertised to the client.
	/// <p>
	/// The filter is called by
	/// <see cref="ReceivePack">ReceivePack</see>
	/// and
	/// <see cref="UploadPack">UploadPack</see>
	/// to ensure
	/// that the refs are filtered before they are advertised to the client.
	/// <p>
	/// This can be used by applications to control visibility of certain refs based
	/// on a custom set of rules.
	/// </remarks>
	public abstract class RefFilter
	{
		private sealed class _RefFilter_61 : RefFilter
		{
			public _RefFilter_61()
			{
			}

			public override IDictionary<string, Ref> Filter(IDictionary<string, Ref> refs)
			{
				return refs;
			}
		}

		/// <summary>The default filter, allows all refs to be shown.</summary>
		/// <remarks>The default filter, allows all refs to be shown.</remarks>
		public static readonly RefFilter DEFAULT = new _RefFilter_61();

		/// <summary>
		/// Filters a
		/// <code>Map</code>
		/// of refs before it is advertised to the client.
		/// </summary>
		/// <param name="refs">the refs which this method need to consider.</param>
		/// <returns>the filtered map of refs.</returns>
		public abstract IDictionary<string, Ref> Filter(IDictionary<string, Ref> refs);
	}
}
