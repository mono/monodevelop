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

using System;
using ICSharpCode.SharpZipLib.Zip.Compression;
using Sharpen;

namespace NGit
{
	/// <summary>Creates zlib based inflaters as necessary for object decompression.</summary>
	/// <remarks>Creates zlib based inflaters as necessary for object decompression.</remarks>
	public class InflaterCache
	{
		private const int SZ = 4;

		private static readonly Inflater[] inflaterCache;

		private static int openInflaterCount;

		static InflaterCache()
		{
			inflaterCache = new Inflater[SZ];
		}

		/// <summary>Obtain an Inflater for decompression.</summary>
		/// <remarks>
		/// Obtain an Inflater for decompression.
		/// <p>
		/// Inflaters obtained through this cache should be returned (if possible) by
		/// <see cref="Release(ICSharpCode.SharpZipLib.Zip.Compression.Inflater)">Release(ICSharpCode.SharpZipLib.Zip.Compression.Inflater)
		/// 	</see>
		/// to avoid garbage collection and reallocation.
		/// </remarks>
		/// <returns>an available inflater. Never null.</returns>
		public static Inflater Get()
		{
			Inflater r = GetImpl();
			return r != null ? r : new Inflater(false);
		}

		private static Inflater GetImpl()
		{
			lock (typeof(InflaterCache))
			{
				if (openInflaterCount > 0)
				{
					Inflater r = inflaterCache[--openInflaterCount];
					inflaterCache[openInflaterCount] = null;
					return r;
				}
				return null;
			}
		}

		/// <summary>Release an inflater previously obtained from this cache.</summary>
		/// <remarks>Release an inflater previously obtained from this cache.</remarks>
		/// <param name="i">
		/// the inflater to return. May be null, in which case this method
		/// does nothing.
		/// </param>
		public static void Release(Inflater i)
		{
			if (i != null)
			{
				i.Reset();
				if (ReleaseImpl(i))
				{
					i.Finish();
				}
			}
		}

		private static bool ReleaseImpl(Inflater i)
		{
			lock (typeof(InflaterCache))
			{
				if (openInflaterCount < SZ)
				{
					inflaterCache[openInflaterCount++] = i;
					return false;
				}
				return true;
			}
		}

		public InflaterCache()
		{
			throw new NotSupportedException();
		}
	}
}
