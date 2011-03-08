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
using ICSharpCode.SharpZipLib;
using NGit;
using NGit.Errors;
using NGit.Storage.File;
using NGit.Storage.Pack;
using NGit.Util.IO;
using Sharpen;

namespace NGit.Storage.File
{
	internal class LargePackedDeltaObject : ObjectLoader
	{
		private const long SIZE_UNKNOWN = -1;

		private int type;

		private long size;

		private readonly long objectOffset;

		private readonly long baseOffset;

		private readonly int headerLength;

		private readonly PackFile pack;

		private readonly FileObjectDatabase db;

		internal LargePackedDeltaObject(long objectOffset, long baseOffset, int headerLength
			, PackFile pack, FileObjectDatabase db)
		{
			this.type = Constants.OBJ_BAD;
			this.size = SIZE_UNKNOWN;
			this.objectOffset = objectOffset;
			this.baseOffset = baseOffset;
			this.headerLength = headerLength;
			this.pack = pack;
			this.db = db;
		}

		public override int GetType()
		{
			if (type == Constants.OBJ_BAD)
			{
				WindowCursor wc = new WindowCursor(db);
				try
				{
					type = pack.GetObjectType(wc, objectOffset);
				}
				catch (IOException)
				{
					// If the pack file cannot be pinned into the cursor, it
					// probably was repacked recently. Go find the object
					// again and get the type from that location instead.
					//
					try
					{
						type = wc.Open(GetObjectId()).GetType();
					}
					catch (IOException)
					{
					}
				}
				finally
				{
					// "He's dead, Jim." We just can't discover the type
					// and the interface isn't supposed to be lazy here.
					// Report an invalid type code instead, callers will
					// wind up bailing out with an error at some point.
					wc.Release();
				}
			}
			return type;
		}

		public override long GetSize()
		{
			if (size == SIZE_UNKNOWN)
			{
				WindowCursor wc = new WindowCursor(db);
				try
				{
					byte[] b = pack.GetDeltaHeader(wc, objectOffset + headerLength);
					size = BinaryDelta.GetResultSize(b);
				}
				catch (SharpZipBaseException)
				{
				}
				catch (IOException)
				{
					// The zlib stream for the delta is corrupt. We probably
					// cannot access the object. Keep the size negative and
					// report that bogus result to the caller.
					// If the pack file cannot be pinned into the cursor, it
					// probably was repacked recently. Go find the object
					// again and get the size from that location instead.
					//
					try
					{
						size = wc.Open(GetObjectId()).GetSize();
					}
					catch (IOException)
					{
					}
				}
				finally
				{
					// "He's dead, Jim." We just can't discover the size
					// and the interface isn't supposed to be lazy here.
					// Report an invalid type code instead, callers will
					// wind up bailing out with an error at some point.
					wc.Release();
				}
			}
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
			// If the object was recently unpacked, its available loose.
			// The loose format is going to be faster to access than a
			// delta applied on top of a base. Use that whenever we can.
			//
			ObjectId myId = GetObjectId();
			WindowCursor wc = new WindowCursor(db);
			ObjectLoader ldr = db.OpenObject2(wc, myId.Name, myId);
			if (ldr != null)
			{
				return ldr.OpenStream();
			}
			InputStream @in = Open(wc);
			@in = new BufferedInputStream(@in, 8192);
			// While we inflate the object, also deflate it back as a loose
			// object. This will later be cleaned up by a gc pass, but until
			// then we will reuse the loose form by the above code path.
			//
			int myType = GetType();
			long mySize = GetSize();
			ObjectDirectoryInserter odi = ((ObjectDirectoryInserter)db.NewInserter());
			FilePath tmp = odi.NewTempFile();
			DeflaterOutputStream dOut = odi.Compress(new FileOutputStream(tmp));
			odi.WriteHeader(dOut, myType, mySize);
			@in = new TeeInputStream(@in, dOut);
			return new _Filter_195(this, odi, wc, tmp, myId, myType, mySize, @in);
		}

		private sealed class _Filter_195 : ObjectStream.Filter
		{
			public _Filter_195(LargePackedDeltaObject _enclosing, ObjectDirectoryInserter odi
				, WindowCursor wc, FilePath tmp, ObjectId myId, int baseArg1, long baseArg2, InputStream
				 baseArg3) : base(baseArg1, baseArg2, baseArg3)
			{
				this._enclosing = _enclosing;
				this.odi = odi;
				this.wc = wc;
				this.tmp = tmp;
				this.myId = myId;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				base.Close();
				odi.Release();
				wc.Release();
				this._enclosing.db.InsertUnpackedObject(tmp, myId, true);
			}

			private readonly LargePackedDeltaObject _enclosing;

			private readonly ObjectDirectoryInserter odi;

			private readonly WindowCursor wc;

			private readonly FilePath tmp;

			private readonly ObjectId myId;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		private InputStream Open(WindowCursor wc)
		{
			InputStream delta;
			try
			{
				delta = new PackInputStream(pack, objectOffset + headerLength, wc);
			}
			catch (IOException)
			{
				// If the pack file cannot be pinned into the cursor, it
				// probably was repacked recently. Go find the object
				// again and open the stream from that location instead.
				//
				return wc.Open(GetObjectId()).OpenStream();
			}
			delta = new InflaterInputStream(delta);
			ObjectLoader @base = pack.Load(wc, baseOffset);
			DeltaStream ds = new _DeltaStream_223(@base, wc, delta);
			// This code path should never be used as DeltaStream
			// is supposed to open the stream first, which would
			// initialize the size for us directly from the stream.
			if (type == Constants.OBJ_BAD)
			{
				if (!(@base is NGit.Storage.File.LargePackedDeltaObject))
				{
					type = @base.GetType();
				}
			}
			if (size == SIZE_UNKNOWN)
			{
				size = ds.GetSize();
			}
			return ds;
		}

		private sealed class _DeltaStream_223 : DeltaStream
		{
			public _DeltaStream_223(ObjectLoader @base, WindowCursor wc, InputStream baseArg1
				) : base(baseArg1)
			{
				this.@base = @base;
				this.wc = wc;
				this.baseSize = NGit.Storage.File.LargePackedDeltaObject.SIZE_UNKNOWN;
			}

			private long baseSize;

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override InputStream OpenBase()
			{
				InputStream @in;
				if (@base is NGit.Storage.File.LargePackedDeltaObject)
				{
					@in = ((NGit.Storage.File.LargePackedDeltaObject)@base).Open(wc);
				}
				else
				{
					@in = @base.OpenStream();
				}
				if (this.baseSize == NGit.Storage.File.LargePackedDeltaObject.SIZE_UNKNOWN)
				{
					if (@in is DeltaStream)
					{
						this.baseSize = ((DeltaStream)@in).GetSize();
					}
					else
					{
						if (@in is ObjectStream)
						{
							this.baseSize = ((ObjectStream)@in).GetSize();
						}
					}
				}
				return @in;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override long GetBaseSize()
			{
				if (this.baseSize == NGit.Storage.File.LargePackedDeltaObject.SIZE_UNKNOWN)
				{
					this.baseSize = @base.GetSize();
				}
				return this.baseSize;
			}

			private readonly ObjectLoader @base;

			private readonly WindowCursor wc;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ObjectId GetObjectId()
		{
			return pack.FindObjectForOffset(objectOffset);
		}
	}
}
