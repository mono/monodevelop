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
using ICSharpCode.SharpZipLib.Zip.Compression;
using NGit;
using NGit.Errors;
using NGit.Storage.File;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>Loose object loader.</summary>
	/// <remarks>Loose object loader. This class loads an object not stored in a pack.</remarks>
	public class UnpackedObject
	{
		private const int BUFFER_SIZE = 8192;

		/// <summary>Parse an object from the unpacked object format.</summary>
		/// <remarks>Parse an object from the unpacked object format.</remarks>
		/// <param name="raw">complete contents of the compressed object.</param>
		/// <param name="id">
		/// expected ObjectId of the object, used only for error reporting
		/// in exceptions.
		/// </param>
		/// <returns>loader to read the inflated contents.</returns>
		/// <exception cref="System.IO.IOException">the object cannot be parsed.</exception>
		public static ObjectLoader Parse(byte[] raw, AnyObjectId id)
		{
			WindowCursor wc = new WindowCursor(null);
			try
			{
				return Open(new ByteArrayInputStream(raw), null, id, wc);
			}
			finally
			{
				wc.Release();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static ObjectLoader Open(InputStream @in, FilePath path, AnyObjectId id, 
			WindowCursor wc)
		{
			try
			{
				@in = Buffer(@in);
				@in.Mark(20);
				byte[] hdr = new byte[64];
				IOUtil.ReadFully(@in, hdr, 0, 2);
				if (IsStandardFormat(hdr))
				{
					@in.Reset();
					Inflater inf = wc.Inflater();
					InputStream zIn = Inflate(@in, inf);
					int avail = ReadSome(zIn, hdr, 0, 64);
					if (avail < 5)
					{
						throw new CorruptObjectException(id, JGitText.Get().corruptObjectNoHeader);
					}
					MutableInteger p = new MutableInteger();
					int type = Constants.DecodeTypeString(id, hdr, unchecked((byte)' '), p);
					long size = RawParseUtils.ParseLongBase10(hdr, p.value, p);
					if (size < 0)
					{
						throw new CorruptObjectException(id, JGitText.Get().corruptObjectNegativeSize);
					}
					if (hdr[p.value++] != 0)
					{
						throw new CorruptObjectException(id, JGitText.Get().corruptObjectGarbageAfterSize
							);
					}
					if (path == null && int.MaxValue < size)
					{
						LargeObjectException.ExceedsByteArrayLimit e;
						e = new LargeObjectException.ExceedsByteArrayLimit();
						e.SetObjectId(id);
						throw e;
					}
					if (size < wc.GetStreamFileThreshold() || path == null)
					{
						byte[] data = new byte[(int)size];
						int n = avail - p.value;
						if (n > 0)
						{
							System.Array.Copy(hdr, p.value, data, 0, n);
						}
						IOUtil.ReadFully(zIn, data, n, data.Length - n);
						CheckValidEndOfStream(@in, inf, id, hdr);
						return new ObjectLoader.SmallObject(type, data);
					}
					return new UnpackedObject.LargeObject(type, size, path, id, wc.db);
				}
				else
				{
					ReadSome(@in, hdr, 2, 18);
					int c = hdr[0] & unchecked((int)(0xff));
					int type = (c >> 4) & 7;
					long size = c & 15;
					int shift = 4;
					int p = 1;
					while ((c & unchecked((int)(0x80))) != 0)
					{
						c = hdr[p++] & unchecked((int)(0xff));
						size += (c & unchecked((int)(0x7f))) << shift;
						shift += 7;
					}
					switch (type)
					{
						case Constants.OBJ_COMMIT:
						case Constants.OBJ_TREE:
						case Constants.OBJ_BLOB:
						case Constants.OBJ_TAG:
						{
							// Acceptable types for a loose object.
							break;
						}

						default:
						{
							throw new CorruptObjectException(id, JGitText.Get().corruptObjectInvalidType);
						}
					}
					if (path == null && int.MaxValue < size)
					{
						LargeObjectException.ExceedsByteArrayLimit e;
						e = new LargeObjectException.ExceedsByteArrayLimit();
						e.SetObjectId(id);
						throw e;
					}
					if (size < wc.GetStreamFileThreshold() || path == null)
					{
						@in.Reset();
						IOUtil.SkipFully(@in, p);
						Inflater inf = wc.Inflater();
						InputStream zIn = Inflate(@in, inf);
						byte[] data = new byte[(int)size];
						IOUtil.ReadFully(zIn, data, 0, data.Length);
						CheckValidEndOfStream(@in, inf, id, hdr);
						return new ObjectLoader.SmallObject(type, data);
					}
					return new UnpackedObject.LargeObject(type, size, path, id, wc.db);
				}
			}
			catch (ZipException)
			{
				throw new CorruptObjectException(id, JGitText.Get().corruptObjectBadStream);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static long GetSize(InputStream @in, AnyObjectId id, WindowCursor wc)
		{
			try
			{
				@in = Buffer(@in);
				@in.Mark(20);
				byte[] hdr = new byte[64];
				IOUtil.ReadFully(@in, hdr, 0, 2);
				if (IsStandardFormat(hdr))
				{
					@in.Reset();
					Inflater inf = wc.Inflater();
					InputStream zIn = Inflate(@in, inf);
					int avail = ReadSome(zIn, hdr, 0, 64);
					if (avail < 5)
					{
						throw new CorruptObjectException(id, JGitText.Get().corruptObjectNoHeader);
					}
					MutableInteger p = new MutableInteger();
					Constants.DecodeTypeString(id, hdr, unchecked((byte)' '), p);
					long size = RawParseUtils.ParseLongBase10(hdr, p.value, p);
					if (size < 0)
					{
						throw new CorruptObjectException(id, JGitText.Get().corruptObjectNegativeSize);
					}
					return size;
				}
				else
				{
					ReadSome(@in, hdr, 2, 18);
					int c = hdr[0] & unchecked((int)(0xff));
					long size = c & 15;
					int shift = 4;
					int p = 1;
					while ((c & unchecked((int)(0x80))) != 0)
					{
						c = hdr[p++] & unchecked((int)(0xff));
						size += (c & unchecked((int)(0x7f))) << shift;
						shift += 7;
					}
					return size;
				}
			}
			catch (ZipException)
			{
				throw new CorruptObjectException(id, JGitText.Get().corruptObjectBadStream);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		private static void CheckValidEndOfStream(InputStream @in, Inflater inf, AnyObjectId
			 id, byte[] buf)
		{
			for (; ; )
			{
				int r;
				try
				{
					r = inf.Inflate(buf);
				}
				catch (DataFormatException)
				{
					throw new CorruptObjectException(id, JGitText.Get().corruptObjectBadStream);
				}
				if (r != 0)
				{
					throw new CorruptObjectException(id, JGitText.Get().corruptObjectIncorrectLength);
				}
				if (inf.IsFinished)
				{
					if (inf.RemainingInput != 0 || @in.Read() != -1)
					{
						throw new CorruptObjectException(id, JGitText.Get().corruptObjectBadStream);
					}
					break;
				}
				if (!inf.IsNeedingInput)
				{
					throw new CorruptObjectException(id, JGitText.Get().corruptObjectBadStream);
				}
				r = @in.Read(buf);
				if (r <= 0)
				{
					throw new CorruptObjectException(id, JGitText.Get().corruptObjectBadStream);
				}
				inf.SetInput(buf, 0, r);
			}
		}

		private static bool IsStandardFormat(byte[] hdr)
		{
			// Try to determine if this is a standard format loose object or
			// a pack style loose object. The standard format is completely
			// compressed with zlib so the first byte must be 0x78 (15-bit
			// window size, deflated) and the first 16 bit word must be
			// evenly divisible by 31. Otherwise its a pack style object.
			//
			int fb = hdr[0] & unchecked((int)(0xff));
			return fb == unchecked((int)(0x78)) && (((fb << 8) | hdr[1] & unchecked((int)(0xff
				))) % 31) == 0;
		}

		private static InputStream Inflate(InputStream @in, long size, ObjectId id)
		{
			Inflater inf = InflaterCache.Get();
			return new _InflaterInputStream_286(size, id, @in, inf);
		}

		private sealed class _InflaterInputStream_286 : InflaterInputStream
		{
			public _InflaterInputStream_286(long size, ObjectId id, InputStream baseArg1, Inflater
				 baseArg2) : base(baseArg1, baseArg2)
			{
				this.size = size;
				this.id = id;
				this.remaining = size;
			}

			private long remaining;

			/// <exception cref="System.IO.IOException"></exception>
			public override int Read(byte[] b, int off, int cnt)
			{
				try
				{
					int r = base.Read(b, off, cnt);
					if (r > 0)
					{
						this.remaining -= r;
					}
					return r;
				}
				catch (ZipException)
				{
					throw new CorruptObjectException(id, JGitText.Get().corruptObjectBadStream);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				try
				{
					if (this.remaining <= 0)
					{
						UnpackedObject.CheckValidEndOfStream(this.@in, this.inf, id, new byte[64]);
					}
				}
				finally
				{
					InflaterCache.Release(this.inf);
					base.Close();
				}
			}

			private readonly long size;

			private readonly ObjectId id;
		}

		private static InflaterInputStream Inflate(InputStream @in, Inflater inf)
		{
			return new InflaterInputStream(@in, inf, BUFFER_SIZE);
		}

		private static BufferedInputStream Buffer(InputStream @in)
		{
			return new BufferedInputStream(@in, BUFFER_SIZE);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static int ReadSome(InputStream @in, byte[] hdr, int off, int cnt)
		{
			int avail = 0;
			while (0 < cnt)
			{
				int n = @in.Read(hdr, off, cnt);
				if (n < 0)
				{
					break;
				}
				avail += n;
				off += n;
				cnt -= n;
			}
			return avail;
		}

		internal sealed class LargeObject : ObjectLoader
		{
			private readonly int type;

			private readonly long size;

			private readonly FilePath path;

			private readonly ObjectId id;

			private readonly FileObjectDatabase source;

			internal LargeObject(int type, long size, FilePath path, AnyObjectId id, FileObjectDatabase
				 db)
			{
				this.type = type;
				this.size = size;
				this.path = path;
				this.id = id.Copy();
				this.source = db;
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
				throw new LargeObjectException(id);
			}

			/// <exception cref="NGit.Errors.MissingObjectException"></exception>
			/// <exception cref="System.IO.IOException"></exception>
			public override ObjectStream OpenStream()
			{
				InputStream @in;
				try
				{
					@in = Buffer(new FileInputStream(path));
				}
				catch (FileNotFoundException)
				{
					// If the loose file no longer exists, it may have been
					// moved into a pack file in the mean time. Try again
					// to locate the object.
					//
					return source.Open(id, type).OpenStream();
				}
				bool ok = false;
				try
				{
					byte[] hdr = new byte[64];
					@in.Mark(20);
					IOUtil.ReadFully(@in, hdr, 0, 2);
					if (IsStandardFormat(hdr))
					{
						@in.Reset();
						@in = Buffer(Inflate(@in, size, id));
						while (0 < @in.Read())
						{
							continue;
						}
					}
					else
					{
						ReadSome(@in, hdr, 2, 18);
						int c = hdr[0] & unchecked((int)(0xff));
						int p = 1;
						while ((c & unchecked((int)(0x80))) != 0)
						{
							c = hdr[p++] & unchecked((int)(0xff));
						}
						@in.Reset();
						IOUtil.SkipFully(@in, p);
						@in = Buffer(Inflate(@in, size, id));
					}
					ok = true;
					return new ObjectStream.Filter(type, size, @in);
				}
				finally
				{
					if (!ok)
					{
						@in.Close();
					}
				}
			}
		}
	}
}
