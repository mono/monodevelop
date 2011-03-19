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
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Description of an object stored in a pack file, including offset.</summary>
	/// <remarks>
	/// Description of an object stored in a pack file, including offset.
	/// <p>
	/// When objects are stored in packs Git needs the ObjectId and the offset
	/// (starting position of the object data) to perform random-access reads of
	/// objects from the pack. This extension of ObjectId includes the offset.
	/// </remarks>
	[System.Serializable]
	public class PackedObjectInfo : ObjectIdOwnerMap.Entry
	{
		private long offset;

		private int crc;

		internal PackedObjectInfo(long headerOffset, int packedCRC, AnyObjectId id) : base
			(id)
		{
			offset = headerOffset;
			crc = packedCRC;
		}

		/// <summary>Create a new structure to remember information about an object.</summary>
		/// <remarks>Create a new structure to remember information about an object.</remarks>
		/// <param name="id">the identity of the object the new instance tracks.</param>
		protected internal PackedObjectInfo(AnyObjectId id) : base(id)
		{
		}

		/// <returns>
		/// offset in pack when object has been already written, or 0 if it
		/// has not been written yet
		/// </returns>
		public virtual long GetOffset()
		{
			return offset;
		}

		/// <summary>Set the offset in pack when object has been written to.</summary>
		/// <remarks>Set the offset in pack when object has been written to.</remarks>
		/// <param name="offset">offset where written object starts</param>
		public virtual void SetOffset(long offset)
		{
			this.offset = offset;
		}

		/// <returns>the 32 bit CRC checksum for the packed data.</returns>
		public virtual int GetCRC()
		{
			return crc;
		}

		/// <summary>Record the 32 bit CRC checksum for the packed data.</summary>
		/// <remarks>Record the 32 bit CRC checksum for the packed data.</remarks>
		/// <param name="crc">
		/// checksum of all packed data (including object type code,
		/// inflated length and delta base reference) as computed by
		/// <see cref="Sharpen.CRC32">Sharpen.CRC32</see>
		/// .
		/// </param>
		public virtual void SetCRC(int crc)
		{
			this.crc = crc;
		}
	}
}
