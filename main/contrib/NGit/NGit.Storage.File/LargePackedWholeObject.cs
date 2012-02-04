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

using System.IO;
using NGit;
using NGit.Errors;
using NGit.Storage.File;
using Sharpen;

namespace NGit.Storage.File
{
	internal class LargePackedWholeObject : ObjectLoader
	{
		private readonly int type;

		private readonly long size;

		private readonly long objectOffset;

		private readonly int headerLength;

		private readonly PackFile pack;

		private readonly FileObjectDatabase db;

		internal LargePackedWholeObject(int type, long size, long objectOffset, int headerLength
			, PackFile pack, FileObjectDatabase db)
		{
			this.type = type;
			this.size = size;
			this.objectOffset = objectOffset;
			this.headerLength = headerLength;
			this.pack = pack;
			this.db = db;
		}

		public override int GetType()
		{
			return type;
		}

		public override long GetSize()
		{
			return size;
		}

		public override bool IsLarge()
		{
			return true;
		}

		/// <exception cref="NGit.Errors.LargeObjectException"></exception>
		public override byte[] GetCachedBytes()
		{
			try
			{
				throw new LargeObjectException(GetObjectId());
			}
			catch (IOException cannotObtainId)
			{
				LargeObjectException err = new LargeObjectException();
				Sharpen.Extensions.InitCause(err, cannotObtainId);
				throw err;
			}
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override ObjectStream OpenStream()
		{
			WindowCursor wc = new WindowCursor(db);
			InputStream @in;
			try
			{
				@in = new PackInputStream(pack, objectOffset + headerLength, wc);
			}
			catch (IOException)
			{
				// If the pack file cannot be pinned into the cursor, it
				// probably was repacked recently. Go find the object
				// again and open the stream from that location instead.
				//
				return wc.Open(GetObjectId(), type).OpenStream();
			}
			@in = new BufferedInputStream(new InflaterInputStream(@in, wc.Inflater(), 8192), 
				8192);
			//
			//
			//
			//
			//
			return new ObjectStream.Filter(type, size, @in);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ObjectId GetObjectId()
		{
			return pack.FindObjectForOffset(objectOffset);
		}
	}
}
