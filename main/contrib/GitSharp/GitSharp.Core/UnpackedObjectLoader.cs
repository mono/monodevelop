/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using GitSharp.Core.Exceptions;
using System.IO;
using GitSharp.Core.Util;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace GitSharp.Core
{
	/// <summary>
	/// Loose object loader. This class loads an object not stored in a pack.
	/// </summary>
	public class UnpackedObjectLoader : ObjectLoader
	{
		private readonly int _objectType;
		private readonly int _objectSize;
		private readonly byte[] _bytes;

		/// <summary>
		/// Construct an ObjectLoader to read from the file.
		/// </summary>
		/// <param name="path">location of the loose object to read.</param>
		/// <param name="id">Expected identity of the object being loaded, if known.</param>
		///	<exception cref="FileNotFoundException">
		/// The loose object file does not exist.
		/// </exception>
		/// <exception cref="IOException">
		/// The loose object file exists, but is corrupt.
		/// </exception>
		public UnpackedObjectLoader(FileSystemInfo path, AnyObjectId id)
			: this(ReadCompressed(path), id)
		{
		}

		/// <summary>
		/// Construct an ObjectLoader from a loose object's compressed form.
		/// </summary>
		/// <param name="compressed">
		/// Entire content of the loose object file.
		/// </param>
		///	<exception cref="CorruptObjectException">
		///	The compressed data supplied does not match the format for a
		///	valid loose object.
		/// </exception>
		public UnpackedObjectLoader(byte[] compressed)
			: this(compressed, null)
		{
		}

		private static byte[] ReadCompressed(FileSystemInfo path)
		{
			using (var inStream = new FileStream(path.FullName, System.IO.FileMode.Open, FileAccess.Read))
			{
				var compressed = new byte[(int)inStream.Length];
				IO.ReadFully(inStream, compressed, 0, compressed.Length);
				return compressed;
			}
		}

		private UnpackedObjectLoader(byte[] compressed, AnyObjectId id)
		{
			// Try to determine if this is a legacy format loose object or
			// a new style loose object. The legacy format was completely
			// compressed with zlib so the first byte must be 0x78 (15-bit
			// window size, deflated) and the first 16 bit word must be
			// evenly divisible by 31. Otherwise its a new style loose
			// object.
			//
			Inflater inflater = InflaterCache.Instance.get();
			try
			{
				int fb = compressed[0] & 0xff;
				if (fb == 0x78 && (((fb << 8) | compressed[1] & 0xff) % 31) == 0)
				{
					inflater.SetInput(compressed);
					var hdr = new byte[64];
					int avail = 0;
					while (!inflater.IsFinished && avail < hdr.Length)
					{
						try
						{
							avail += inflater.Inflate(hdr, avail, hdr.Length - avail);
						}
						catch (IOException dfe)
						{
							var coe = new CorruptObjectException(id, "bad stream", dfe);
							//inflater.end();
							throw coe;
						}
					}

					if (avail < 5)
					{
						throw new CorruptObjectException(id, "no header");
					}

					var p = new MutableInteger();
					_objectType = Constants.decodeTypeString(id, hdr, (byte)' ', p);
					_objectSize = RawParseUtils.parseBase10(hdr, p.value, p);

					if (_objectSize < 0)
					{
						throw new CorruptObjectException(id, "negative size");
					}

					if (hdr[p.value++] != 0)
					{
						throw new CorruptObjectException(id, "garbage after size");
					}

					_bytes = new byte[_objectSize];

					if (p.value < avail)
					{
						Array.Copy(hdr, p.value, _bytes, 0, avail - p.value);
					}

					Decompress(id, inflater, avail - p.value);
				}
				else
				{
					int p = 0;
					int c = compressed[p++] & 0xff;
					int typeCode = (c >> 4) & 7;
					int size = c & 15;
					int shift = 4;
					while ((c & 0x80) != 0)
					{
						c = compressed[p++] & 0xff;
						size += (c & 0x7f) << shift;
						shift += 7;
					}

					switch (typeCode)
					{
						case Constants.OBJ_COMMIT:
						case Constants.OBJ_TREE:
						case Constants.OBJ_BLOB:
						case Constants.OBJ_TAG:
							_objectType = typeCode;
							break;

						default:
							throw new CorruptObjectException(id, "invalid type");
					}

					_objectSize = size;
					_bytes = new byte[_objectSize];
					inflater.SetInput(compressed, p, compressed.Length - p);
					Decompress(id, inflater, 0);
				}
			}
			finally
			{
				InflaterCache.Instance.release(inflater);
			}
		}

		private void Decompress(AnyObjectId id, Inflater inf, int p)
		{
			try
			{
				while (!inf.IsFinished)
				{
					p += inf.Inflate(_bytes, p, _objectSize - p);
				}
			}
			catch (IOException dfe)
			{
				var coe = new CorruptObjectException(id, "bad stream", dfe);
				throw coe;
			}

			if (p != _objectSize)
			{
				throw new CorruptObjectException(id, "incorrect Length");
			}
		}

		public override int Type
		{
			get { return _objectType; }
			protected set { }
		}

		public override long Size
		{
			get { return _objectSize; }
			protected set { }
		}

		public override byte[] CachedBytes
		{
			get { return _bytes; }
			protected set { }
		}

		public override int RawType
		{
			get { return _objectType; }
		}

		public override long RawSize
		{
			get { return _objectSize; }
		}
	}
}
