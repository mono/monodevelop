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
using NGit.Storage.Pack;
using Sharpen;

namespace NGit.Storage.Pack
{
	/// <summary>
	/// Describes a pack file
	/// <see cref="ObjectReuseAsIs">ObjectReuseAsIs</see>
	/// can append onto a stream.
	/// </summary>
	public abstract class CachedPack
	{
		/// <summary>Objects that start this pack.</summary>
		/// <remarks>
		/// Objects that start this pack.
		/// <p>
		/// All objects reachable from the tips are contained within this pack. If
		/// <see cref="PackWriter">PackWriter</see>
		/// is going to include everything reachable from all of
		/// these objects, this cached pack is eligible to be appended directly onto
		/// the output pack stream.
		/// </remarks>
		/// <returns>the tip objects that describe this pack.</returns>
		public abstract ICollection<ObjectId> GetTips();

		/// <summary>Get the number of objects in this pack.</summary>
		/// <remarks>Get the number of objects in this pack.</remarks>
		/// <returns>the total object count for the pack.</returns>
		/// <exception cref="System.IO.IOException">if the object count cannot be read.</exception>
		public abstract long GetObjectCount();

		/// <summary>Determine if the pack contains the requested objects.</summary>
		/// <remarks>Determine if the pack contains the requested objects.</remarks>
		/// <?></?>
		/// <param name="toFind">the objects to search for.</param>
		/// <returns>the objects contained in the pack.</returns>
		/// <exception cref="System.IO.IOException">the pack cannot be accessed</exception>
		public abstract ICollection<ObjectId> HasObject<T>(Iterable<T> toFind) where T:ObjectId;
	}
}
