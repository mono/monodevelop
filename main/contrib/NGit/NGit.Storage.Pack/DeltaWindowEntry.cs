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

using NGit.Storage.Pack;
using Sharpen;

namespace NGit.Storage.Pack
{
	internal class DeltaWindowEntry
	{
		internal ObjectToPack @object;

		/// <summary>Complete contents of this object.</summary>
		/// <remarks>Complete contents of this object. Lazily loaded.</remarks>
		internal byte[] buffer;

		/// <summary>Index of this object's content, to encode other deltas.</summary>
		/// <remarks>Index of this object's content, to encode other deltas. Lazily loaded.</remarks>
		internal DeltaIndex index;

		internal virtual void Set(ObjectToPack @object)
		{
			this.@object = @object;
			this.index = null;
			this.buffer = null;
		}

		/// <returns>current delta chain depth of this object.</returns>
		internal virtual int Depth()
		{
			return @object.GetDeltaDepth();
		}

		/// <returns>type of the object in this window entry.</returns>
		internal virtual int Type()
		{
			return @object.GetType();
		}

		/// <returns>estimated unpacked size of the object, in bytes .</returns>
		internal virtual int Size()
		{
			return @object.GetWeight();
		}

		/// <returns>true if there is no object stored in this entry.</returns>
		internal virtual bool Empty()
		{
			return @object == null;
		}
	}
}
