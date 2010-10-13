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
using NGit.Storage.Pack;
using Sharpen;

namespace NGit.Storage.Pack
{
	/// <summary>
	/// An object representation
	/// <see cref="PackWriter">PackWriter</see>
	/// can consider for packing.
	/// </summary>
	public class StoredObjectRepresentation
	{
		/// <summary>
		/// Special unknown value for
		/// <see cref="GetWeight()">GetWeight()</see>
		/// .
		/// </summary>
		public const int WEIGHT_UNKNOWN = int.MaxValue;

		/// <summary>Stored in pack format, as a delta to another object.</summary>
		/// <remarks>Stored in pack format, as a delta to another object.</remarks>
		public const int PACK_DELTA = 0;

		/// <summary>Stored in pack format, without delta.</summary>
		/// <remarks>Stored in pack format, without delta.</remarks>
		public const int PACK_WHOLE = 1;

		/// <summary>Only available after inflating to canonical format.</summary>
		/// <remarks>Only available after inflating to canonical format.</remarks>
		public const int FORMAT_OTHER = 2;

		/// <returns>
		/// relative size of this object's packed form. The special value
		/// <see cref="WEIGHT_UNKNOWN">WEIGHT_UNKNOWN</see>
		/// can be returned to indicate the
		/// implementation doesn't know, or cannot supply the weight up
		/// front.
		/// </returns>
		public virtual int GetWeight()
		{
			return WEIGHT_UNKNOWN;
		}

		/// <returns>
		/// true if this is a delta against another object and this is stored
		/// in pack delta format.
		/// </returns>
		public virtual int GetFormat()
		{
			return FORMAT_OTHER;
		}

		/// <returns>
		/// identity of the object this delta applies to in order to recover
		/// the original object content. This method should only be called if
		/// <see cref="GetFormat()">GetFormat()</see>
		/// returned
		/// <see cref="PACK_DELTA">PACK_DELTA</see>
		/// .
		/// </returns>
		public virtual ObjectId GetDeltaBase()
		{
			return null;
		}
	}
}
